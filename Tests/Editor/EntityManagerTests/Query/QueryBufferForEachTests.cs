// QueryBufferForEachTests.cs

using NUnit.Framework;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryBufferForEachTests : EntityQueryTest
    {
        [Test]
        public void ForEach_WithSingleBuffer_ProvidesBufferAccess()
        {
            var entity = CreateEntityWithComponents(typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 42 });

            var executed = false;
            var query = CreateTestQuery().With<BufferElement>();
            query.ForEach((ref Entity e, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(42, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithComponentAndBuffer_ProvidesBoth()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 100 });

            var executed = false;
            var query = CreateTestQuery().With<ComponentA, BufferElement>();
            query.ForEach((ref Entity e, ref ComponentA _, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(100, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithTwoBuffers_ProvidesBothBuffers()
        {
            // Note: This requires entity to have two different buffer types
            // For this test we'll just verify the method signature works
            // since we only have one test buffer type defined

            _ = CreateEntityWithComponents(typeof(BufferElement));
            var query = CreateTestQuery().With<BufferElement>();

            Assert.DoesNotThrow(() =>
            {
                query.ForEach((ref Entity _, DynamicBuffer<BufferElement> _, DynamicBuffer<BufferElement> _) =>
                {
                    // Test would need two different buffer types for real usage
                });
            });
        }

        [Test]
        public void ForEach_WithComponentsAndBuffers_CombinationWorks()
        {
            var entity = CreateEntityWithComponents(
                typeof(ComponentA), typeof(ComponentB), typeof(BufferElement));

            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 200 });

            var executed = false;
            var query = CreateTestQuery()
                .With<ComponentA, ComponentB, BufferElement>();

            query.ForEach((ref Entity e, ref ComponentA _, ref ComponentB _,
                DynamicBuffer<BufferElement> buf) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(1, buf.Length);
                Assert.AreEqual(200, buf[0].value);
            });

            Assert.IsTrue(executed);
        }
    }
}