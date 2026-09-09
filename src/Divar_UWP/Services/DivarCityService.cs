using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarCityService : IDivarCityService
    {
        private const string CitiesPath = "v8/places/cities";
        private static readonly BoundedMemoryCache<IList<DivarCity>> Cache = new BoundedMemoryCache<IList<DivarCity>>(1);
        private readonly IDivarApiClient _apiClient;

        public DivarCityService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<IList<DivarCity>>> GetCitiesAsync(CancellationToken cancellationToken)
        {
            IList<DivarCity> cached;
            if (Cache.TryGet(CitiesPath, out cached)) return ServiceResult<IList<DivarCity>>.Success(cached);
            var response = await _apiClient.GetAsync(CitiesPath, cancellationToken);
            if (!response.IsSuccess)
            {
                return ServiceResult<IList<DivarCity>>.Failure("دریافت شهرها ممکن نشد. اتصال اینترنت را بررسی کنید.");
            }

            try
            {
                var root = JsonObject.Parse(response.Content);
                var topSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                JsonArray topCities;
                if (root.TryGetValue("top_cities", out var topValue) && topValue.ValueType == JsonValueType.Array)
                {
                    topCities = topValue.GetArray();
                    foreach (var value in topCities)
                    {
                        if (value.ValueType == JsonValueType.String)
                        {
                            topSlugs.Add(value.GetString());
                        }
                    }
                }

                var result = new List<DivarCity>();
                foreach (var value in root.GetNamedArray("cities"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var city = value.GetObject();
                    var slug = GetString(city, "slug");
                    var centroid = GetObject(city, "centroid");
                    result.Add(new DivarCity
                    {
                        Id = GetNumberString(city, "id"),
                        Name = GetString(city, "name"),
                        Slug = slug,
                        ParentId = GetNumberString(city, "parent"),
                        IsTopCity = topSlugs.Contains(slug),
                        Latitude = GetNumber(centroid, "latitude"),
                        Longitude = GetNumber(centroid, "longitude")
                    });
                }

                var ordered = result
                    .OrderByDescending(city => city.IsTopCity)
                    .ThenBy(city => city.Name, StringComparer.CurrentCulture)
                    .ToList();
                Cache.Set(CitiesPath, ordered, TimeSpan.FromHours(6));
                return ServiceResult<IList<DivarCity>>.Success(ordered);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return ServiceResult<IList<DivarCity>>.Failure("پاسخ فهرست شهرها قابل پردازش نبود.");
            }
        }

        public static void ClearCache() { Cache.Clear(); }

        public async Task<ServiceResult<DivarCity>> FindCityAsync(double latitude, double longitude, CancellationToken cancellationToken)
        {
            var citiesResult = await GetCitiesAsync(cancellationToken);
            if (!citiesResult.IsSuccess)
            {
                return ServiceResult<DivarCity>.Failure(citiesResult.ErrorMessage);
            }

            var city = citiesResult.Value
                .OrderBy(item => DistanceSquared(latitude, longitude, item.Latitude, item.Longitude))
                .FirstOrDefault();
            return city == null
                ? ServiceResult<DivarCity>.Failure("شهر نزدیک پیدا نشد.")
                : ServiceResult<DivarCity>.Success(city);
        }

        private static double DistanceSquared(double latitude, double longitude, double cityLatitude, double cityLongitude)
        {
            var latitudeDelta = latitude - cityLatitude;
            var longitudeDelta = longitude - cityLongitude;
            return latitudeDelta * latitudeDelta + longitudeDelta * longitudeDelta;
        }

        private static string GetString(JsonObject value, string key)
        {
            return value == null ? string.Empty : value.GetNamedString(key, string.Empty);
        }

        private static string GetNumberString(JsonObject value, string key)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var number = value.GetNamedNumber(key, double.NaN);
            return double.IsNaN(number) ? string.Empty : number.ToString("0", CultureInfo.InvariantCulture);
        }

        private static double GetNumber(JsonObject value, string key)
        {
            return value == null ? 0 : value.GetNamedNumber(key, 0);
        }

        private static JsonObject GetObject(JsonObject value, string key)
        {
            IJsonValue child;
            return value != null && value.TryGetValue(key, out child) && child.ValueType == JsonValueType.Object
                ? child.GetObject()
                : null;
        }
    }
}
