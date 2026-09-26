using System;
using System.Collections.Generic;

namespace ValheimTelemetry.Tracking
{
    internal sealed class PortalTripDetector
    {
        internal sealed class Portal
        {
            public string Id;
            public string TargetId;
            public string Type;
            public string Tag;
            public float X;
            public float Y;
            public float Z;
        }

        internal sealed class Trip
        {
            public Portal From;
            public Portal To;
        }

        private sealed class PlayerState
        {
            public bool HasPosition;
            public float X;
            public float Y;
            public float Z;
            public float SampledAt;
            public Portal Source;
            public float SourceSeenAt;
            public float LastTripAt = float.NegativeInfinity;
        }

        private const float PortalRadius = 7.5f;
        private const float MinimumJump = 12f;
        private const float MaximumSampleGap = 5f;
        private const float SourceRetention = 10f;
        private const float TripCooldown = 5f;
        private readonly Dictionary<long, PlayerState> _players = new Dictionary<long, PlayerState>();

        public Trip Observe(long peerId, float x, float y, float z, float now, IList<Portal> portals)
        {
            if (peerId == 0L || portals == null) return null;
            if (!_players.TryGetValue(peerId, out PlayerState state))
            {
                if (_players.Count >= 64) _players.Clear();
                state = new PlayerState();
                _players[peerId] = state;
            }

            Portal destination = FindNearest(x, y, z, portals, PortalRadius);
            Trip trip = null;
            if (state.HasPosition
                && state.Source != null
                && now - state.SampledAt <= MaximumSampleGap
                && now - state.SourceSeenAt <= SourceRetention
                && now - state.LastTripAt >= TripCooldown
                && DistanceSquared(state.X, state.Y, state.Z, x, y, z) >= MinimumJump * MinimumJump
                && destination != null
                && string.Equals(state.Source.TargetId, destination.Id, StringComparison.Ordinal))
            {
                trip = new Trip { From = state.Source, To = destination };
                state.LastTripAt = now;
            }

            state.HasPosition = true;
            state.X = x;
            state.Y = y;
            state.Z = z;
            state.SampledAt = now;
            state.Source = IsConnected(destination, portals) ? destination : null;
            state.SourceSeenAt = state.Source == null ? float.NegativeInfinity : now;
            return trip;
        }

        public void Remove(long peerId) => _players.Remove(peerId);

        private static bool IsConnected(Portal portal, IList<Portal> portals)
        {
            if (portal == null || string.IsNullOrEmpty(portal.TargetId)) return false;
            for (int i = 0; i < portals.Count; i++)
            {
                if (string.Equals(portals[i]?.Id, portal.TargetId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static Portal FindNearest(float x, float y, float z, IList<Portal> portals, float radius)
        {
            Portal nearest = null;
            float nearestDistance = radius * radius;
            for (int i = 0; i < portals.Count; i++)
            {
                Portal portal = portals[i];
                if (portal == null) continue;
                float distance = DistanceSquared(x, y, z, portal.X, portal.Y, portal.Z);
                if (distance <= nearestDistance)
                {
                    nearest = portal;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        private static float DistanceSquared(float ax, float ay, float az, float bx, float by, float bz)
        {
            float dx = ax - bx;
            float dy = ay - by;
            float dz = az - bz;
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
