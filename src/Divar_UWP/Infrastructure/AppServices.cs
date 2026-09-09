using System;
using Divar_UWP.Services;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Infrastructure
{
    public sealed class AppServices
    {
        private static readonly AppServices Instance = new AppServices();

        private AppServices()
        {
            CredentialProvider = new PasswordVaultDivarCredentialProvider();
            ApiClient = new DivarApiClient(CredentialProvider);
            SelectionStore = new DivarSelectionStore();
            CityService = new DivarCityService(ApiClient);
            CategoryService = new DivarCategoryService(ApiClient);
            SearchService = new DivarSearchService(ApiClient);
            FilterService = new DivarFilterService(ApiClient);
            PostService = new DivarPostService(ApiClient);
            ContactService = new DivarContactService(ApiClient);
            AuthService = new DivarAuthService(ApiClient, CredentialProvider);
            BookmarkService = new DivarBookmarkService(ApiClient);
            MyDivarService = new DivarMyDivarService(ApiClient);
        }

        public static AppServices Current
        {
            get { return Instance; }
        }

        public IDivarCredentialProvider CredentialProvider { get; private set; }

        public IDivarApiClient ApiClient { get; private set; }

        public IDivarSelectionStore SelectionStore { get; private set; }

        public IDivarCityService CityService { get; private set; }

        public IDivarCategoryService CategoryService { get; private set; }

        public IDivarSearchService SearchService { get; private set; }

        public IDivarFilterService FilterService { get; private set; }

        public IDivarPostService PostService { get; private set; }

        public IDivarContactService ContactService { get; private set; }

        public IDivarAuthService AuthService { get; private set; }

        public IDivarBookmarkService BookmarkService { get; private set; }

        public IDivarMyDivarService MyDivarService { get; private set; }

        public INavigationService Navigation { get; private set; }

        public void InitializeNavigation(Frame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            Navigation = new NavigationService(frame);
        }

        public void TrimCaches(string reason)
        {
            DivarImageCache.Clear();
            DivarApiClient.ClearPublicCache();
            DivarCityService.ClearCache();
            DivarCategoryService.ClearCache();
            DivarSearchService.ClearCache();
            DivarDiagnostics.CacheTrim(reason);
        }
    }
}
