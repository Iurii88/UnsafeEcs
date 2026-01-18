using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Worlds;
using UnsafeEcs.Tests.Editor.EntityManagerTests;

namespace UnsafeEcs.Tests.Editor.ChunkEdgeCaseTests
{
    /// <summary>
    /// Tests for edge cases in ComponentChunk and BufferChunk:
    /// - Zero capacity initialization
    /// - Resize from null/empty state
    /// - Memory safety with null pointers
    /// </summary>
    [TestFixture]
    public class ChunkZeroCapacityTests : UnsafeEcsQueryBaseTest
    {
        private struct TestComponent : IComponent
        {
            public int value;
        }

        private struct TestBufferElement : IBufferElement
        {
            public int value;
        }

        #region ComponentChunk Zero Capacity Tests

        [Test]
        public unsafe void ComponentChunk_ZeroCapacity_DoesNotCrash()
        {
            // This tests the fix: capacity=0 should not allocate memory
            // Previously this would call Malloc(0) which is undefined behavior
            var managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref entityManager);
            var chunk = new ComponentChunk(
                componentSize: UnsafeUtility.SizeOf<TestComponent>(),
                capacity: 0,
                typeIndex: 0,
                managerPtr: managerPtr
            );

            // Verify null pointers when capacity is 0
            Assert.IsTrue(chunk.ptr == null, "ptr should be null when capacity is 0");
            Assert.IsTrue(chunk.entityIds == null, "entityIds should be null when capacity is 0");
            Assert.AreEqual(0, chunk.capacity);
            Assert.AreEqual(0, chunk.length);

            chunk.Dispose();
        }

        [Test]
        public unsafe void ComponentChunk_ZeroCapacity_AddTriggersResize()
        {
            // Create chunk with zero capacity, then add - should auto-resize
            var managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref entityManager);
            var chunk = new ComponentChunk(
                componentSize: UnsafeUtility.SizeOf<TestComponent>(),
                capacity: 0,
                typeIndex: 0,
                managerPtr: managerPtr
            );

            var entity = entityManager.CreateEntity();
            var component = new TestComponent { value = 42 };

            // Add should resize from 0 to minimum capacity
            chunk.Add(entity.id, &component);

            Assert.AreEqual(1, chunk.length);
            Assert.Greater(chunk.capacity, 0);
            Assert.IsTrue(chunk.ptr != null);
            Assert.IsTrue(chunk.entityIds != null);

            // Verify data integrity
            var retrievedPtr = (TestComponent*)chunk.GetComponentPtr(entity.id);
            Assert.IsTrue(retrievedPtr != null);
            Assert.AreEqual(42, retrievedPtr->value);

            chunk.Dispose();
        }

        [Test]
        public unsafe void ComponentChunk_ZeroCapacity_DisposeIsSafe()
        {
            // Dispose on zero-capacity chunk should not crash (no double-free, no free(null) issues)
            var managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref entityManager);
            var chunk = new ComponentChunk(
                componentSize: UnsafeUtility.SizeOf<TestComponent>(),
                capacity: 0,
                typeIndex: 0,
                managerPtr: managerPtr
            );

            // This should not throw or crash
            Assert.DoesNotThrow(() => chunk.Dispose());
        }

        [Test]
        public unsafe void ComponentChunk_ResizeFromZero_WorksCorrectly()
        {
            // Tests that Resize properly handles null pointers from zero-capacity initialization
            var managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref entityManager);
            var chunk = new ComponentChunk(
                componentSize: UnsafeUtility.SizeOf<TestComponent>(),
                capacity: 0,
                typeIndex: 0,
                managerPtr: managerPtr
            );

            // Explicitly resize from 0 - should not crash due to null checks in Resize
            chunk.Resize(10);

            Assert.AreEqual(10, chunk.capacity);
            Assert.IsTrue(chunk.ptr != null);
            Assert.IsTrue(chunk.entityIds != null);
            Assert.AreEqual(0, chunk.length); // Length should still be 0

            chunk.Dispose();
        }

        #endregion

        #region BufferChunk Resize Edge Cases

        [Test]
        public unsafe void BufferChunk_ResizeWithEmptyLength_DoesNotCopyZeroBytes()
        {
            // This tests the fix: Resize when length=0 should not call MemCpy with 0 size
            var managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref entityManager);

            // Create with small initial capacity
            var chunk = new BufferChunk(
                elementSize: UnsafeUtility.SizeOf<TestBufferElement>(),
                initialCapacity: 2,
                lastEntityId: 10,
                typeIndex: 0,
                managerPtr: managerPtr
            );

            Assert.AreEqual(0, chunk.length); // No buffers added yet

            // Resize while length is 0 - this should not crash
            Assert.DoesNotThrow(() => chunk.Resize(20));

            Assert.AreEqual(20, chunk.capacity);
            Assert.IsTrue(chunk.ptr != null);
            Assert.IsTrue(chunk.entityIds != null);

            chunk.Dispose();
        }

        [Test]
        public unsafe void BufferChunk_ResizePreservesData()
        {
            // Verify that resize properly preserves existing buffer data
            var entity = entityManager.CreateEntity();
            entityManager.AddBuffer<TestBufferElement>(entity);
            var buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            // Add some data
            buffer.Add(new TestBufferElement { value = 100 });
            buffer.Add(new TestBufferElement { value = 200 });
            buffer.Add(new TestBufferElement { value = 300 });

            // Get buffer again (in case internal state changed)
            buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            // Verify data survived
            Assert.AreEqual(3, buffer.Length);
            Assert.AreEqual(100, buffer[0].value);
            Assert.AreEqual(200, buffer[1].value);
            Assert.AreEqual(300, buffer[2].value);
        }

        [Test]
        public void BufferChunk_MultipleEntities_ResizeHandledCorrectly()
        {
            // Create many entities with buffers to trigger resize
            const int entityCount = 50;
            var entities = new Entity[entityCount];

            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = entityManager.CreateEntity();
                entityManager.AddBuffer<TestBufferElement>(entities[i]);
                var buffer = entityManager.GetBuffer<TestBufferElement>(entities[i]);
                buffer.Add(new TestBufferElement { value = i * 10 });
            }

            // Verify all data
            for (int i = 0; i < entityCount; i++)
            {
                var buffer = entityManager.GetBuffer<TestBufferElement>(entities[i]);
                Assert.AreEqual(1, buffer.Length, $"Entity {i} should have 1 element");
                Assert.AreEqual(i * 10, buffer[0].value, $"Entity {i} should have value {i * 10}");
            }
        }

        #endregion

        #region Combined Edge Cases

        [Test]
        public void Component_AddRemoveAdd_WorksWithResizes()
        {
            // Stress test: add, remove, add pattern that might trigger edge cases
            var entity = entityManager.CreateEntity();

            for (int i = 0; i < 100; i++)
            {
                entityManager.AddComponent(entity, new TestComponent { value = i });
                Assert.AreEqual(i, entityManager.GetComponent<TestComponent>(entity).value);
                entityManager.RemoveComponent<TestComponent>(entity);
                Assert.IsFalse(entityManager.HasComponent<TestComponent>(entity));
            }
        }

        [Test]
        public void ManyEntities_ComponentChunkResizes_DataIntegrity()
        {
            // Create many entities to force chunk resizes
            const int entityCount = 1000;
            var entities = new Entity[entityCount];

            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = entityManager.CreateEntity();
                entityManager.AddComponent(entities[i], new TestComponent { value = i });
            }

            // Verify all data after many resizes
            for (int i = 0; i < entityCount; i++)
            {
                Assert.AreEqual(i, entityManager.GetComponent<TestComponent>(entities[i]).value,
                    $"Entity {i} should have value {i}");
            }
        }

        [Test]
        public void Buffer_AddManyElements_ForcesMultipleResizes()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddBuffer<TestBufferElement>(entity);
            var buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            // Add enough elements to force multiple resizes (default capacity is 8)
            const int elementCount = 500;
            for (int i = 0; i < elementCount; i++)
            {
                buffer.Add(new TestBufferElement { value = i });
            }

            // Re-get buffer (pointer might have changed)
            buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            // Verify all data
            Assert.AreEqual(elementCount, buffer.Length);
            for (int i = 0; i < elementCount; i++)
            {
                Assert.AreEqual(i, buffer[i].value, $"Element {i} should have value {i}");
            }
        }

        #endregion
    }
}
