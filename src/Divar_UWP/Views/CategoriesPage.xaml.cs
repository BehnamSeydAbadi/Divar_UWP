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
    public sealed partial class CategoriesPage : Page
    {
        private CancellationTokenSource _loadCancellation;
        private readonly CategoriesViewModel _viewModel;
        private string _initialToken;

        public CategoriesPage()
        {
            InitializeComponent();
            _viewModel = new CategoriesViewModel(AppServices.Current.CategoryService, AppServices.Current.SelectionStore);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _initialToken = e.Parameter as string;
            StartLoading();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) { CancelLoading(); base.OnNavigatedFrom(e); }

        private async void StartLoading()
        {
            CancelLoading();
            _loadCancellation = new CancellationTokenSource();
            try { await _viewModel.LoadAsync(_initialToken, _loadCancellation.Token); }
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
        private void OnCategoryItemClick(object sender, ItemClickEventArgs e) { _viewModel.SelectCategory(e.ClickedItem as DivarCategory); }
        private void OnGoUpClick(object sender, RoutedEventArgs e) { _viewModel.GoUp(); }
    }
}
