using System;
using NUnit.Framework;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.WorldTests
{
    [TestFixture]
    public class WorldDataTests : UnsafeEcsBaseTest
    {
        private World m_world;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_world = WorldManager.Worlds[0];
        }

        #region SetData / GetData Tests

        [Test]
        public void SetData_StoresValue_GetDataReturnsIt()
        {
            var testData = new TestData { value = 42, name = "test" };

            m_world.SetData(testData);
            var retrieved = m_world.GetData<TestData>();

            Assert.AreEqual(42, retrieved.value);
            Assert.AreEqual("test", retrieved.name);
        }

        [Test]
        public void SetData_OverwritesExistingValue()
        {
            m_world.SetData(new TestData { value = 1 });
            m_world.SetData(new TestData { value = 2 });

            var retrieved = m_world.GetData<TestData>();

            Assert.AreEqual(2, retrieved.value);
        }

        [Test]
        public void SetData_DifferentTypes_StoredIndependently()
        {
            m_world.SetData(new TestData { value = 42 });
            m_world.SetData(new OtherData { id = 100 });

            Assert.AreEqual(42, m_world.GetData<TestData>().value);
            Assert.AreEqual(100, m_world.GetData<OtherData>().id);
        }

        [Test]
        public void GetData_ThrowsKeyNotFoundException_WhenNotSet()
        {
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
            {
                m_world.GetData<TestData>();
            });
        }

        #endregion

        #region TryGetData Tests

        [Test]
        public void TryGetData_ReturnsTrue_WhenDataExists()
        {
            m_world.SetData(new TestData { value = 42 });

            var result = m_world.TryGetData<TestData>(out var data);

            Assert.IsTrue(result);
            Assert.AreEqual(42, data.value);
        }

        [Test]
        public void TryGetData_ReturnsFalse_WhenDataDoesNotExist()
        {
            var result = m_world.TryGetData<TestData>(out var data);

            Assert.IsFalse(result);
            Assert.AreEqual(default(TestData), data);
        }

        #endregion

        #region HasData Tests

        [Test]
        public void HasData_ReturnsTrue_WhenDataExists()
        {
            m_world.SetData(new TestData { value = 42 });

            Assert.IsTrue(m_world.HasData<TestData>());
        }

        [Test]
        public void HasData_ReturnsFalse_WhenDataDoesNotExist()
        {
            Assert.IsFalse(m_world.HasData<TestData>());
        }

        #endregion

        #region RemoveData Tests

        [Test]
        public void RemoveData_ReturnsTrue_WhenDataExisted()
        {
            m_world.SetData(new TestData { value = 42 });

            var result = m_world.RemoveData<TestData>();

            Assert.IsTrue(result);
            Assert.IsFalse(m_world.HasData<TestData>());
        }

        [Test]
        public void RemoveData_ReturnsFalse_WhenDataDidNotExist()
        {
            var result = m_world.RemoveData<TestData>();

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveData_OnlyRemovesSpecifiedType()
        {
            m_world.SetData(new TestData { value = 42 });
            m_world.SetData(new OtherData { id = 100 });

            m_world.RemoveData<TestData>();

            Assert.IsFalse(m_world.HasData<TestData>());
            Assert.IsTrue(m_world.HasData<OtherData>());
        }

        #endregion

        #region GetOrCreateData Tests

        [Test]
        public void GetOrCreateData_WithFactory_ReturnsExisting_WhenDataExists()
        {
            m_world.SetData(new TestData { value = 42 });
            var factoryCalled = false;

            var result = m_world.GetOrCreateData(() =>
            {
                factoryCalled = true;
                return new TestData { value = 100 };
            });

            Assert.AreEqual(42, result.value);
            Assert.IsFalse(factoryCalled);
        }

        [Test]
        public void GetOrCreateData_WithFactory_CreatesNew_WhenDataDoesNotExist()
        {
            var factoryCalled = false;

            var result = m_world.GetOrCreateData(() =>
            {
                factoryCalled = true;
                return new TestData { value = 100 };
            });

            Assert.AreEqual(100, result.value);
            Assert.IsTrue(factoryCalled);
            Assert.IsTrue(m_world.HasData<TestData>());
        }

        [Test]
        public void GetOrCreateData_WithDefaultConstructor_ReturnsExisting_WhenDataExists()
        {
            m_world.SetData(new DefaultConstructibleData { value = 42 });

            var result = m_world.GetOrCreateData<DefaultConstructibleData>();

            Assert.AreEqual(42, result.value);
        }

        [Test]
        public void GetOrCreateData_WithDefaultConstructor_CreatesNew_WhenDataDoesNotExist()
        {
            var result = m_world.GetOrCreateData<DefaultConstructibleData>();

            Assert.AreEqual(0, result.value); // Default value
            Assert.IsTrue(m_world.HasData<DefaultConstructibleData>());
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_RemovesAllData()
        {
            m_world.SetData(new TestData { value = 42 });
            m_world.SetData(new OtherData { id = 100 });

            m_world.data.Clear();

            Assert.IsFalse(m_world.HasData<TestData>());
            Assert.IsFalse(m_world.HasData<OtherData>());
        }

        #endregion

        #region WorldData Direct Access Tests

        [Test]
        public void WorldData_DirectAccess_WorksSameAsConvenienceMethods()
        {
            var testData = new TestData { value = 42 };

            // Using direct WorldData access
            m_world.data.SetData(testData);

            // Verify through convenience methods
            Assert.AreEqual(42, m_world.GetData<TestData>().value);

            // And vice versa
            m_world.SetData(new TestData { value = 100 });
            Assert.AreEqual(100, m_world.data.GetData<TestData>().value);
        }

        #endregion

        #region Reference Type Tests

        [Test]
        public void SetData_WorksWithReferenceTypes()
        {
            var refData = new ReferenceTypeData { items = new[] { 1, 2, 3 } };

            m_world.SetData(refData);
            var retrieved = m_world.GetData<ReferenceTypeData>();

            Assert.AreEqual(3, retrieved.items.Length);
            Assert.AreEqual(1, retrieved.items[0]);
        }

        [Test]
        public void SetData_ReferenceType_StoresSameInstance()
        {
            var refData = new ReferenceTypeData { items = new[] { 1, 2, 3 } };

            m_world.SetData(refData);
            var retrieved = m_world.GetData<ReferenceTypeData>();

            Assert.AreSame(refData, retrieved);
        }

        #endregion

        #region Test Data Types

        private struct TestData
        {
            public int value;
            public string name;
        }

        private struct OtherData
        {
            public int id;
        }

        private class DefaultConstructibleData
        {
            public int value;
        }

        private class ReferenceTypeData
        {
            public int[] items;
        }

        #endregion
    }
}
