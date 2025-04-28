// QueryExecutionTests.cs

using System;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryExecutionTests : EntityQueryTest
    {
        [Test]
        public void Fetch_UpdatesWhenComponentsChange()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery().With<ComponentA>();
            Assert.AreEqual(1, query.Fetch().Length);

            entityManager.RemoveComponent<ComponentA>(entity);
            var results = query.Fetch();
            Assert.AreEqual(0, results.Length);
        }

        [Test]
        public void Fetch_ReturnsMatchingEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));
            _ = CreateEntityWithComponents(typeof(ComponentB)); // Should not match

            var query = CreateTestQuery().With<ComponentA>();
            var results = query.Fetch();

            Assert.AreEqual(2, results.Length);
            AssertContainsEntity(results, entity1);
            AssertContainsEntity(results, entity2);
        }

        [Test]
        public void FetchReadOnly_ReturnsMatchingEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));

            var query = CreateTestQuery().With<ComponentA>();
            var results = query.FetchReadOnly();

            Assert.AreEqual(2, results.Length);
            AssertContainsEntity(results, entity1);
            AssertContainsEntity(results, entity2);
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

        private static void AssertContainsEntity(ReadOnlySpan<Entity> span, Entity expected)
        {
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].id == expected.id && span[i].version == expected.version)
                    return;
            }

            Assert.Fail($"Entity with id {expected.id} and version {expected.version} not found in span");
        }
    }
}