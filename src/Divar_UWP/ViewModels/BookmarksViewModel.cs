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
    public sealed class BookmarksViewModel : PageViewModelBase
    {
        private readonly IDivarBookmarkService _bookmarkService;
        private readonly IDivarAuthService _authService;
        private readonly INavigationService _navigation;
        private bool _isLoading;
        private bool _hasError;
        private bool _isEmpty;
        private bool _needsLogin;
        private string _message;

        public BookmarksViewModel(IDivarBookmarkService bookmarkService, IDivarAuthService authService, INavigationService navigation)
            : base("نشان‌ها")
        {
            _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Posts = new ObservableCollection<DivarPostSummary>();
            LoginCommand = new RelayCommand(() => _navigation.Navigate(typeof(AuthPage)));
        }

        public ObservableCollection<DivarPostSummary> Posts { get; private set; }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public bool IsEmpty { get { return _isEmpty; } private set { SetProperty(ref _isEmpty, value); } }
        public bool NeedsLogin { get { return _needsLogin; } private set { SetProperty(ref _needsLogin, value); } }
        public string Message { get { return _message; } private set { SetProperty(ref _message, value); } }
        public ICommand LoginCommand { get; private set; }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            Posts.Clear(); HasError = false; IsEmpty = false; Message = string.Empty;
            NeedsLogin = !_authService.IsAuthenticated;
            if (NeedsLogin) { Message = "برای دیدن نشان‌ها وارد حساب شوید."; return; }
            IsLoading = true;
            try
            {
                var result = await _bookmarkService.GetBookmarksAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!result.IsSuccess)
                {
                    Message = result.ErrorMessage; NeedsLogin = !_authService.IsAuthenticated; HasError = !NeedsLogin; return;
                }
                foreach (var bookmark in result.Value) if (bookmark.Post != null) Posts.Add(bookmark.Post);
                IsEmpty = Posts.Count == 0;
            }
            finally { IsLoading = false; }
        }

        public void OpenPost(DivarPostSummary post)
        {
            if (post == null || string.IsNullOrWhiteSpace(post.Token)) return;
            _navigation.Navigate(typeof(PostDetailsPage), new DivarPostNavigationParameter { Token = post.Token });
        }
    }
}
