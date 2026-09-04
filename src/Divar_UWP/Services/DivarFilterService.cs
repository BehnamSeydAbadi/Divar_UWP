using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarFilterService : IDivarFilterService
    {
        private const string FiltersPath = "v8/postlist/w/filters";
        private readonly IDivarApiClient _apiClient;
        private static readonly HttpClient WebClient = CreateWebClient();

        public DivarFilterService(IDivarApiClient apiClient) { _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient)); }

        public async Task<ServiceResult<IList<DivarFilterDefinition>>> GetFiltersAsync(DivarCity city, string categorySlug, string searchDataJson, CancellationToken cancellationToken)
        {
            if (city == null) return ServiceResult<IList<DivarFilterDefinition>>.Failure("ابتدا یک شهر انتخاب کنید.");
            try
            {
                var body = new JsonObject();
                var ids = new JsonArray(); ids.Add(JsonValue.CreateStringValue(city.Id)); body["city_ids"] = ids;
                JsonObject searchData;
                if (!string.IsNullOrWhiteSpace(searchDataJson) && JsonObject.TryParse(searchDataJson, out searchData)) body["search_data"] = searchData;
                else body["search_data"] = CreateSearchData(categorySlug);

                var response = await _apiClient.PostJsonAsync(FiltersPath, body.Stringify(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                IList<DivarFilterDefinition> apiFilters = null;
                if (response.IsSuccess)
                {
                    JsonObject root;
                    if (JsonObject.TryParse(response.Content, out root)) apiFilters = ParseApiResponse(root);
                }

                // The public filter service can return the ROOT schema for a category request.
                // In that case read the same DTO schema embedded by Divar in its category page.
                if (!string.IsNullOrWhiteSpace(categorySlug) && !string.Equals(categorySlug, "ROOT", StringComparison.OrdinalIgnoreCase))
                {
                    var pageFilters = await TryReadCategoryPageAsync(city.Slug, categorySlug, cancellationToken);
                    if (pageFilters != null && pageFilters.Count > 0) return ServiceResult<IList<DivarFilterDefinition>>.Success(pageFilters);
                }
                if (apiFilters != null) return ServiceResult<IList<DivarFilterDefinition>>.Success(apiFilters);
                return ServiceResult<IList<DivarFilterDefinition>>.Failure("دریافت فیلترها ممکن نشد.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<IList<DivarFilterDefinition>>.Failure("پاسخ فیلترها قابل خواندن نبود."); }
        }

        private static JsonObject CreateSearchData(string categorySlug)
        {
            var value = new JsonObject(); value["value"] = JsonValue.CreateStringValue(string.IsNullOrWhiteSpace(categorySlug) ? "ROOT" : categorySlug);
            var str = new JsonObject(); str["str"] = value;
            var data = new JsonObject(); data["category"] = str;
            var form = new JsonObject(); form["data"] = data;
            var search = new JsonObject(); search["form_data"] = form;
            return search;
        }

        private static IList<DivarFilterDefinition> ParseApiResponse(JsonObject root)
        {
            JsonObject page; JsonArray widgets;
            if (!TryObject(root, "page", out page) || !TryArray(page, "widget_list", out widgets)) return null;
            return ParseWidgets(widgets, false);
        }

        private static async Task<IList<DivarFilterDefinition>> TryReadCategoryPageAsync(string citySlug, string categorySlug, CancellationToken token)
        {
            var route = ResolveWebRoute(categorySlug);
            var uri = new Uri("https://divar.ir/s/" + Uri.EscapeDataString(citySlug) + "/" + Uri.EscapeDataString(route));
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            using (var response = await WebClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, token))
            {
                if (!response.IsSuccessStatusCode) return null;
                var html = await response.Content.ReadAsStringAsync();
                token.ThrowIfCancellationRequested();
                var json = ExtractObject(html, "\"filtersPage\":");
                JsonObject page;
                if (json == null || !JsonObject.TryParse(json, out page)) return null;
                JsonArray widgets;
                if (!TryArray(page, "widgetList", out widgets)) return null;
                return ParseWidgets(widgets, true);
            }
        }

        private static IList<DivarFilterDefinition> ParseWidgets(JsonArray widgets, bool normalized)
        {
            var result = new List<DivarFilterDefinition>();
            string pendingTitle = null;
            foreach (var value in widgets)
            {
                if (value.ValueType != JsonValueType.Object) continue;
                var widget = value.GetObject();
                if (normalized)
                {
                    JsonObject dto;
                    if (!TryObject(widget, "dto", out dto)) continue;
                    widget = dto;
                }
                var type = String(widget, "widget_type");
                JsonObject data;
                if (!TryObject(widget, "data", out data)) continue;
                if (type == "TITLE_ROW") { pendingTitle = String(data, "text"); continue; }
                JsonObject field;
                if (!TryObject(data, "field", out field)) continue;
                var key = String(field, "key");
                var fieldType = String(field, "type");
                var title = String(data, "title");
                if (string.IsNullOrWhiteSpace(title)) title = pendingTitle;
                if (string.IsNullOrWhiteSpace(title)) title = String(data, "filter_page_title");
                if (string.IsNullOrWhiteSpace(title)) title = String(data, "bottom_sheet_title");
                pendingTitle = null;
                DivarFilterDefinition filter = null;
                if (fieldType == "number_range") filter = NewFilter(key, title, DivarFilterKind.NumberRange, String(data, "unit"));
                else if (fieldType == "boolean") filter = NewFilter(key, title, DivarFilterKind.Boolean, null);
                else if (fieldType == "str") filter = NewFilter(key, title, DivarFilterKind.SingleSelect, null);
                else if (fieldType == "repeated_string" && type.IndexOf("LAZY", StringComparison.OrdinalIgnoreCase) < 0) filter = NewFilter(key, title, DivarFilterKind.MultiSelect, null);
                if (filter == null || string.IsNullOrWhiteSpace(filter.Title)) continue;
                JsonArray options;
                if (TryArray(data, "options", out options))
                {
                    foreach (var optionValue in options)
                    {
                        if (optionValue.ValueType != JsonValueType.Object) continue;
                        var option = optionValue.GetObject();
                        var optionValueText = String(option, "value");
                        if (string.IsNullOrWhiteSpace(optionValueText)) optionValueText = String(option, "key");
                        var display = String(option, "display");
                        if (string.IsNullOrWhiteSpace(display)) display = String(option, "title");
                        if (!string.IsNullOrWhiteSpace(optionValueText)) filter.Options.Add(new DivarFilterOption { Value = optionValueText, Display = display });
                    }
                }
                result.Add(filter);
            }
            return result;
        }

        private static DivarFilterDefinition NewFilter(string key, string title, DivarFilterKind kind, string unit)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                if (key == "has-photo") title = "عکس‌دار";
                else title = key;
            }
            return new DivarFilterDefinition { Key = key, Title = title, Kind = kind, Unit = unit };
        }

        private static string ResolveWebRoute(string slug)
        {
            if (string.Equals(slug, "light", StringComparison.OrdinalIgnoreCase)) return "car";
            if (string.Equals(slug, "apartment-sell", StringComparison.OrdinalIgnoreCase)) return "buy-apartment";
            if (string.Equals(slug, "apartment-rent", StringComparison.OrdinalIgnoreCase)) return "rent-apartment";
            return slug;
        }

        private static string ExtractObject(string source, string marker)
        {
            var markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0) return null;
            var start = source.IndexOf('{', markerIndex + marker.Length);
            if (start < 0) return null;
            var depth = 0; var inString = false; var escaped = false;
            for (var i = start; i < source.Length; i++)
            {
                var c = source[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return source.Substring(start, i - start + 1);
            }
            return null;
        }

        private static HttpClient CreateWebClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; ARM; Mobile) AppleWebKit/537.36 DivarUWP/1.0");
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fa-IR"));
            return client;
        }

        private static string String(JsonObject o, string key) { IJsonValue v; return o.TryGetValue(key, out v) && v.ValueType == JsonValueType.String ? v.GetString() : string.Empty; }
        private static bool TryObject(JsonObject o, string key, out JsonObject result) { result = null; IJsonValue v; if (!o.TryGetValue(key, out v) || v.ValueType != JsonValueType.Object) return false; result = v.GetObject(); return true; }
        private static bool TryArray(JsonObject o, string key, out JsonArray result) { result = null; IJsonValue v; if (!o.TryGetValue(key, out v) || v.ValueType != JsonValueType.Array) return false; result = v.GetArray(); return true; }
    }
}
