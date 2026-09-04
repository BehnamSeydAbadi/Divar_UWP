using System.Threading;
using System.Threading.Tasks;

namespace Divar_UWP.Infrastructure
{
    public sealed class EmptyDivarCredentialProvider : IDivarCredentialProvider
    {
        public Task<string> GetFrontTokenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(string.Empty);
        }
    }
}
