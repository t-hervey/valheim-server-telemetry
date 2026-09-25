using System.Collections.Generic;
using UnityEngine;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class RecentHitTracker
    {
        private sealed class Entry
        {
            public ZDOID Target;
            public PlayerIdentity Player;
            public float Time;
        }

        private readonly Dictionary<ZDOID, Entry> _entries = new Dictionary<ZDOID, Entry>();
        private readonly Queue<Entry> _order = new Queue<Entry>();
        private const int Capacity = 8192;
        private const float RetentionSeconds = 30f;

        public void Record(ZDOID target, HitData hit)
        {
            if (target.IsNone() || hit == null || hit.m_attacker.IsNone())
            {
                return;
            }
            PlayerIdentity identity = PlayerUtil.FromCharacter(hit.m_attacker);
            if (identity == null)
            {
                return;
            }
            Entry entry = new Entry { Target = target, Player = identity, Time = Time.realtimeSinceStartup };
            _entries[target] = entry;
            _order.Enqueue(entry);
            Trim(entry.Time);
        }

        public PlayerIdentity Resolve(ZDOID target, float maximumAgeSeconds)
        {
            float now = Time.realtimeSinceStartup;
            Trim(now);
            return _entries.TryGetValue(target, out Entry entry) && now - entry.Time <= maximumAgeSeconds ? entry.Player : null;
        }

        private void Trim(float now)
        {
            while (_order.Count > 0 && (_order.Count > Capacity || now - _order.Peek().Time > RetentionSeconds))
            {
                Entry old = _order.Dequeue();
                if (_entries.TryGetValue(old.Target, out Entry current) && ReferenceEquals(old, current))
                {
                    _entries.Remove(old.Target);
                }
            }
        }
    }
}
