using System;
using Windows.UI.Xaml.Media.Imaging;

namespace Divar_UWP.Infrastructure
{
    public static class DivarImageCache
    {
        private static readonly BoundedMemoryCache<BitmapImage> Thumbnails = new BoundedMemoryCache<BitmapImage>(48);

        public static BitmapImage GetThumbnail(string url, int decodePixelWidth)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            var key = decodePixelWidth + "|" + url;
            BitmapImage existing;
            if (Thumbnails.TryGet(key, out existing)) return existing;

            var image = new BitmapImage { DecodePixelWidth = decodePixelWidth };
            image.ImageFailed += (sender, args) => DivarDiagnostics.ImageFailure(url, args.ErrorMessage);
            image.UriSource = uri;
            Thumbnails.Set(key, image, TimeSpan.FromMinutes(10));
            return image;
        }

        public static BitmapImage CreatePreview(string url, int decodePixelWidth)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            var image = new BitmapImage { DecodePixelWidth = decodePixelWidth };
            image.ImageFailed += (sender, args) => DivarDiagnostics.ImageFailure(url, args.ErrorMessage);
            image.UriSource = uri;
            return image;
        }

        public static void Clear()
        {
            Thumbnails.Clear();
        }
    }
}
