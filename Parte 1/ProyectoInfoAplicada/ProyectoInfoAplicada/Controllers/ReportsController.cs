using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProyectoInfoAplicada.Dto;
using ProyectoInfoAplicada.Repository;
using ProyectoInfoAplicada.Services;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ProyectoInfoAplicada.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ILogger<ReportsController> _consoleLogger;
        private readonly ILoggerService _fileLogger;
        private readonly IConfiguration _config;
        private readonly ICustomerRepository _customerRepository;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ISendPdfEnpointService _reportJobService;


        public ReportsController(
                                 ILogger<ReportsController> logger,
                                 ILoggerService fileLogger,
                                 IConfiguration config,
                                 ICustomerRepository customerRepository,
                                 IBackgroundJobClient backgroundJobClient,
                                 ISendPdfEnpointService reportJobService)
        {
            _consoleLogger = logger;
            _fileLogger = fileLogger;
            _config = config;
            _customerRepository = customerRepository;
            _backgroundJobClient = backgroundJobClient;
            _reportJobService = reportJobService;

        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> EnqueueReport([FromBody] ReportSimpleRequest req)
        {
            // 0) ModelState básico (Required)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1) CorrelationId: header X-Correlation-ID o body; si no existe, generar
            var headerCorr = Request.Headers.ContainsKey("X-Correlation-ID") ? Request.Headers["X-Correlation-ID"].ToString() : null;
            if (string.IsNullOrWhiteSpace(req.CorrelationId))
                req.CorrelationId = headerCorr ?? Guid.NewGuid().ToString();

            // 2) Validaciones
            if (req.CustomerId <= 0)
            {
                await _fileLogger.AppendSimpleLog(req.CorrelationId, $"Validación fallida: CustomerId inválido ({req.CustomerId})");
                return BadRequest(new { Error = "CustomerId inválido", CorrelationId = req.CorrelationId });
            }

            if (req.StartDate == default || req.EndDate == default)
            {
                await _fileLogger.AppendSimpleLog(req.CorrelationId, $"Validación fallida: Fechas no válidas. StartDate={req.StartDate}, EndDate={req.EndDate}");
                return BadRequest(new { Error = "Fechas inválidas", CorrelationId = req.CorrelationId });
            }

            if (req.StartDate > req.EndDate)
            {
                await _fileLogger.AppendSimpleLog(req.CorrelationId, $"Validación fallida: StartDate > EndDate. Start={req.StartDate}, End={req.EndDate}");
                return BadRequest(new { Error = "StartDate debe ser <= EndDate", CorrelationId = req.CorrelationId });
            }

            // 3) Validar existencia del cliente — 

            if (!await IsValidCustomerAsync(req.CustomerId))
            {
                await _fileLogger.AppendSimpleLog(req.CorrelationId, $"Validación fallida: CustomerId {req.CustomerId} no existe");
                return NotFound(new { Error = "Customer no encontrado", CorrelationId = req.CorrelationId });
            }

            // 4) Log de recepción (Serilog/Console + archivo por Correlation)
            _consoleLogger.LogInformation("Reporte solicitado. CustomerId={cid}, Start={start}, End={end}, CorrelationId={corr}",
                req.CustomerId, req.StartDate, req.EndDate, req.CorrelationId);
           
            
            // 5) Determinar delay (por defecto 5 minutos si no se pasa DelayMinutes)
            var defaultDelay = _config.GetValue<int?>("DefaultDelayMinutes") ?? 5;
            var delayMinutes = (req.DelayMinutes.HasValue && req.DelayMinutes.Value >= 0) ? req.DelayMinutes.Value : defaultDelay;

            var payload = new
            {
                CustomerId = req.CustomerId,
                StartDate = req.StartDate,
                EndDate = req.EndDate
            };

            // nombre del servicio y endpoint que quieres dejar en el log
            await _fileLogger.AppendCompletePetitionLog(
                req.CorrelationId!,
                service: "PdfGenerationServer",
                endpoint: "/api/reports",              
                payload: payload,
                success: true                           // recepción fue OK
            );

            //ESTO ENCOLA A HANGIFRE fijese si InvokeGeneralPdfInteral sirve para llamar a su endpoint xd
            // 6) Programar job que llamará internamente al servicio de jobs 
            //Llamar al endpoint 
            var jobId = _backgroundJobClient.Schedule<ISendPdfEnpointService>(
            svc => _reportJobService.SendToPdfEndpoint(req),             
            TimeSpan.FromMinutes(delayMinutes));


            // 7) Responder 202 Accepted con metadata
            return Accepted(new { CorrelationId = req.CorrelationId, ScheduledInMinutes = delayMinutes });
        }

        private Task<bool> IsValidCustomerAsync(int customerId)
        {
            //comprobar en DB si el cliente existe.
            return _customerRepository.ExistsCustomerIdAsync(customerId);

        }
    }

}