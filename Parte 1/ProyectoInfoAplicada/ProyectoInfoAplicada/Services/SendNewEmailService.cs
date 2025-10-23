using ProyectoInfoAplicada.Dto;

namespace ProyectoInfoAplicada.Services
{
    public class SendNewEmailService : ISendNewEmailService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILoggerService _fileLogger;
        private readonly IConfiguration _config;
        private readonly ILogger<SendNewEmailService> _consoleLogger;

        public SendNewEmailService(
            IHttpClientFactory httpFactory,
            ILoggerService fileLogger,
            IConfiguration config,
            ILogger<SendNewEmailService> logger)
        {
            _httpFactory = httpFactory;
            _fileLogger = fileLogger;
            _config = config;
            _consoleLogger = logger;
        }

        public async Task createNewEmailJob(EmailCorrelationRequest request)
        {
            var correlation = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString() : request.CorrelationId;

            // Hardcodeados 
            string toAddress = "itsgamc@gmail.com";
            string subject = "Prueba Gmail API";
            string body = "Adjunto PDF desde Gmail API";

            var emailServiceBase = _config.GetValue<string>("EmailUrl")?.TrimEnd('/')
                ?? throw new InvalidOperationException("EmailService:BaseUrl no configurado");

            var endpoint = $"{emailServiceBase}/api/email/send";

            var payload = new
            {
                correlation_id = correlation,
                to = toAddress,
                subject = subject,
                body = body
            };

            try
            {
                var client = _httpFactory.CreateClient("emailServiceClient");
                var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload)
                };
                // opcional: pasa correlation como header también
                httpReq.Headers.Add("X-Correlation-ID", correlation);

                _consoleLogger.LogInformation("SendNewEmailService: POST a {url} CorrelationId={corr}", endpoint, correlation);

                var resp = await client.SendAsync(httpReq);
                var respText = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    await _fileLogger.AppendCompletePetitionLog(
                        correlation,
                        service: "EmailService",
                        endpoint: endpoint,
                        payload: new { payload, Response = respText },
                        success: true);

                    _consoleLogger.LogInformation("Email solicitado correctamente. CorrelationId={corr}", correlation);
                }
                else
                {
                    await _fileLogger.AppendCompletePetitionLog(
                        correlation,
                        service: "EmailService",
                        endpoint: endpoint,
                        payload: new { payload, Status = (int)resp.StatusCode, Response = respText },
                        success: false);

                    _consoleLogger.LogWarning("EmailService devolvió {status}. CorrelationId={corr}", resp.StatusCode, correlation);
                    // Lanzar para que Hangfire reintente (según tu política)
                    throw new InvalidOperationException($"Email service error: {(int)resp.StatusCode} {respText}");
                }
            }
            catch (Exception ex)
            {
                await _fileLogger.AppendCompletePetitionLog(
                    correlation,
                    service: "EmailService",
                    endpoint: endpoint,
                    payload: new { Error = ex.Message },
                    success: false);

                _consoleLogger.LogError(ex, "Excepción al llamar EmailService. CorrelationId={corr}", correlation);
                throw; // dejar que Hangfire maneje reintentos
            }
        }
    }
}
