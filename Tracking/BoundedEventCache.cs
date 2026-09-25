using System.Collections.Generic;

namespace ValheimTelemetry.Tracking
{
    internal sealed class BoundedEventCache
    {
        private readonly int _capacity;
        private readonly HashSet<ZDOID> _set = new HashSet<ZDOID>();
        private readonly Queue<ZDOID> _order = new Queue<ZDOID>();

        public BoundedEventCache(int capacity)
        {
            _capacity = capacity;
        }

        public bool Add(ZDOID id)
        {
            if (!_set.Add(id))
            {
                return false;
            }
            _order.Enqueue(id);
            while (_order.Count > _capacity)
            {
                _set.Remove(_order.Dequeue());
            }
            return true;
        }

        public bool Contains(ZDOID id) => _set.Contains(id);
    }
}
