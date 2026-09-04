using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class SearchViewModel : PageViewModelBase
    {
        public SearchViewModel(INavigationService navigation)
            : base("جستجو")
        {
            ShowResultsCommand = new RelayCommand(() => navigation.Navigate(typeof(PostListPage)));
        }

        public ICommand ShowResultsCommand { get; private set; }
    }
}
