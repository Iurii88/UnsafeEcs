using Unity.Burst;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Serialization
{
    public static unsafe class ComponentTypeSerializer
    {
        [BurstCompile]
        public static byte[] SerializeTypeInfo()
        {
            // Count the number of registered types
            int count = ComponentTypeManager.TypeCount.Data;
            int size = 4 + count * 8; // 4 bytes for count, 8 bytes per type (hash)

            var output = new byte[size];
            fixed (byte* ptr = output)
            {
                // Write count
                *(int*)ptr = count;

                // Write all type hashes in registration order
                int position = 4;
                for (int i = 0; i < count; i++)
                {
                    long hash = ComponentTypeManager.TypeOrder.Data[i];
                    *(long*)(ptr + position) = hash;
                    position += 8;
                }
            }

            return output;
        }

        [BurstCompile]
        public static void DeserializeTypeInfo(MemoryRegion memoryRegion)
        {
            byte* ptr = memoryRegion.ptr;

            int count = *(int*)ptr;
            int position = 4;

            // Clear existing types
            ComponentTypeManager.Clear();

            for (var i = 0; i < count; i++)
            {
                var hash = *(long*)(ptr + position);
                position += 8;

                // This will register the type in the correct order
                ComponentTypeManager.GetTypeIndexFromHash(hash);
            }
        }
    }
}