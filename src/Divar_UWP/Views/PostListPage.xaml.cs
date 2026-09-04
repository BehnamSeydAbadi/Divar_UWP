using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Views
{
    public sealed partial class PostListPage : Page
    {
        public PostListPage()
        {
            InitializeComponent();
            DataContext = new PostListViewModel(AppServices.Current.Navigation);
        }
    }
}
