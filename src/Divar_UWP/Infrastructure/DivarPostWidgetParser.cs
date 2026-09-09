using System;
using System.Collections.Generic;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Infrastructure
{
    public static class DivarPostWidgetParser
    {
        public static IList<DivarPostSummary> Parse(JsonObject root)
        {
            var posts = new List<DivarPostSummary>();
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            Visit(root, posts, tokens);
            return posts;
        }

        private static void Visit(IJsonValue value, IList<DivarPostSummary> posts, ISet<string> tokens)
        {
            if (value == null) return;
            if (value.ValueType == JsonValueType.Array)
            {
                foreach (var item in value.GetArray()) Visit(item, posts, tokens);
                return;
            }
            if (value.ValueType != JsonValueType.Object) return;

            var valueObject = value.GetObject();
            var widgetType = String(valueObject, "widget_type");
            JsonObject data;
            if (widgetType.IndexOf("POST", StringComparison.OrdinalIgnoreCase) >= 0 &&
                Object(valueObject, "data", out data))
            {
                var post = ParsePost(data);
                if (post != null && tokens.Add(post.Token)) posts.Add(post);
            }

            foreach (var pair in valueObject) Visit(pair.Value, posts, tokens);
        }

        private static DivarPostSummary ParsePost(JsonObject data)
        {
            var token = First(data, "token", "post_token");
            JsonObject action, payload;
            if (string.IsNullOrWhiteSpace(token) && Object(data, "action", out action) && Object(action, "payload", out payload))
                token = First(payload, "token", "post_token");
            if (string.IsNullOrWhiteSpace(token)) return null;

            var thumbnail = First(data, "image_url", "thumbnail_url");
            JsonObject image;
            if (string.IsNullOrWhiteSpace(thumbnail) && Object(data, "image", out image))
                thumbnail = First(image, "thumbnail_url", "url");

            return new DivarPostSummary
            {
                Token = token,
                Title = First(data, "title", "name"),
                TopDescription = First(data, "top_description_text", "subtitle"),
                MiddleDescription = First(data, "middle_description_text", "price"),
                BottomDescription = First(data, "bottom_description_text", "location"),
                ThumbnailUrl = DivarImageUrl.ToMobileCompatible(thumbnail),
                BadgeText = First(data, "status_text", "red_text", "status"),
                ImageCount = Number(data, "image_count"),
                HasChat = Boolean(data, "has_chat"),
                LocationText = Location(data)
            };
        }

        private static string Location(JsonObject data)
        {
            JsonObject action, payload, webInfo;
            if (!Object(data, "action", out action) || !Object(action, "payload", out payload) || !Object(payload, "web_info", out webInfo)) return string.Empty;
            return First(webInfo, "district_persian", "city_persian");
        }

        private static string First(JsonObject value, params string[] keys)
        {
            foreach (var key in keys)
            {
                var result = String(value, key);
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
            return string.Empty;
        }

        private static string String(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.String ? item.GetString() : string.Empty;
        }

        private static bool Boolean(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.Boolean && item.GetBoolean();
        }

        private static int Number(JsonObject value, string key)
        {
            IJsonValue item;
            return value.TryGetValue(key, out item) && item.ValueType == JsonValueType.Number ? (int)item.GetNumber() : 0;
        }

        private static bool Object(JsonObject value, string key, out JsonObject result)
        {
            result = null;
            IJsonValue item;
            if (!value.TryGetValue(key, out item) || item.ValueType != JsonValueType.Object) return false;
            result = item.GetObject();
            return true;
        }
    }
}
