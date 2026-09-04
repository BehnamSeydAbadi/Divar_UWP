using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Views
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            DataContext = new HomeViewModel(AppServices.Current.Navigation);
        }
    }
}
