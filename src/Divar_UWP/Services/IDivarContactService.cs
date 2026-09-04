using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarContactService
    {
        Task<ServiceResult<DivarContactResult>> GetPhoneAsync(string postToken, string contactUuid, string trackerSessionId, CancellationToken cancellationToken);
    }
}
