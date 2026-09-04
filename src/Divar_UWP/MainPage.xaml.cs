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
        public MainPage()
        {
            InitializeComponent();

            AppServices.Current.InitializeNavigation(ContentFrame);
            DataContext = new ShellViewModel(AppServices.Current.Navigation);

            ContentFrame.Navigated += OnContentFrameNavigated;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            AppServices.Current.Navigation.Navigate(typeof(CitySelectionPage));
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
            if (!AppServices.Current.Navigation.CanGoBack)
            {
                return;
            }

            e.Handled = true;
            AppServices.Current.Navigation.GoBack();
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
