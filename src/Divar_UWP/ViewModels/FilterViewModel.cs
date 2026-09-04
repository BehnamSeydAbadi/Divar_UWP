using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;
using Windows.Data.Json;

namespace Divar_UWP.ViewModels
{
    public sealed class FilterViewModel : PageViewModelBase
    {
        private readonly IDivarFilterService _filterService;
        private readonly IDivarSelectionStore _selectionStore;
        private readonly INavigationService _navigation;
        private DivarFeedNavigationParameter _parameter;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage;
        private string _validationMessage;

        public FilterViewModel(IDivarFilterService filterService, IDivarSelectionStore selectionStore, INavigationService navigation)
            : base("فیلترها")
        {
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Filters = new ObservableCollection<DivarFilterDefinition>();
        }

        public ObservableCollection<DivarFilterDefinition> Filters { get; private set; }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public string ErrorMessage { get { return _errorMessage; } private set { SetProperty(ref _errorMessage, value); } }
        public string ValidationMessage { get { return _validationMessage; } private set { SetProperty(ref _validationMessage, value); } }

        public async Task LoadAsync(DivarFeedNavigationParameter parameter, CancellationToken cancellationToken)
        {
            _parameter = parameter ?? new DivarFeedNavigationParameter();
            IsLoading = true; HasError = false; ErrorMessage = string.Empty; ValidationMessage = string.Empty; Filters.Clear();
            try
            {
                var result = await _filterService.GetFiltersAsync(_selectionStore.GetSelectedCity(), _parameter.CategorySlug, null, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!result.IsSuccess) { HasError = true; ErrorMessage = result.ErrorMessage; return; }
                foreach (var filter in result.Value) Filters.Add(filter);
                RestoreValues(_parameter.FilterDataJson);
            }
            finally { IsLoading = false; }
        }

        public void Clear()
        {
            foreach (var filter in Filters)
            {
                filter.Minimum = string.Empty; filter.Maximum = string.Empty; filter.BooleanValue = false; filter.SelectedOption = null;
                foreach (var option in filter.Options) option.IsSelected = false;
            }
            ValidationMessage = string.Empty;
        }

        public void Apply()
        {
            JsonObject values; int active;
            if (!TryBuildValues(out values, out active)) return;
            _parameter.FilterDataJson = values.Count == 0 ? null : values.Stringify();
            _parameter.ActiveFilterCount = active;
            if (_navigation.CanGoBack) _navigation.GoBack();
        }

        private bool TryBuildValues(out JsonObject values, out int active)
        {
            values = new JsonObject(); active = 0; ValidationMessage = string.Empty;
            foreach (var filter in Filters)
            {
                if (filter.Kind == DivarFilterKind.NumberRange)
                {
                    var min = NormalizeDigits(filter.Minimum); var max = NormalizeDigits(filter.Maximum);
                    long minValue, maxValue;
                    if ((!string.IsNullOrWhiteSpace(min) && !long.TryParse(min, NumberStyles.None, CultureInfo.InvariantCulture, out minValue)) ||
                        (!string.IsNullOrWhiteSpace(max) && !long.TryParse(max, NumberStyles.None, CultureInfo.InvariantCulture, out maxValue)))
                    { ValidationMessage = "مقدار «" + filter.Title + "» باید عدد باشد."; return false; }
                    if (!string.IsNullOrWhiteSpace(min) && !string.IsNullOrWhiteSpace(max) && long.Parse(min) > long.Parse(max))
                    { ValidationMessage = "حداقل «" + filter.Title + "» نباید از حداکثر بیشتر باشد."; return false; }
                    if (string.IsNullOrWhiteSpace(min) && string.IsNullOrWhiteSpace(max)) continue;
                    var range = new JsonObject();
                    if (!string.IsNullOrWhiteSpace(min)) range["minimum"] = JsonValue.CreateStringValue(min);
                    if (!string.IsNullOrWhiteSpace(max)) range["maximum"] = JsonValue.CreateStringValue(max);
                    var typed = new JsonObject(); typed["number_range"] = range; values[filter.Key] = typed; active++;
                }
                else if (filter.Kind == DivarFilterKind.Boolean && filter.BooleanValue)
                {
                    var value = new JsonObject(); value["value"] = JsonValue.CreateBooleanValue(true);
                    var typed = new JsonObject(); typed["boolean"] = value; values[filter.Key] = typed; active++;
                }
                else if (filter.Kind == DivarFilterKind.SingleSelect && filter.SelectedOption != null)
                {
                    var value = new JsonObject(); value["value"] = JsonValue.CreateStringValue(filter.SelectedOption.Value);
                    var typed = new JsonObject(); typed["str"] = value; values[filter.Key] = typed; active++;
                }
                else if (filter.Kind == DivarFilterKind.MultiSelect)
                {
                    var selected = filter.Options.Where(x => x.IsSelected).ToList();
                    if (selected.Count == 0) continue;
                    var array = new JsonArray(); foreach (var option in selected) array.Add(JsonValue.CreateStringValue(option.Value));
                    var value = new JsonObject(); value["value"] = array;
                    var typed = new JsonObject(); typed["repeated_string"] = value; values[filter.Key] = typed; active++;
                }
            }
            return true;
        }

        private void RestoreValues(string json)
        {
            JsonObject values;
            if (string.IsNullOrWhiteSpace(json) || !JsonObject.TryParse(json, out values)) return;
            foreach (var filter in Filters)
            {
                IJsonValue raw;
                if (!values.TryGetValue(filter.Key, out raw) || raw.ValueType != JsonValueType.Object) continue;
                var typed = raw.GetObject(); JsonObject value;
                if (filter.Kind == DivarFilterKind.NumberRange && TryObject(typed, "number_range", out value))
                { filter.Minimum = String(value, "minimum"); filter.Maximum = String(value, "maximum"); }
                else if (filter.Kind == DivarFilterKind.Boolean && TryObject(typed, "boolean", out value)) filter.BooleanValue = Bool(value, "value");
                else if (filter.Kind == DivarFilterKind.SingleSelect && TryObject(typed, "str", out value))
                { var selected = String(value, "value"); filter.SelectedOption = filter.Options.FirstOrDefault(x => x.Value == selected); }
                else if (filter.Kind == DivarFilterKind.MultiSelect && TryObject(typed, "repeated_string", out value))
                {
                    JsonArray array; if (!TryArray(value, "value", out array)) continue;
                    foreach (var option in filter.Options) option.IsSelected = array.Any(x => x.ValueType == JsonValueType.String && x.GetString() == option.Value);
                }
            }
        }

        private static string NormalizeDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var chars = value.Trim().Where(c => c != ',' && c != '٬' && c != ' ').Select(c => c >= '۰' && c <= '۹' ? (char)('0' + c - '۰') : c >= '٠' && c <= '٩' ? (char)('0' + c - '٠') : c).ToArray();
            return new string(chars);
        }

        private static string String(JsonObject o, string key) { IJsonValue v; return o.TryGetValue(key, out v) && v.ValueType == JsonValueType.String ? v.GetString() : string.Empty; }
        private static bool Bool(JsonObject o, string key) { IJsonValue v; return o.TryGetValue(key, out v) && v.ValueType == JsonValueType.Boolean && v.GetBoolean(); }
        private static bool TryObject(JsonObject o, string key, out JsonObject result) { result = null; IJsonValue v; if (!o.TryGetValue(key, out v) || v.ValueType != JsonValueType.Object) return false; result = v.GetObject(); return true; }
        private static bool TryArray(JsonObject o, string key, out JsonArray result) { result = null; IJsonValue v; if (!o.TryGetValue(key, out v) || v.ValueType != JsonValueType.Array) return false; result = v.GetArray(); return true; }
    }
}
