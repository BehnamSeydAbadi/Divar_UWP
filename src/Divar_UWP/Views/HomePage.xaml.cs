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
    public sealed partial class HomePage : Page
    {
        private CancellationTokenSource _loadCancellation;
        private readonly HomeViewModel _viewModel;

        public HomePage()
        {
            InitializeComponent();
            _viewModel = new HomeViewModel(AppServices.Current.CategoryService, AppServices.Current.SelectionStore, AppServices.Current.Navigation);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); StartLoading(); }
        protected override void OnNavigatedFrom(NavigationEventArgs e) { CancelLoading(); base.OnNavigatedFrom(e); }

        private async void StartLoading()
        {
            CancelLoading();
            _loadCancellation = new CancellationTokenSource();
            try { await _viewModel.LoadAsync(_loadCancellation.Token); }
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
        private void OnCategoryItemClick(object sender, ItemClickEventArgs e) { _viewModel.OpenCategory(e.ClickedItem as DivarCategory); }
    }
}
