using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    // Union type for storing either a ComponentChunk or BufferComponentChunk
    public unsafe struct ChunkUnion : IDisposable
    {
        public void* chunkPtr; // Pointer to either ComponentChunk or BufferComponentChunk
        public bool isBuffer; // Type discriminator flag

        // Helper methods to safely access the appropriate type
        public ComponentChunk* AsComponentChunk() => isBuffer ? null : (ComponentChunk*)chunkPtr;
        public BufferChunk* AsBufferChunk() => isBuffer ? (BufferChunk*)chunkPtr : null;

        // Factory methods to create instances
        public static ChunkUnion FromComponentChunk(ComponentChunk* chunk)
        {
            return new ChunkUnion
            {
                chunkPtr = chunk,
                isBuffer = false
            };
        }

        public static ChunkUnion FromBufferChunk(BufferChunk* chunk)
        {
            return new ChunkUnion
            {
                chunkPtr = chunk,
                isBuffer = true
            };
        }

        // Check if the union contains a valid chunk
        public bool IsValid => chunkPtr != null;

        // Dispose the contained chunk
        public void Dispose()
        {
            if (chunkPtr != null)
            {
                if (isBuffer)
                {
                    ((BufferChunk*)chunkPtr)->Dispose();
                    UnsafeUtility.Free(chunkPtr, Allocator.Persistent);
                }
                else
                {
                    ((ComponentChunk*)chunkPtr)->Dispose();
                    UnsafeUtility.Free(chunkPtr, Allocator.Persistent);
                }

                chunkPtr = null;
            }
        }
    }
}