// using System;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using UnsafeEcs.Core.Components;
// using UnsafeEcs.Core.DynamicBuffers;
// using UnsafeEcs.Core.Utils;
//
// namespace UnsafeEcs.Core.Entities
// {
//     /// <summary>
//     /// Manages entity lifecycle and component storage
//     /// </summary>
//     public unsafe partial struct EntityManager
//     {
//         /// <summary>
//         /// Migrates an entity from source manager to this manager, copying all components and buffers
//         /// </summary>
//         /// <param name="sourceEntity">Entity to migrate</param>
//         /// <param name="sourceManager">Source entity manager</param>
//         /// <returns>Newly created entity in this manager</returns>
//         public Entity MigrateEntity(Entity sourceEntity, ReferenceWrapper<EntityManager> sourceManager)
//         {
//             if (!sourceManager.Value.IsEntityAlive(sourceEntity))
//                 throw new InvalidOperationException($"Source entity {sourceEntity} is not alive in source manager");
//
//             // Create new entity in destination
//             var destEntity = CreateEntity();
//
//             // Get source archetype
//             ref var sourceArchetype = ref sourceManager.Value.entityArchetypes.Ptr[sourceEntity.id];
//
//             // Copy all components
//             foreach (var componentIndex in sourceArchetype.componentBits)
//             {
//                 MigrateComponent(sourceEntity, destEntity, componentIndex, sourceManager);
//             }
//
//             return destEntity;
//         }
//
//         /// <summary>
//         /// Migrates multiple entities from source manager to this manager
//         /// </summary>
//         /// <param name="sourceEntities">Entities to migrate</param>
//         /// <param name="sourceManager">Source entity manager</param>
//         /// <returns>Array of new entities in this manager</returns>
//         public NativeArray<Entity> MigrateEntities(NativeArray<Entity> sourceEntities, ReferenceWrapper<EntityManager> sourceManager)
//         {
//             var destEntities = new NativeArray<Entity>(sourceEntities.Length, Allocator.Temp);
//
//             for (var i = 0; i < sourceEntities.Length; i++)
//             {
//                 destEntities[i] = MigrateEntity(sourceEntities[i], sourceManager);
//             }
//
//             return destEntities;
//         }
//
//         /// <summary>
//         /// Internal method to migrate a regular component between managers
//         /// </summary>
//         private void MigrateComponent(Entity sourceEntity, Entity destEntity, int componentIndex, ReferenceWrapper<EntityManager> sourceManager)
//         {
//             ref var srcManager = ref sourceManager.Value;
//
//             if (srcManager.bufferChunks.ContainsKey(componentIndex))
//             {
//                 MigrateBufferComponent(sourceEntity, destEntity, componentIndex, sourceManager);
//                 return;
//             }
//
//             if (!srcManager.componentChunks.ContainsKey(componentIndex))
//                 return;
//
//             var sourceChunk = srcManager.componentChunks[componentIndex];
//
//             if (!sourceChunk.entityToIndex.TryGetValue(sourceEntity.id, out var sourceIndex))
//                 return;
//
//             // Get source component data
//             var srcPtr = (byte*)sourceChunk.ptr + sourceIndex * sourceChunk.componentSize;
//
//             // Ensure destination chunk exists
//             if (!componentChunks.TryGetValue(componentIndex, out var destChunk))
//             {
//                 destChunk = new ComponentChunk(sourceChunk.componentSize, InitialEntityCapacity);
//                 componentChunks[componentIndex] = destChunk;
//             }
//
//             // Resize if needed
//             if (destChunk.length >= destChunk.capacity)
//                 destChunk.Resize(destChunk.capacity * 2);
//
//             // Copy data
//             var dstPtr = (byte*)destChunk.ptr + destChunk.length * destChunk.componentSize;
//             UnsafeUtility.MemCpy(dstPtr, srcPtr, sourceChunk.componentSize);
//
//             // Update indices
//             destChunk.entityToIndex.Add(destEntity.id, destChunk.length);
//             destChunk.indexToEntity.Add(destChunk.length, destEntity.id);
//             destChunk.length++;
//
//             // Update archetype
//             ref var archetype = ref entityArchetypes.Ptr[destEntity.id];
//             archetype.componentBits.SetComponent(componentIndex);
//
//             // Update version
//             IncrementComponentVersion(componentIndex);
//             componentChunks[componentIndex] = destChunk;
//         }
//
//         /// <summary>
//         /// Internal method to migrate a buffer component between managers
//         /// </summary>
//         private void MigrateBufferComponent(Entity sourceEntity, Entity destEntity, int componentIndex, ReferenceWrapper<EntityManager> sourceManager)
//         {
//             ref var srcManager = ref sourceManager.Value;
//             var sourceChunk = srcManager.bufferChunks[componentIndex];
//
//             if (!sourceChunk.entityToIndex.TryGetValue(sourceEntity.id, out var sourceIndex))
//                 return;
//
//             // Ensure destination chunk exists
//             if (!bufferChunks.TryGetValue(componentIndex, out var destChunk))
//             {
//                 destChunk = new BufferComponentChunk(sourceChunk.elementSize, InitialEntityCapacity);
//                 bufferChunks[componentIndex] = destChunk;
//             }
//
//             // Resize if needed
//             if (destChunk.length >= destChunk.capacity)
//                 destChunk.Resize(destChunk.capacity * 2);
//
//             // Initialize destination buffer
//             destChunk.InitializeBuffer(destChunk.length);
//
//             // Get buffer headers
//             var sourceHeader = (BufferHeader*)(sourceChunk.ptr + sourceIndex * sourceChunk.headerSize);
//             var destHeader = (BufferHeader*)(destChunk.ptr + destChunk.length * destChunk.headerSize);
//
//             // Copy buffer contents
//             if (sourceHeader->length > 0)
//             {
//                 var capacity = Math.Max(sourceHeader->length, sourceHeader->capacity);
//                 destHeader->pointer = (byte*)UnsafeUtility.Malloc(
//                     capacity * sourceChunk.elementSize,
//                     UnsafeUtility.AlignOf<int>(),
//                     Allocator.Persistent
//                 );
//                 destHeader->capacity = capacity;
//                 destHeader->length = sourceHeader->length;
//
//                 if (sourceHeader->pointer != null)
//                 {
//                     UnsafeUtility.MemCpy(
//                         destHeader->pointer,
//                         sourceHeader->pointer,
//                         sourceHeader->length * sourceChunk.elementSize
//                     );
//                 }
//             }
//
//             // Update indices
//             destChunk.entityToIndex.Add(destEntity.id, destChunk.length);
//             destChunk.indexToEntity.Add(destChunk.length, destEntity.id);
//             destChunk.length++;
//
//             // Update archetype
//             ref var archetype = ref entityArchetypes.Ptr[destEntity.id];
//             archetype.componentBits.SetComponent(componentIndex);
//
//             // Update version
//             IncrementComponentVersion(componentIndex);
//             bufferChunks[componentIndex] = destChunk;
//         }
//
//         /// <summary>
//         /// Creates an exact copy of an entity within the same manager
//         /// </summary>
//         /// <param name="sourceEntity">Entity to clone</param>
//         /// <returns>New cloned entity</returns>
//         public Entity CloneEntity(Entity sourceEntity)
//         {
//             if (!IsEntityAlive(sourceEntity))
//                 throw new InvalidOperationException($"Source entity {sourceEntity} is not alive");
//
//             var newEntity = CreateEntity();
//             ref var sourceArchetype = ref entityArchetypes.Ptr[sourceEntity.id];
//
//             foreach (var componentIndex in sourceArchetype.componentBits)
//             {
//                 if (componentChunks.ContainsKey(componentIndex))
//                 {
//                     var chunk = componentChunks[componentIndex];
//                     if (chunk.entityToIndex.TryGetValue(sourceEntity.id, out var sourceIndex))
//                     {
//                         if (chunk.length >= chunk.capacity)
//                             chunk.Resize(chunk.capacity * 2);
//
//                         var srcPtr = (byte*)chunk.ptr + sourceIndex * chunk.componentSize;
//                         var dstPtr = (byte*)chunk.ptr + chunk.length * chunk.componentSize;
//                         UnsafeUtility.MemCpy(dstPtr, srcPtr, chunk.componentSize);
//
//                         chunk.entityToIndex.Add(newEntity.id, chunk.length);
//                         chunk.indexToEntity.Add(chunk.length, newEntity.id);
//                         chunk.length++;
//
//                         componentChunks[componentIndex] = chunk;
//                     }
//                 }
//                 else if (bufferChunks.ContainsKey(componentIndex))
//                 {
//                     var chunk = bufferChunks[componentIndex];
//                     if (chunk.entityToIndex.TryGetValue(sourceEntity.id, out var sourceIndex))
//                     {
//                         if (chunk.length >= chunk.capacity)
//                             chunk.Resize(chunk.capacity * 2);
//
//                         chunk.InitializeBuffer(chunk.length);
//
//                         var sourceHeader = (BufferHeader*)(chunk.ptr + sourceIndex * chunk.headerSize);
//                         var destHeader = (BufferHeader*)(chunk.ptr + chunk.length * chunk.headerSize);
//
//                         if (sourceHeader->length > 0)
//                         {
//                             var capacity = Math.Max(sourceHeader->length, sourceHeader->capacity);
//                             destHeader->pointer = (byte*)UnsafeUtility.Malloc(
//                                 capacity * chunk.elementSize,
//                                 UnsafeUtility.AlignOf<int>(),
//                                 Allocator.Persistent
//                             );
//                             destHeader->capacity = capacity;
//                             destHeader->length = sourceHeader->length;
//
//                             if (sourceHeader->pointer != null)
//                             {
//                                 UnsafeUtility.MemCpy(
//                                     destHeader->pointer,
//                                     sourceHeader->pointer,
//                                     sourceHeader->length * chunk.elementSize
//                                 );
//                             }
//                         }
//
//                         chunk.entityToIndex.Add(newEntity.id, chunk.length);
//                         chunk.indexToEntity.Add(chunk.length, newEntity.id);
//                         chunk.length++;
//
//                         bufferChunks[componentIndex] = chunk;
//                     }
//                 }
//             }
//             
//             foreach (var componentIndex in sourceArchetype.componentBits)
//             {
//                 IncrementComponentVersion(componentIndex);
//             }
//
//             return newEntity;
//         }
//
//         /// <summary>
//         /// Creates copies of multiple entities within the same manager
//         /// </summary>
//         /// <param name="sourceEntities">Entities to clone</param>
//         /// <returns>Array of cloned entities</returns>
//         public NativeArray<Entity> CloneEntities(NativeArray<Entity> sourceEntities)
//         {
//             var destEntities = new NativeArray<Entity>(sourceEntities.Length, Allocator.Temp);
//
//             for (var i = 0; i < sourceEntities.Length; i++)
//             {
//                 destEntities[i] = CloneEntity(sourceEntities[i]);
//             }
//
//             return destEntities;
//         }
//     }
// }