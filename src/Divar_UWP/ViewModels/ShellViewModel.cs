using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class ShellViewModel
    {
        private readonly INavigationService _navigation;

        public ShellViewModel(INavigationService navigation)
        {
            _navigation = navigation;
            ShowCitiesCommand = new RelayCommand(() => _navigation.Navigate(typeof(CitySelectionPage)));
            ShowHomeCommand = new RelayCommand(() => _navigation.Navigate(typeof(HomePage)));
            ShowSearchCommand = new RelayCommand(() => _navigation.Navigate(typeof(SearchPage)));
            ShowPostListCommand = new RelayCommand(() => _navigation.Navigate(typeof(PostListPage)));
        }

        public ICommand ShowCitiesCommand { get; private set; }

        public ICommand ShowHomeCommand { get; private set; }

        public ICommand ShowSearchCommand { get; private set; }

        public ICommand ShowPostListCommand { get; private set; }
    }
}
