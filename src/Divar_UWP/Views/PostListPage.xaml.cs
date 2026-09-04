using System;
using System.Threading;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Divar_UWP.Views
{
    public sealed partial class PostListPage : Page
    {
        private readonly PostListViewModel _viewModel;
        private CancellationTokenSource _loadCancellation;
        private DivarFeedNavigationParameter _parameter;

        public PostListPage()
        {
            InitializeComponent();
            _viewModel = new PostListViewModel(AppServices.Current.SearchService, AppServices.Current.SelectionStore);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _parameter = e.Parameter as DivarFeedNavigationParameter;
            StartLoading();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CancelLoading();
            base.OnNavigatedFrom(e);
        }

        private async void StartLoading()
        {
            CancelLoading();
            _loadCancellation = new CancellationTokenSource();
            try { await _viewModel.InitializeAsync(_parameter, _loadCancellation.Token); }
            catch (OperationCanceledException) { }
        }

        private void CancelLoading()
        {
            if (_loadCancellation == null) return;
            _loadCancellation.Cancel();
            _loadCancellation.Dispose();
            _loadCancellation = null;
        }

        private void OnRetryClick(object sender, RoutedEventArgs e) { StartLoading(); }
    }
}
