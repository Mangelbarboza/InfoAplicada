using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDFGenerationServer.Models.DTO;
using PDFGenerationServer.Services;
namespace PDFGenerationServer.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        
        private readonly PdfReportService _pdfService;
        private readonly ILogProducer _logger;
        public OrdersController(PdfReportService pdfService, ILogProducer logger)
        { 
            _pdfService = pdfService;
            _logger = logger;
        }
       //debe ser un post
        [HttpPost("GeneratePdf")]
        public async Task<IActionResult> GenerateOrdersPdf([FromBody] ReportRequestDTO request, [FromHeader(Name = "Correlation-ID")] string correlationId)
        {
            
            var customerId = request.CustomerId;
            var startDate = request.StartDate;
            var endDate = request.EndDate;
            var filePath = await _pdfService.GenerateReportPDF(customerId, startDate, endDate);
            // Llama al servicio que ya genera el PDF
            //var filePath = await _pdfService.GenerateReportPDF(customerId, startDate, endDate);

            if (string.IsNullOrWhiteSpace(filePath))
                return NotFound($"No se encontraron órdenes para el cliente {customerId}");
            //Ejemplo de Logger
           await GenerateLogMessage(correlationId, customerId, startDate, endDate, filePath);

            
            // Devuelve el nombre del archivo y la ruta generada
            return Ok(new {  Message = "PDF generado correctamente", FilePath = filePath });

        }

        private async Task GenerateLogMessage(string correlationId, int customerId, DateTime startDate, DateTime endDate, string filePath)
        {
            //string correlationId = Guid.NewGuid().ToString();
            var log = new LogMessageDTO
            {
                CorrelationId = correlationId,
                Service = "PdfGenerationServer",
                Endpoint = "/api/orders/GeneratePdf",
                TimeStrap = DateTime.UtcNow,
                Playload = new ReportRequestDTO
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                },
                Success = filePath != null
            };
            await _logger.sendLog(log);
        }
    }

}
