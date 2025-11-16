using System.Collections.Generic;

namespace UnsafeEcs.Core.Components.Managed
{
    internal static class ManagedStorageRegistry
    {
        private static int m_nextId = 1;
        private static readonly Dictionary<int, ManagedStorage> Storages = new();

        public static int Register(ManagedStorage storage)
        {
            var id = m_nextId++;
            Storages[id] = storage;
            return id;
        }

        public static void Unregister(int id)
        {
            Storages.Remove(id);
        }

        public static ManagedStorage Get(int id)
        {
            return Storages[id];
        }
    }
}