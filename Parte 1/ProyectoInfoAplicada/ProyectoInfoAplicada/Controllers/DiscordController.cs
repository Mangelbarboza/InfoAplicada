using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoInfoAplicada.Dto;
using ProyectoInfoAplicada.Services;

namespace ProyectoInfoAplicada.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/discord")]
    public class DiscordController : ControllerBase
    {
        private readonly ILoggerService _fileLogger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public DiscordController(ILoggerService fileLogger, IBackgroundJobClient backgroundJobClient)
        {
            _fileLogger = fileLogger;
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpPost("enqueue-simple")]
        public async Task<IActionResult> EnqueueSimpleDiscord([FromBody] DiscordCorrelationRequest req)
        {
            if (req == null)
                return BadRequest("Request vacío");

            if (string.IsNullOrWhiteSpace(req.CorrelationId))
                req.CorrelationId = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(req.RecipientId))
                return BadRequest("El campo RecipientId es obligatorio");

            // Encola el trabajo para ejecutarse dentro de 1 minuto
            var jobId = _backgroundJobClient.Schedule<ISendNewDiscordMessage>(
                svc => svc.createNewDiscordMessage(req),
                TimeSpan.FromMinutes(1)
            );

            await _fileLogger.AppendCompletePetitionLog(
                req.CorrelationId!,
                service: "DiscordEnqueue",
                endpoint: "/api/discord/enqueue-simple",
                payload: new { JobId = jobId },
                success: true
            );

            return Accepted(new { CorrelationId = req.CorrelationId, JobId = jobId });
        }
    }
}
