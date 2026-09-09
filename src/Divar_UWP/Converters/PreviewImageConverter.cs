using System;
using Divar_UWP.Infrastructure;
using Windows.UI.Xaml.Data;

namespace Divar_UWP.Converters
{
    public sealed class PreviewImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return DivarImageCache.CreatePreview(value as string, 720);
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) { throw new NotSupportedException(); }
    }
}
