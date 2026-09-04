using System.Windows.Input;
using Divar_UWP.Infrastructure;

namespace Divar_UWP.ViewModels
{
    public sealed class PostDetailsViewModel : PageViewModelBase
    {
        public PostDetailsViewModel(INavigationService navigation)
            : base("جزئیات آگهی")
        {
            BackCommand = new RelayCommand(navigation.GoBack);
        }

        public ICommand BackCommand { get; private set; }
    }
}
