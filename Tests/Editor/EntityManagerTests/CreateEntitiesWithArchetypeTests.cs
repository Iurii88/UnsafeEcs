using NUnit.Framework;
using Unity.Collections;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests
{
    // Test components
    public struct TestComponentA : IComponent
    {
        public int Value;
    }

    public struct TestComponentB : IComponent
    {
        public float Value;
    }

    public struct TestComponentC : IComponent
    {
        public double Value;
    }

    [TestFixture]
    public unsafe class CreateEntitiesWithArchetypeTests : UnsafeEcsQueryBaseTest
    {
        [Test]
        public void CreateEntities_WithArchetype_CreatesCorrectCount()
        {
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .Build();

            var entities = entityManager.CreateEntities(archetype, 10, Allocator.Temp);

            Assert.AreEqual(10, entities.m_length);

            for (var i = 0; i < entities.m_length; i++)
            {
                Assert.IsTrue(entityManager.IsEntityAlive(entities.Ptr[i]));
                Assert.IsTrue(entityManager.HasComponent<TestComponentA>(entities.Ptr[i]));
            }

            entities.Dispose();
        }

        [Test]
        public void CreateEntities_WithMultipleComponents_AllComponentsAdded()
        {
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .With<TestComponentB>()
                .With<TestComponentC>()
                .Build();

            var entities = entityManager.CreateEntities(archetype, 5, Allocator.Temp);

            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                Assert.IsTrue(entityManager.HasComponent<TestComponentA>(entity));
                Assert.IsTrue(entityManager.HasComponent<TestComponentB>(entity));
                Assert.IsTrue(entityManager.HasComponent<TestComponentC>(entity));
            }

            entities.Dispose();
        }

        [Test]
        public void CreateEntities_AfterGetComponentArrayForDifferentType_WorksCorrectly()
        {
            // This test covers the bug where GetComponentArray extends chunks array
            // but doesn't initialize the chunk, then CreateEntities assumes chunk exists

            // First, get component array for EntityName (or any other type)
            // This extends the chunks array but doesn't create chunks for other types
            var entityNameArray = entityManager.GetComponentArray<EntityName>();

            // Now create entities with a different archetype
            // Before the fix, this would fail with NullReferenceException
            // because chunks array was extended but chunks[typeIndex] was null
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .With<TestComponentB>()
                .Build();

            var entities = entityManager.CreateEntities(archetype, 10, Allocator.Temp);

            Assert.AreEqual(10, entities.m_length);

            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                Assert.IsTrue(entityManager.HasComponent<TestComponentA>(entity));
                Assert.IsTrue(entityManager.HasComponent<TestComponentB>(entity));
            }

            entities.Dispose();
        }

        [Test]
        public void CreateEntities_AfterMultipleGetComponentArray_WorksCorrectly()
        {
            // Get multiple component arrays to extend chunks array
            entityManager.GetComponentArray<EntityName>();
            entityManager.GetComponentArray<TestComponentC>();

            // Create entities with components that weren't pre-requested
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .Build();

            var entities = entityManager.CreateEntities(archetype, 5, Allocator.Temp);

            Assert.AreEqual(5, entities.m_length);

            for (var i = 0; i < entities.m_length; i++)
            {
                Assert.IsTrue(entityManager.HasComponent<TestComponentA>(entities.Ptr[i]));
            }

            entities.Dispose();
        }

        [Test]
        public void CreateEntities_MultipleBatches_AllEntitiesValid()
        {
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .Build();

            var batch1 = entityManager.CreateEntities(archetype, 10, Allocator.Temp);
            var batch2 = entityManager.CreateEntities(archetype, 20, Allocator.Temp);
            var batch3 = entityManager.CreateEntities(archetype, 5, Allocator.Temp);

            Assert.AreEqual(10, batch1.m_length);
            Assert.AreEqual(20, batch2.m_length);
            Assert.AreEqual(5, batch3.m_length);

            // Verify all entities are alive and have component
            var componentArray = entityManager.GetComponentArray<TestComponentA>();
            Assert.AreEqual(35, componentArray.Length);

            batch1.Dispose();
            batch2.Dispose();
            batch3.Dispose();
        }

        [Test]
        public void CreateEntities_ThenAddEntityName_WorksCorrectly()
        {
            // Create entities first
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .Build();

            var entities = entityManager.CreateEntities(archetype, 3, Allocator.Temp);

            // Now add EntityName to some entities
            var entityNameArray = entityManager.GetComponentArray<EntityName>();

            var name0 = new EntityName("Entity_0");
            var name1 = new EntityName("Entity_1");

            entityNameArray.Add(entities.Ptr[0].id, ref name0);
            entityNameArray.Add(entities.Ptr[1].id, ref name1);

            // Verify
            Assert.IsTrue(entityNameArray.Has(entities.Ptr[0]));
            Assert.IsTrue(entityNameArray.Has(entities.Ptr[1]));
            Assert.IsFalse(entityNameArray.Has(entities.Ptr[2]));

            Assert.AreEqual("Entity_0", entityNameArray.Get(entities.Ptr[0]).Value.ToString());
            Assert.AreEqual("Entity_1", entityNameArray.Get(entities.Ptr[1]).Value.ToString());

            entities.Dispose();
        }

        [Test]
        public void CreateEntities_InterleavedWithGetComponentArray_WorksCorrectly()
        {
            // Interleave CreateEntities and GetComponentArray calls

            var archetype1 = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .Build();

            var batch1 = entityManager.CreateEntities(archetype1, 5, Allocator.Temp);

            // Get array for different type (extends chunks)
            var entityNameArray = entityManager.GetComponentArray<EntityName>();

            var archetype2 = EntityArchetypeBuilder.Create()
                .With<TestComponentB>()
                .Build();

            var batch2 = entityManager.CreateEntities(archetype2, 5, Allocator.Temp);

            // Get another array
            var compCArray = entityManager.GetComponentArray<TestComponentC>();

            var archetype3 = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .With<TestComponentB>()
                .Build();

            var batch3 = entityManager.CreateEntities(archetype3, 5, Allocator.Temp);

            // Verify all batches
            Assert.AreEqual(5, batch1.m_length);
            Assert.AreEqual(5, batch2.m_length);
            Assert.AreEqual(5, batch3.m_length);

            // Verify component counts
            var compAArray = entityManager.GetComponentArray<TestComponentA>();
            var compBArray = entityManager.GetComponentArray<TestComponentB>();

            Assert.AreEqual(10, compAArray.Length); // batch1 (5) + batch3 (5)
            Assert.AreEqual(10, compBArray.Length); // batch2 (5) + batch3 (5)

            batch1.Dispose();
            batch2.Dispose();
            batch3.Dispose();
        }

        [Test]
        public void CreateEntities_ChunksArrayExtendedButEmpty_HandledCorrectly()
        {
            // Manually extend chunks array by getting component array for high-index type
            // This simulates the scenario where chunks array is large but mostly empty

            entityManager.GetComponentArray<EntityName>();
            entityManager.GetComponentArray<TestComponentA>();
            entityManager.GetComponentArray<TestComponentB>();
            entityManager.GetComponentArray<TestComponentC>();

            // Now create entities - all chunk slots should be properly initialized
            var archetype = EntityArchetypeBuilder.Create()
                .With<TestComponentA>()
                .With<TestComponentB>()
                .With<TestComponentC>()
                .Build();

            var entities = entityManager.CreateEntities(archetype, 100, Allocator.Temp);

            Assert.AreEqual(100, entities.m_length);

            var compAArray = entityManager.GetComponentArray<TestComponentA>();
            var compBArray = entityManager.GetComponentArray<TestComponentB>();
            var compCArray = entityManager.GetComponentArray<TestComponentC>();

            Assert.AreEqual(100, compAArray.Length);
            Assert.AreEqual(100, compBArray.Length);
            Assert.AreEqual(100, compCArray.Length);

            // Verify we can access all components
            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                ref var a = ref compAArray.Get(entity);
                ref var b = ref compBArray.Get(entity);
                ref var c = ref compCArray.Get(entity);

                // Set values
                a.Value = i;
                b.Value = i * 1.5f;
                c.Value = i * 2.0;
            }

            // Verify values
            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                Assert.AreEqual(i, compAArray.Get(entity).Value);
                Assert.AreEqual(i * 1.5f, compBArray.Get(entity).Value, 0.001f);
                Assert.AreEqual(i * 2.0, compCArray.Get(entity).Value, 0.001);
            }

            entities.Dispose();
        }
    }
}
