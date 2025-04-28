// QueryConstructionTests.cs

using NUnit.Framework;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryConstructionTests : EntityQueryTest
    {
        [Test]
        public void With_SingleComponent_AddsToWithMask()
        {
            var query = CreateTestQuery().With<ComponentA>();
            Assert.IsTrue(query.withMask.HasComponent(TypeManager.GetComponentTypeIndex<ComponentA>()));
        }

        [Test]
        public void With_MultipleComponents_AddsAllToWithMask()
        {
            var query = CreateTestQuery()
                .With<ComponentA, ComponentB, ComponentC>();

            Assert.IsTrue(query.withMask.HasComponent(TypeManager.GetComponentTypeIndex<ComponentA>()));
            Assert.IsTrue(query.withMask.HasComponent(TypeManager.GetComponentTypeIndex<ComponentB>()));
            Assert.IsTrue(query.withMask.HasComponent(TypeManager.GetComponentTypeIndex<ComponentC>()));
        }

        [Test]
        public void Without_SingleComponent_AddsToWithoutMask()
        {
            var query = CreateTestQuery().Without<ComponentA>();
            Assert.IsTrue(query.withoutMask.HasComponent(TypeManager.GetComponentTypeIndex<ComponentA>()));
        }

        [Test]
        public void WithAny_SingleComponent_AddsToWithAnyMask()
        {
            var query = CreateTestQuery().WithAny<ComponentA>();
            Assert.IsTrue(query.withAnyMask.HasComponent(TypeManager.GetComponentTypeIndex<ComponentA>()));
        }

        [Test]
        public void MatchesQuery_WithAll_MatchesCorrectEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));
            var entity3 = CreateEntityWithComponents(typeof(ComponentB));

            var query = CreateTestQuery().With<ComponentA, ComponentB>();

            Assert.IsTrue(query.MatchesQuery(entityManager.entityArchetypes[entity1.id].componentBits));
            Assert.IsFalse(query.MatchesQuery(entityManager.entityArchetypes[entity2.id].componentBits));
            Assert.IsFalse(query.MatchesQuery(entityManager.entityArchetypes[entity3.id].componentBits));
        }

        [Test]
        public void MatchesQuery_Without_ExcludesCorrectEntities()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA));

            var query = CreateTestQuery()
                .With<ComponentA>()
                .Without<ComponentB>();

            Assert.IsFalse(query.MatchesQuery(entityManager.entityArchetypes[entity1.id].componentBits));
            Assert.IsTrue(query.MatchesQuery(entityManager.entityArchetypes[entity2.id].componentBits));
        }

        [Test]
        public void MatchesQuery_WithAny_MatchesAnyOfComponents()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentB));
            var entity3 = CreateEntityWithComponents(typeof(ComponentC));

            var query = CreateTestQuery()
                .WithAny<ComponentA, ComponentB>();

            Assert.IsTrue(query.MatchesQuery(entityManager.entityArchetypes[entity1.id].componentBits));
            Assert.IsTrue(query.MatchesQuery(entityManager.entityArchetypes[entity2.id].componentBits));
            Assert.IsFalse(query.MatchesQuery(entityManager.entityArchetypes[entity3.id].componentBits));
        }

        [Test]
        public void Equals_ReturnsTrueForSameQueries()
        {
            var query1 = CreateTestQuery()
                .With<ComponentA>()
                .Without<ComponentB>()
                .WithAny<ComponentC>();

            var query2 = CreateTestQuery()
                .With<ComponentA>()
                .Without<ComponentB>()
                .WithAny<ComponentC>();

            Assert.IsTrue(query1.Equals(query2));
        }

        [Test]
        public void GetHashCode_ReturnsSameForSameQueries()
        {
            var query1 = CreateTestQuery().With<ComponentA>();
            var query2 = CreateTestQuery().With<ComponentA>();

            Assert.AreEqual(query1.GetHashCode(), query2.GetHashCode());
        }
    }
}