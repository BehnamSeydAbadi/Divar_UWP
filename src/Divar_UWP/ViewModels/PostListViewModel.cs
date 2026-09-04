using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class PostListViewModel : PageViewModelBase
    {
        private readonly IDivarSearchService _searchService;
        private readonly IDivarSelectionStore _selectionStore;
        private readonly INavigationService _navigation;
        private readonly HashSet<string> _knownTokens = new HashSet<string>(StringComparer.Ordinal);
        private DivarFeedNavigationParameter _parameter;
        private CancellationToken _pageCancellationToken;
        private string _paginationDataJson;
        private string _searchDataJson;
        private bool _isLoading;
        private bool _isLoadingNextPage;
        private bool _hasError;
        private bool _isEmpty;
        private string _errorMessage;
        private string _feedTitle;
        private string _filterButtonText;

        public PostListViewModel(IDivarSearchService searchService, IDivarSelectionStore selectionStore, INavigationService navigation)
            : base("آگهی‌ها")
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Posts = new IncrementalPostCollection(LoadNextPageAsync);
            FeedTitle = "آگهی‌ها";
            OpenFiltersCommand = new RelayCommand(() => _navigation.Navigate(typeof(FilterPage), _parameter));
        }

        public IncrementalPostCollection Posts { get; private set; }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool IsLoadingNextPage { get { return _isLoadingNextPage; } private set { SetProperty(ref _isLoadingNextPage, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public bool IsEmpty { get { return _isEmpty; } private set { SetProperty(ref _isEmpty, value); } }
        public string ErrorMessage { get { return _errorMessage; } private set { SetProperty(ref _errorMessage, value); } }
        public string FeedTitle { get { return _feedTitle; } private set { SetProperty(ref _feedTitle, value); } }
        public string FilterButtonText { get { return _filterButtonText; } private set { SetProperty(ref _filterButtonText, value); } }
        public ICommand OpenFiltersCommand { get; private set; }

        public void OpenPost(DivarPostSummary post)
        {
            if (post == null || string.IsNullOrWhiteSpace(post.Token)) return;
            _navigation.Navigate(typeof(PostDetailsPage), new DivarPostNavigationParameter { Token = post.Token });
        }

        public async Task InitializeAsync(DivarFeedNavigationParameter parameter, CancellationToken cancellationToken)
        {
            _parameter = parameter ?? new DivarFeedNavigationParameter();
            _pageCancellationToken = cancellationToken;
            FilterButtonText = _parameter.ActiveFilterCount > 0 ? "فیلترها (" + _parameter.ActiveFilterCount + ")" : "فیلترها";
            FeedTitle = string.IsNullOrWhiteSpace(_parameter.Title)
                ? (string.IsNullOrWhiteSpace(_parameter.Query) ? "آگهی‌ها" : "نتایج «" + _parameter.Query.Trim() + "»")
                : _parameter.Title;
            Posts.SetHasMoreItems(false);
            Posts.Clear();
            _knownTokens.Clear();
            _paginationDataJson = null;
            _searchDataJson = null;
            HasError = false;
            IsEmpty = false;
            ErrorMessage = string.Empty;
            IsLoading = true;
            try
            {
                var loaded = await LoadPageAsync(false, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                IsEmpty = loaded == 0 && !HasError;
            }
            finally { IsLoading = false; }
        }

        public void Resume(CancellationToken cancellationToken)
        {
            _pageCancellationToken = cancellationToken;
        }

        private async Task<uint> LoadNextPageAsync(CancellationToken incrementalCancellationToken)
        {
            if (IsLoadingNextPage || !Posts.HasMoreItems) return 0;
            IsLoadingNextPage = true;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_pageCancellationToken, incrementalCancellationToken))
            {
                try { return await LoadPageAsync(true, linked.Token); }
                catch (OperationCanceledException) { return 0; }
                finally { IsLoadingNextPage = false; }
            }
        }

        private async Task<uint> LoadPageAsync(bool isNextPage, CancellationToken cancellationToken)
        {
            var city = _selectionStore.GetSelectedCity();
            if (city == null)
            {
                HasError = true;
                ErrorMessage = "ابتدا یک شهر انتخاب کنید.";
                Posts.SetHasMoreItems(false);
                return 0;
            }
            var request = new DivarSearchRequest
            {
                Query = _parameter.Query,
                CategorySlug = string.IsNullOrWhiteSpace(_parameter.CategorySlug) ? "ROOT" : _parameter.CategorySlug,
                PaginationDataJson = isNextPage ? _paginationDataJson : null,
                SearchDataJson = isNextPage ? _searchDataJson : null,
                FilterDataJson = _parameter.FilterDataJson
            };
            request.CityIds.Add(city.Id);
            var result = await _searchService.SearchAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!result.IsSuccess)
            {
                HasError = Posts.Count == 0;
                ErrorMessage = result.ErrorMessage;
                Posts.SetHasMoreItems(false);
                return 0;
            }
            HasError = false;
            ErrorMessage = string.Empty;
            var page = result.Value;
            uint added = 0;
            foreach (var post in page.Items)
            {
                if (string.IsNullOrWhiteSpace(post.Token) || !_knownTokens.Add(post.Token)) continue;
                Posts.Add(post);
                added++;
            }
            _paginationDataJson = page.PaginationDataJson;
            _searchDataJson = page.SearchDataJson;
            Posts.SetHasMoreItems(page.HasNextPage && !string.IsNullOrWhiteSpace(_paginationDataJson));
            return added;
        }
    }
}
