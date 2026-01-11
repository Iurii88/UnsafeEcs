// QueryFetchToArrayTests.cs

using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryFetchToArrayTests : EntityQueryTest
    {
        private NativeArray<Entity> m_entities;
        private NativeReference<int> m_count;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_entities = new NativeArray<Entity>(16, Allocator.Persistent);
            m_count = new NativeReference<int>(Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            m_entities.Dispose();
            m_count.Dispose();
        }

        [Test]
        public void Fetch_ToArray_FillsArrayWithMatchingEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            _ = CreateEntityWithComponents(typeof(ComponentB)); // Should not match

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(m_entities, m_count);
            handle.Complete();

            Assert.AreEqual(2, m_count.Value);
            AssertContainsEntity(m_entities, m_count.Value, entity1);
            AssertContainsEntity(m_entities, m_count.Value, entity2);
        }

        [Test]
        public void Fetch_ToArray_ReturnsZeroCountWhenNoMatches()
        {
            _ = CreateEntityWithComponents(typeof(ComponentB));

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(m_entities, m_count);
            handle.Complete();

            Assert.AreEqual(0, m_count.Value);
        }

        [Test]
        public void Fetch_ToArray_ChainsDependencyCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();

            var firstJob = new DummyJob().Schedule();
            var fetchHandle = query.Fetch(m_entities, m_count, firstJob);

            Assert.IsFalse(fetchHandle.IsCompleted);
            fetchHandle.Complete();

            Assert.AreEqual(1, m_count.Value);
            AssertContainsEntity(m_entities, m_count.Value, entity);
        }

        [Test]
        public void Fetch_ToArray_CanBeUsedAsInputForNextJob()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();
            var fetchHandle = query.Fetch(m_entities, m_count);

            var processHandle = new ProcessEntitiesArrayJob
            {
                entities = m_entities,
                count = m_count
            }.Schedule(fetchHandle);

            processHandle.Complete();

            Assert.AreEqual(1, m_count.Value);
        }

        [Test]
        public void Fetch_ToArray_WorksWithMultipleQueries()
        {
            var entityA = CreateEntityWithComponents(typeof(ComponentA));
            var entityB = CreateEntityWithComponents(typeof(ComponentB));

            var queryA = CreateTestQuery().With<ComponentA>();
            var queryB = CreateTestQuery().With<ComponentB>();

            var arrayA = new NativeArray<Entity>(16, Allocator.Persistent);
            var arrayB = new NativeArray<Entity>(16, Allocator.Persistent);
            var countA = new NativeReference<int>(Allocator.Persistent);
            var countB = new NativeReference<int>(Allocator.Persistent);

            try
            {
                var handleA = queryA.Fetch(arrayA, countA);
                var handleB = queryB.Fetch(arrayB, countB);

                JobHandle.CompleteAll(ref handleA, ref handleB);

                Assert.AreEqual(1, countA.Value);
                Assert.AreEqual(1, countB.Value);
                AssertContainsEntity(arrayA, countA.Value, entityA);
                AssertContainsEntity(arrayB, countB.Value, entityB);
            }
            finally
            {
                arrayA.Dispose();
                arrayB.Dispose();
                countA.Dispose();
                countB.Dispose();
            }
        }

        [Test]
        public void Fetch_ToArray_HandlesComplexQuery()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            _ = CreateEntityWithComponents(typeof(ComponentA)); // Missing ComponentB
            _ = CreateEntityWithComponents(typeof(ComponentB)); // Missing ComponentA
            var entity2 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC));

            var query = CreateTestQuery().With<ComponentA, ComponentB>();
            var handle = query.Fetch(m_entities, m_count);
            handle.Complete();

            Assert.AreEqual(2, m_count.Value);
            AssertContainsEntity(m_entities, m_count.Value, entity1);
            AssertContainsEntity(m_entities, m_count.Value, entity2);
        }

        [Test]
        public void Fetch_ToArray_HandlesWithoutFilter()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));
            _ = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB)); // Should be excluded

            var query = CreateTestQuery().With<ComponentA>().Without<ComponentB>();
            var handle = query.Fetch(m_entities, m_count);
            handle.Complete();

            Assert.AreEqual(1, m_count.Value);
            AssertContainsEntity(m_entities, m_count.Value, entity);
        }

        [Test]
        public void Fetch_ToArray_LimitsToArrayCapacity()
        {
            // Create more entities than array can hold
            var smallArray = new NativeArray<Entity>(3, Allocator.Persistent);
            var count = new NativeReference<int>(Allocator.Persistent);

            try
            {
                for (var i = 0; i < 10; i++)
                {
                    CreateEntityWithComponents(typeof(ComponentA));
                }

                var query = CreateTestQuery().With<ComponentA>();
                var handle = query.Fetch(smallArray, count);
                handle.Complete();

                Assert.AreEqual(3, count.Value);
            }
            finally
            {
                smallArray.Dispose();
                count.Dispose();
            }
        }

        [Test]
        public void Fetch_ToArray_OverwritesPreviousData()
        {
            // Pre-populate the array with dummy data
            for (var i = 0; i < m_entities.Length; i++)
            {
                m_entities[i] = new Entity { id = 999 + i, version = 1 };
            }

            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(m_entities, m_count);
            handle.Complete();

            Assert.AreEqual(1, m_count.Value);
            Assert.AreEqual(entity.id, m_entities[0].id);
        }

        [Test]
        public void Fetch_ToArray_WithFilter_FillsArrayWithFilteredEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));
            var entity3 = CreateEntityWithComponents(typeof(ComponentA));

            var filter = new EvenIdFilter();

            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(m_entities, m_count, filter);
            handle.Complete();

            var expectedCount = 0;
            if (entity1.id % 2 == 0) expectedCount++;
            if (entity2.id % 2 == 0) expectedCount++;
            if (entity3.id % 2 == 0) expectedCount++;

            Assert.AreEqual(expectedCount, m_count.Value);
        }

        [Test]
        public void Fetch_ToArray_WithFilter_ChainsDependencyCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var filter = new AllPassFilter();
            var query = CreateTestQuery().With<ComponentA>();

            var firstJob = new DummyJob().Schedule();
            var fetchHandle = query.Fetch(m_entities, m_count, filter, firstJob);

            Assert.IsFalse(fetchHandle.IsCompleted);
            fetchHandle.Complete();

            Assert.AreEqual(1, m_count.Value);
            AssertContainsEntity(m_entities, m_count.Value, entity);
        }

        [Test]
        public void Fetch_ToArray_WithFilter_LimitsToArrayCapacity()
        {
            var smallArray = new NativeArray<Entity>(2, Allocator.Persistent);
            var count = new NativeReference<int>(Allocator.Persistent);

            try
            {
                for (var i = 0; i < 10; i++)
                {
                    CreateEntityWithComponents(typeof(ComponentA));
                }

                var filter = new AllPassFilter();
                var query = CreateTestQuery().With<ComponentA>();
                var handle = query.Fetch(smallArray, count, filter);
                handle.Complete();

                Assert.AreEqual(2, count.Value);
            }
            finally
            {
                smallArray.Dispose();
                count.Dispose();
            }
        }

        [Test]
        public void Fetch_ToArray_WithFilter_ReturnsZeroCountWhenNoMatches()
        {
            CreateEntityWithComponents(typeof(ComponentA));
            CreateEntityWithComponents(typeof(ComponentA));

            var filter = new NoPassFilter();
            var query = CreateTestQuery().With<ComponentA>();
            var handle = query.Fetch(m_entities, m_count, filter);
            handle.Complete();

            Assert.AreEqual(0, m_count.Value);
        }

        private static void AssertContainsEntity(NativeArray<Entity> array, int count, Entity expected)
        {
            for (var i = 0; i < count; i++)
            {
                if (array[i].id == expected.id && array[i].version == expected.version)
                    return;
            }

            Assert.Fail($"Entity with id {expected.id} and version {expected.version} not found in array");
        }

        private struct DummyJob : IJob
        {
            public void Execute()
            {
            }
        }

        private struct ProcessEntitiesArrayJob : IJob
        {
            [ReadOnly] public NativeArray<Entity> entities;
            [ReadOnly] public NativeReference<int> count;

            public void Execute()
            {
                var _ = count.Value;
                for (var i = 0; i < count.Value; i++)
                {
                    var __ = entities[i];
                }
            }
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

        private struct NoPassFilter : IQueryFilter
        {
            public bool Validate(Entity entity)
            {
                return false;
            }
        }
    }
}
