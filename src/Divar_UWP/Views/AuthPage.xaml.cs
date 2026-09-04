using System;
using System.Threading;
using Divar_UWP.Infrastructure;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Divar_UWP.Views
{
    public sealed partial class AuthPage : Page
    {
        private readonly AuthViewModel _viewModel;
        private CancellationTokenSource _cancellation;
        public AuthPage() { InitializeComponent(); _viewModel = new AuthViewModel(AppServices.Current.AuthService); DataContext = _viewModel; }
        protected override void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); _viewModel.RefreshState(); }
        protected override void OnNavigatedFrom(NavigationEventArgs e) { Cancel(); base.OnNavigatedFrom(e); }
        private async void OnSendCodeClick(object sender, RoutedEventArgs e) { await RunAsync(() => _viewModel.SendCodeAsync(_cancellation.Token)); }
        private async void OnConfirmCodeClick(object sender, RoutedEventArgs e) { await RunAsync(() => _viewModel.ConfirmCodeAsync(_cancellation.Token)); if (_viewModel.IsAuthenticated) AppServices.Current.Navigation.Navigate(typeof(PostListPage)); }
        private async void OnLogoutClick(object sender, RoutedEventArgs e) { await RunAsync(() => _viewModel.LogoutAsync(_cancellation.Token)); }
        private async System.Threading.Tasks.Task RunAsync(Func<System.Threading.Tasks.Task> action) { Cancel(); _cancellation = new CancellationTokenSource(); try { await action(); } catch (OperationCanceledException) { } }
        private void Cancel() { if (_cancellation == null) return; _cancellation.Cancel(); _cancellation.Dispose(); _cancellation = null; }
    }
}
