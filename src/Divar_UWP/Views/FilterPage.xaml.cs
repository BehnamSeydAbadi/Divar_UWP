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
    public sealed partial class FilterPage : Page
    {
        private readonly FilterViewModel _viewModel;
        private CancellationTokenSource _cancellation;
        private DivarFeedNavigationParameter _parameter;

        public FilterPage()
        {
            InitializeComponent();
            _viewModel = new FilterViewModel(AppServices.Current.FilterService, AppServices.Current.SelectionStore, AppServices.Current.Navigation);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); _parameter = e.Parameter as DivarFeedNavigationParameter; StartLoading(); }
        protected override void OnNavigatedFrom(NavigationEventArgs e) { Cancel(); base.OnNavigatedFrom(e); }
        private async void StartLoading() { Cancel(); _cancellation = new CancellationTokenSource(); try { await _viewModel.LoadAsync(_parameter, _cancellation.Token); } catch (OperationCanceledException) { } }
        private void Cancel() { if (_cancellation == null) return; _cancellation.Cancel(); _cancellation.Dispose(); _cancellation = null; }
        private void OnRetryClick(object sender, RoutedEventArgs e) { StartLoading(); }
        private void OnClearClick(object sender, RoutedEventArgs e) { _viewModel.Clear(); }
        private void OnApplyClick(object sender, RoutedEventArgs e) { _viewModel.Apply(); }
    }
}
