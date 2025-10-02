using System.ComponentModel.DataAnnotations;

namespace ProyectoInfoAplicada.Dto
{
    public class ReportSimpleRequest
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Opcionales
        public string? CorrelationId { get; set; }
        public int? DelayMinutes { get; set; } 
    }
}
