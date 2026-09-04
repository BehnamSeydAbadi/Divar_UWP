using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarAuthService
    {
        bool IsAuthenticated { get; }

        Task<ServiceResult<DivarAuthChallenge>> SendCodeAsync(
            string phoneNumber,
            CancellationToken cancellationToken);

        Task<ServiceResult<bool>> ConfirmCodeAsync(
            DivarAuthChallenge challenge,
            string code,
            CancellationToken cancellationToken);

        Task<ServiceResult<bool>> SignOutAsync(CancellationToken cancellationToken);
    }
}
