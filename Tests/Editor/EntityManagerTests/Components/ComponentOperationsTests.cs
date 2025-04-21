// ComponentOperationsTests.cs

using System;
using NUnit.Framework;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Components
{
    public struct TestComponent : IComponent
    {
        public int value;
    }

    public struct AnotherComponent : IComponent
    {
        public float value;
    }

    [TestFixture]
    public class ComponentOperationsTests : UnsafeEcsQueryBaseTest
    {
        [Test]
        public void AddComponent_CanBeRetrieved()
        {
            var entity = entityManager.CreateEntity();
            var component = new TestComponent { value = 42 };

            entityManager.AddComponent(entity, component);
            ref var retrieved = ref entityManager.GetComponent<TestComponent>(entity);

            Assert.AreEqual(42, retrieved.value);
        }

        [Test]
        public void RemoveComponent_RemovesFromEntity()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new TestComponent());

            Assert.IsTrue(entityManager.HasComponent<TestComponent>(entity));
            entityManager.RemoveComponent<TestComponent>(entity);
            Assert.IsFalse(entityManager.HasComponent<TestComponent>(entity));
        }

        [Test]
        public void SetComponent_UpdatesExistingComponent()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new TestComponent { value = 10 });

            entityManager.SetComponent(entity, new TestComponent { value = 20 });
            ref var component = ref entityManager.GetComponent<TestComponent>(entity);

            Assert.AreEqual(20, component.value);
        }

        [Test]
        public void GetOrAddComponent_AddsIfMissing()
        {
            var entity = entityManager.CreateEntity();
            ref var component = ref entityManager.GetOrAddComponent<TestComponent>(entity);
            component.value = 100;

            Assert.IsTrue(entityManager.HasComponent<TestComponent>(entity));
            Assert.AreEqual(100, entityManager.GetComponent<TestComponent>(entity).value);
        }

        [Test]
        public void TryGetComponent_ReturnsFalseForMissingComponent()
        {
            var entity = entityManager.CreateEntity();
            Assert.IsFalse(entityManager.TryGetComponent<TestComponent>(entity, out _));
        }

        [Test]
        public void MultipleComponents_CanBeAddedToEntity()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new TestComponent());
            entityManager.AddComponent(entity, new AnotherComponent());

            Assert.IsTrue(entityManager.HasComponent<TestComponent>(entity));
            Assert.IsTrue(entityManager.HasComponent<AnotherComponent>(entity));
        }

        [Test]
        public void GetComponent_ThrowsForMissingComponent()
        {
            var entity = entityManager.CreateEntity();
            Assert.Throws<InvalidOperationException>(() =>
                entityManager.GetComponent<TestComponent>(entity));
        }
    }
}