using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDFGenerationServer.Models.DTO;
using PDFGenerationServer.Services;
using static System.Net.WebRequestMethods;
namespace PDFGenerationServer.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        
        private readonly PdfReportService _pdfService;
        private readonly ILogProducer _logger;

        private static readonly HttpClient _httpClient = new HttpClient();
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
            var filePath = await _pdfService.GenerateReportPDF(customerId, startDate, endDate, correlationId);
            // Llama al servicio que ya genera el PDF
            //var filePath = await _pdfService.GenerateReportPDF(customerId, startDate, endDate);

            if (string.IsNullOrWhiteSpace(filePath))
                return NotFound($"No se encontraron órdenes para el cliente {customerId}");
            //Ejemplo de Logger
           await GenerateLogMessage(correlationId, customerId, startDate, endDate, filePath);

            //INVOCAR AL JOB DE HANFIRE
            await sendJobToHangFireEmail(correlationId);

            await sendJobToHangFireDiscord(correlationId, "700581845850390578");

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
                TimeStrap = DateTime.UtcNow.ToString("o"),
                Payload = new ReportRequestDTO
                {
                    CustomerId = customerId,
                    StartDate = startDate,
                    EndDate = endDate
                },
                Success = filePath != null
            };
            await _logger.sendLog(log);
        }

        private async Task sendJobToHangFireEmail(string correlationId)
        {
            string BASE_URL = "http://localhost:5100";
            var req = JsonContent.Create(new { correlationId });

            var res = await _httpClient.PostAsync($"{BASE_URL}/api/emails/enqueue-simple", req);

            var success = res.IsSuccessStatusCode;
            var message = success
                ? "Se le avisó a Hangfire que mande un email"
                : "No se pudo encolar la petición de email";

            var log = new LogMessageDTO
            {
                CorrelationId = correlationId,
                Service = "PdfGenerationServer",
                Endpoint = "/api/orders/GeneratePdf",
                TimeStrap = DateTime.UtcNow.ToString("o"),
                Payload = new { Message = message },
                Success = success
            };

            await _logger.sendLog(log);
        }

        private async Task sendJobToHangFireDiscord(string correlationId, string recipientId)
        {
            string BASE_URL = "http://localhost:5100";
            var req = JsonContent.Create(new
            {
                correlationId,
                recipientId
            });

            var res = await _httpClient.PostAsync($"{BASE_URL}/api/discord/enqueue-simple", req);

            var success = res.IsSuccessStatusCode;
            var message = success
                ? "Se le avisó a Hangfire que mande un mensaje por Discord"
                : "No se pudo encolar la petición de Discord";

            var log = new LogMessageDTO
            {
                CorrelationId = correlationId,
                Service = "PdfGenerationServer",
                Endpoint = "/api/orders/GeneratePdf",
                TimeStrap = DateTime.UtcNow.ToString("o"),
                Payload = new { Message = message },
                Success = success
            };

            await _logger.sendLog(log);
        }
    }

}
