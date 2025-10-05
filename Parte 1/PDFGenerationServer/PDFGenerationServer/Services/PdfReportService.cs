using PDFGenerationServer.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Companion;
using System.Globalization;
using System.Net.Http;  // Angel: lo agregue para la conexion con el api del local storage
using System.Net.Http.Headers;

namespace PDFGenerationServer.Services
{
    public class PdfReportService
    {
        private readonly OrdersData _orders;
        private static readonly HttpClient _httpClient = new HttpClient();  // Ángel: Se inicializa el cliente HTTP aquí, una sola vez.

        public PdfReportService(OrdersData orders)
        {
            _orders = orders;
           
        }

        public async Task<string> GenerateReportPDF(int customerId, DateTime startDate, DateTime endDate, string correlationId)
        {
            var orders = await _orders.GetOrdersByCustomer(customerId, startDate, endDate);

            if (!orders.Any())
            {
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
                        .Text($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss} |  ID Usuario: {customerId}")
                        .FontSize(10);
                });
            });

            // Ángel: Cambié el método para que se genere en memoria y se envíe a la API.
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            var storageServerUrl = "http://127.0.0.1:8000/api/storage/upload";
            var fileName = $"Orders_{customerId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(correlationId), "correlationId");
            form.Add(new StringContent(customerId.ToString()), "clientId");
            form.Add(new StringContent(fileName), "fileName");
            form.Add(new StreamContent(stream), "file", fileName);

            var response = await _httpClient.PostAsync(storageServerUrl, form);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"PDF con Correlation ID {correlationId} enviado al Storage Server.");
                return $"Archivo {fileName} guardado en el Storage Server.";
            }
            else
            {
                Console.WriteLine($"Error al enviar el PDF al Storage Server. Código: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Contenido del error: {errorContent}");
                throw new Exception("Error al subir el archivo al Storage Server.");
            }
        }
    }
}
