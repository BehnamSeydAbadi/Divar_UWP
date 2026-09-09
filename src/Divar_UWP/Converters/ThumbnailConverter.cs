using System;
using Divar_UWP.Infrastructure;
using Windows.UI.Xaml.Data;

namespace Divar_UWP.Converters
{
    public sealed class ThumbnailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return DivarImageCache.GetThumbnail(value as string, 192);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
