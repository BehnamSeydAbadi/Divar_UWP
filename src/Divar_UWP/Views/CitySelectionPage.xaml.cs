using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Views
{
    public sealed partial class CitySelectionPage : Page
    {
        public CitySelectionPage()
        {
            InitializeComponent();
            DataContext = new CitySelectionViewModel(AppServices.Current.Navigation);
        }
    }
}
