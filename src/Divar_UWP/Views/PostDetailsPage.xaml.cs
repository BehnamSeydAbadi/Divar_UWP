using System;
using System.Threading;
using Divar_UWP.Infrastructure;
using Divar_UWP.Models;
using Divar_UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.System;

namespace Divar_UWP.Views
{
    public sealed partial class PostDetailsPage : Page
    {
        private readonly PostDetailsViewModel _viewModel;
        private CancellationTokenSource _cancellation;
        private CancellationTokenSource _contactCancellation;
        private CancellationTokenSource _bookmarkCancellation;
        private string _token;

        public PostDetailsPage()
        {
            InitializeComponent();
            _viewModel = new PostDetailsViewModel(AppServices.Current.PostService, AppServices.Current.ContactService, AppServices.Current.BookmarkService, AppServices.Current.AuthService);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) { base.OnNavigatedTo(e); var parameter = e.Parameter as DivarPostNavigationParameter; _token = parameter == null ? null : parameter.Token; StartLoading(); }
        protected override void OnNavigatedFrom(NavigationEventArgs e) { CloseImageViewer(); CancelBookmark(); CancelContact(); Cancel(); base.OnNavigatedFrom(e); }
        private async void StartLoading() { Cancel(); _cancellation = new CancellationTokenSource(); try { await _viewModel.LoadAsync(_token, _cancellation.Token); } catch (OperationCanceledException) { } }
        private void Cancel() { if (_cancellation == null) return; _cancellation.Cancel(); _cancellation.Dispose(); _cancellation = null; }
        private void CancelContact() { if (_contactCancellation == null) return; _contactCancellation.Cancel(); _contactCancellation.Dispose(); _contactCancellation = null; }
        private void CancelBookmark() { if (_bookmarkCancellation == null) return; _bookmarkCancellation.Cancel(); _bookmarkCancellation.Dispose(); _bookmarkCancellation = null; }
        private void OnRetryClick(object sender, RoutedEventArgs e) { StartLoading(); }
        private async void OnContactClick(object sender, RoutedEventArgs e)
        {
            CancelContact();
            _contactCancellation = new CancellationTokenSource();
            try { await _viewModel.RequestPhoneAsync(_contactCancellation.Token); }
            catch (OperationCanceledException) { }
        }

        private async void OnCallClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.PhoneNumber)) return;
            await Launcher.LaunchUriAsync(new Uri("tel:" + _viewModel.PhoneNumber));
        }
        private async void OnBookmarkClick(object sender, RoutedEventArgs e)
        {
            CancelBookmark();
            _bookmarkCancellation = new CancellationTokenSource();
            try { await _viewModel.ToggleBookmarkAsync(_bookmarkCancellation.Token); }
            catch (OperationCanceledException) { }
        }
        private void OnGallerySelectionChanged(object sender, SelectionChangedEventArgs e) { var gallery = sender as FlipView; if (gallery != null) _viewModel.SelectImage(gallery.SelectedIndex); }
        private void OnThumbnailClick(object sender, ItemClickEventArgs e) { var image = e.ClickedItem as DivarPostImage; if (image == null || _viewModel.Details == null) return; _viewModel.SelectImage(_viewModel.Details.Images.IndexOf(image)); Gallery.SelectedIndex = _viewModel.SelectedImageIndex; }
        private void OnGalleryImageTapped(object sender, TappedRoutedEventArgs e) { OpenImageViewer(_viewModel.SelectedImageIndex); }
        private void OnCloseImageViewerClick(object sender, RoutedEventArgs e) { CloseImageViewer(); }

        private void OnPreviousImageClick(object sender, RoutedEventArgs e)
        {
            ShowImageInViewer(_viewModel.SelectedImageIndex - 1);
        }

        private void OnNextImageClick(object sender, RoutedEventArgs e)
        {
            ShowImageInViewer(_viewModel.SelectedImageIndex + 1);
        }

        private void OpenImageViewer(int imageIndex)
        {
            if (_viewModel.Details == null || _viewModel.Details.Images.Count == 0) return;
            ImageViewerOverlay.Visibility = Visibility.Visible;
            ShowImageInViewer(imageIndex);
        }

        private void ShowImageInViewer(int imageIndex)
        {
            if (_viewModel.Details == null || imageIndex < 0 || imageIndex >= _viewModel.Details.Images.Count) return;
            _viewModel.SelectImage(imageIndex);
            Gallery.SelectedIndex = imageIndex;
            var image = _viewModel.Details.Images[imageIndex];
            var sourceUrl = string.IsNullOrWhiteSpace(image.FullUrl) ? image.DisplayUrl : image.FullUrl;
            if (!string.IsNullOrWhiteSpace(sourceUrl)) ImageViewerImage.Source = new BitmapImage(new Uri(sourceUrl));
            ImageViewerScrollViewer.ChangeView(null, null, 1.0f, true);
            PreviousImageButton.IsEnabled = imageIndex > 0;
            NextImageButton.IsEnabled = imageIndex < _viewModel.Details.Images.Count - 1;
        }

        private void CloseImageViewer()
        {
            ImageViewerOverlay.Visibility = Visibility.Collapsed;
            ImageViewerImage.Source = null;
        }
    }
}
