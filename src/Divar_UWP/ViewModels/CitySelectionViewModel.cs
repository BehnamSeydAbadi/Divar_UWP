using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class CitySelectionViewModel : PageViewModelBase
    {
        public CitySelectionViewModel(INavigationService navigation)
            : base("انتخاب شهر")
        {
            ContinueCommand = new RelayCommand(() => navigation.Navigate(typeof(HomePage)));
        }

        public ICommand ContinueCommand { get; private set; }
    }
}
