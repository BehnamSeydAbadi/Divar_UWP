using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Views
{
    public sealed partial class SearchPage : Page
    {
        public SearchPage()
        {
            InitializeComponent();
            DataContext = new SearchViewModel(AppServices.Current.Navigation);
        }
    }
}
