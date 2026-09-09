using System;
using System.Threading;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Divar_UWP.Views
{
    public sealed partial class MyDivarPage : Page
    {
        private readonly MyDivarViewModel _viewModel;
        private CancellationTokenSource _cancellation;

        public MyDivarPage()
        {
            InitializeComponent();
            _viewModel = new MyDivarViewModel(AppServices.Current.MyDivarService, AppServices.Current.AuthService, AppServices.Current.Navigation);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); StartLoading(); }
        protected override void OnNavigatedFrom(NavigationEventArgs e) { Cancel(); base.OnNavigatedFrom(e); }
        private async void StartLoading() { Cancel(); _cancellation = new CancellationTokenSource(); try { await _viewModel.LoadAsync(_cancellation.Token); } catch (OperationCanceledException) { } }
        private void Cancel() { if (_cancellation == null) return; _cancellation.Cancel(); _cancellation.Dispose(); _cancellation = null; }
        private void OnRetryClick(object sender, RoutedEventArgs e) { StartLoading(); }
        private void OnPostItemClick(object sender, ItemClickEventArgs e) { _viewModel.OpenPost(e.ClickedItem as DivarPostSummary); }
    }
}
