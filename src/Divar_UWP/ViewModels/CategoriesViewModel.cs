using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;

namespace Divar_UWP.ViewModels
{
    public sealed class CategoriesViewModel : PageViewModelBase
    {
        private readonly IDivarCategoryService _categoryService;
        private readonly IDivarSelectionStore _selectionStore;
        private readonly Stack<CategoryLevel> _history = new Stack<CategoryLevel>();
        private IList<DivarCategory> _roots = new List<DivarCategory>();
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage;
        private string _currentTitle;
        private string _statusMessage;
        private bool _canGoUp;

        public CategoriesViewModel(IDivarCategoryService categoryService, IDivarSelectionStore selectionStore)
            : base("دسته‌بندی‌ها")
        {
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
            Categories = new ObservableCollection<DivarCategory>();
            CurrentTitle = "همهٔ دسته‌بندی‌ها";
        }

        public ObservableCollection<DivarCategory> Categories { get; private set; }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public string ErrorMessage { get { return _errorMessage; } private set { SetProperty(ref _errorMessage, value); } }
        public string CurrentTitle { get { return _currentTitle; } private set { SetProperty(ref _currentTitle, value); } }
        public string StatusMessage { get { return _statusMessage; } private set { SetProperty(ref _statusMessage, value); } }
        public bool CanGoUp { get { return _canGoUp; } private set { SetProperty(ref _canGoUp, value); } }

        public async Task LoadAsync(string initialToken, CancellationToken cancellationToken)
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            StatusMessage = string.Empty;
            _history.Clear();
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

                _roots = result.Value ?? new List<DivarCategory>();
                var initial = FindByToken(_roots, initialToken);
                if (initial != null && initial.HasChildren)
                {
                    _history.Push(new CategoryLevel("همهٔ دسته‌بندی‌ها", _roots));
                    ShowLevel(initial.Name, initial.Children);
                }
                else
                {
                    ShowLevel("همهٔ دسته‌بندی‌ها", _roots);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void SelectCategory(DivarCategory category)
        {
            if (category == null) return;
            _selectionStore.SaveSelectedCategoryToken(category.Token);
            StatusMessage = string.Empty;
            if (category.HasChildren)
            {
                _history.Push(new CategoryLevel(CurrentTitle, Categories.ToList()));
                ShowLevel(category.Name, category.Children);
            }
            else
            {
                StatusMessage = "دستهٔ «" + category.Name + "» انتخاب شد؛ خوراک آن در برش بعدی متصل می‌شود.";
            }
        }

        public void GoUp()
        {
            if (_history.Count == 0) return;
            var previous = _history.Pop();
            ShowLevel(previous.Title, previous.Categories);
        }

        private void ShowLevel(string title, IEnumerable<DivarCategory> categories)
        {
            CurrentTitle = title;
            Categories.Clear();
            foreach (var category in categories) Categories.Add(category);
            CanGoUp = _history.Count > 0;
        }

        private static DivarCategory FindByToken(IEnumerable<DivarCategory> categories, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            foreach (var category in categories)
            {
                if (string.Equals(category.Token, token, StringComparison.OrdinalIgnoreCase)) return category;
                var child = FindByToken(category.Children, token);
                if (child != null) return child;
            }
            return null;
        }

        private sealed class CategoryLevel
        {
            public CategoryLevel(string title, IList<DivarCategory> categories) { Title = title; Categories = categories; }
            public string Title { get; private set; }
            public IList<DivarCategory> Categories { get; private set; }
        }
    }
}
