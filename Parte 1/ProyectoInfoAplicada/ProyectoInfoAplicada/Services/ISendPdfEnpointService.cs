using ProyectoInfoAplicada.Dto;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ProyectoInfoAplicada.Services
{
    public interface ISendPdfEnpointService
    {
        Task SendToPdfEndpoint(ReportSimpleRequest request);
    }

}
