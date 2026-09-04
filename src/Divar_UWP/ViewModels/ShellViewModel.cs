using System;
using System.Windows.Input;
using Divar_UWP.Infrastructure;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class ShellViewModel : ObservableObject
    {
        private readonly IDivarSelectionStore _selectionStore;
        private string _currentCityName;

        public ShellViewModel(INavigationService navigation, IDivarSelectionStore selectionStore)
        {
            if (navigation == null) throw new ArgumentNullException(nameof(navigation));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            ShowCitiesCommand = new RelayCommand(() => navigation.Navigate(typeof(CitySelectionPage)));
            ShowHomeCommand = new RelayCommand(() => navigation.Navigate(typeof(HomePage)));
            ShowCategoriesCommand = new RelayCommand(() => navigation.Navigate(typeof(CategoriesPage)));
            ShowSearchCommand = new RelayCommand(() => navigation.Navigate(typeof(SearchPage)));
            ShowPostListCommand = new RelayCommand(() => navigation.Navigate(typeof(PostListPage)));
            RefreshSelection();
            _selectionStore.CityChanged += OnCityChanged;
        }

        public string CurrentCityName { get { return _currentCityName; } private set { SetProperty(ref _currentCityName, value); } }
        public ICommand ShowCitiesCommand { get; private set; }
        public ICommand ShowHomeCommand { get; private set; }
        public ICommand ShowCategoriesCommand { get; private set; }
        public ICommand ShowSearchCommand { get; private set; }
        public ICommand ShowPostListCommand { get; private set; }

        public void RefreshSelection()
        {
            var city = _selectionStore.GetSelectedCity();
            CurrentCityName = city == null ? "انتخاب شهر" : city.Name;
        }

        private void OnCityChanged(object sender, EventArgs e) { RefreshSelection(); }
    }
}
