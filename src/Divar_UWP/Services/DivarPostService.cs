using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarPostService : IDivarPostService
    {
        private readonly IDivarApiClient _apiClient;

        public DivarPostService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<DivarPostDetails>> GetPostAsync(string postToken, string trackerSessionId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(postToken)) return ServiceResult<DivarPostDetails>.Failure("شناسهٔ آگهی معتبر نیست.");
            try
            {
                var response = await _apiClient.GetAsync("v8/posts-v2/web/" + Uri.EscapeDataString(postToken), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!response.IsSuccess) return ServiceResult<DivarPostDetails>.Failure("دریافت جزئیات آگهی ممکن نشد.");
                JsonObject root;
                if (!JsonObject.TryParse(response.Content, out root)) return ServiceResult<DivarPostDetails>.Failure("پاسخ جزئیات آگهی قابل خواندن نیست.");
                return ServiceResult<DivarPostDetails>.Success(Parse(root, postToken));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return ServiceResult<DivarPostDetails>.Failure("خطایی هنگام خواندن جزئیات آگهی رخ داد."); }
        }

        private static DivarPostDetails Parse(JsonObject root, string token)
        {
            var post = new DivarPostDetails { Token = token };
            JsonObject share;
            if (TryObject(root, "share", out share)) post.ShareUrl = String(share, "web_url");
            JsonObject contact;
            if (TryObject(root, "contact", out contact))
            {
                post.ChatEnabled = Bool(contact, "chat_enabled");
                post.ContactUuid = String(contact, "contact_uuid");
            }

            JsonArray sections;
            if (!TryArray(root, "sections", out sections)) return post;
            var breadcrumb = new List<string>();
            foreach (var sectionValue in sections)
            {
                if (sectionValue.ValueType != JsonValueType.Object) continue;
                var section = sectionValue.GetObject();
                var sectionName = String(section, "section_name");
                JsonArray widgets;
                if (!TryArray(section, "widgets", out widgets)) continue;
                foreach (var widgetValue in widgets)
                {
                    if (widgetValue.ValueType != JsonValueType.Object) continue;
                    var widget = widgetValue.GetObject();
                    var type = String(widget, "widget_type");
                    JsonObject data;
                    if (!TryObject(widget, "data", out data)) continue;
                    if (type == "BREADCRUMB")
                    {
                        JsonArray parents;
                        if (TryArray(data, "parent_items", out parents))
                            foreach (var parent in parents.Where(x => x.ValueType == JsonValueType.Object))
                            {
                                var title = String(parent.GetObject(), "title");
                                if (!string.IsNullOrWhiteSpace(title)) breadcrumb.Add(title);
                            }
                    }
                    else if (type == "LEGEND_TITLE_ROW" && string.IsNullOrWhiteSpace(post.Title)) post.Title = String(data, "title");
                    else if (type == "EXPANDABLE_SECTION" && sectionName == "TITLE" && string.IsNullOrWhiteSpace(post.LocationText)) post.LocationText = String(data, "title");
                    else if (type == "DESCRIPTION_ROW" && sectionName == "DESCRIPTION" && string.IsNullOrWhiteSpace(post.Description)) post.Description = String(data, "text");
                    else if (type == "IMAGE_CAROUSEL") ParseImages(data, post);
                    else if (sectionName == "LIST_DATA") ParseAttribute(type, data, post);
                    else if (type.IndexOf("BUSINESS", StringComparison.OrdinalIgnoreCase) >= 0 && string.IsNullOrWhiteSpace(post.SellerInfo)) post.SellerInfo = FirstText(data, "title", "subtitle", "name");
                }
            }
            post.BreadcrumbText = string.Join(" › ", breadcrumb);
            JsonObject seo, info;
            if (TryObject(root, "seo", out seo) && TryObject(seo, "web_info", out info))
            {
                if (string.IsNullOrWhiteSpace(post.Title)) post.Title = String(info, "title");
                if (string.IsNullOrWhiteSpace(post.LocationText))
                {
                    var city = String(info, "city_persian");
                    var district = String(info, "district_persian");
                    post.LocationText = string.IsNullOrWhiteSpace(district) ? city : city + "، " + district;
                }
            }
            return post;
        }

        private static void ParseImages(JsonObject data, DivarPostDetails post)
        {
            if (post.Images.Count > 0) return;
            JsonArray items;
            if (!TryArray(data, "items", out items)) return;
            foreach (var itemValue in items)
            {
                if (itemValue.ValueType != JsonValueType.Object) continue;
                JsonObject image;
                if (!TryObject(itemValue.GetObject(), "image", out image)) continue;
                var full = DivarImageUrl.ToMobileCompatible(String(image, "url"));
                var thumbnail = DivarImageUrl.ToMobileCompatible(String(image, "thumbnail_url"));
                if (string.IsNullOrWhiteSpace(full) && string.IsNullOrWhiteSpace(thumbnail)) continue;
                post.Images.Add(new DivarPostImage { FullUrl = full, ThumbnailUrl = thumbnail, AltText = String(image, "alt"), DisplayUrl = string.IsNullOrWhiteSpace(thumbnail) ? full : thumbnail });
                post.ImageUrls.Add(full);
            }
        }

        private static void ParseAttribute(string type, JsonObject data, DivarPostDetails post)
        {
            if (type == "GROUP_INFO_ROW")
            {
                JsonArray items;
                if (!TryArray(data, "items", out items)) return;
                foreach (var item in items.Where(x => x.ValueType == JsonValueType.Object)) AddAttribute(post, String(item.GetObject(), "title"), String(item.GetObject(), "value"));
                return;
            }
            if (type == "UNEXPANDABLE_ROW")
            {
                var title = String(data, "title"); var value = String(data, "value");
                AddAttribute(post, title, value);
                if (string.IsNullOrWhiteSpace(post.PriceText) && IsPrice(title)) post.PriceText = value;
                return;
            }
            if (type == "SCORE_ROW") AddAttribute(post, String(data, "title"), String(data, "descriptive_score"));
        }

        private static bool IsPrice(string title)
        {
            return !string.IsNullOrWhiteSpace(title) && (title.IndexOf("قیمت", StringComparison.Ordinal) >= 0 || title.IndexOf("ودیعه", StringComparison.Ordinal) >= 0 || title.IndexOf("اجاره", StringComparison.Ordinal) >= 0 || title.IndexOf("رهن", StringComparison.Ordinal) >= 0);
        }

        private static void AddAttribute(DivarPostDetails post, string title, string value)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(value)) return;
            post.Attributes.Add(new DivarPostAttribute { Title = title, Value = value });
        }

        private static string FirstText(JsonObject value, params string[] keys) { foreach (var key in keys) { var text = String(value, key); if (!string.IsNullOrWhiteSpace(text)) return text; } return string.Empty; }
        private static string String(JsonObject value, string key) { IJsonValue item; return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.String ? item.GetString() : string.Empty; }
        private static bool Bool(JsonObject value, string key) { IJsonValue item; return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.Boolean && item.GetBoolean(); }
        private static bool TryObject(JsonObject value, string key, out JsonObject result) { result = null; IJsonValue item; if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Object) return false; result = item.GetObject(); return true; }
        private static bool TryArray(JsonObject value, string key, out JsonArray result) { result = null; IJsonValue item; if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Array) return false; result = item.GetArray(); return true; }
    }
}
