using Unity.Burst;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Serialization
{
    public static unsafe class TypeManagerSerializer
    {
        [BurstCompile]
        public static byte[] SerializeTypeInfo()
        {
            // Count the number of registered types
            int count = TypeManager.TypeCount.Data;

            // Calculate size: 4 bytes for count + for each type: 
            // 8 bytes (hash) + 4 bytes (size) + 1 byte (isBuffer)
            int size = 4 + count * 13;

            var output = new byte[size];
            fixed (byte* ptr = output)
            {
                // Write count
                *(int*)ptr = count;

                // Write all type data in registration order
                int position = 4;
                for (int i = 0; i < count; i++)
                {
                    // Write type hash
                    long hash = TypeManager.TypeOrder.Data[i];
                    *(long*)(ptr + position) = hash;
                    position += 8;

                    // Write type size
                    int typeSize = TypeManager.TypeSizes.Data[i];
                    *(int*)(ptr + position) = typeSize;
                    position += 4;

                    // Write isBuffer flag
                    bool isBuffer = TypeManager.IsBufferList.Data[i];
                    *(bool*)(ptr + position) = isBuffer;
                    position += 1;
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
            TypeManager.Clear();

            for (var i = 0; i < count; i++)
            {
                // Read type hash
                var hash = *(long*)(ptr + position);
                position += 8;

                // Read type size
                var typeSize = *(int*)(ptr + position);
                position += 4;

                // Read isBuffer flag
                var isBuffer = *(bool*)(ptr + position);
                position += 1;

                // Register the type in the correct order
                int typeIndex = TypeManager.GetTypeIndexFromHash(hash);

                // Ensure we have enough space in our lists
                while (TypeManager.TypeSizes.Data.Length <= typeIndex)
                {
                    TypeManager.TypeSizes.Data.Add(0);
                }

                // Set the appropriate values
                TypeManager.TypeSizes.Data[typeIndex] = typeSize;
                TypeManager.IsBufferList.Data[typeIndex] = isBuffer;
            }
        }
    }
}