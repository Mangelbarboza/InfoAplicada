class LogMessageDTO {
  constructor({
    correlationId,
    service,
    endpoint,
    timeStrap = new Date().toISOString(),
    payload = {},
    success = false
  }) {
    this.correlationId = correlationId;
    this.service = service;
    this.endpoint = endpoint;
    this.timeStrap = timeStrap; 
    this.payload = payload;     
    this.success = success;
  }
}

module.exports = LogMessageDTO;
