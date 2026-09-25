using System.Collections.Generic;
using UnityEngine;
using ValheimTelemetry.Config;
using ValheimTelemetry.Telemetry;
using ValheimTelemetry.Util;

namespace ValheimTelemetry.Tracking
{
    internal sealed class LifecycleTracker
    {
        private sealed class PendingCreate
        {
            public ZDOID Id;
            public float Due;
            public int Attempts;
        }

        private sealed class RecentCreate
        {
            public ZDOID Id;
            public float Time;
        }

        private readonly RecentHitTracker _hits = new RecentHitTracker();
        private readonly MobTracker _mobs;
        private readonly PieceTracker _pieces;
        private readonly PortalTracker _portals;
        private readonly ShipTracker _ships;
        private readonly TameTracker _tames;
        private readonly TreeTracker _trees;
        private readonly Queue<PendingCreate> _pending = new Queue<PendingCreate>();
        private readonly Dictionary<ZDOID, float> _recentCreateTimes = new Dictionary<ZDOID, float>();
        private readonly Queue<RecentCreate> _recentCreateOrder = new Queue<RecentCreate>();
        private readonly BoundedEventCache _zeroHealth = new BoundedEventCache(16384);
        private readonly BoundedTimedCache _recentDamage = new BoundedTimedCache(16384, 60f);

        public LifecycleTracker(PluginConfig config, ITelemetrySink sink)
        {
            _mobs = new MobTracker(config, sink, _hits);
            _pieces = new PieceTracker(config, sink, _hits);
            _portals = new PortalTracker(config, sink, _hits);
            _ships = new ShipTracker(config, sink, _hits);
            _tames = new TameTracker(config, sink);
            _trees = new TreeTracker(config, sink, _hits);
        }

        public void Created(ZDO zdo)
        {
            if (zdo == null || _pending.Count >= 16384) return;
            float now = Time.realtimeSinceStartup;
            _pending.Enqueue(new PendingCreate { Id = zdo.m_uid, Due = now + 0.5f, Attempts = 0 });
            _recentCreateTimes[zdo.m_uid] = now;
            _recentCreateOrder.Enqueue(new RecentCreate { Id = zdo.m_uid, Time = now });
            TrimRecentCreates(now);
        }

        public void Tick(float now)
        {
            TrimRecentCreates(now);
            _trees.Tick(now);
            int available = _pending.Count;
            for (int i = 0; i < available; i++)
            {
                PendingCreate pending = _pending.Dequeue();
                if (pending.Due > now)
                {
                    _pending.Enqueue(pending);
                    continue;
                }
                ZDO zdo = ZDOMan.instance?.GetZDO(pending.Id);
                PrefabInfo info = PrefabUtil.Describe(zdo);
                if (zdo == null) continue;
                if (info == null && pending.Attempts++ < 8)
                {
                    pending.Due = now + 0.25f;
                    _pending.Enqueue(pending);
                    continue;
                }
                if (info == null) continue;
                if (info.IsTreeBase) _trees.StandingTreeCreated(zdo.GetPosition(), now);
                _mobs.Spawned(zdo, info);
                _portals.Built(zdo, info);
                _ships.Built(zdo, info);
                _pieces.Built(zdo, info);
            }
        }

        public void Destroyed(ZDO zdo)
        {
            if (zdo == null) return;
            PrefabInfo info = PrefabUtil.Describe(zdo);
            if (info == null) return;
            if (info.IsTreeBase)
            {
                _trees.Felled(zdo, info);
            }
            else if (info.IsTreeSapling)
            {
                _trees.SaplingDestroyed(zdo, info);
            }
            else if (info.IsMob && IsProbableMobDeath(zdo))
            {
                _mobs.Killed(zdo, info);
            }
            _portals.Destroyed(zdo, info);
            _ships.Destroyed(zdo, info);
            _pieces.Destroyed(zdo, info);
        }

        public void HealthChanged(ZDO zdo, bool hadOldHealth, float oldHealth)
        {
            if (zdo == null) return;
            float health = zdo.GetFloat(ZDOVars.s_health, float.PositiveInfinity);
            if (IsRecentlyCreated(zdo.m_uid, 5f)) return;
            if (!float.IsPositiveInfinity(health) && (!hadOldHealth || health < oldHealth))
            {
                _recentDamage.Touch(zdo.m_uid);
            }
            if (health > 0f || (hadOldHealth && oldHealth <= 0f)) return;
            _zeroHealth.Add(zdo.m_uid);
            PrefabInfo info = PrefabUtil.Describe(zdo);
            if (info == null) return;
            if (info.IsTreeBase) _trees.Felled(zdo, info);
            else if (info.IsMob) _mobs.Killed(zdo, info);
        }

        public void TamedChanged(ZDO zdo, bool oldTamed)
        {
            if (zdo == null || oldTamed || !zdo.GetBool(ZDOVars.s_tamed, false) || IsRecentlyCreated(zdo.m_uid, 5f)) return;
            _tames.Tamed(zdo, PrefabUtil.Describe(zdo));
        }

        public void PortalTagChanged(ZDO zdo, string oldTag)
        {
            if (zdo == null || IsRecentlyCreated(zdo.m_uid, 5f)) return;
            _portals.TagChanged(zdo, PrefabUtil.Describe(zdo), oldTag);
        }

        public void RoutedDamage(ZDOID target, HitData hit) => _hits.Record(target, hit);

        private bool IsProbableMobDeath(ZDO zdo)
        {
            if (_zeroHealth.Contains(zdo.m_uid) || zdo.GetFloat(ZDOVars.s_health, float.PositiveInfinity) <= 0f)
            {
                return true;
            }
            if (_recentDamage.ContainsRecent(zdo.m_uid, 30f))
            {
                return true;
            }

            // These vanilla flags have explicit non-death destroy paths. Require
            // health evidence for them; ordinary persistent creatures have no
            // vanilla unload-time ZDO destruction and are therefore deaths.
            return !zdo.GetBool(ZDOVars.s_despawnInDay, false)
                && !zdo.GetBool(ZDOVars.s_eventCreature, false)
                && zdo.GetInt(ZDOVars.s_maxInstances, 0) <= 0;
        }

        private bool IsRecentlyCreated(ZDOID id, float seconds)
        {
            return _recentCreateTimes.TryGetValue(id, out float time) && Time.realtimeSinceStartup - time <= seconds;
        }

        private void TrimRecentCreates(float now)
        {
            while (_recentCreateOrder.Count > 0 && (_recentCreateOrder.Count > 16384 || now - _recentCreateOrder.Peek().Time > 30f))
            {
                RecentCreate old = _recentCreateOrder.Dequeue();
                if (_recentCreateTimes.TryGetValue(old.Id, out float current) && current == old.Time)
                {
                    _recentCreateTimes.Remove(old.Id);
                }
            }
        }
    }
}
