using System.Runtime.CompilerServices;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Core.Components.Managed
{
    public struct ManagedRef<T> : IComponent where T : class
    {
        public int objectId;
        public int version;
        public int storageId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            var storage = ManagedStorageRegistry.Get(storageId);
            return storage.Get(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeId()
        {
            return ManagedTypeManager.GetTypeIndex<T>();
        }
    }
}