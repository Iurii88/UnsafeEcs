using System.Collections.Generic;

namespace UnsafeEcs.Core.Components.Managed
{
    public class ManagedStorage
    {
        private readonly Dictionary<int, object> poolsByTypeId = new();
        private readonly int storageId;

        public ManagedStorage()
        {
            storageId = ManagedStorageRegistry.Register(this);
        }

        ~ManagedStorage()
        {
            ManagedStorageRegistry.Unregister(storageId);
        }

        public ManagedRef<T> Add<T>(T obj) where T : class
        {
            var typeId = ManagedRef<T>.GetTypeId();
            var pool = GetOrCreatePool<T>(typeId);

            var objectId = pool.Add(obj);
            return new ManagedRef<T>
            {
                objectId = objectId,
                version = pool.GetVersion(objectId),
                storageId = storageId
            };
        }

        public T Get<T>(ref ManagedRef<T> refComp) where T : class
        {
            var typeId = ManagedRef<T>.GetTypeId();
            var pool = (ManagedPool<T>)poolsByTypeId[typeId];
            return pool.Get(refComp.objectId, refComp.version);
        }

        /// <summary>
        /// Gets an object by type ID, object ID, and version. Used by editor tools.
        /// </summary>
        public object GetByTypeId(int typeId, int objectId, int version)
        {
            if (!poolsByTypeId.TryGetValue(typeId, out var pool))
                return null;

            // Use reflection to call Get on the pool
            var poolType = pool.GetType();
            var getMethod = poolType.GetMethod("Get");
            if (getMethod == null)
                return null;

            return getMethod.Invoke(pool, new object[] { objectId, version });
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