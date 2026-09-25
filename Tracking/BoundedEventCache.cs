namespace ValheimTelemetry.Tracking
{
    internal sealed class BoundedEventCache
    {
        private readonly BoundedSet<ZDOID> _ids;

        public BoundedEventCache(int capacity)
        {
            _ids = new BoundedSet<ZDOID>(capacity);
        }

        public bool Add(ZDOID id) => _ids.Add(id);

        public bool Contains(ZDOID id) => _ids.Contains(id);
    }
}
