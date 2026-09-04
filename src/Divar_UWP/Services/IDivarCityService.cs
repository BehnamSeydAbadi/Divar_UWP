using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarCityService
    {
        Task<ServiceResult<IList<DivarCity>>> GetCitiesAsync(CancellationToken cancellationToken);

        Task<ServiceResult<DivarCity>> FindCityAsync(double latitude, double longitude, CancellationToken cancellationToken);
    }
}
