using System.Collections.Generic;

namespace UnsafeEcs.Core.Components.Managed
{
    public class ManagedStorage
    {
        private readonly Dictionary<int, object> poolsByTypeId = new();

        // Type-level cache to avoid dictionary lookups on Get
        private static class PoolCache<T> where T : class
        {
            public static ManagedPool<T> pool;
            public static ManagedStorage owner;
        }

        public ManagedRef<T> Add<T>(T obj) where T : class
        {
            var typeId = ManagedRef<T>.GetTypeId();
            var pool = GetOrCreatePool<T>(typeId);

            var objectId = pool.Add(obj);
            return new ManagedRef<T>
            {
                objectId = objectId,
                version = pool.GetVersion(objectId)
            };
        }

        public T Get<T>(ref ManagedRef<T> refComp) where T : class
        {
            // Check cached pool first
            var pool = PoolCache<T>.pool;
            if (pool == null || PoolCache<T>.owner != this)
            {
                // Cache miss - lookup and cache
                var typeId = ManagedRef<T>.GetTypeId();
                pool = (ManagedPool<T>)poolsByTypeId[typeId];
                PoolCache<T>.pool = pool;
                PoolCache<T>.owner = this;
            }
            return pool.Get(refComp.objectId, refComp.version);
        }

        public void Remove<T>(ManagedRef<T> refComp) where T : class
        {
            var typeId = ManagedRef<T>.GetTypeId();
            if (poolsByTypeId.TryGetValue(typeId, out var pool))
                ((ManagedPool<T>)pool).Remove(refComp.objectId);
        }

        private ManagedPool<T> GetOrCreatePool<T>(int typeId) where T : class
        {
            if (!poolsByTypeId.TryGetValue(typeId, out var pool))
            {
                pool = new ManagedPool<T>();
                poolsByTypeId[typeId] = pool;
            }

            return (ManagedPool<T>)pool;
        }
    }
}