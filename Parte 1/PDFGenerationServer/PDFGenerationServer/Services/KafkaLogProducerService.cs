using Confluent.Kafka;
using PDFGenerationServer.Models.DTO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PDFGenerationServer.Services
{
    public class KafkaLogProducerService : ILogProducer
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

        public async Task sendLog(LogMessageDTO log)
        {
            try
            {
                string jsonMessage = JsonSerializer.Serialize(log);

                var deliveryResult = await _producer.ProduceAsync(
                    _topic,
                    new Message<string, string> { Key = log.CorrelationId, Value = jsonMessage });

                Console.WriteLine($"Log enviado a Kafka. Tópico: {deliveryResult.Topic}, Partición: {deliveryResult.Partition}, Offset: {deliveryResult.Offset}, Key: {deliveryResult.Key}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Error al enviar log a Kafka: {e.Error.Reason}");
            }
        }
    }
}