using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class PostListViewModel : PageViewModelBase
    {
        public PostListViewModel(INavigationService navigation)
            : base("آگهی‌ها")
        {
            ShowPostDetailsCommand = new RelayCommand(
                () => navigation.Navigate(typeof(PostDetailsPage), "skeleton"));
        }

        public ICommand ShowPostDetailsCommand { get; private set; }
    }
}
