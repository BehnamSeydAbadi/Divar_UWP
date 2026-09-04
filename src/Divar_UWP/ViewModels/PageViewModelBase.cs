using Divar_UWP.Infrastructure;

namespace Divar_UWP.ViewModels
{
    public abstract class PageViewModelBase : ObservableObject
    {
        protected PageViewModelBase(string title)
        {
            Title = title;
        }

        public string Title { get; private set; }
    }
}
