using System;
using System.Threading;
using System.Threading.Tasks;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.Services;

namespace Divar_UWP.ViewModels
{
    public sealed class PostDetailsViewModel : PageViewModelBase
    {
        private readonly IDivarPostService _postService;
        private DivarPostDetails _details;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage;
        private int _selectedImageIndex;

        public PostDetailsViewModel(IDivarPostService postService)
            : base("جزئیات آگهی")
        {
            _postService = postService ?? throw new ArgumentNullException(nameof(postService));
        }

        public DivarPostDetails Details { get { return _details; } private set { SetProperty(ref _details, value); OnPropertyChanged("HasImages"); OnPropertyChanged("HasDescription"); OnPropertyChanged("HasPrice"); OnPropertyChanged("HasAttributes"); } }
        public bool IsLoading { get { return _isLoading; } private set { SetProperty(ref _isLoading, value); } }
        public bool HasError { get { return _hasError; } private set { SetProperty(ref _hasError, value); } }
        public string ErrorMessage { get { return _errorMessage; } private set { SetProperty(ref _errorMessage, value); } }
        public int SelectedImageIndex { get { return _selectedImageIndex; } private set { SetProperty(ref _selectedImageIndex, value); } }
        public bool HasImages { get { return Details != null && Details.Images.Count > 0; } }
        public bool HasDescription { get { return Details != null && !string.IsNullOrWhiteSpace(Details.Description); } }
        public bool HasPrice { get { return Details != null && !string.IsNullOrWhiteSpace(Details.PriceText); } }
        public bool HasAttributes { get { return Details != null && Details.Attributes.Count > 0; } }

        public async Task LoadAsync(string token, CancellationToken cancellationToken)
        {
            IsLoading = true; HasError = false; ErrorMessage = string.Empty; Details = null;
            try
            {
                var result = await _postService.GetPostAsync(token, null, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!result.IsSuccess) { HasError = true; ErrorMessage = result.ErrorMessage; return; }
                Details = result.Value;
                SelectImage(0);
            }
            finally { IsLoading = false; }
        }

        public void SelectImage(int index)
        {
            if (Details == null || index < 0 || index >= Details.Images.Count) return;
            SelectedImageIndex = index;
            var image = Details.Images[index];
            if (!string.IsNullOrWhiteSpace(image.FullUrl)) image.DisplayUrl = image.FullUrl;
        }
    }
}
