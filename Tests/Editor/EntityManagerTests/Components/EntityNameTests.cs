using NUnit.Framework;
using Unity.Collections;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Components
{
    [TestFixture]
    public class EntityNameTests : UnsafeEcsQueryBaseTest
    {
        [Test]
        public void EntityName_CreateFromString_StoresValue()
        {
            var name = EntityName.FromString("TestEntity");
            Assert.AreEqual("TestEntity", name.Value.ToString());
        }

        [Test]
        public void EntityName_ImplicitConversionFromString_Works()
        {
            EntityName name = "MyEntity";
            Assert.AreEqual("MyEntity", name.Value.ToString());
        }

        [Test]
        public void EntityName_ImplicitConversionToString_Works()
        {
            EntityName name = "ConvertMe";
            string str = name;
            Assert.AreEqual("ConvertMe", str);
        }

        [Test]
        public void EntityName_ToString_ReturnsValue()
        {
            EntityName name = "ToStringTest";
            Assert.AreEqual("ToStringTest", name.ToString());
        }

        [Test]
        public void EntityName_CreateFromFixedString32_Works()
        {
            var fixedStr = new FixedString32Bytes("FixedString32");
            var name = new EntityName(fixedStr);
            Assert.AreEqual("FixedString32", name.Value.ToString());
        }

        [Test]
        public void EntityName_CreateFromFixedString64_Works()
        {
            var fixedStr = new FixedString64Bytes("FixedString64");
            var name = new EntityName(fixedStr);
            Assert.AreEqual("FixedString64", name.Value.ToString());
        }

        [Test]
        public void EntityName_AddToEntity_CanBeRetrieved()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "Player");

            var retrievedName = entityManager.GetComponent<EntityName>(entity);
            Assert.AreEqual("Player", retrievedName.Value.ToString());
        }

        [Test]
        public void EntityName_ModifyViaComponentArray_Works()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "Original");

            var array = entityManager.GetComponentArray<EntityName>();
            ref var nameRef = ref array.Get(entity);
            nameRef = "Modified";

            Assert.AreEqual("Modified", entityManager.GetComponent<EntityName>(entity).Value.ToString());
        }

        [Test]
        public void EntityName_MultipleEntities_EachHasOwnName()
        {
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            var entity3 = entityManager.CreateEntity();

            entityManager.AddComponent<EntityName>(entity1, "Entity_1");
            entityManager.AddComponent<EntityName>(entity2, "Entity_2");
            entityManager.AddComponent<EntityName>(entity3, "Entity_3");

            Assert.AreEqual("Entity_1", entityManager.GetComponent<EntityName>(entity1).Value.ToString());
            Assert.AreEqual("Entity_2", entityManager.GetComponent<EntityName>(entity2).Value.ToString());
            Assert.AreEqual("Entity_3", entityManager.GetComponent<EntityName>(entity3).Value.ToString());
        }

        [Test]
        public void EntityName_TryGet_ReturnsFalseWhenMissing()
        {
            var entity = entityManager.CreateEntity();
            // Don't add EntityName

            var array = entityManager.GetComponentArray<EntityName>();
            var result = array.TryGet(entity, out var name);

            Assert.IsFalse(result);
        }

        [Test]
        public void EntityName_TryGet_ReturnsTrueWhenPresent()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "Present");

            var array = entityManager.GetComponentArray<EntityName>();
            var result = array.TryGet(entity, out var name);

            Assert.IsTrue(result);
            Assert.AreEqual("Present", name.Value.ToString());
        }

        [Test]
        public void EntityName_Has_ReturnsFalseWhenMissing()
        {
            var entity = entityManager.CreateEntity();

            var array = entityManager.GetComponentArray<EntityName>();
            Assert.IsFalse(array.Has(entity));
        }

        [Test]
        public void EntityName_Has_ReturnsTrueWhenPresent()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "HasName");

            var array = entityManager.GetComponentArray<EntityName>();
            Assert.IsTrue(array.Has(entity));
        }

        [Test]
        public void EntityName_Remove_ComponentNoLongerPresent()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "ToRemove");

            Assert.IsTrue(entityManager.HasComponent<EntityName>(entity));

            entityManager.RemoveComponent<EntityName>(entity);

            Assert.IsFalse(entityManager.HasComponent<EntityName>(entity));
        }

        [Test]
        public void EntityName_LongName_TruncatedTo61Chars()
        {
            var entity = entityManager.CreateEntity();

            // FixedString64Bytes can hold up to 61 UTF-8 characters (3 bytes for length header)
            var longName = new string('A', 100);
            entityManager.AddComponent(entity, EntityName.FromString(longName));

            var retrieved = entityManager.GetComponent<EntityName>(entity);
            // Should be truncated to MaxLength without throwing
            Assert.AreEqual(EntityName.MaxLength, retrieved.Value.Length);
            Assert.AreEqual(new string('A', EntityName.MaxLength), retrieved.Value.ToString());
        }

        [Test]
        public void EntityName_EmptyString_Works()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "");

            var retrieved = entityManager.GetComponent<EntityName>(entity);
            Assert.AreEqual("", retrieved.Value.ToString());
        }

        [Test]
        public void EntityName_SpecialCharacters_Works()
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddComponent<EntityName>(entity, "Test_Entity-01 (Clone)");

            var retrieved = entityManager.GetComponent<EntityName>(entity);
            Assert.AreEqual("Test_Entity-01 (Clone)", retrieved.Value.ToString());
        }

        [Test]
        public void EntityName_AddViaComponentArray_Works()
        {
            var entity = entityManager.CreateEntity();

            var array = entityManager.GetComponentArray<EntityName>();
            EntityName name = "ArrayAdded";
            array.Add(entity, ref name);

            Assert.IsTrue(entityManager.HasComponent<EntityName>(entity));
            Assert.AreEqual("ArrayAdded", entityManager.GetComponent<EntityName>(entity).Value.ToString());
        }
    }
}
