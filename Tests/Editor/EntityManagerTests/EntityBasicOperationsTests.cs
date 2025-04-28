// EntityBasicOperationsTests.cs

using NUnit.Framework;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests
{
    [TestFixture]
    public class EntityBasicOperationsTests : UnsafeEcsQueryBaseTest
    {
        [Test]
        public void CreateEntity_ReturnsValidEntity()
        {
            var entity = entityManager.CreateEntity();
            Assert.IsTrue(entity.IsAlive());
            Assert.IsTrue(entityManager.IsEntityAlive(entity));
        }

        [Test]
        public void DestroyEntity_MarksEntityAsDead()
        {
            var entity = entityManager.CreateEntity();
            entityManager.DestroyEntity(entity);
            Assert.IsFalse(entityManager.IsEntityAlive(entity));
        }

        [Test]
        public void DestroyEntity_RecyclesEntityID()
        {
            var entity1 = entityManager.CreateEntity();
            var id1 = entity1.id;
            entityManager.DestroyEntity(entity1);

            var entity2 = entityManager.CreateEntity();
            Assert.AreEqual(id1, entity2.id);
            Assert.AreEqual(2, entity2.version); // Version should increment
        }

        [Test]
        public void IsEntityAlive_ReturnsFalseForInvalidEntity()
        {
            var invalidEntity = new Entity { id = -1, version = 1 };
            Assert.IsFalse(entityManager.IsEntityAlive(invalidEntity));
        }

        [Test]
        public void MultipleEntities_GetUniqueIDs()
        {
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            Assert.AreNotEqual(entity1.id, entity2.id);
        }
    }
}