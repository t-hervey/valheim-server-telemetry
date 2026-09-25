using System.Collections.Generic;
using UnityEngine;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class TreeTracker
    {
        private sealed class PendingSapling
        {
            public ZDOID Id;
            public string Type;
            public string DisplayName;
            public Vector3 Position;
            public PlayerIdentity Player;
            public float Due;
            public bool ReplacedByGrownTree;
        }

        private sealed class RecentTree
        {
            public Vector3 Position;
            public float Time;
        }

        private readonly PluginConfig _config;
        private readonly ITelemetrySink _sink;
        private readonly RecentHitTracker _hits;
        private readonly BoundedEventCache _felled = new BoundedEventCache(8192);
        private readonly Queue<PendingSapling> _pendingSaplings = new Queue<PendingSapling>();
        private readonly Queue<RecentTree> _recentTrees = new Queue<RecentTree>();
        private const float SaplingDecisionDelaySeconds = 3f;
        private const float GrowthMatchDistance = 2.5f;

        public TreeTracker(PluginConfig config, ITelemetrySink sink, RecentHitTracker hits)
        {
            _config = config; _sink = sink; _hits = hits;
        }

        public void Felled(ZDO zdo, PrefabInfo info)
        {
            if (!_config.Trees || info == null || !info.IsTreeBase || !_felled.Add(zdo.m_uid)) return;
            Emit(info.Type, info.DisplayName, zdo.GetPosition(), _hits.Resolve(zdo.m_uid, 0.75f));
        }

        public void SaplingDestroyed(ZDO zdo, PrefabInfo info)
        {
            if (!_config.Trees || info == null || !info.IsTreeSapling || _felled.Contains(zdo.m_uid)) return;
            float now = Time.realtimeSinceStartup;
            TrimRecentTrees(now);
            var pending = new PendingSapling
            {
                Id = zdo.m_uid,
                Type = info.Type,
                DisplayName = info.DisplayName,
                Position = zdo.GetPosition(),
                Player = _hits.Resolve(zdo.m_uid, 0.75f),
                Due = now + SaplingDecisionDelaySeconds,
                ReplacedByGrownTree = HasRecentTreeAt(zdo.GetPosition(), now)
            };
            _pendingSaplings.Enqueue(pending);
            while (_pendingSaplings.Count > 1024)
            {
                EmitPending(_pendingSaplings.Dequeue());
            }
        }

        public void StandingTreeCreated(Vector3 position, float now)
        {
            _recentTrees.Enqueue(new RecentTree { Position = position, Time = now });
            TrimRecentTrees(now);
            foreach (PendingSapling pending in _pendingSaplings)
            {
                if (Vector3.Distance(pending.Position, position) <= GrowthMatchDistance)
                {
                    pending.ReplacedByGrownTree = true;
                }
            }
        }

        public void Tick(float now)
        {
            TrimRecentTrees(now);
            int count = _pendingSaplings.Count;
            for (int i = 0; i < count; i++)
            {
                PendingSapling pending = _pendingSaplings.Dequeue();
                if (pending.Due > now)
                {
                    _pendingSaplings.Enqueue(pending);
                    continue;
                }
                EmitPending(pending);
            }
        }

        private void EmitPending(PendingSapling pending)
        {
            if (pending.ReplacedByGrownTree || !_felled.Add(pending.Id)) return;
            Emit(pending.Type, pending.DisplayName, pending.Position, pending.Player);
        }

        private void Emit(string type, string displayName, Vector3 position, PlayerIdentity player)
        {
            var telemetryEvent = new TelemetryEvent()
                .Add("tree_type", type)
                .Add("tree_display_name", displayName);
            PlayerUtil.Add(telemetryEvent, player, _config);
            ZdoUtil.AddPosition(telemetryEvent, position, _config);
            _sink.Emit("tree_felled", telemetryEvent);
        }

        private bool HasRecentTreeAt(Vector3 position, float now)
        {
            foreach (RecentTree tree in _recentTrees)
            {
                if (now - tree.Time <= 5f && Vector3.Distance(position, tree.Position) <= GrowthMatchDistance)
                {
                    return true;
                }
            }
            return false;
        }

        private void TrimRecentTrees(float now)
        {
            while (_recentTrees.Count > 0 && (_recentTrees.Count > 1024 || now - _recentTrees.Peek().Time > 10f))
            {
                _recentTrees.Dequeue();
            }
        }
    }
}
