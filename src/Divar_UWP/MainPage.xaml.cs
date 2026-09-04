using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Divar_UWP.Views;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Divar_UWP
{
    public sealed partial class MainPage : Page
    {
        private readonly ShellViewModel _shellViewModel;

        public MainPage()
        {
            InitializeComponent();

            AppServices.Current.InitializeNavigation(ContentFrame);
            _shellViewModel = new ShellViewModel(AppServices.Current.Navigation, AppServices.Current.SelectionStore, AppServices.Current.AuthService);
            DataContext = _shellViewModel;

            ContentFrame.Navigated += OnContentFrameNavigated;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            AppServices.Current.Navigation.Navigate(
                AppServices.Current.SelectionStore.GetSelectedCity() == null
                    ? typeof(CitySelectionPage)
                    : typeof(PostListPage));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            UpdateBackButton();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
        }

        private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            UpdateBackButton();
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (NavigationSplitView.IsPaneOpen)
            {
                e.Handled = true;
                NavigationSplitView.IsPaneOpen = false;
                return;
            }

            if (!AppServices.Current.Navigation.CanGoBack)
            {
                return;
            }

            e.Handled = true;
            AppServices.Current.Navigation.GoBack();
        }

        private void OnMenuButtonClick(object sender, RoutedEventArgs e)
        {
            _shellViewModel.RefreshSelection();
            _shellViewModel.RefreshAuthState();
            NavigationSplitView.IsPaneOpen = !NavigationSplitView.IsPaneOpen;
        }

        private void OnNavigationItemClick(object sender, RoutedEventArgs e)
        {
            NavigationSplitView.IsPaneOpen = false;
        }

        private static void UpdateBackButton()
        {
            var navigation = AppServices.Current.Navigation;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                navigation != null && navigation.CanGoBack
                    ? AppViewBackButtonVisibility.Visible
                    : AppViewBackButtonVisibility.Collapsed;
        }
    }
}
