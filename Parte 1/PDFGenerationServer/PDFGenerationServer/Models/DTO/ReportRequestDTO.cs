namespace PDFGenerationServer.Models.DTO
{
    public class ReportRequestDTO
    {
        public int CustomerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
