using ProyectoInfoAplicada.Dto;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ProyectoInfoAplicada.Services
{
    public interface IReportGeneratorService
    {
        Task<string> GeneratePdfAsync(ReportRequest req);
    }
}
