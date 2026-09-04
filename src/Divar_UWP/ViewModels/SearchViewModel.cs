using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class SearchViewModel : PageViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly IDivarSearchService _searchService;
        private readonly IDivarSelectionStore _selectionStore;
        private readonly RelayCommand _showResultsCommand;
        private CancellationTokenSource _suggestionCancellation;
        private string _query;
        private bool _isLoadingSuggestions;
        private string _suggestionMessage;

        public SearchViewModel(INavigationService navigation, IDivarSearchService searchService, IDivarSelectionStore selectionStore)
            : base("جستجو")
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            Suggestions = new ObservableCollection<DivarSearchSuggestion>();
            _showResultsCommand = new RelayCommand(parameter => ShowResults(), parameter => !string.IsNullOrWhiteSpace(Query));
            ShowResultsCommand = _showResultsCommand;
        }

        public ObservableCollection<DivarSearchSuggestion> Suggestions { get; private set; }
        public ICommand ShowResultsCommand { get; private set; }
        public string Query
        {
            get { return _query; }
            set
            {
                if (!SetProperty(ref _query, value)) return;
                _showResultsCommand.RaiseCanExecuteChanged();
                BeginSuggestionLookup();
            }
        }
        public bool IsLoadingSuggestions { get { return _isLoadingSuggestions; } private set { SetProperty(ref _isLoadingSuggestions, value); } }
        public string SuggestionMessage { get { return _suggestionMessage; } private set { SetProperty(ref _suggestionMessage, value); } }

        public void SubmitSuggestion(DivarSearchSuggestion suggestion)
        {
            if (suggestion == null || string.IsNullOrWhiteSpace(suggestion.DisplayText)) return;
            Query = suggestion.DisplayText;
            ShowResults();
        }

        public void CancelPendingWork()
        {
            if (_suggestionCancellation != null)
            {
                _suggestionCancellation.Cancel();
                _suggestionCancellation.Dispose();
                _suggestionCancellation = null;
            }
            IsLoadingSuggestions = false;
        }

        private void ShowResults()
        {
            var query = (Query ?? string.Empty).Trim();
            if (query.Length == 0) return;
            CancelPendingWork();
            _navigation.Navigate(typeof(PostListPage), new DivarFeedNavigationParameter
            {
                Query = query,
                CategorySlug = "ROOT",
                Title = "نتایج «" + query + "»"
            });
        }

        private async void BeginSuggestionLookup()
        {
            CancelPendingWork();
            Suggestions.Clear();
            SuggestionMessage = string.Empty;
            var query = (Query ?? string.Empty).Trim();
            if (query.Length < 2) return;
            _suggestionCancellation = new CancellationTokenSource();
            var token = _suggestionCancellation.Token;
            try
            {
                await Task.Delay(450, token);
                var city = _selectionStore.GetSelectedCity();
                if (city == null) return;
                IsLoadingSuggestions = true;
                var result = await _searchService.GetSuggestionsAsync(query, new[] { city.Id }, token);
                token.ThrowIfCancellationRequested();
                if (!result.IsSuccess) { SuggestionMessage = result.ErrorMessage; return; }
                foreach (var suggestion in result.Value) Suggestions.Add(suggestion);
            }
            catch (OperationCanceledException) { }
            finally { if (!token.IsCancellationRequested) IsLoadingSuggestions = false; }
        }
    }
}
