using System.Collections.ObjectModel;
using Divar_UWP.Infrastructure;

namespace Divar_UWP.Models
{
    public sealed class DivarFilterDefinition : ObservableObject
    {
        private string _minimum;
        private string _maximum;
        private bool _booleanValue;
        private DivarFilterOption _selectedOption;

        public DivarFilterDefinition() { Options = new ObservableCollection<DivarFilterOption>(); }
        public string Key { get; set; }
        public string Title { get; set; }
        public string Unit { get; set; }
        public DivarFilterKind Kind { get; set; }
        public ObservableCollection<DivarFilterOption> Options { get; private set; }
        public string Minimum { get { return _minimum; } set { SetProperty(ref _minimum, value); } }
        public string Maximum { get { return _maximum; } set { SetProperty(ref _maximum, value); } }
        public bool BooleanValue { get { return _booleanValue; } set { SetProperty(ref _booleanValue, value); } }
        public DivarFilterOption SelectedOption { get { return _selectedOption; } set { SetProperty(ref _selectedOption, value); } }
        public bool IsNumberRange { get { return Kind == DivarFilterKind.NumberRange; } }
        public bool IsBoolean { get { return Kind == DivarFilterKind.Boolean; } }
        public bool IsSingleSelect { get { return Kind == DivarFilterKind.SingleSelect; } }
        public bool IsMultiSelect { get { return Kind == DivarFilterKind.MultiSelect; } }
    }
}
