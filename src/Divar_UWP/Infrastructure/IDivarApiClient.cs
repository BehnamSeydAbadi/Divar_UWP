using System.Threading;
using System.Threading.Tasks;

namespace Divar_UWP.Infrastructure
{
    public interface IDivarApiClient
    {
        Task<DivarApiResponse> GetAsync(
            string relativePath,
            CancellationToken cancellationToken,
            bool authenticated = false);

        Task<DivarApiResponse> PostJsonAsync(
            string relativePath,
            string json,
            CancellationToken cancellationToken,
            bool authenticated = false);
    }
}
