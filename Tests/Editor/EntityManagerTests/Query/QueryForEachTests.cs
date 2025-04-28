// QueryForEachTests.cs

using NUnit.Framework;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryForEachTests : EntityQueryTest
    {
        [Test]
        public void ForEach_WithoutComponents_ExecutesForAllMatching()
        {
            var entity1 = CreateEntityWithComponents(typeof(ComponentA));
            var entity2 = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));
            CreateEntityWithComponents(typeof(ComponentB)); // Should not match

            var count = 0;
            var query = CreateTestQuery().With<ComponentA>();
            query.ForEach((ref Entity entity) =>
            {
                count++;
                Assert.IsTrue(entity == entity1 || entity == entity2);
            });

            Assert.AreEqual(2, count);
        }

        [Test]
        public void ForEach_WithOneComponent_ProvidesComponentRef()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA));
            entityManager.SetComponent(entity, new ComponentA());

            var executed = false;
            var query = CreateTestQuery().With<ComponentA>();
            query.ForEach((ref Entity e, ref ComponentA _) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithMultipleComponents_ProvidesAllComponentRefs()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB));

            var executed = false;
            var query = CreateTestQuery().With<ComponentA, ComponentB>();
            query.ForEach((ref Entity e, ref ComponentA _, ref ComponentB _) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithFiveComponents_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(
                typeof(ComponentA), typeof(ComponentB), typeof(ComponentC),
                typeof(ComponentD), typeof(ComponentE));

            var executed = false;
            var query = CreateTestQuery()
                .With<ComponentA, ComponentB, ComponentC, ComponentD, ComponentE>();

            query.ForEach((ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _, ref ComponentD _, ref ComponentE _) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
            });

            Assert.IsTrue(executed);
        }
    }
}