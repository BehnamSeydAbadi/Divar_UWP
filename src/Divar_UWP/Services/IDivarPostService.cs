using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarPostService
    {
        Task<ServiceResult<DivarPostDetails>> GetPostAsync(
            string postToken,
            string trackerSessionId,
            CancellationToken cancellationToken);
    }
}
