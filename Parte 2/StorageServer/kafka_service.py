from kafka import KafkaProducer
import json

class KafkaLogProducer:
    def __init__(self, broker_address, topic_name):
        self.producer = KafkaProducer(
            bootstrap_servers=[broker_address],
            value_serializer=lambda v: json.dumps(v).encode('utf-8')
        )
        self.topic = topic_name

    def send_log(self, log_message):
        try:
            # Enviar el mensaje al tópico
            future = self.producer.send(self.topic, value=log_message)
            
            
            record_metadata = future.get(timeout=10)
            print(f"Log enviado a Kafka: {record_metadata.topic} [partición {record_metadata.partition}] @ offset {record_metadata.offset}")
        except Exception as e:
            print(f"Error al enviar log a Kafka: {e}")