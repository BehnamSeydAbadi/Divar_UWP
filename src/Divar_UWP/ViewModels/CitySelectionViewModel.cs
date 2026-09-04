using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;
using Divar_UWP.Views;

namespace Divar_UWP.ViewModels
{
    public sealed class CitySelectionViewModel : PageViewModelBase
    {
        private readonly IDivarCityService _cityService;
        private readonly IDivarSelectionStore _selectionStore;
        private readonly INavigationService _navigation;
        private IList<DivarCity> _allCities = new List<DivarCity>();
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage;

        public CitySelectionViewModel(IDivarCityService cityService, IDivarSelectionStore selectionStore, INavigationService navigation)
            : base("انتخاب شهر")
        {
            _cityService = cityService ?? throw new ArgumentNullException(nameof(cityService));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Cities = new ObservableCollection<DivarCity>();
        }

        public ObservableCollection<DivarCity> Cities { get; private set; }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public string ErrorMessage { get { return _errorMessage; } private set { SetProperty(ref _errorMessage, value); } }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            try
            {
                var result = await _cityService.GetCitiesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!result.IsSuccess)
                {
                    HasError = true;
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _allCities = result.Value ?? new List<DivarCity>();
                ApplyFilter(string.Empty);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ApplyFilter(string query)
        {
            var normalized = (query ?? string.Empty).Trim();
            var filtered = normalized.Length == 0
                ? _allCities
                : _allCities.Where(city => city.Name != null && city.Name.IndexOf(normalized, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            Cities.Clear();
            foreach (var city in filtered)
            {
                Cities.Add(city);
            }
        }

        public void SelectCity(DivarCity city)
        {
            if (city == null) return;
            _selectionStore.SaveSelectedCity(city);
            _navigation.Navigate(typeof(PostListPage));
        }
    }
}
