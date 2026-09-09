using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarBookmarkService : IDivarBookmarkService
    {
        private const string ListPath = "v8/yaad-v2/bookmarks-tab/widgets";
        private const string SetPath = "yaad/bookmark-v2";
        private const string DeletePath = "v8/yaad-v2/bookmark?token=";
        private readonly IDivarApiClient _apiClient;
        private readonly HashSet<string> _knownTokens = new HashSet<string>(StringComparer.Ordinal);
        private bool _hasLoaded;

        public DivarBookmarkService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<IList<DivarBookmark>>> GetBookmarksAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _apiClient.GetAsync(ListPath, cancellationToken, true);
                cancellationToken.ThrowIfCancellationRequested();
                if (!response.IsSuccess) return ServiceResult<IList<DivarBookmark>>.Failure(Error(response, "دریافت نشان‌ها ممکن نشد."));

                JsonObject root;
                if (!JsonObject.TryParse(response.Content, out root))
                    return ServiceResult<IList<DivarBookmark>>.Failure("پاسخ نشان‌ها قابل خواندن نیست.");

                var posts = DivarPostWidgetParser.Parse(root);
                var notes = ReadNotes(root);
                var bookmarkTokens = new HashSet<string>(ReadBookmarkTokens(root), StringComparer.Ordinal);
                var bookmarks = posts.Select(post => new DivarBookmark
                {
                    PostToken = post.Token,
                    Post = post,
                    Note = notes.ContainsKey(post.Token) ? notes[post.Token] : string.Empty
                }).ToList();

                _knownTokens.Clear();
                foreach (var token in bookmarkTokens) _knownTokens.Add(token);
                if (!root.ContainsKey("bookmarks"))
                    foreach (var post in posts) _knownTokens.Add(post.Token);
                _hasLoaded = true;
                return ServiceResult<IList<DivarBookmark>>.Success(bookmarks);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<IList<DivarBookmark>>.Failure("خطایی هنگام خواندن نشان‌ها رخ داد."); }
        }

        public async Task<ServiceResult<bool>> IsBookmarkedAsync(string postToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(postToken)) return ServiceResult<bool>.Failure("شناسهٔ آگهی معتبر نیست.");
            if (!_hasLoaded)
            {
                var loaded = await GetBookmarksAsync(cancellationToken);
                if (!loaded.IsSuccess) return ServiceResult<bool>.Failure(loaded.ErrorMessage);
            }
            return ServiceResult<bool>.Success(_knownTokens.Contains(postToken));
        }

        public async Task<ServiceResult<bool>> SetBookmarkedAsync(string postToken, bool isBookmarked, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(postToken)) return ServiceResult<bool>.Failure("شناسهٔ آگهی معتبر نیست.");
            try
            {
                if (!_hasLoaded)
                {
                    var current = await IsBookmarkedAsync(postToken, cancellationToken);
                    if (!current.IsSuccess) return current;
                    if (current.Value == isBookmarked) return current;
                }
                else if (_knownTokens.Contains(postToken) == isBookmarked)
                {
                    return ServiceResult<bool>.Success(isBookmarked);
                }

                DivarApiResponse response;
                if (isBookmarked)
                {
                    var requestData = new JsonObject();
                    requestData["token"] = JsonValue.CreateStringValue(postToken);
                    var request = new JsonObject();
                    request["request_data"] = requestData;
                    response = await _apiClient.PostJsonAsync(SetPath, request.Stringify(), cancellationToken, true);
                }
                else
                {
                    response = await _apiClient.DeleteAsync(DeletePath + Uri.EscapeDataString(postToken), cancellationToken, true);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (!response.IsSuccess) return ServiceResult<bool>.Failure(Error(response, "تغییر وضعیت نشان ممکن نشد."));

                // Do not treat transport success as bookmark success. The WebApp action
                // response is server-driven, so verify the resulting account state.
                _hasLoaded = false;
                var refreshed = await GetBookmarksAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!refreshed.IsSuccess) return ServiceResult<bool>.Failure(refreshed.ErrorMessage);
                var confirmed = _knownTokens.Contains(postToken);
                return confirmed == isBookmarked
                    ? ServiceResult<bool>.Success(confirmed)
                    : ServiceResult<bool>.Failure("دیوار تغییر نشان را تأیید نکرد. دوباره تلاش کنید.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<bool>.Failure("خطایی هنگام تغییر نشان رخ داد."); }
        }

        private static IEnumerable<string> ReadBookmarkTokens(JsonObject root)
        {
            JsonArray values;
            if (!Array(root, "bookmarks", out values)) yield break;
            foreach (var value in values)
            {
                if (value.ValueType == JsonValueType.String) yield return value.GetString();
                else if (value.ValueType == JsonValueType.Object)
                {
                    var token = String(value.GetObject(), "token");
                    if (string.IsNullOrWhiteSpace(token)) token = String(value.GetObject(), "post_token");
                    if (!string.IsNullOrWhiteSpace(token)) yield return token;
                }
            }
        }

        private static IDictionary<string, string> ReadNotes(JsonObject root)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            JsonArray values;
            if (!Array(root, "notes", out values)) return result;
            foreach (var value in values)
            {
                if (value.ValueType != JsonValueType.Object) continue;
                var item = value.GetObject();
                var token = String(item, "token");
                if (string.IsNullOrWhiteSpace(token)) token = String(item, "post_token");
                var note = String(item, "note");
                if (!string.IsNullOrWhiteSpace(token)) result[token] = note;
            }
            return result;
        }

        private static string Error(DivarApiResponse response, string fallback)
        {
            return response.StatusCode == HttpStatusCode.Unauthorized || response.ErrorMessage == "Authentication is required."
                ? "نشست شما منقضی شده است. دوباره وارد حساب شوید."
                : fallback;
        }

        private static string String(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.String ? item.GetString() : string.Empty;
        }

        private static bool Array(JsonObject value, string key, out JsonArray result)
        {
            result = null;
            IJsonValue item;
            if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Array) return false;
            result = item.GetArray();
            return true;
        }
    }
}
