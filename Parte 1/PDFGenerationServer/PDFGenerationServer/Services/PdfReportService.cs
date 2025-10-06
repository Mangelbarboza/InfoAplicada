using PDFGenerationServer.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;
using PDFGenerationServer.Models.DTO; // Para LogMessageDTO
using System.Linq;

namespace PDFGenerationServer.Services
{
    public class PdfReportService
    {
        private readonly OrdersData _orders;
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly ILogProducer _logProducer; // Ángel: Se inyecta la dependencia aquí.

        public PdfReportService(OrdersData orders, ILogProducer logProducer)
        {
            _orders = orders;
            _logProducer = logProducer;
        }

        public async Task<string> GenerateReportPDF(int customerId, DateTime startDate, DateTime endDate, string correlationId)
        {
            var orders = await _orders.GetOrdersByCustomer(customerId, startDate, endDate);

            if (!orders.Any())
            {
                var errorLog = new LogMessageDTO
                {
                    CorrelationId = correlationId,
                    Service = "PDF Server",
                    Endpoint = "/api/Orders/GeneratePdf",
                    TimeStrap = DateTime.UtcNow.ToString("o"),
                    Playload = new { error = "No se encontraron órdenes para el cliente." },
                    Success = false
                };
                await _logProducer.sendLog(errorLog);
                return null;
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.Header()
                        .Text($"Reporte de Órdenes del Cliente {customerId}")
                        .Bold().FontSize(18).AlignCenter();

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(2).Padding(8).Text("#").SemiBold();
                                header.Cell().BorderBottom(2).Padding(8).Text("Orden ID").SemiBold();
                                header.Cell().BorderBottom(2).Padding(8).Text("Fecha").SemiBold();
                                header.Cell().BorderBottom(2).Padding(8).Text("Total").SemiBold();
                            });

                            int i = 1;
                            foreach (var order in orders)
                            {
                                table.Cell().BorderBottom(1).Padding(8).Text(i.ToString());
                                table.Cell().BorderBottom(1).Padding(8).Text(order.SalesOrderId.ToString());
                                table.Cell().BorderBottom(1).Padding(8).Text(order.OrderDate.ToString("yyyy-MM-dd"));
                                table.Cell().BorderBottom(1).Padding(8).Text(order.TotalDue.ToString("C", new CultureInfo("es-CR")));
                                i++;
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | ID Usuario: {customerId}")
                        .FontSize(10);
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            var storageServerUrl = "http://127.0.0.1:8000/api/storage/upload";
            var fileName = $"{correlationId}_Orders_{customerId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(correlationId), "correlationId");
            form.Add(new StringContent(customerId.ToString()), "clientId");
            form.Add(new StringContent(fileName), "fileName");
            form.Add(new StreamContent(stream), "file", fileName);

            var response = await _httpClient.PostAsync(storageServerUrl, form);

            if (response.IsSuccessStatusCode)
            {
                var logMessage = new LogMessageDTO
                {
                    CorrelationId = correlationId,
                    Service = "PDF Server",
                    Endpoint = "/api/storage/upload",
                    TimeStrap = DateTime.UtcNow.ToString("o"),
                    Playload = new { fileName = fileName },
                    Success = true
                };
                await _logProducer.sendLog(logMessage);
                return $"Archivo {fileName} guardado en el Storage Server.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorLog = new LogMessageDTO
                {
                    CorrelationId = correlationId,
                    Service = "PDF Server",
                    Endpoint = "/api/storage/upload",
                    TimeStrap = DateTime.UtcNow.ToString("o"),
                    Playload = new { error = errorContent },
                    Success = false
                };
                await _logProducer.sendLog(errorLog);
                throw new Exception("Error al subir el archivo al Storage Server.");
            }
        }
    }
}