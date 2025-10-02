using PDFGenerationServer.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Companion;
using System.Globalization;

namespace PDFGenerationServer.Services
{
    public class PdfReportService
    {
        private readonly OrdersData _orders;

        public PdfReportService(OrdersData orders)
        {
            _orders = orders;
        }

        public async Task<String> GenerateReportPDF(int customerId, DateTime startDate, DateTime endDate)
        {
            var orders = await _orders.GetOrdersByCustomer(customerId, startDate, endDate);

            if (!orders.Any())
            {
                return null;
            }

            var folderPath = Path.Combine("wwwroot", "reports", DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(folderPath)) 
            { 
                Directory.CreateDirectory(folderPath);
            }

            var fileName = $"Orders_{customerId}_{DateTime.Now:HHmmss}.pdf";
            var filePath = Path.Combine(folderPath, fileName);
            // Generar PDF
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
                            //Columnas de la tabla
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // GUIA
                                columns.RelativeColumn(1); // SalesOrderId
                                columns.RelativeColumn(1); // Fecha
                                columns.RelativeColumn(1); // Total
                            });

                            // Header de tabla
                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(2).Padding(8).Text("#").SemiBold();
                                header.Cell().BorderBottom(2).Padding(8).Text("Orden ID").SemiBold();
                                header.Cell().BorderBottom(2).Padding(8).Text("Fecha").SemiBold();
                                header.Cell().BorderBottom(2).Padding(8).Text("Total").SemiBold();
                            });

                            // Filas
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

            //  Guardar archivo
            document.GeneratePdf(filePath);
            //document.ShowInCompanion();
            
            return filePath;

        }
    }
}
