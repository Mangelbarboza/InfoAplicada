
using PDFGenerationServer.Models.DTO;
using PDFGenerationServer.Services;
using System.Text.Json;

namespace PDFGenerationServer.Data
{
    public class FileLogProducer : ILogProducer
    {
        private readonly string _filePath;

        public FileLogProducer()
        {
            _filePath = "logs.txt";
        }
        public async Task sendLog(LogMessageDTO log)
        {
            var json = JsonSerializer.Serialize(log, new JsonSerializerOptions
            {
                WriteIndented = true
            }); 
            await File.AppendAllTextAsync( _filePath, json + Environment.NewLine);
        }
    }
}
