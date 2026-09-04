using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Divar_UWP.Views
{
    public sealed partial class SearchPage : Page
    {
        private readonly SearchViewModel _viewModel;

        public SearchPage()
        {
            InitializeComponent();
            _viewModel = new SearchViewModel(AppServices.Current.Navigation, AppServices.Current.SearchService, AppServices.Current.SelectionStore);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _viewModel.CancelPendingWork();
            base.OnNavigatedFrom(e);
        }

        private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            var command = _viewModel.ShowResultsCommand;
            if (command.CanExecute(null)) command.Execute(null);
        }

        private void OnSuggestionClick(object sender, ItemClickEventArgs e)
        {
            _viewModel.SubmitSuggestion(e.ClickedItem as DivarSearchSuggestion);
        }
    }
}
