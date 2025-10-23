using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoInfoAplicada.Dto;
using ProyectoInfoAplicada.Services;

namespace ProyectoInfoAplicada.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/emails")]
    public class EmailsController : ControllerBase
    {
        private readonly ILoggerService _fileLogger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public EmailsController(ILoggerService fileLogger, IBackgroundJobClient backgroundJobClient)
        {
            _fileLogger = fileLogger;
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpPost("enqueue-simple")]
        public async Task<IActionResult> EnqueueSimpleEmail([FromBody] EmailCorrelationRequest req)
        {
            if (req == null)
                return BadRequest("Request vacío");

            if (string.IsNullOrWhiteSpace(req.CorrelationId))
                req.CorrelationId = Guid.NewGuid().ToString();

            var jobId = _backgroundJobClient.Schedule<ISendNewEmailService>(svc => svc.createNewEmailJob(req), TimeSpan.FromMinutes(1));

            await _fileLogger.AppendCompletePetitionLog(req.CorrelationId!, "EmailEnqueue", "/api/emails/enqueue-simple", new { JobId = jobId }, true);

            return Accepted(new { CorrelationId = req.CorrelationId, JobId = jobId });
        }
    }
}
