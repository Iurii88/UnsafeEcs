using System;
using System.Collections.Generic;
using Unity.Burst;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Serialization
{
    public static unsafe class EcsSerializer
    {
        private const int MagicNumber = 0x454353; // "ECS" in hex
        private const int Version = 1;

        [BurstCompile]
        public static byte[] Serialize()
        {
            // First serialize component type info
            byte[] typeInfoData = ComponentTypeSerializer.SerializeTypeInfo();

            // Then serialize all worlds
            var worldsData = new List<byte[]>(WorldManager.Worlds.Count);
            foreach (var world in WorldManager.Worlds)
            {
                byte[] worldBytes = WorldSerializer.Serialize(world);
                worldsData.Add(worldBytes);
            }

            // Calculate total size needed
            int totalSize =
                4 + // magic
                4 + // version
                4 + // type info size
                typeInfoData.Length + // type info data
                4; // world count

            // Add sizes of all worlds
            foreach (var worldBytes in worldsData)
            {
                totalSize += 4 + worldBytes.Length; // size + data for each world
            }

            // Allocate output array
            var output = new byte[totalSize];
            int position = 0;

            // Write magic number
            Buffer.BlockCopy(BitConverter.GetBytes(MagicNumber), 0, output, position, 4);
            position += 4;

            // Write version
            Buffer.BlockCopy(BitConverter.GetBytes(Version), 0, output, position, 4);
            position += 4;

            // Write type info size
            Buffer.BlockCopy(BitConverter.GetBytes(typeInfoData.Length), 0, output, position, 4);
            position += 4;

            // Write type info data
            Buffer.BlockCopy(typeInfoData, 0, output, position, typeInfoData.Length);
            position += typeInfoData.Length;

            // Write world count
            Buffer.BlockCopy(BitConverter.GetBytes(worldsData.Count), 0, output, position, 4);
            position += 4;

            // Write each world
            foreach (var worldBytes in worldsData)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(worldBytes.Length), 0, output, position, 4);
                position += 4;

                Buffer.BlockCopy(worldBytes, 0, output, position, worldBytes.Length);
                position += worldBytes.Length;
            }

            return output;
        }

        public static void Deserialize(MemoryRegion memoryRegion)
        {
            var ptr = memoryRegion.ptr;
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

            // Deserialize type info
            ComponentTypeSerializer.DeserializeTypeInfo(new MemoryRegion(ptr + position, typeInfoSize));
            position += typeInfoSize;

            // Read world count
            var worldCount = *(int*)(ptr + position);
            position += 4;

            // Deserialize each world
            for (var i = 0; i < worldCount; i++)
            {
                var worldDataSize = *(int*)(ptr + position);
                position += 4;

                WorldSerializer.Deserialize(new MemoryRegion(ptr + position, worldDataSize), WorldManager.Worlds[i]);
                position += worldDataSize;
            }
        }
    }
}