using System;
using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Services;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class ShellViewModel : ObservableObject
    {
        private readonly IDivarSelectionStore _selectionStore;
        private readonly IDivarAuthService _authService;
        private string _currentCityName;
        private string _authMenuText;

        public ShellViewModel(INavigationService navigation, IDivarSelectionStore selectionStore, IDivarAuthService authService)
        {
            if (navigation == null) throw new ArgumentNullException(nameof(navigation));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            ShowCitiesCommand = new RelayCommand(() => navigation.Navigate(typeof(CitySelectionPage)));
            ShowHomeCommand = new RelayCommand(() => navigation.Navigate(typeof(HomePage)));
            ShowCategoriesCommand = new RelayCommand(() => navigation.Navigate(typeof(CategoriesPage)));
            ShowSearchCommand = new RelayCommand(() => navigation.Navigate(typeof(SearchPage)));
            ShowPostListCommand = new RelayCommand(() => navigation.Navigate(typeof(PostListPage)));
            ShowBookmarksCommand = new RelayCommand(() => navigation.Navigate(typeof(BookmarksPage)));
            ShowMyDivarCommand = new RelayCommand(() => navigation.Navigate(typeof(MyDivarPage)));
            ShowAuthCommand = new RelayCommand(() => navigation.Navigate(typeof(AuthPage)));
            RefreshSelection();
            RefreshAuthState();
            _selectionStore.CityChanged += OnCityChanged;
        }

        public string CurrentCityName { get { return _currentCityName; } private set { SetProperty(ref _currentCityName, value); } }
        public ICommand ShowCitiesCommand { get; private set; }
        public ICommand ShowHomeCommand { get; private set; }
        public ICommand ShowCategoriesCommand { get; private set; }
        public ICommand ShowSearchCommand { get; private set; }
        public ICommand ShowPostListCommand { get; private set; }
        public ICommand ShowBookmarksCommand { get; private set; }
        public ICommand ShowMyDivarCommand { get; private set; }
        public ICommand ShowAuthCommand { get; private set; }
        public string AuthMenuText { get { return _authMenuText; } private set { SetProperty(ref _authMenuText, value); } }

        public void RefreshSelection()
        {
            var city = _selectionStore.GetSelectedCity();
            CurrentCityName = city == null ? "انتخاب شهر" : city.Name;
        }

        public void RefreshAuthState() { AuthMenuText = _authService.IsAuthenticated ? "خروج از حساب" : "ورود به حساب"; }

        private void OnCityChanged(object sender, EventArgs e) { RefreshSelection(); }
    }
}
