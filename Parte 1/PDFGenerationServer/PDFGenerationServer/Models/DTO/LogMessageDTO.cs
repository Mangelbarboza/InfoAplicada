namespace PDFGenerationServer.Models.DTO
{
    public class LogMessageDTO
    {
        public string CorrelationId { get; set; }
        public string Service { get; set; }
        public string Endpoint { get; set; }
        public string TimeStrap { get; set; } // aqui
        public object Payload { get; set; }   // y aqui, cambie el nombre porque estaba mal escrito xd
        public bool Success { get; set; }
    }
}