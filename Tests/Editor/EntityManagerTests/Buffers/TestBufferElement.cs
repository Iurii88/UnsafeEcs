// BufferOperationsTests.cs

using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Buffers
{
    public struct TestBufferElement : IBufferElement
    {
        public int value;
    }

    [TestFixture]
    public class BufferOperationsTests : UnsafeEcsQueryBaseTest
    {
        [Test]
        public void AddBuffer_CreatesEmptyBuffer()
        {
            var entity = entityManager.CreateEntity();
            var buffer = entityManager.AddBuffer<TestBufferElement>(entity);

            Assert.AreEqual(0, buffer.Length);
            Assert.IsTrue(entityManager.HasBuffer<TestBufferElement>(entity));
        }

        [Test]
        public void GetBuffer_CanModifyElements()
        {
            var entity = entityManager.CreateEntity();
            var buffer = entityManager.AddBuffer<TestBufferElement>(entity);

            buffer.Add(new TestBufferElement { value = 1 });
            buffer.Add(new TestBufferElement { value = 2 });

            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(1, buffer[0].value);
            Assert.AreEqual(2, buffer[1].value);
        }

        [Test]
        public void RemoveBuffer_RemovesBufferComponent()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddBuffer<TestBufferElement>(entity);

            entityManager.RemoveBuffer<TestBufferElement>(entity);
            Assert.IsFalse(entityManager.HasBuffer<TestBufferElement>(entity));
        }

        [Test]
        public void SetBuffer_ReplacesExistingData()
        {
            var entity = entityManager.CreateEntity();
            var initialData = new[]
            {
                new TestBufferElement { value = 1 },
                new TestBufferElement { value = 2 }
            };

            entityManager.SetBuffer(entity, initialData);
            var buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(1, buffer[0].value);
            Assert.AreEqual(2, buffer[1].value);
        }

        [Test]
        public void AppendToBuffer_AddsToExisting()
        {
            var entity = entityManager.CreateEntity();
            entityManager.SetBuffer(entity, new[] { new TestBufferElement { value = 1 } });

            var additionalData = new[]
            {
                new TestBufferElement { value = 2 },
                new TestBufferElement { value = 3 }
            };

            entityManager.AppendToBuffer(entity, additionalData);
            var buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            Assert.AreEqual(3, buffer.Length);
            var nativeArray = buffer.ToNativeArray(Allocator.Temp);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 },
                nativeArray.ToArray().Select(x => x.value));
            nativeArray.Dispose();
        }

        [Test]
        public void ClearBuffer_RemovesAllElements()
        {
            var entity = entityManager.CreateEntity();
            entityManager.SetBuffer(entity, new[]
            {
                new TestBufferElement { value = 1 },
                new TestBufferElement { value = 2 }
            });

            entityManager.ClearBuffer<TestBufferElement>(entity);
            var buffer = entityManager.GetBuffer<TestBufferElement>(entity);

            Assert.AreEqual(0, buffer.Length);
        }
    }
}