using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Views
{
    public sealed partial class PostDetailsPage : Page
    {
        public PostDetailsPage()
        {
            InitializeComponent();
            DataContext = new PostDetailsViewModel(AppServices.Current.Navigation);
        }
    }
}
