using NUnit.Framework;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Components
{
    public struct PositionComponent : IComponent
    {
        public float x;
        public float y;
        public float z;
    }

    public struct VelocityComponent : IComponent
    {
        public float vx;
        public float vy;
        public float vz;
    }

    public struct HealthComponent : IComponent
    {
        public int current;
        public int max;
    }

    [TestFixture]
    public class ComponentArrayTests : UnsafeEcsQueryBaseTest
    {
        #region GetComponentArray with TypeIndex

        [Test]
        public void GetComponentArray_WithTypeIndex_ReturnsValidArray()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 1, y = 2, z = 3 });

            var typeIndex = TypeManager.GetComponentTypeIndex<PositionComponent>();
            var array = entityManager.GetComponentArray<PositionComponent>(typeIndex);

            ref var pos = ref array.Get(entity);
            Assert.AreEqual(1f, pos.x);
            Assert.AreEqual(2f, pos.y);
            Assert.AreEqual(3f, pos.z);
        }

        [Test]
        public void GetComponentArray_WithTypeIndex_MatchesGenericOverload()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 10, y = 20, z = 30 });

            var typeIndex = TypeManager.GetComponentTypeIndex<PositionComponent>();
            var arrayWithIndex = entityManager.GetComponentArray<PositionComponent>(typeIndex);
            var arrayGeneric = entityManager.GetComponentArray<PositionComponent>();

            ref var posWithIndex = ref arrayWithIndex.Get(entity);
            ref var posGeneric = ref arrayGeneric.Get(entity);

            Assert.AreEqual(posGeneric.x, posWithIndex.x);
            Assert.AreEqual(posGeneric.y, posWithIndex.y);
            Assert.AreEqual(posGeneric.z, posWithIndex.z);
        }

        [Test]
        public void GetComponentArray_CachedTypeIndex_WorksAcrossMultipleCalls()
        {
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponent(entity1, new PositionComponent { x = 1, y = 1, z = 1 });
            entityManager.AddComponent(entity2, new PositionComponent { x = 2, y = 2, z = 2 });

            // Cache the type index once
            var typeIndex = TypeManager.GetComponentTypeIndex<PositionComponent>();

            // Use it multiple times (simulating multiple update frames)
            for (var i = 0; i < 10; i++)
            {
                var array = entityManager.GetComponentArray<PositionComponent>(typeIndex);
                ref var pos1 = ref array.Get(entity1);
                ref var pos2 = ref array.Get(entity2);

                Assert.AreEqual(1f, pos1.x);
                Assert.AreEqual(2f, pos2.x);
            }
        }

        [Test]
        public void GetComponentArray_MultipleTypes_CachedIndicesWorkCorrectly()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 100, y = 200, z = 300 });
            entityManager.AddComponent(entity, new VelocityComponent { vx = 1, vy = 2, vz = 3 });
            entityManager.AddComponent(entity, new HealthComponent { current = 50, max = 100 });

            // Cache all type indices
            var posIndex = TypeManager.GetComponentTypeIndex<PositionComponent>();
            var velIndex = TypeManager.GetComponentTypeIndex<VelocityComponent>();
            var healthIndex = TypeManager.GetComponentTypeIndex<HealthComponent>();

            // Verify all work correctly
            var positions = entityManager.GetComponentArray<PositionComponent>(posIndex);
            var velocities = entityManager.GetComponentArray<VelocityComponent>(velIndex);
            var healths = entityManager.GetComponentArray<HealthComponent>(healthIndex);

            Assert.AreEqual(100f, positions.Get(entity).x);
            Assert.AreEqual(1f, velocities.Get(entity).vx);
            Assert.AreEqual(50, healths.Get(entity).current);
        }

        #endregion

        #region ComponentArray Caching

        [Test]
        public void ComponentArray_CachedInstance_RemainsValidAfterAddingEntities()
        {
            // Create initial entity and cache the array
            var entity1 = entityManager.CreateEntity();
            entityManager.AddComponent(entity1, new PositionComponent { x = 1, y = 1, z = 1 });

            var cachedArray = entityManager.GetComponentArray<PositionComponent>();

            // Add more entities after caching
            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponent(entity2, new PositionComponent { x = 2, y = 2, z = 2 });

            var entity3 = entityManager.CreateEntity();
            entityManager.AddComponent(entity3, new PositionComponent { x = 3, y = 3, z = 3 });

            // Cached array should still work for all entities
            Assert.AreEqual(1f, cachedArray.Get(entity1).x);
            Assert.AreEqual(2f, cachedArray.Get(entity2).x);
            Assert.AreEqual(3f, cachedArray.Get(entity3).x);
        }

        [Test]
        public void ComponentArray_CachedInstance_RemainsValidAfterRemovingComponents()
        {
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            var entity3 = entityManager.CreateEntity();

            entityManager.AddComponent(entity1, new PositionComponent { x = 1, y = 1, z = 1 });
            entityManager.AddComponent(entity2, new PositionComponent { x = 2, y = 2, z = 2 });
            entityManager.AddComponent(entity3, new PositionComponent { x = 3, y = 3, z = 3 });

            var cachedArray = entityManager.GetComponentArray<PositionComponent>();

            // Remove component from entity2
            entityManager.RemoveComponent<PositionComponent>(entity2);

            // Cached array should still work for remaining entities
            Assert.AreEqual(1f, cachedArray.Get(entity1).x);
            Assert.AreEqual(3f, cachedArray.Get(entity3).x);
            Assert.IsFalse(cachedArray.Has(entity2));
        }

        [Test]
        public void ComponentArray_CachedInstance_ReflectsModifications()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 0, y = 0, z = 0 });

            var cachedArray = entityManager.GetComponentArray<PositionComponent>();

            // Modify through different array instance
            var freshArray = entityManager.GetComponentArray<PositionComponent>();
            ref var pos = ref freshArray.Get(entity);
            pos.x = 999;
            pos.y = 888;
            pos.z = 777;

            // Cached array should see the modification
            ref var cachedPos = ref cachedArray.Get(entity);
            Assert.AreEqual(999f, cachedPos.x);
            Assert.AreEqual(888f, cachedPos.y);
            Assert.AreEqual(777f, cachedPos.z);
        }

        [Test]
        public void ComponentArray_CachedInstance_WorksWithManyEntities()
        {
            const int entityCount = 1000;

            // Cache array before creating entities
            var cachedArray = entityManager.GetComponentArray<PositionComponent>();

            // Create many entities and store references
            var entities = new Core.Entities.Entity[entityCount];
            for (var i = 0; i < entityCount; i++)
            {
                entities[i] = entityManager.CreateEntity();
                entityManager.AddComponent(entities[i], new PositionComponent { x = i, y = i * 2, z = i * 3 });
            }

            // Verify all entities through cached array
            Assert.AreEqual(entityCount, cachedArray.Length);

            // Spot check some values using stored entity references
            ref var pos500 = ref cachedArray.Get(entities[500]);
            Assert.AreEqual(500f, pos500.x);
            Assert.AreEqual(1000f, pos500.y);
            Assert.AreEqual(1500f, pos500.z);

            ref var pos0 = ref cachedArray.Get(entities[0]);
            Assert.AreEqual(0f, pos0.x);

            ref var pos999 = ref cachedArray.Get(entities[999]);
            Assert.AreEqual(999f, pos999.x);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetComponentArray_BeforeAnyEntityCreated_ReturnsValidEmptyArray()
        {
            // Get array before any entities exist
            var array = entityManager.GetComponentArray<PositionComponent>();

            Assert.AreEqual(0, array.Length);

            // Now add an entity
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 42, y = 0, z = 0 });

            // Same array type should now have the entity
            var arrayAfter = entityManager.GetComponentArray<PositionComponent>();
            Assert.AreEqual(1, arrayAfter.Length);
            Assert.AreEqual(42f, arrayAfter.Get(entity).x);
        }

        [Test]
        public void GetComponentArray_CachedBeforeEntityCreated_StillWorks()
        {
            // Cache array before any entities with this component exist
            var cachedArray = entityManager.GetComponentArray<PositionComponent>();
            Assert.AreEqual(0, cachedArray.Length);

            // Add entities after caching
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 123, y = 456, z = 789 });

            // Cached array should work
            Assert.AreEqual(1, cachedArray.Length);
            Assert.AreEqual(123f, cachedArray.Get(entity).x);
        }

        [Test]
        public void GetComponentArray_TypeIndexConsistentAcrossWorldResets()
        {
            // Get type index in first world setup
            var typeIndex1 = TypeManager.GetComponentTypeIndex<PositionComponent>();

            // The type index should be consistent within the same session
            var typeIndex2 = TypeManager.GetComponentTypeIndex<PositionComponent>();

            Assert.AreEqual(typeIndex1, typeIndex2);
        }

        [Test]
        public void ComponentArray_SimulateSystemUpdatePattern()
        {
            // This test simulates a typical system usage pattern
            // where arrays are fetched each frame but type indices are cached

            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponent(entity1, new PositionComponent { x = 0, y = 0, z = 0 });
            entityManager.AddComponent(entity1, new VelocityComponent { vx = 1, vy = 0, vz = 0 });
            entityManager.AddComponent(entity2, new PositionComponent { x = 10, y = 0, z = 0 });
            entityManager.AddComponent(entity2, new VelocityComponent { vx = -1, vy = 0, vz = 0 });

            // Cache type indices (done once in OnAwake)
            var posIndex = TypeManager.GetComponentTypeIndex<PositionComponent>();
            var velIndex = TypeManager.GetComponentTypeIndex<VelocityComponent>();

            // Simulate 100 update frames
            for (var frame = 0; frame < 100; frame++)
            {
                // Get arrays using cached indices (done each frame in OnUpdate)
                var positions = entityManager.GetComponentArray<PositionComponent>(posIndex);
                var velocities = entityManager.GetComponentArray<VelocityComponent>(velIndex);

                // Update positions based on velocities
                ref var pos1 = ref positions.Get(entity1);
                ref var vel1 = ref velocities.Get(entity1);
                pos1.x += vel1.vx;

                ref var pos2 = ref positions.Get(entity2);
                ref var vel2 = ref velocities.Get(entity2);
                pos2.x += vel2.vx;
            }

            // Verify final positions
            var finalPositions = entityManager.GetComponentArray<PositionComponent>();
            Assert.AreEqual(100f, finalPositions.Get(entity1).x); // Started at 0, moved +1 per frame
            Assert.AreEqual(-90f, finalPositions.Get(entity2).x); // Started at 10, moved -1 per frame
        }

        [Test]
        public void ComponentArray_CachedInstancePattern_SimulatesSystemUsage()
        {
            // This test simulates caching ComponentArray instances directly
            // (most aggressive optimization - cache the array itself)

            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent { x = 0, y = 0, z = 0 });
            entityManager.AddComponent(entity, new VelocityComponent { vx = 5, vy = 0, vz = 0 });

            // Cache arrays directly (done once in OnAwake)
            var positions = entityManager.GetComponentArray<PositionComponent>();
            var velocities = entityManager.GetComponentArray<VelocityComponent>();

            // Simulate updates using cached arrays
            for (var frame = 0; frame < 50; frame++)
            {
                ref var pos = ref positions.Get(entity);
                ref var vel = ref velocities.Get(entity);
                pos.x += vel.vx;
            }

            // Verify
            Assert.AreEqual(250f, positions.Get(entity).x); // 50 frames * 5 velocity
        }

        #endregion
    }
}
