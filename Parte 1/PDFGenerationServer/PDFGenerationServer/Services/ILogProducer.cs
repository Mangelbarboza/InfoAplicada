using PDFGenerationServer.Models.DTO;

namespace PDFGenerationServer.Services
{
    public interface ILogProducer
    {
        Task sendLog(LogMessageDTO log);
    }
}
