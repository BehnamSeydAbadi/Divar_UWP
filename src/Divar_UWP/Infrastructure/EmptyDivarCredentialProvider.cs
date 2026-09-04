using System.Threading;
using System.Threading.Tasks;

namespace Divar_UWP.Infrastructure
{
    public sealed class EmptyDivarCredentialProvider : IDivarCredentialProvider
    {
        public bool HasFrontToken { get { return false; } }

        public Task<string> GetFrontTokenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(string.Empty);
        }

        public Task SaveFrontTokenAsync(string token, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); return Task.CompletedTask; }
        public Task ClearFrontTokenAsync(CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); return Task.CompletedTask; }
    }
}
