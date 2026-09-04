using System;

namespace Divar_UWP.Infrastructure
{
    public interface INavigationService
    {
        bool CanGoBack { get; }

        bool Navigate(Type pageType, object parameter = null);

        void GoBack();
    }
}
