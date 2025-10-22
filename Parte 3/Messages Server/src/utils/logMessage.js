const { Kafka } = require('kafkajs');
const logger = require('../utils/logger');

const kafka = new Kafka({
  clientId: 'message-server',
  brokers: ['localhost:9092'] 
});

const producer = kafka.producer();

class LogProducer {
  async sendLog(logMessage) {
    try {
      await producer.connect();
      await producer.send({
        topic: 'logs-messageServer',
        messages: [{ value: JSON.stringify(logMessage) }]
      });
      logger.info('Log enviado a Kafka correctamente');
      await producer.disconnect();
    } catch (err) {
      logger.error('Error enviando log a Kafka', { error: err.message });
    }
  }
}

module.exports = new LogProducer();
