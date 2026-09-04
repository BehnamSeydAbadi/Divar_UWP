using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarSearchService : IDivarSearchService
    {
        private const string SearchPath = "v8/postlist/w/search";
        private readonly IDivarApiClient _apiClient;

        public DivarSearchService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<DivarPage<DivarPostSummary>>> SearchAsync(DivarSearchRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.CityIds.Count == 0)
                return ServiceResult<DivarPage<DivarPostSummary>>.Failure("ابتدا یک شهر انتخاب کنید.");

            try
            {
                var response = await _apiClient.PostJsonAsync(SearchPath, BuildRequest(request).Stringify(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!response.IsSuccess)
                    return ServiceResult<DivarPage<DivarPostSummary>>.Failure("دریافت آگهی‌ها ممکن نشد. اتصال اینترنت را بررسی کنید.");

                JsonObject root;
                if (!JsonObject.TryParse(response.Content, out root))
                    return ServiceResult<DivarPage<DivarPostSummary>>.Failure("پاسخ دریافتی از دیوار قابل خواندن نیست.");

                return ServiceResult<DivarPage<DivarPostSummary>>.Success(ParsePage(root));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                return ServiceResult<DivarPage<DivarPostSummary>>.Failure("خطایی هنگام خواندن آگهی‌ها رخ داد.");
            }
        }

        public async Task<ServiceResult<IList<DivarSearchSuggestion>>> GetSuggestionsAsync(string query, IList<string> cityIds, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query) || cityIds == null || cityIds.Count == 0)
                return ServiceResult<IList<DivarSearchSuggestion>>.Success(new List<DivarSearchSuggestion>());

            var request = new DivarSearchRequest { Query = query.Trim(), CategorySlug = "ROOT" };
            foreach (var id in cityIds) request.CityIds.Add(id);
            var response = await _apiClient.PostJsonAsync(SearchPath, BuildRequest(request).Stringify(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!response.IsSuccess)
                return ServiceResult<IList<DivarSearchSuggestion>>.Failure("دریافت پیشنهادها ممکن نشد.");

            JsonObject root;
            if (!JsonObject.TryParse(response.Content, out root))
                return ServiceResult<IList<DivarSearchSuggestion>>.Failure("پاسخ پیشنهادها قابل خواندن نیست.");

            // The current endpoint only occasionally returns server-authored suggestions.
            // Never fabricate suggestions from post titles.
            var suggestions = new List<DivarSearchSuggestion>();
            JsonArray widgets;
            if (TryGetArray(root, "list_top_widgets", out widgets))
            {
                foreach (var value in widgets)
                {
                    if (value.ValueType != JsonValueType.Object) continue;
                    var widget = value.GetObject();
                    var type = GetString(widget, "widget_type");
                    if (type.IndexOf("SUGGEST", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    JsonObject data;
                    if (!TryGetObject(widget, "data", out data)) continue;
                    var title = GetString(data, "title");
                    if (string.IsNullOrWhiteSpace(title)) continue;
                    suggestions.Add(new DivarSearchSuggestion
                    {
                        Title = title,
                        Subtitle = GetString(data, "subtitle"),
                        DisplayText = title,
                        SearchDataJson = data.Stringify()
                    });
                }
            }
            return ServiceResult<IList<DivarSearchSuggestion>>.Success(suggestions);
        }

        private static JsonObject BuildRequest(DivarSearchRequest request)
        {
            var root = new JsonObject();
            var cityIds = new JsonArray();
            foreach (var id in request.CityIds) cityIds.Add(JsonValue.CreateStringValue(id));
            root["city_ids"] = cityIds;

            JsonObject searchData;
            if (!string.IsNullOrWhiteSpace(request.SearchDataJson) && JsonObject.TryParse(request.SearchDataJson, out searchData))
            {
                root["search_data"] = searchData;
            }
            else
            {
                var data = new JsonObject();
                data["category"] = StringField(string.IsNullOrWhiteSpace(request.CategorySlug) ? "ROOT" : request.CategorySlug);
                if (!string.IsNullOrWhiteSpace(request.Query)) data["query"] = StringField(request.Query.Trim());
                var formData = new JsonObject();
                formData["data"] = data;
                searchData = new JsonObject();
                searchData["form_data"] = formData;
                root["search_data"] = searchData;
            }

            JsonObject pagination;
            if (!string.IsNullOrWhiteSpace(request.PaginationDataJson) && JsonObject.TryParse(request.PaginationDataJson, out pagination))
                root["pagination_data"] = pagination;
            return root;
        }

        private static JsonObject StringField(string value)
        {
            var inner = new JsonObject();
            inner["value"] = JsonValue.CreateStringValue(value);
            var outer = new JsonObject();
            outer["str"] = inner;
            return outer;
        }

        private static DivarPage<DivarPostSummary> ParsePage(JsonObject root)
        {
            var page = new DivarPage<DivarPostSummary>();
            JsonArray widgets;
            if (TryGetArray(root, "list_widgets", out widgets))
            {
                foreach (var value in widgets)
                {
                    if (value.ValueType != JsonValueType.Object) continue;
                    var widget = value.GetObject();
                    if (!string.Equals(GetString(widget, "widget_type"), "POST_ROW", StringComparison.OrdinalIgnoreCase)) continue;
                    JsonObject data;
                    if (!TryGetObject(widget, "data", out data)) continue;
                    var token = GetString(data, "token");
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    page.Items.Add(new DivarPostSummary
                    {
                        Token = token,
                        Title = GetString(data, "title"),
                        TopDescription = GetString(data, "top_description_text"),
                        MiddleDescription = GetString(data, "middle_description_text"),
                        BottomDescription = GetString(data, "bottom_description_text"),
                        ThumbnailUrl = GetString(data, "image_url"),
                        BadgeText = GetString(data, "red_text"),
                        ImageCount = GetInt(data, "image_count"),
                        HasChat = GetBool(data, "has_chat"),
                        LocationText = GetWebInfoLocation(data)
                    });
                }
            }

            JsonObject pagination;
            if (TryGetObject(root, "pagination", out pagination))
            {
                page.HasNextPage = GetBool(pagination, "has_next_page");
                JsonObject data;
                if (TryGetObject(pagination, "data", out data)) page.PaginationDataJson = data.Stringify();
            }
            JsonObject searchData;
            if (TryGetObject(root, "search_data", out searchData)) page.SearchDataJson = searchData.Stringify();
            return page;
        }

        private static string GetWebInfoLocation(JsonObject post)
        {
            JsonObject action, payload, webInfo;
            if (!TryGetObject(post, "action", out action) || !TryGetObject(action, "payload", out payload) || !TryGetObject(payload, "web_info", out webInfo)) return string.Empty;
            var district = GetString(webInfo, "district_persian");
            return string.IsNullOrWhiteSpace(district) ? GetString(webInfo, "city_persian") : district;
        }

        private static string GetString(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.String ? item.GetString() : string.Empty;
        }

        private static bool GetBool(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.Boolean && item.GetBoolean();
        }

        private static int GetInt(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.Number ? (int)item.GetNumber() : 0;
        }

        private static bool TryGetObject(JsonObject value, string key, out JsonObject result)
        {
            result = null; IJsonValue item;
            if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Object) return false;
            result = item.GetObject(); return true;
        }

        private static bool TryGetArray(JsonObject value, string key, out JsonArray result)
        {
            result = null; IJsonValue item;
            if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Array) return false;
            result = item.GetArray(); return true;
        }
    }
}
