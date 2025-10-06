using Confluent.Kafka;
using ProyectoInfoAplicada.Models.DTO; 
using System.Text.Json;
using System.Threading.Tasks;

namespace ProyectoInfoAplicada.Services
{
    public class KafkaLogProducerService : ILoggerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topic;

        public KafkaLogProducerService(string kafkaBroker, string topicName)
        {
            var config = new ProducerConfig { BootstrapServers = kafkaBroker };
            _producer = new ProducerBuilder<string, string>(config)
                .SetValueSerializer(Serializers.Utf8)
                .Build();
            _topic = topicName;
        }

        public async Task AppendSimpleLog(string correlationId, string message)
        {
            var logMessage = new LogMessageDTO
            {
                CorrelationId = correlationId,
                Service = "Hangfire Server",
                Endpoint = "Simple Log",
                Timestamp = DateTime.UtcNow.ToString("o"),
                Payload = new { message = message },
                Success = true
            };
            await sendLog(logMessage);
        }

        public async Task AppendCompletePetitionLog(string correlationId, string service, string endpoint, object payload, bool success)
        {
            var logMessage = new LogMessageDTO
            {
                CorrelationId = correlationId,
                Service = service,
                Endpoint = endpoint,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Payload = payload,
                Success = success
            };
            await sendLog(logMessage);
        }

        private async Task sendLog(LogMessageDTO log)
        {
            try
            {
                string jsonMessage = JsonSerializer.Serialize(log);
                var deliveryResult = await _producer.ProduceAsync(
                    _topic,
                    new Message<string, string> { Key = log.CorrelationId, Value = jsonMessage });
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Error al enviar log a Kafka: {e.Error.Reason}");
            }
        }
    }
}