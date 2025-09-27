using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Serialization
{
    public static unsafe class WorldSerializer
    {
        public static byte[] Serialize(World world)
        {
            // Serialize EntityManager first
            var entityManagerData = EntityManagerSerializer.Serialize(world.entityManagerWrapper);

            // Calculate total size needed for world serialization
            var totalSize =
                4 + // magic
                4 + // version
                4 * 4 + // 4 float fields
                entityManagerData.Length; // entity manager data

            // Allocate byte array for the serialized data
            var output = new byte[totalSize];

            fixed (byte* ptr = output)
            {
                var position = 0;

                // Write magic number ("WLD")
                *(int*)(ptr + position) = 0x574C44;
                position += 4;

                // Write version
                *(int*)(ptr + position) = 1;
                position += 4;

                // Write world timing fields
                *(float*)(ptr + position) = world.deltaTime;
                position += 4;

                *(float*)(ptr + position) = world.elapsedDeltaTime;
                position += 4;

                *(float*)(ptr + position) = world.fixedDeltaTime;
                position += 4;

                *(float*)(ptr + position) = world.elapsedFixedDeltaTime;
                position += 4;

                // Write entity manager data size
                *(int*)(ptr + position) = entityManagerData.Length;
                position += 4;

                // Write entity manager data
                fixed (byte* srcPtr = entityManagerData)
                    UnsafeUtility.MemCpy(ptr + position, srcPtr, entityManagerData.Length);

                position += entityManagerData.Length;
            }

            return output;
        }

        public static World Deserialize(MemoryRegion memoryRegion, World world)
        {
            var ptr = memoryRegion.ptr;
            var position = 0;

            // Read magic
            var magic = *(int*)(ptr + position);
            position += 4;

            if (magic != 0x574C44) // "WLD" in hex
                throw new InvalidDataException("Invalid world data format");

            // Read version
            var version = *(int*)(ptr + position);
            position += 4;

            if (version != 1)
                throw new InvalidDataException($"Unsupported world version: {version}");

            // Read world timing fields
            var deltaTime = *(float*)(ptr + position);
            position += 4;

            var elapsedDeltaTime = *(float*)(ptr + position);
            position += 4;

            var fixedDeltaTime = *(float*)(ptr + position);
            position += 4;

            var elapsedFixedDeltaTime = *(float*)(ptr + position);
            position += 4;

            // Read entity manager data size
            var entityManagerSize = *(int*)(ptr + position);
            position += 4;

            // Restore timing fields
            world.deltaTime = deltaTime;
            world.elapsedDeltaTime = elapsedDeltaTime;
            world.fixedDeltaTime = fixedDeltaTime;
            world.elapsedFixedDeltaTime = elapsedFixedDeltaTime;

            world.EntityManager.Clear();

            EntityManagerDeserializer.Deserialize(new MemoryRegion(ptr + position, entityManagerSize), ref world.EntityManager);
            position += entityManagerSize;

            return world;
        }
    }
}