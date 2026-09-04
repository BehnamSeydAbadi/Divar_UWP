using System;

namespace Divar_UWP.Infrastructure
{
    /// <summary>
    /// Provides image URLs that the Windows 10 Mobile image decoder can display.
    /// Divar's Web API prefers WebP, while the 15063 decoder reliably supports
    /// the equivalent JPEG CDN variants.
    /// </summary>
    public static class DivarImageUrl
    {
        public static string ToMobileCompatible(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.IndexOf("divarcdn.com", StringComparison.OrdinalIgnoreCase) < 0)
                return url ?? string.Empty;

            if (url.IndexOf("/webp_thumbnail/", StringComparison.OrdinalIgnoreCase) >= 0)
                return ToJpeg(url.Replace("/webp_thumbnail/", "/thumbnail/"));

            if (url.IndexOf("/webp_post/", StringComparison.OrdinalIgnoreCase) >= 0)
                return ToJpeg(url.Replace("/webp_post/", "/post/"));

            return url;
        }

        private static string ToJpeg(string url)
        {
            return url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                ? url.Substring(0, url.Length - 5) + ".jpg"
                : url;
        }
    }
}
