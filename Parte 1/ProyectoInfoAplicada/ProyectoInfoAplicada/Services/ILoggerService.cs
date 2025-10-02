namespace ProyectoInfoAplicada.Services
{
    public interface ILoggerService
    {

        Task AppendSimpleLog(string correlationId, string message);
        Task AppendCompletePetitionLog(string correlationId, string service, string endpoint, object payload, bool success);
    }


}
