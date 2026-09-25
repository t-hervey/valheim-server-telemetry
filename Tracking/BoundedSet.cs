using System;
using System.Collections.Generic;

namespace ValheimTelemetry.Tracking
{
    internal sealed class BoundedSet<T>
    {
        private readonly int _capacity;
        private readonly HashSet<T> _set = new HashSet<T>();
        private readonly Queue<T> _order = new Queue<T>();

        public BoundedSet(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public bool Add(T value)
        {
            if (!_set.Add(value))
            {
                return false;
            }
            _order.Enqueue(value);
            while (_order.Count > _capacity)
            {
                _set.Remove(_order.Dequeue());
            }
            return true;
        }

        public bool Contains(T value) => _set.Contains(value);
    }
}
