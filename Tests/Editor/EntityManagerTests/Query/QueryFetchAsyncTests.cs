// QueryFetchAsyncTests.cs

using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryFetchAsyncTests : EntityQueryTest
    {
        private UnsafeList<Entity> m_entities;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_entities = new UnsafeList<Entity>(16, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            m_entities.Dispose();
        }

        [Test]
        public void Fetch_WithDependency_FillsListWithMatchingEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            _ = CreateEntityWithComponents(typeof(ComponentB)); // Should not match

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(ref m_entities, default);
            handle.Complete();

            Assert.AreEqual(2, m_entities.Length);
            AssertContainsEntity(m_entities, entity1);
            AssertContainsEntity(m_entities, entity2);
        }

        [Test]
        public void Fetch_WithDependency_ClearsListBeforeFilling()
        {
            // Pre-populate the list
            m_entities.Add(new Entity { id = 999, version = 1 });
            m_entities.Add(new Entity { id = 998, version = 1 });

            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(ref m_entities, default);
            handle.Complete();

            Assert.AreEqual(1, m_entities.Length);
            AssertContainsEntity(m_entities, entity);
        }

        [Test]
        public void Fetch_WithDependency_ReturnsEmptyListWhenNoMatches()
        {
            _ = CreateEntityWithComponents(typeof(ComponentB));

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(ref m_entities, default);
            handle.Complete();

            Assert.AreEqual(0, m_entities.Length);
        }

        [Test]
        public void Fetch_WithDependency_ChainsDependencyCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();

            // Create a dummy job that the fetch should wait for
            var firstJob = new DummyJob().Schedule();
            var fetchHandle = query.Fetch(ref m_entities, firstJob);

            // The fetch should depend on firstJob
            Assert.IsFalse(fetchHandle.IsCompleted);
            fetchHandle.Complete();

            Assert.AreEqual(1, m_entities.Length);
            AssertContainsEntity(m_entities, entity);
        }

        [Test]
        public void Fetch_WithDependency_CanBeUsedAsInputForNextJob()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();
            var fetchHandle = query.Fetch(ref m_entities, default);

            // Schedule a job that uses the entity list
            var processHandle = new ProcessEntitiesJob
            {
                entities = m_entities
            }.Schedule(fetchHandle);

            processHandle.Complete();

            Assert.AreEqual(1, m_entities.Length);
        }

        [Test]
        public void Fetch_WithDependency_WorksWithMultipleQueries()
        {
            var entityA = CreateEntityWithComponents(typeof(ComponentA));
            var entityB = CreateEntityWithComponents(typeof(ComponentB));

            var queryA = CreateTestQuery().With<ComponentA>();
            var queryB = CreateTestQuery().With<ComponentB>();

            var listA = new UnsafeList<Entity>(16, Allocator.Persistent);
            var listB = new UnsafeList<Entity>(16, Allocator.Persistent);

            try
            {
                var handleA = queryA.Fetch(ref listA, default);
                var handleB = queryB.Fetch(ref listB, default);

                JobHandle.CompleteAll(ref handleA, ref handleB);

                Assert.AreEqual(1, listA.Length);
                Assert.AreEqual(1, listB.Length);
                AssertContainsEntity(listA, entityA);
                AssertContainsEntity(listB, entityB);
            }
            finally
            {
                listA.Dispose();
                listB.Dispose();
            }
        }

        [Test]
        public void Fetch_WithDependency_HandlesComplexQuery()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            _ = CreateEntityWithComponents(typeof(ComponentA)); // Missing ComponentB
            _ = CreateEntityWithComponents(typeof(ComponentB)); // Missing ComponentA
            var entity2 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));

            var query = CreateTestQuery().With<ComponentA, ComponentB>();
            var handle = query.Fetch(ref m_entities, default);
            handle.Complete();

            Assert.AreEqual(2, m_entities.Length);
            AssertContainsEntity(m_entities, entity1);
            AssertContainsEntity(m_entities, entity2);
        }

        [Test]
        public void Fetch_WithDependency_HandlesWithoutFilter()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));
            _ = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB)); // Should be excluded

            var query = CreateTestQuery().With<ComponentA>().Without<ComponentB>();
            var handle = query.Fetch(ref m_entities, default);
            handle.Complete();

            Assert.AreEqual(1, m_entities.Length);
            AssertContainsEntity(m_entities, entity);
        }

        [Test]
        public void Fetch_WithDependency_ReusesListAcrossMultipleFetches()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();

            // First fetch
            var handle1 = query.Fetch(ref m_entities, default);
            handle1.Complete();
            Assert.AreEqual(1, m_entities.Length);

            // Create another entity
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));

            // Second fetch should clear and refill
            var handle2 = query.Fetch(ref m_entities, default);
            handle2.Complete();
            Assert.AreEqual(2, m_entities.Length);
            AssertContainsEntity(m_entities, entity1);
            AssertContainsEntity(m_entities, entity2);
        }

        private static unsafe void AssertContainsEntity(UnsafeList<Entity> list, Entity expected)
        {
            for (var i = 0; i < list.Length; i++)
            {
                if (list.Ptr[i].id == expected.id && list.Ptr[i].version == expected.version)
                    return;
            }

            Assert.Fail($"Entity with id {expected.id} and version {expected.version} not found in list");
        }

        private struct DummyJob : IJob
        {
            public void Execute()
            {
                // Do nothing, just for dependency testing
            }
        }

        private struct ProcessEntitiesJob : IJob
        {
            [ReadOnly] public UnsafeList<Entity> entities;

            public void Execute()
            {
                // Just verify we can access the entities
                var _ = entities.Length;
            }
        }

        [Test]
        public void Fetch_WithFilter_FillsListWithFilteredEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));
            var entity3 = CreateEntityWithComponents(typeof(ComponentA));

            // Filter that only accepts entities with even IDs
            var filter = new EvenIdFilter();

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(ref m_entities, filter, default);
            handle.Complete();

            // Count how many have even IDs
            var expectedCount = 0;
            if (entity1.id % 2 == 0) expectedCount++;
            if (entity2.id % 2 == 0) expectedCount++;
            if (entity3.id % 2 == 0) expectedCount++;

            Assert.AreEqual(expectedCount, m_entities.Length);
        }

        [Test]
        public void Fetch_WithFilter_ChainsDependencyCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var filter = new AllPassFilter();
            var query = CreateTestQuery().With<ComponentA>();

            var firstJob = new DummyJob().Schedule();
            var fetchHandle = query.Fetch(ref m_entities, filter, firstJob);

            Assert.IsFalse(fetchHandle.IsCompleted);
            fetchHandle.Complete();

            Assert.AreEqual(1, m_entities.Length);
            AssertContainsEntity(m_entities, entity);
        }

        private struct EvenIdFilter : IQueryFilter
        {
            public bool Validate(Entity entity)
            {
                return entity.id % 2 == 0;
            }
        }

        private struct AllPassFilter : IQueryFilter
        {
            public bool Validate(Entity entity)
            {
                return true;
            }
        }
    }
}
