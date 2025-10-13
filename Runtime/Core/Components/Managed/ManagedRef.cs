using System.Runtime.CompilerServices;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Core.Components.Managed
{
    public struct ManagedRef<T> : IComponent where T : class
    {
        public int objectId;
        public int version;

        public T Get(ManagedStorage managedStorage)
        {
            return managedStorage.Get(ref this);
        }

        public T Get(World world)
        {
            return world.managedStorage.Get(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeId()
        {
            return ManagedTypeManager.GetTypeIndex<T>();
        }
    }
}