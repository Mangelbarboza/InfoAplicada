using System;

namespace ProyectoInfoAplicada.Models.DTO
{
    public class LogMessageDTO
    {
        public string CorrelationId { get; set; }
        public string Service { get; set; }
        public string Endpoint { get; set; }
        public string Timestamp { get; set; }
        public object Payload { get; set; }
        public bool Success { get; set; }
    }
}