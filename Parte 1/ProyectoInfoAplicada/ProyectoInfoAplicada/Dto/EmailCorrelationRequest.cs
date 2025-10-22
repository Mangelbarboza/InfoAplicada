namespace ProyectoInfoAplicada.Dto
{
    public class EmailCorrelationRequest
    {
        // opcional: si viene vacío se generará uno en el servicio
        public string? CorrelationId { get; set; }
    }
}