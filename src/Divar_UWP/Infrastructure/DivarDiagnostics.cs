using System;
using System.Diagnostics;

namespace Divar_UWP.Infrastructure
{
    public static class DivarDiagnostics
    {
        public static void Api(string method, string path, long milliseconds, int? statusCode, bool cacheHit)
        {
            Write("API", method + " " + SafePath(path) + " " + milliseconds + "ms status=" + (statusCode.HasValue ? statusCode.Value.ToString() : "network") + " cache=" + cacheHit);
        }

        public static void FeedPage(bool nextPage, int received, int added, bool hasNextPage)
        {
            Write("FEED", "next=" + nextPage + " received=" + received + " added=" + added + " hasNext=" + hasNextPage);
        }

        public static void ImageFailure(string url, string error)
        {
            Write("IMAGE", SafePath(url) + " " + (error ?? string.Empty));
        }

        public static void AuthenticationExpired()
        {
            Write("AUTH", "session-expired");
        }

        public static void ChatReconnect(int attempt, bool succeeded)
        {
            Write("CHAT", "reconnect-attempt=" + attempt + " success=" + succeeded);
        }

        public static void CacheTrim(string reason)
        {
            Write("CACHE", "trim reason=" + reason);
        }

        private static void Write(string area, string message)
        {
            Debug.WriteLine("[Divar_UWP][" + area + "] " + message);
        }

        private static string SafePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var query = value.IndexOf('?');
            return query < 0 ? value : value.Substring(0, query);
        }
    }
}
