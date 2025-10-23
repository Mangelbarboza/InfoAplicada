using ProyectoInfoAplicada.Dto;

namespace ProyectoInfoAplicada.Services
{
    public class SendDiscordMessageService : ISendNewDiscordMessage
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILoggerService _fileLogger;
        private readonly IConfiguration _config;
        private readonly ILogger<SendDiscordMessageService> _consoleLogger;

        public SendDiscordMessageService(
            IHttpClientFactory httpFactory,
            ILoggerService fileLogger,
            IConfiguration config,
            ILogger<SendDiscordMessageService> logger)
        {
            _httpFactory = httpFactory;
            _fileLogger = fileLogger;
            _config = config;
            _consoleLogger = logger;
        }

        public async Task createNewDiscordMessage(DiscordCorrelationRequest request)
        {
            var correlation = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? Guid.NewGuid().ToString()
                : request.CorrelationId;

            var recipientId = request.RecipientId;

            if (string.IsNullOrWhiteSpace(recipientId))
                throw new ArgumentException("RecipientId es obligatorio para enviar mensaje a Discord");

            var messagingServiceBase = _config.GetValue<string>("MessageUrl")?.TrimEnd('/')
                ?? throw new InvalidOperationException("MessagingService:BaseUrl no configurado");

            var endpoint = $"{messagingServiceBase}/api/messaging/send";

            var payload = new
            {
                correlationId = correlation,
                recipientId = recipientId
            };

            try
            {
                var client = _httpFactory.CreateClient("messagingServiceClient");
                var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload)
                };
                httpReq.Headers.Add("X-Correlation-ID", correlation);

                _consoleLogger.LogInformation(
                    "SendDiscordMessageService: POST a {url} CorrelationId={corr}, Recipient={rec}",
                    endpoint, correlation, recipientId
                );

                var resp = await client.SendAsync(httpReq);
                var respText = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    await _fileLogger.AppendCompletePetitionLog(
                        correlation,
                        service: "MessagingService",
                        endpoint: endpoint,
                        payload: new { payload, Response = respText },
                        success: true);

                    _consoleLogger.LogInformation("Mensaje de Discord encolado correctamente. CorrelationId={corr}", correlation);
                }
                else
                {
                    await _fileLogger.AppendCompletePetitionLog(
                        correlation,
                        service: "MessagingService",
                        endpoint: endpoint,
                        payload: new { payload, Status = (int)resp.StatusCode, Response = respText },
                        success: false);

                    _consoleLogger.LogWarning(
                        "MessagingService devolvió {status}. CorrelationId={corr}",
                        resp.StatusCode, correlation);

                    throw new InvalidOperationException($"Messaging service error: {(int)resp.StatusCode} {respText}");
                }
            }
            catch (Exception ex)
            {
                await _fileLogger.AppendCompletePetitionLog(
                    correlation,
                    service: "MessagingService",
                    endpoint: endpoint,
                    payload: new { Error = ex.Message },
                    success: false);

                _consoleLogger.LogError(ex, "Excepción al llamar MessagingService. CorrelationId={corr}", correlation);
                throw;
            }
        }
    }
}
