// using System;
// using Unity.Collections;
// using UnsafeEcs.Core.Utils;
// using UnsafeEcs.Core.Worlds;
//
// namespace UnsafeEcs.Core.Entities
// {
//     /// <summary>
//     /// Provides utility methods for entity management operations
//     /// </summary>
//     public static class EntityMigration
//     {
//         /// <summary>
//         /// Migrates an entity between two worlds
//         /// </summary>
//         /// <param name="sourceWorld">Source world containing the entity</param>
//         /// <param name="destinationWorld">Destination world for the entity</param>
//         /// <param name="entity">Entity to migrate</param>
//         /// <returns>New entity in destination world</returns>
//         public static Entity MigrateEntity(World sourceWorld, World destinationWorld, Entity entity)
//         {
//             if (!sourceWorld.EntityManager.IsEntityAlive(entity))
//             {
//                 throw new InvalidOperationException($"Cannot migrate entity {entity} - it doesn't exist in source world");
//             }
//
//             return destinationWorld.EntityManager.MigrateEntity(entity, sourceWorld.entityManagerWrapper);
//         }
//
//         /// <summary>
//         /// Migrates multiple entities between two worlds
//         /// </summary>
//         /// <param name="sourceWorld">Source world containing the entities</param>
//         /// <param name="destinationWorld">Destination world for the entities</param>
//         /// <param name="entities">Entities to migrate</param>
//         /// <returns>Array of new entities in destination world</returns>
//         public static NativeArray<Entity> MigrateEntities(World sourceWorld, World destinationWorld, NativeArray<Entity> entities)
//         {
//             foreach (var entity in entities)
//             {
//                 if (!sourceWorld.EntityManager.IsEntityAlive(entity))
//                 {
//                     throw new InvalidOperationException($"Cannot migrate entity {entity} - it doesn't exist in source world");
//                 }
//             }
//
//             return destinationWorld.EntityManager.MigrateEntities(entities, sourceWorld.entityManagerWrapper);
//         }
//
//         /// <summary>
//         /// Migrates an entity between two entity managers
//         /// </summary>
//         /// <param name="sourceManager">Source entity manager</param>
//         /// <param name="destinationManager">Destination entity manager</param>
//         /// <param name="entity">Entity to migrate</param>
//         /// <returns>New entity in destination manager</returns>
//         public static Entity MigrateEntityDirect(ReferenceWrapper<EntityManager> sourceManager, ReferenceWrapper<EntityManager> destinationManager, Entity entity)
//         {
//             if (!sourceManager.Value.IsEntityAlive(entity))
//             {
//                 throw new InvalidOperationException($"Cannot migrate entity {entity} - it doesn't exist in source manager");
//             }
//
//             return destinationManager.Value.MigrateEntity(entity, sourceManager);
//         }
//
//         /// <summary>
//         /// Migrates multiple entities between two entity managers
//         /// </summary>
//         /// <param name="sourceManager">Source entity manager</param>
//         /// <param name="destinationManager">Destination entity manager</param>
//         /// <param name="entities">Entities to migrate</param>
//         /// <returns>Array of new entities in destination manager</returns>
//         public static NativeArray<Entity> MigrateEntitiesDirect(ReferenceWrapper<EntityManager> sourceManager, ReferenceWrapper<EntityManager> destinationManager, NativeArray<Entity> entities)
//         {
//             foreach (var entity in entities)
//             {
//                 if (!sourceManager.Value.IsEntityAlive(entity))
//                 {
//                     throw new InvalidOperationException($"Cannot migrate entity {entity} - it doesn't exist in source manager");
//                 }
//             }
//
//             return destinationManager.Value.MigrateEntities(entities, sourceManager);
//         }
//     }
// }