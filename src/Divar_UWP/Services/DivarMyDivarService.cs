using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarMyDivarService : IDivarMyDivarService
    {
        private const string ProfilePath = "v8/user-profile";
        private const string MyPostsPath = "v8/my-posts/w/list-v2";
        private readonly IDivarApiClient _apiClient;

        public DivarMyDivarService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<DivarMyDivarData>> GetAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = new DivarMyDivarData
                {
                    DisplayName = "کاربر دیوار"
                };

                // Profile decoration is optional. A temporary failure here must not hide
                // the user's posts; only a real authentication failure stops the flow.
                var profileResponse = await _apiClient.GetAsync(ProfilePath, cancellationToken, true);
                cancellationToken.ThrowIfCancellationRequested();
                if (!profileResponse.IsSuccess && IsAuthenticationFailure(profileResponse))
                    return ServiceResult<DivarMyDivarData>.Failure(Error(profileResponse, string.Empty));

                JsonObject profile;
                if (profileResponse.IsSuccess && JsonObject.TryParse(profileResponse.Content, out profile))
                {
                    var displayName = FindString(profile, new[] { "display_name", "user_name", "name" });
                    if (!string.IsNullOrWhiteSpace(displayName)) result.DisplayName = displayName;
                    result.PhoneNumber = FindString(profile, new[] { "masked_phone", "phone_number", "phone" });
                }

                var postsResponse = await _apiClient.PostJsonAsync(MyPostsPath, "{}", cancellationToken, true);
                cancellationToken.ThrowIfCancellationRequested();
                if (!postsResponse.IsSuccess) return ServiceResult<DivarMyDivarData>.Failure(Error(postsResponse, "دریافت آگهی‌های من ممکن نشد."));

                JsonObject posts;
                if (!JsonObject.TryParse(postsResponse.Content, out posts))
                    return ServiceResult<DivarMyDivarData>.Failure("پاسخ آگهی‌های من قابل خواندن نیست.");
                foreach (var post in DivarPostWidgetParser.Parse(posts)) result.Posts.Add(post);
                return ServiceResult<DivarMyDivarData>.Success(result);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<DivarMyDivarData>.Failure("خطایی هنگام خواندن دیوار من رخ داد."); }
        }

        private static string FindString(IJsonValue value, string[] keys)
        {
            if (value == null) return string.Empty;
            if (value.ValueType == JsonValueType.Array)
            {
                foreach (var item in value.GetArray())
                {
                    var found = FindString(item, keys);
                    if (!string.IsNullOrWhiteSpace(found)) return found;
                }
            }
            else if (value.ValueType == JsonValueType.Object)
            {
                foreach (var pair in value.GetObject())
                {
                    if (System.Array.IndexOf(keys, pair.Key) >= 0 && pair.Value.ValueType == JsonValueType.String) return pair.Value.GetString();
                    var found = FindString(pair.Value, keys);
                    if (!string.IsNullOrWhiteSpace(found)) return found;
                }
            }
            return string.Empty;
        }

        private static string Error(DivarApiResponse response, string fallback)
        {
            return IsAuthenticationFailure(response)
                ? "نشست شما منقضی شده است. دوباره وارد حساب شوید."
                : fallback;
        }

        private static bool IsAuthenticationFailure(DivarApiResponse response)
        {
            return response.StatusCode == HttpStatusCode.Unauthorized || response.ErrorMessage == "Authentication is required.";
        }
    }
}
