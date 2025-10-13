using System.Collections.Generic;

namespace UnsafeEcs.Core.Components.Managed
{
    public class ManagedPool<T> where T : class
    {
        private readonly List<T> objects = new();
        private readonly List<int> versions = new();
        private readonly Stack<int> freeIndices = new();

        public int Add(T obj)
        {
            int index;
            if (freeIndices.Count > 0)
            {
                index = freeIndices.Pop();
                objects[index] = obj;
                versions[index]++;
            }
            else
            {
                index = objects.Count;
                objects.Add(obj);
                versions.Add(0);
            }

            return index;
        }

        public T Get(int objectId, int version)
        {
            if (objectId < 0 || objectId >= objects.Count)
                return null;

            if (versions[objectId] != version)
                return null;

            return objects[objectId];
        }

        public int GetVersion(int objectId)
        {
            return versions[objectId];
        }

        public void Remove(int objectId)
        {
            if (objectId < 0 || objectId >= objects.Count)
                return;

            objects[objectId] = null;
            versions[objectId]++;
            freeIndices.Push(objectId);
        }
    }
}