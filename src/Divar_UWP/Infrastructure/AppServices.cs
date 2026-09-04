using System;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Infrastructure
{
    public sealed class AppServices
    {
        private static readonly AppServices Instance = new AppServices();

        private AppServices()
        {
            CredentialProvider = new EmptyDivarCredentialProvider();
            ApiClient = new DivarApiClient(CredentialProvider);
        }

        public static AppServices Current
        {
            get { return Instance; }
        }

        public IDivarCredentialProvider CredentialProvider { get; private set; }

        public IDivarApiClient ApiClient { get; private set; }

        public INavigationService Navigation { get; private set; }

        public void InitializeNavigation(Frame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            Navigation = new NavigationService(frame);
        }
    }
}
