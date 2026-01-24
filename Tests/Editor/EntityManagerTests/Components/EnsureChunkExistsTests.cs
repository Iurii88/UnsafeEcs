using NUnit.Framework;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Tests.Editor.EntityManagerTests;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Components
{
    public struct UnusedComponent : IComponent
    {
        public int value;
    }

    public struct AnotherUnusedComponent : IComponent
    {
        public float x;
        public float y;
    }

    public struct UnusedBufferElement : IBufferElement
    {
        public int data;
    }

    [TestFixture]
    public class EnsureChunkExistsTests : UnsafeEcsQueryBaseTest
    {
        #region Component Chunk Tests

        [Test]
        public void EnsureChunkExists_CreatesChunkBeforeAnyEntityHasComponent()
        {
            // Ensure chunk exists before any entity has this component
            entityManager.EnsureChunkExists<UnusedComponent>();

            // GetComponentArray should return valid array with zero length
            var array = entityManager.GetComponentArray<UnusedComponent>();
            Assert.AreEqual(0, array.Length);

            // Verify chunk was actually created
            var typeIndex = TypeManager.GetComponentTypeIndex<UnusedComponent>();
            Assert.IsTrue(typeIndex < entityManager.chunks.Length);
            Assert.IsTrue(entityManager.chunks[typeIndex].IsValid);
        }

        [Test]
        public void EnsureChunkExists_IsIdempotent()
        {
            // Call multiple times - should not throw or cause issues
            entityManager.EnsureChunkExists<UnusedComponent>();
            entityManager.EnsureChunkExists<UnusedComponent>();
            entityManager.EnsureChunkExists<UnusedComponent>();

            var array = entityManager.GetComponentArray<UnusedComponent>();
            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void EnsureChunkExists_AllowsAddingComponentsAfterward()
        {
            entityManager.EnsureChunkExists<UnusedComponent>();

            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new UnusedComponent { value = 42 });

            var array = entityManager.GetComponentArray<UnusedComponent>();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(42, array.Get(entity).value);
        }

        [Test]
        public void EnsureChunkExists_WorksWithMultipleComponentTypes()
        {
            entityManager.EnsureChunkExists<UnusedComponent>();
            entityManager.EnsureChunkExists<AnotherUnusedComponent>();

            var array1 = entityManager.GetComponentArray<UnusedComponent>();
            var array2 = entityManager.GetComponentArray<AnotherUnusedComponent>();

            Assert.AreEqual(0, array1.Length);
            Assert.AreEqual(0, array2.Length);

            // Add entities with different components
            var entity1 = entityManager.CreateEntity();
            entityManager.AddComponent(entity1, new UnusedComponent { value = 1 });

            var entity2 = entityManager.CreateEntity();
            entityManager.AddComponent(entity2, new AnotherUnusedComponent { x = 2, y = 3 });

            Assert.AreEqual(1, array1.Length);
            Assert.AreEqual(1, array2.Length);
        }

        [Test]
        public void EnsureChunkExists_ChunkRemainsValidAfterEntityOperations()
        {
            entityManager.EnsureChunkExists<UnusedComponent>();
            var typeIndex = TypeManager.GetComponentTypeIndex<UnusedComponent>();

            // Create and destroy entities
            for (var i = 0; i < 10; i++)
            {
                var entity = entityManager.CreateEntity();
                entityManager.AddComponent(entity, new UnusedComponent { value = i });
            }

            var array = entityManager.GetComponentArray<UnusedComponent>();
            Assert.AreEqual(10, array.Length);

            // Chunk should still be valid
            Assert.IsTrue(typeIndex < entityManager.chunks.Length);
        }

        [Test]
        public void EnsureChunkExists_DoesNotAffectExistingChunks()
        {
            // First add a component normally
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new UnusedComponent { value = 100 });

            // Then call EnsureChunkExists
            entityManager.EnsureChunkExists<UnusedComponent>();

            // Original data should be preserved
            var array = entityManager.GetComponentArray<UnusedComponent>();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(100, array.Get(entity).value);
        }

        #endregion

        #region Buffer Chunk Tests

        [Test]
        public void EnsureBufferChunkExists_CreatesChunkBeforeAnyEntityHasBuffer()
        {
            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();

            var array = entityManager.GetBufferArray<UnusedBufferElement>();
            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void EnsureBufferChunkExists_IsIdempotent()
        {
            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();
            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();
            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();

            var array = entityManager.GetBufferArray<UnusedBufferElement>();
            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void EnsureBufferChunkExists_AllowsAddingBuffersAfterward()
        {
            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();

            var entity = entityManager.CreateEntity();
            var buffer = entityManager.AddBuffer<UnusedBufferElement>(entity);
            buffer.Add(new UnusedBufferElement { data = 42 });

            var array = entityManager.GetBufferArray<UnusedBufferElement>();
            Assert.AreEqual(1, array.Length);

            var retrievedBuffer = entityManager.GetBuffer<UnusedBufferElement>(entity);
            Assert.AreEqual(1, retrievedBuffer.Length);
            Assert.AreEqual(42, retrievedBuffer[0].data);
        }

        [Test]
        public void EnsureBufferChunkExists_DoesNotAffectExistingBuffers()
        {
            var entity = entityManager.CreateEntity();
            var buffer = entityManager.AddBuffer<UnusedBufferElement>(entity);
            buffer.Add(new UnusedBufferElement { data = 100 });

            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();

            var retrievedBuffer = entityManager.GetBuffer<UnusedBufferElement>(entity);
            Assert.AreEqual(1, retrievedBuffer.Length);
            Assert.AreEqual(100, retrievedBuffer[0].data);
        }

        #endregion

        #region Mixed Component and Buffer Tests

        [Test]
        public void EnsureChunkExists_ComponentAndBufferCanCoexist()
        {
            entityManager.EnsureChunkExists<UnusedComponent>();
            entityManager.EnsureBufferChunkExists<UnusedBufferElement>();

            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new UnusedComponent { value = 1 });
            var buffer = entityManager.AddBuffer<UnusedBufferElement>(entity);
            buffer.Add(new UnusedBufferElement { data = 2 });

            Assert.AreEqual(1, entityManager.GetComponent<UnusedComponent>(entity).value);
            Assert.AreEqual(2, entityManager.GetBuffer<UnusedBufferElement>(entity)[0].data);
        }

        #endregion
    }
}
