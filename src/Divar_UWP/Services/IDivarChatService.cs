using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarChatService
    {
        Task<ServiceResult<DivarChatSession>> InitializeAsync(CancellationToken cancellationToken);
    }
}
