using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Divar_UWP.Models;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Divar_UWP.Infrastructure
{
    public sealed class IncrementalPostCollection : ObservableCollection<DivarPostSummary>, ISupportIncrementalLoading
    {
        private readonly Func<CancellationToken, Task<uint>> _loadMore;
        private bool _hasMoreItems;
        private bool _isLoading;

        public IncrementalPostCollection(Func<CancellationToken, Task<uint>> loadMore)
        {
            _loadMore = loadMore ?? throw new ArgumentNullException(nameof(loadMore));
        }

        public bool HasMoreItems { get { return _hasMoreItems; } }

        public void SetHasMoreItems(bool value)
        {
            if (_hasMoreItems == value) return;
            _hasMoreItems = value;
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("HasMoreItems"));
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                if (_isLoading || !_hasMoreItems) return new LoadMoreItemsResult { Count = 0 };
                _isLoading = true;
                try
                {
                    var loaded = await _loadMore(cancellationToken);
                    return new LoadMoreItemsResult { Count = loaded };
                }
                finally { _isLoading = false; }
            });
        }
    }
}
