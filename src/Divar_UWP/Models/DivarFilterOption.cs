using Divar_UWP.Infrastructure;

namespace Divar_UWP.Models
{
    public sealed class DivarFilterOption : ObservableObject
    {
        private bool _isSelected;

        public string Value { get; set; }
        public string Display { get; set; }
        public bool IsSelected { get { return _isSelected; } set { SetProperty(ref _isSelected, value); } }
    }
}
