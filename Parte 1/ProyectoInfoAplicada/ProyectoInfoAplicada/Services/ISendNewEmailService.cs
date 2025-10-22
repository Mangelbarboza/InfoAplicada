using ProyectoInfoAplicada.Dto;

namespace ProyectoInfoAplicada.Services
{
    public interface ISendNewEmailService
    {
        Task createNewEmailJob(EmailCorrelationRequest request);
    }
}
