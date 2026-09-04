using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarBookmarkService
    {
        Task<ServiceResult<IList<DivarBookmark>>> GetBookmarksAsync(CancellationToken cancellationToken);
    }
}
