namespace PDFGenerationServer.Models.DTO
{
    public class LogMessageDTO
    {
        public string CorrelationId { get; set; }
        public string Service {  get; set; }
        public string Endpoint { get; set; }
        public DateTime TimeStrap {  get; set; }
        public object Playload { get; set; }
        public bool  Success { get; set; }

    }
}
