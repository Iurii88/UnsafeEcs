using System;
using NUnit.Framework;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Components
{
    public struct MissingTestComponent : IComponent
    {
        public int Value;
    }

    public struct AnotherMissingComponent : IComponent
    {
        public float Data;
    }

    [TestFixture]
    public class ComponentErrorMessageTests : UnsafeEcsQueryBaseTest
    {
        [Test]
        public void GetComponent_MissingComponent_ErrorMessageContainsComponentName()
        {
            var entity = entityManager.CreateEntity();
            // Don't add MissingTestComponent

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                entityManager.GetComponent<MissingTestComponent>(entity);
            });

            Assert.IsTrue(exception.Message.Contains("MissingTestComponent"),
                $"Error message should contain component name. Actual: {exception.Message}");
            Assert.IsTrue(exception.Message.Contains(entity.id.ToString()),
                $"Error message should contain entity id. Actual: {exception.Message}");
        }

        [Test]
        public void GetComponent_MissingComponent_ErrorMessageContainsEntityId()
        {
            var entity = entityManager.CreateEntity();

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                entityManager.GetComponent<AnotherMissingComponent>(entity);
            });

            Assert.IsTrue(exception.Message.Contains(entity.id.ToString()),
                $"Error message should contain entity id {entity.id}. Actual: {exception.Message}");
        }

        [Test]
        public void ComponentArray_Get_MissingComponent_ErrorMessageContainsComponentName()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent(entity, new PositionComponent()); // Add different component

            var array = entityManager.GetComponentArray<MissingTestComponent>();

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                array.Get(entity);
            });

            Assert.IsTrue(exception.Message.Contains("MissingTestComponent"),
                $"Error message should contain component name. Actual: {exception.Message}");
            Assert.IsTrue(exception.Message.Contains(entity.id.ToString()),
                $"Error message should contain entity id. Actual: {exception.Message}");
        }

        [Test]
        public void ComponentArray_Get_EntityOutOfRange_ErrorMessageContainsComponentName()
        {
            // Create entity and add component to initialize array
            var entity1 = entityManager.CreateEntity();
            entityManager.AddComponent(entity1, new PositionComponent { x = 1 });

            var array = entityManager.GetComponentArray<PositionComponent>();

            // Create a fake entity with id way out of range
            var fakeEntity = new Entity { id = 99999 };

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                array.Get(fakeEntity);
            });

            Assert.IsTrue(exception.Message.Contains("PositionComponent"),
                $"Error message should contain component name. Actual: {exception.Message}");
            Assert.IsTrue(exception.Message.Contains("99999"),
                $"Error message should contain entity id. Actual: {exception.Message}");
        }

        [Test]
        public void GetComponent_DifferentMissingComponents_ErrorMessagesAreDifferent()
        {
            var entity = entityManager.CreateEntity();

            var exception1 = Assert.Throws<InvalidOperationException>(() =>
            {
                entityManager.GetComponent<MissingTestComponent>(entity);
            });

            var exception2 = Assert.Throws<InvalidOperationException>(() =>
            {
                entityManager.GetComponent<AnotherMissingComponent>(entity);
            });

            // Error messages should be different (contain different component names)
            Assert.AreNotEqual(exception1.Message, exception2.Message);
            Assert.IsTrue(exception1.Message.Contains("MissingTestComponent"));
            Assert.IsTrue(exception2.Message.Contains("AnotherMissingComponent"));
        }

        [Test]
        public void ComponentArray_Get_MultipleEntities_ErrorMessageShowsCorrectEntityId()
        {
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            var entity3 = entityManager.CreateEntity();

            // Only add component to entity2
            entityManager.AddComponent(entity2, new PositionComponent { x = 42 });

            var array = entityManager.GetComponentArray<PositionComponent>();

            // Should work for entity2
            Assert.AreEqual(42f, array.Get(entity2).x);

            // Should fail with correct entity id for entity1
            var exception1 = Assert.Throws<InvalidOperationException>(() => array.Get(entity1));
            Assert.IsTrue(exception1.Message.Contains(entity1.id.ToString()),
                $"Error should mention entity {entity1.id}. Actual: {exception1.Message}");

            // Should fail with correct entity id for entity3
            var exception3 = Assert.Throws<InvalidOperationException>(() => array.Get(entity3));
            Assert.IsTrue(exception3.Message.Contains(entity3.id.ToString()),
                $"Error should mention entity {entity3.id}. Actual: {exception3.Message}");
        }
    }
}
