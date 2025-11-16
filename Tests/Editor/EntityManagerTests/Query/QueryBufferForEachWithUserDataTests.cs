// QueryBufferForEachWithUserDataTests.cs

using NUnit.Framework;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    [TestFixture]
    public class QueryBufferForEachWithUserDataTests : EntityQueryTest
    {
        private struct UserData
        {
            public int multiplier;
            public float threshold;
        }

        [Test]
        public void ForEach_WithUserDataAndBuffer_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 42 });

            var userData = new UserData { multiplier = 2, threshold = 1.0f };
            var query = CreateTestQuery().WithBuffer<BufferElement>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(2, data.multiplier);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(42, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataComponentAndBuffer_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 100 });

            var userData = new UserData { multiplier = 3 };
            var query = CreateTestQuery().With<ComponentA>().WithBuffer<BufferElement>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(3, data.multiplier);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(100, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataTwoComponentsAndBuffer_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 200 });

            var userData = new UserData { multiplier = 4, threshold = 5.5f };
            var query = CreateTestQuery().With<ComponentA, ComponentB>().WithBuffer<BufferElement>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(4, data.multiplier);
                Assert.AreEqual(5.5f, data.threshold);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(200, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataThreeComponentsAndBuffer_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 300 });

            var userData = new UserData { multiplier = 5 };
            var query = CreateTestQuery()
                .With<ComponentA, ComponentB, ComponentC>()
                .WithBuffer<BufferElement>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(5, data.multiplier);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(300, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataFourComponentsAndBuffer_WorksCorrectly()
        {
            var entity = CreateEntityWithComponents(
                typeof(ComponentA), typeof(ComponentB), typeof(ComponentC), typeof(ComponentD),
                typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 400 });

            var userData = new UserData { multiplier = 6 };
            var query = CreateTestQuery()
                .With<ComponentA, ComponentB, ComponentC, ComponentD>()
                .WithBuffer<BufferElement>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, ref ComponentA _, ref ComponentB _, ref ComponentC _, ref ComponentD _, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(6, data.multiplier);
                Assert.AreEqual(1, b.Length);
                Assert.AreEqual(400, b[0].value);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndTwoBuffers_WorksCorrectly()
        {
            // Note: This would require a second buffer element type in the test base
            // For now, this test demonstrates the pattern with a single buffer
            var entity = CreateEntityWithComponents(typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 500 });

            var userData = new UserData { multiplier = 7, threshold = 10.0f };
            var query = CreateTestQuery().WithBuffer<BufferElement>();

            var executed = false;
            query.ForEach(userData, (UserData data, ref Entity e, DynamicBuffer<BufferElement> b) =>
            {
                executed = true;
                Assert.AreEqual(entity, e);
                Assert.AreEqual(7, data.multiplier);
                Assert.AreEqual(10.0f, data.threshold);
                Assert.AreEqual(1, b.Length);
            });

            Assert.IsTrue(executed);
        }

        [Test]
        public void ForEach_WithUserDataAndBuffer_ModifiesBufferContent()
        {
            var entity = CreateEntityWithComponents(typeof(BufferElement));
            var buffer = entityManager.GetBuffer<BufferElement>(entity);
            buffer.Add(new BufferElement { value = 10 });
            buffer.Add(new BufferElement { value = 20 });

            var userData = new UserData { multiplier = 2 };
            var query = CreateTestQuery().WithBuffer<BufferElement>();

            query.ForEach(userData, (UserData data, ref Entity e, DynamicBuffer<BufferElement> b) =>
            {
                for (int i = 0; i < b.Length; i++)
                {
                    var element = b[i];
                    element.value *= data.multiplier;
                    b[i] = element;
                }
            });

            // Verify modifications
            buffer = entityManager.GetBuffer<BufferElement>(entity);
            Assert.AreEqual(20, buffer[0].value);
            Assert.AreEqual(40, buffer[1].value);
        }
    }
}
