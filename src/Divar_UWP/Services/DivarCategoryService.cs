using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Windows.Data.Json;

namespace Divar_UWP.Services
{
    public sealed class DivarCategoryService : IDivarCategoryService
    {
        private const string CategoriesPath = "v1/open-platform/assets/category";
        private const string PathSeparator = " - ";
        private static readonly BoundedMemoryCache<IList<DivarCategory>> Cache = new BoundedMemoryCache<IList<DivarCategory>>(1);
        private readonly IDivarApiClient _apiClient;

        public DivarCategoryService(IDivarApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public async Task<ServiceResult<IList<DivarCategory>>> GetCategoriesAsync(CancellationToken cancellationToken)
        {
            IList<DivarCategory> cached;
            if (Cache.TryGet(CategoriesPath, out cached)) return ServiceResult<IList<DivarCategory>>.Success(cached);
            var response = await _apiClient.GetAsync(CategoriesPath, cancellationToken);
            if (!response.IsSuccess)
            {
                return ServiceResult<IList<DivarCategory>>.Failure("دریافت دسته‌بندی‌ها ممکن نشد. دوباره تلاش کنید.");
            }

            try
            {
                var roots = new List<DivarCategory>();
                var nodesByPath = new Dictionary<string, DivarCategory>(StringComparer.Ordinal);
                var root = JsonObject.Parse(response.Content);

                foreach (var value in root.GetNamedArray("categories"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = value.GetObject();
                    var display = item.GetNamedString("display", string.Empty);
                    var slug = item.GetNamedString("slug", string.Empty);
                    var segments = display.Split(new[] { PathSeparator }, StringSplitOptions.None);
                    if (segments.Length <= 1 || string.Equals(slug, "root", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    DivarCategory parent = null;
                    var path = string.Empty;
                    for (var index = 1; index < segments.Length; index++)
                    {
                        var name = segments[index].Trim();
                        path = path.Length == 0 ? name : path + PathSeparator + name;

                        DivarCategory node;
                        if (!nodesByPath.TryGetValue(path, out node))
                        {
                            node = new DivarCategory { Name = name };
                            nodesByPath[path] = node;
                            if (parent == null)
                            {
                                roots.Add(node);
                            }
                            else
                            {
                                parent.Children.Add(node);
                            }
                        }

                        if (index == segments.Length - 1)
                        {
                            node.Id = slug;
                            node.Token = slug;
                            node.Slug = slug;
                        }

                        parent = node;
                    }
                }

                SetParentTokens(roots, null);
                Cache.Set(CategoriesPath, roots, TimeSpan.FromHours(6));
                return ServiceResult<IList<DivarCategory>>.Success(roots);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return ServiceResult<IList<DivarCategory>>.Failure("پاسخ دسته‌بندی‌ها قابل پردازش نبود.");
            }
        }

        public static void ClearCache() { Cache.Clear(); }

        private static void SetParentTokens(IEnumerable<DivarCategory> categories, string parentToken)
        {
            foreach (var category in categories)
            {
                category.ParentToken = parentToken;
                SetParentTokens(category.Children, category.Token ?? parentToken);
            }
        }
    }
}
