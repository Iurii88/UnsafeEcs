using System.Diagnostics;
using System.IO;
using Unity.Collections;
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
                UnsafeUtility.MemCpy(ptr + position, entityManagerData.GetUnsafePtr(), entityManagerData.Length);
                position += entityManagerData.Length;
            }

            entityManagerData.Dispose();

            return output;
        }

        public static World Deserialize(byte[] data, World world)
        {
            fixed (byte* ptr = data)
            {
                int position = 0;

                // Read magic
                int magic = *(int*)(ptr + position);
                position += 4;

                if (magic != 0x574C44) // "WLD" in hex
                    throw new InvalidDataException("Invalid world data format");

                // Read version
                int version = *(int*)(ptr + position);
                position += 4;

                if (version != 1)
                    throw new InvalidDataException($"Unsupported world version: {version}");

                // Read world timing fields
                float deltaTime = *(float*)(ptr + position);
                position += 4;

                float elapsedDeltaTime = *(float*)(ptr + position);
                position += 4;

                float fixedDeltaTime = *(float*)(ptr + position);
                position += 4;

                float elapsedFixedDeltaTime = *(float*)(ptr + position);
                position += 4;

                // Read entity manager data size
                int entityManagerSize = *(int*)(ptr + position);
                position += 4;

                // Restore timing fields
                world.deltaTime = deltaTime;
                world.elapsedDeltaTime = elapsedDeltaTime;
                world.fixedDeltaTime = fixedDeltaTime;
                world.elapsedFixedDeltaTime = elapsedFixedDeltaTime;

                world.EntityManager.Clear();
                var entityManagerData = new NativeArray<byte>(entityManagerSize, Allocator.TempJob);
                UnsafeUtility.MemCpy(entityManagerData.GetUnsafePtr(), ptr + position, entityManagerSize);
                
                EntityManagerDeserializer.Deserialize(entityManagerData, ref world.EntityManager);
                
                entityManagerData.Dispose();
                
                return world;
            }
        }
    }
}