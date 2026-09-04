using System;
using Divar_UWP.Models;
using Windows.Storage;

namespace Divar_UWP.Infrastructure
{
    public sealed class DivarSelectionStore : IDivarSelectionStore
    {
        private const string CityIdKey = "SelectedCity.Id";
        private const string CityNameKey = "SelectedCity.Name";
        private const string CitySlugKey = "SelectedCity.Slug";
        private const string CategoryTokenKey = "SelectedCategory.Token";

        private readonly ApplicationDataContainer _settings;

        public DivarSelectionStore()
        {
            _settings = ApplicationData.Current.LocalSettings;
        }

        public event EventHandler CityChanged;

        public DivarCity GetSelectedCity()
        {
            var id = ReadString(CityIdKey);
            var name = ReadString(CityNameKey);
            var slug = ReadString(CitySlugKey);

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            return new DivarCity { Id = id, Name = name, Slug = slug };
        }

        public void SaveSelectedCity(DivarCity city)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.Id) || string.IsNullOrWhiteSpace(city.Slug))
            {
                throw new ArgumentException("A valid city is required.", nameof(city));
            }

            _settings.Values[CityIdKey] = city.Id;
            _settings.Values[CityNameKey] = city.Name ?? string.Empty;
            _settings.Values[CitySlugKey] = city.Slug;

            var handler = CityChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public string GetSelectedCategoryToken()
        {
            return ReadString(CategoryTokenKey);
        }

        public void SaveSelectedCategoryToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _settings.Values.Remove(CategoryTokenKey);
                return;
            }

            _settings.Values[CategoryTokenKey] = token;
        }

        private string ReadString(string key)
        {
            object value;
            return _settings.Values.TryGetValue(key, out value) ? value as string : null;
        }
    }
}
