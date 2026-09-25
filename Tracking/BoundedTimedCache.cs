using System.Collections.Generic;
using UnityEngine;

namespace ValheimTelemetry.Tracking
{
    internal sealed class BoundedTimedCache
    {
        private sealed class Entry
        {
            public ZDOID Id;
            public float Time;
        }

        private readonly int _capacity;
        private readonly float _retentionSeconds;
        private readonly Dictionary<ZDOID, float> _times = new Dictionary<ZDOID, float>();
        private readonly Queue<Entry> _order = new Queue<Entry>();

        public BoundedTimedCache(int capacity, float retentionSeconds)
        {
            _capacity = capacity;
            _retentionSeconds = retentionSeconds;
        }

        public void Touch(ZDOID id)
        {
            float now = Time.realtimeSinceStartup;
            _times[id] = now;
            _order.Enqueue(new Entry { Id = id, Time = now });
            Trim(now);
        }

        public bool ContainsRecent(ZDOID id, float maximumAgeSeconds)
        {
            float now = Time.realtimeSinceStartup;
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
