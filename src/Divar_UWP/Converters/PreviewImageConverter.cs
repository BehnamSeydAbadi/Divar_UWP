using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace Divar_UWP.Converters
{
    public sealed class PreviewImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Uri uri;
            var url = value as string;
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out uri)) return null;
            return new BitmapImage(uri) { DecodePixelWidth = 640 };
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) { throw new NotSupportedException(); }
    }
}
