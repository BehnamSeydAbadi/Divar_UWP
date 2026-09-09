using System;
using System.Collections.Generic;

namespace Divar_UWP.Infrastructure
{
    public sealed class BoundedMemoryCache<T>
    {
        private readonly int _capacity;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly Queue<string> _order = new Queue<string>();
        private readonly object _gate = new object();

        public BoundedMemoryCache(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public bool TryGet(string key, out T value)
        {
            value = default(T);
            if (string.IsNullOrWhiteSpace(key)) return false;
            lock (_gate)
            {
                Entry entry;
                if (!_entries.TryGetValue(key, out entry)) return false;
                if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    _entries.Remove(key);
                    return false;
                }
                value = entry.Value;
                return true;
            }
        }

        public void Set(string key, T value, TimeSpan lifetime)
        {
            if (string.IsNullOrWhiteSpace(key) || lifetime <= TimeSpan.Zero) return;
            lock (_gate)
            {
                if (_entries.ContainsKey(key))
                {
                    _entries[key] = new Entry { Value = value, ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime) };
                    return;
                }
                _entries[key] = new Entry { Value = value, ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime) };
                _order.Enqueue(key);
                while (_entries.Count > _capacity && _order.Count > 0)
                {
                    var oldest = _order.Dequeue();
                    if (!string.Equals(oldest, key, StringComparison.Ordinal)) _entries.Remove(oldest);
                }
                if (_order.Count > _capacity * 4) RebuildOrder();
            }
        }

        public void Clear()
        {
            lock (_gate) { _entries.Clear(); _order.Clear(); }
        }

        private void RebuildOrder()
        {
            _order.Clear();
            foreach (var key in _entries.Keys) _order.Enqueue(key);
        }

        private sealed class Entry
        {
            public T Value { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}
