using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;

namespace Divar_UWP.Services
{
    public interface IDivarSearchService
    {
        Task<ServiceResult<DivarPage<DivarPostSummary>>> SearchAsync(
            DivarSearchRequest request,
            CancellationToken cancellationToken);

        Task<ServiceResult<IList<DivarSearchSuggestion>>> GetSuggestionsAsync(
            string query,
            IList<string> cityIds,
            CancellationToken cancellationToken);
    }
}
