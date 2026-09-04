using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class HomeViewModel : PageViewModelBase
    {
        public HomeViewModel(INavigationService navigation)
            : base("دیوار")
        {
            SearchCommand = new RelayCommand(() => navigation.Navigate(typeof(SearchPage)));
            BrowseCommand = new RelayCommand(() => navigation.Navigate(typeof(PostListPage)));
        }

        public ICommand SearchCommand { get; private set; }

        public ICommand BrowseCommand { get; private set; }
    }
}
