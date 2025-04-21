using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Serialization
{
    public static unsafe class EcsSerializer
    {
        private const int MagicNumber = 0x454353; // "ECS" in hex
        private const int Version = 1;

        [BurstCompile]
        public static NativeArray<byte> Serialize(Allocator allocator = Allocator.TempJob)
        {
            // First serialize component type info
            var typeInfoData = ComponentTypeSerializer.SerializeTypeInfo(Allocator.Temp);

            // Then serialize all worlds
            var worldsData = new NativeList<NativeArray<byte>>(WorldManager.Worlds.Count, Allocator.Temp);
            foreach (var world in WorldManager.Worlds)
            {
                var worldBytes = WorldSerializer.Serialize(world);
                var nativeBytes = new NativeArray<byte>(worldBytes.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                UnsafeUtility.MemCpy(nativeBytes.GetUnsafePtr(), UnsafeUtility.AddressOf(ref worldBytes[0]), worldBytes.Length);
                worldsData.Add(nativeBytes);
            }

            // Calculate total size needed
            int totalSize =
                4 + // magic
                4 + // version
                4 + // type info size
                typeInfoData.Length + // type info data
                4; // world count

            // Add sizes of all worlds
            for (int i = 0; i < worldsData.Length; i++)
            {
                totalSize += 4 + worldsData[i].Length; // size + data for each world
            }

            // Allocate output array
            var output = new NativeArray<byte>(totalSize, allocator, NativeArrayOptions.UninitializedMemory);
            byte* ptr = (byte*)output.GetUnsafePtr();
            int position = 0;

            // Write magic number
            *(int*)(ptr + position) = MagicNumber;
            position += 4;

            // Write version
            *(int*)(ptr + position) = Version;
            position += 4;

            // Write type info size
            *(int*)(ptr + position) = typeInfoData.Length;
            position += 4;

            // Write type info data
            UnsafeUtility.MemCpy(ptr + position, typeInfoData.GetUnsafePtr(), typeInfoData.Length);
            position += typeInfoData.Length;

            // Write world count
            *(int*)(ptr + position) = worldsData.Length;
            position += 4;

            // Write each world
            for (int i = 0; i < worldsData.Length; i++)
            {
                var worldData = worldsData[i];
                *(int*)(ptr + position) = worldData.Length;
                position += 4;

                UnsafeUtility.MemCpy(ptr + position, worldData.GetUnsafePtr(), worldData.Length);
                position += worldData.Length;

                worldData.Dispose();
            }

            // Cleanup
            typeInfoData.Dispose();
            worldsData.Dispose();

            return output;
        }

        public static void Deserialize(byte[] data)
        {
            fixed (byte* ptr = data)
            {
                int position = 0;

                // Read magic number
                int magic = *(int*)(ptr + position);
                position += 4;

                if (magic != MagicNumber)
                    throw new InvalidOperationException("Invalid data format");

                // Read version
                int version = *(int*)(ptr + position);
                position += 4;

                if (version != Version)
                    throw new InvalidOperationException($"Unsupported version: {version}");

                // Read type info size
                int typeInfoSize = *(int*)(ptr + position);
                position += 4;

                // Extract type info data
                var typeInfoData = new NativeArray<byte>(typeInfoSize, Allocator.Temp);
                UnsafeUtility.MemCpy(typeInfoData.GetUnsafePtr(), ptr + position, typeInfoSize);
                position += typeInfoSize;

                // Deserialize type info
                ComponentTypeSerializer.DeserializeTypeInfo(typeInfoData);
                typeInfoData.Dispose();

                // Read world count
                var worldCount = *(int*)(ptr + position);
                position += 4;

                // Deserialize each world
                for (var i = 0; i < worldCount; i++)
                {
                    var worldDataSize = *(int*)(ptr + position);
                    position += 4;

                    var worldDataArray = new byte[worldDataSize];
                    fixed (byte* destPtr = worldDataArray)
                        Buffer.MemoryCopy(ptr + position, destPtr, worldDataSize, worldDataSize);

                    position += worldDataSize;
                    WorldSerializer.Deserialize(worldDataArray, WorldManager.Worlds[i]);
                }
            }
        }
    }
}