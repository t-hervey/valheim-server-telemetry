using System;
using System.Collections.Generic;

namespace ValheimTelemetry.Tracking
{
    internal sealed class BoundedTimedCache<T>
    {
        private sealed class Entry
        {
            public T Id;
            public float Time;
        }

        private readonly int _capacity;
        private readonly float _retentionSeconds;
        private readonly Func<float> _clock;
        private readonly Dictionary<T, float> _times = new Dictionary<T, float>();
        private readonly Queue<Entry> _order = new Queue<Entry>();

        internal int Count => _times.Count;

        public BoundedTimedCache(int capacity, float retentionSeconds, Func<float> clock)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (retentionSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(retentionSeconds));
            _capacity = capacity;
            _retentionSeconds = retentionSeconds;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Touch(T id)
        {
            float now = _clock();
            _times[id] = now;
            _order.Enqueue(new Entry { Id = id, Time = now });
            Trim(now);
        }

        public bool ContainsRecent(T id, float maximumAgeSeconds)
        {
            float now = _clock();
            Trim(now);
            return _times.TryGetValue(id, out float time) && now - time <= maximumAgeSeconds;
        }

        private void Trim(float now)
        {
            while (_order.Count > 0 && (_order.Count > _capacity || now - _order.Peek().Time > _retentionSeconds))
            {
                Entry old = _order.Dequeue();
                if (_times.TryGetValue(old.Id, out float current) && current == old.Time)
                {
                    _times.Remove(old.Id);
                }
            }
        }
    }
}
