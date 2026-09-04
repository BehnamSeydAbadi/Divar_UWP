using System;
using Windows.UI.Xaml.Controls;

namespace Divar_UWP.Infrastructure
{
    public sealed class NavigationService : INavigationService
    {
        private readonly Frame _frame;

        public NavigationService(Frame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            _frame = frame;
        }

        public bool CanGoBack
        {
            get { return _frame.CanGoBack; }
        }

        public bool Navigate(Type pageType, object parameter = null)
        {
            if (pageType == null)
            {
                throw new ArgumentNullException(nameof(pageType));
            }

            if (_frame.CurrentSourcePageType == pageType && parameter == null)
            {
                return false;
            }

            return _frame.Navigate(pageType, parameter);
        }

        public void GoBack()
        {
            if (_frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }
    }
}
