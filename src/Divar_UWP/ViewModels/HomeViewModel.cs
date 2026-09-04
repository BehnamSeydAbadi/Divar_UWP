using System;
using System.Collections.Generic;
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
    public sealed class HomeViewModel : PageViewModelBase
    {
        private readonly IDivarCategoryService _categoryService;
        private readonly IDivarSelectionStore _selectionStore;
        private readonly INavigationService _navigation;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage;

        public HomeViewModel(IDivarCategoryService categoryService, IDivarSelectionStore selectionStore, INavigationService navigation)
            : base("دیوار")
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Categories = new ObservableCollection<DivarCategory>();
            OpenCategoriesCommand = new RelayCommand(() => _navigation.Navigate(typeof(CategoriesPage)));
            ChangeCityCommand = new RelayCommand(() => _navigation.Navigate(typeof(CitySelectionPage)));
        }

        public ObservableCollection<DivarCategory> Categories { get; private set; }
        public string SelectedCityName { get { var city = _selectionStore.GetSelectedCity(); return city == null ? "شهر انتخاب نشده" : city.Name; } }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public string ErrorMessage { get { return _errorMessage; } private set { SetProperty(ref _errorMessage, value); } }
        public ICommand OpenCategoriesCommand { get; private set; }
        public ICommand ChangeCityCommand { get; private set; }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            Categories.Clear();
            try
            {
                var result = await _categoryService.GetCategoriesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!result.IsSuccess)
                {
                    HasError = true;
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                foreach (var category in result.Value ?? new List<DivarCategory>()) Categories.Add(category);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void OpenCategory(DivarCategory category)
        {
            if (category == null) return;
            _selectionStore.SaveSelectedCategoryToken(category.Token);
            _navigation.Navigate(typeof(CategoriesPage), category.Token);
        }
    }
}
