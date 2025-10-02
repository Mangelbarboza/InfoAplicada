using ProyectoInfoAplicada.Dto;

namespace ProyectoInfoAplicada.Services
{
    public class SendPdfEndpointService : ISendPdfEnpointService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILoggerService _fileLogger;
        private readonly IConfiguration _config;
        private readonly ILogger<SendPdfEndpointService> _consoleLogger;

        public SendPdfEndpointService(
            IHttpClientFactory httpFactory,
            ILoggerService fileLogger,
            IConfiguration config,
            ILogger<SendPdfEndpointService> logger)
        {
            _httpFactory = httpFactory;
            _fileLogger = fileLogger;
            _config = config;
            _consoleLogger = logger;
        }

        public async Task SendToPdfEndpoint(ReportSimpleRequest request)
        {
            // Asegurar correlation id
            var correlation = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString() : request.CorrelationId;

            // Construir payload con solo los 3 campos requeridos
            var payload = new
            {
                CustomerId = request.CustomerId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            // Resolver URL del callback: PdfCallbackUrl en config o fallback a PublicBaseUrl + /api/pdf/generate
            var callbackUrl = _config.GetValue<string>("PdfCallbackUrl")?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                var baseUrl = _config.GetValue<string>("PublicBaseUrl")?.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                    throw new InvalidOperationException("No se encontró PdfCallbackUrl ni PublicBaseUrl en la configuración.");
                callbackUrl = $"{baseUrl}/api/Orders/GeneratePdf";
            }

            try
            {
                var client = _httpFactory.CreateClient(); // usa cliente por defecto; puedes usar named client para dev
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, callbackUrl)
                {
                    Content = JsonContent.Create(payload)
                };

                // Header de correlation id
                requestMsg.Headers.Add("Correlation-ID", correlation);

                _consoleLogger.LogInformation("ReportJobService: llamando callback {url} CorrelationId={corr}", callbackUrl, correlation);

                // --- LOG DE LA SOLICITUD ANTES DE ENVIARLA ---
                string bodyPreview = requestMsg.Content is not null
                    ? await requestMsg.Content.ReadAsStringAsync()
                    : "(no body)";

                _consoleLogger.LogInformation(
                    "PREVIEW HTTP REQUEST\nMethod: {method}\nURL: {url}\nHeaders: {headers}\nBody: {body}",
                    requestMsg.Method,
                    requestMsg.RequestUri,
                    string.Join("; ", requestMsg.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")),
                    bodyPreview
                );
                // --FIN--

                //SE ENVIA EN ESEPCIFICO LA SOLICITUD HTTP AL ENDPOINT
                var resp = await client.SendAsync(requestMsg);

                var respContent = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    await _fileLogger.AppendCompletePetitionLog(
                        correlation,
                        service: "PdfGenerationServer",
                        endpoint: callbackUrl,
                        payload: new { payload.CustomerId, payload.StartDate, payload.EndDate, Response = respContent },
                        success: true);

                    _consoleLogger.LogInformation("ReportJobService: callback OK. CorrelationId={corr}", correlation);
                }
                else
                {
                    await _fileLogger.AppendCompletePetitionLog(
                        correlation,
                        service: "PdfGenerationServer",
                        endpoint: callbackUrl,
                        payload: new { payload.CustomerId, payload.StartDate, payload.EndDate, Status = (int)resp.StatusCode, Response = respContent },
                        success: false);

                    _consoleLogger.LogWarning("ReportJobService: callback devolvió {status}. CorrelationId={corr}", resp.StatusCode, correlation);
                }
            }
            catch (Exception ex)
            {
                await _fileLogger.AppendCompletePetitionLog(
                    correlation,
                    service: "PdfGenerationServer",
                    endpoint: callbackUrl,
                    payload: new { payload.CustomerId, payload.StartDate, payload.EndDate, Error = ex.Message },
                    success: false);

                _consoleLogger.LogError(ex, "ReportJobService: excepción al llamar callback. CorrelationId={corr}", correlation);
                throw; // dejar que Hangfire maneje reintentos según su política
            }
        }
    }
}
