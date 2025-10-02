using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoInfoAplicada.Services
{
    public class LoggerService : ILoggerService
    {

        private readonly string baseFolder = Path.Combine(AppContext.BaseDirectory, "Logs");
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        public LoggerService()
        {
            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);
        }

        public async Task AppendSimpleLog(string correlationId, string message)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) correlationId = "no-correlation";
            var file = Path.Combine(baseFolder, $"{correlationId}.txt");
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ssZ}] {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(file, line);
        }

        public async Task AppendCompletePetitionLog(string correlationId, string service, string endpoint, object payload, bool success)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString();

            var record = new
            {
                CorrelationId = correlationId,
                Service = service,
                Endpoint = endpoint,
                TimeStamp = DateTime.UtcNow.ToString("o"),
                Payload = payload,
                Success = success
            };

            var json = JsonSerializer.Serialize(record, _jsonOptions);

            var file = Path.Combine(baseFolder, $"{correlationId}.txt");
            // Escribir cada registro JSON en una línea separada
            await File.AppendAllTextAsync(file, json + Environment.NewLine);
        }

    }
}
