using ProyectoInfoAplicada.Dto;

namespace ProyectoInfoAplicada.Services
{
    public interface ISendNewDiscordMessage
    {
        Task createNewDiscordMessage(DiscordCorrelationRequest request);
    }
}
