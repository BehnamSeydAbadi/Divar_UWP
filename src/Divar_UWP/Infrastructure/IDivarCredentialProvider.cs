using System.Threading;
using System.Threading.Tasks;

namespace Divar_UWP.Infrastructure
{
    public interface IDivarCredentialProvider
    {
        Task<string> GetFrontTokenAsync(CancellationToken cancellationToken);
        Task SaveFrontTokenAsync(string token, CancellationToken cancellationToken);
        Task ClearFrontTokenAsync(CancellationToken cancellationToken);
        bool HasFrontToken { get; }
    }
}
