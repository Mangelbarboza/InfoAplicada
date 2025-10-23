namespace ProyectoInfoAplicada.Dto
{
    public class DiscordCorrelationRequest
    {
        public string? CorrelationId { get; set; }
        public string RecipientId { get; set; } = string.Empty;
    }
}
