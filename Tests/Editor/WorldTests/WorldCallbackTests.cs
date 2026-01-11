using System.Collections.Generic;
using NUnit.Framework;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.WorldTests
{
    [TestFixture]
    public class WorldCallbackTests : UnsafeEcsBaseTest
    {
        private World m_world;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_world = WorldManager.Worlds[0];
        }

        #region onSystemAdded Callback Tests

        [Test]
        public void OnSystemAdded_CallbackIsInvoked_WhenSystemIsAdded()
        {
            SystemBase addedSystem = null;
            m_world.onSystemAdded += system => addedSystem = system;

            var testSystem = new TestCallbackSystem();
            m_world.AddRootSystem(testSystem);

            Assert.AreSame(testSystem, addedSystem);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void OnSystemAdded_CallbackIsInvoked_BeforeOnAwake()
        {
            var callOrder = new List<string>();

            m_world.onSystemAdded += system =>
            {
                callOrder.Add("callback");
            };

            var testSystem = new OrderTrackingSystem(callOrder);
            m_world.AddRootSystem(testSystem);

            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual("callback", callOrder[0]);
            Assert.AreEqual("onAwake", callOrder[1]);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void OnSystemAdded_MultipleCallbacks_AllAreInvoked()
        {
            var invocations = new List<int>();

            m_world.onSystemAdded += _ => invocations.Add(1);
            m_world.onSystemAdded += _ => invocations.Add(2);
            m_world.onSystemAdded += _ => invocations.Add(3);

            var testSystem = new TestCallbackSystem();
            m_world.AddRootSystem(testSystem);

            Assert.AreEqual(3, invocations.Count);
            Assert.Contains(1, invocations);
            Assert.Contains(2, invocations);
            Assert.Contains(3, invocations);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void OnSystemAdded_NoCallback_DoesNotThrow()
        {
            // Ensure no callback is set
            m_world.onSystemAdded = null;

            var testSystem = new TestCallbackSystem();

            Assert.DoesNotThrow(() => m_world.AddRootSystem(testSystem));

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void OnSystemAdded_SystemHasWorld_WhenCallbackIsInvoked()
        {
            World worldInCallback = null;
            m_world.onSystemAdded += system => worldInCallback = system.world;

            var testSystem = new TestCallbackSystem();
            m_world.AddRootSystem(testSystem);

            Assert.AreSame(m_world, worldInCallback);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        #endregion

        #region RemoveRootSystem Tests

        [Test]
        public void RemoveRootSystem_CallsOnDestroy()
        {
            var testSystem = new DestroyTrackingSystem();
            m_world.AddRootSystem(testSystem);

            Assert.IsFalse(testSystem.OnDestroyWasCalled);

            m_world.RemoveRootSystem(testSystem);

            Assert.IsTrue(testSystem.OnDestroyWasCalled);
        }

        [Test]
        public void RemoveRootSystem_SetsWorldToNull()
        {
            var testSystem = new TestCallbackSystem();
            m_world.AddRootSystem(testSystem);

            Assert.AreSame(m_world, testSystem.world);

            m_world.RemoveRootSystem(testSystem);

            Assert.IsNull(testSystem.world);
        }

        [Test]
        public void RemoveRootSystem_RemovesFromSystemByType()
        {
            var testSystem = new TestCallbackSystem();
            m_world.AddRootSystem(testSystem);

            Assert.IsTrue(m_world.HasSystem<TestCallbackSystem>());

            m_world.RemoveRootSystem(testSystem);

            Assert.IsFalse(m_world.HasSystem<TestCallbackSystem>());
        }

        [Test]
        public void RemoveRootSystem_RemovesFromRootSystems()
        {
            var testSystem = new TestCallbackSystem();
            var initialCount = m_world.rootSystems.Count;

            m_world.AddRootSystem(testSystem);
            Assert.AreEqual(initialCount + 1, m_world.rootSystems.Count);

            m_world.RemoveRootSystem(testSystem);
            Assert.AreEqual(initialCount, m_world.rootSystems.Count);
        }

        #endregion

        #region AddRootSystem Tests

        [Test]
        public void AddRootSystem_AddsToRootSystems()
        {
            var initialCount = m_world.rootSystems.Count;
            var testSystem = new TestCallbackSystem();

            m_world.AddRootSystem(testSystem);

            Assert.AreEqual(initialCount + 1, m_world.rootSystems.Count);
            Assert.Contains(testSystem, m_world.rootSystems);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void AddRootSystem_AddsToSystemByType()
        {
            var testSystem = new TestCallbackSystem();

            m_world.AddRootSystem(testSystem);

            Assert.IsTrue(m_world.HasSystem<TestCallbackSystem>());
            Assert.AreSame(testSystem, m_world.GetSystem<TestCallbackSystem>());

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void AddRootSystem_SetsWorld()
        {
            var testSystem = new TestCallbackSystem();
            Assert.IsNull(testSystem.world);

            m_world.AddRootSystem(testSystem);

            Assert.AreSame(m_world, testSystem.world);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        [Test]
        public void AddRootSystem_CallsOnAwake()
        {
            var testSystem = new AwakeTrackingSystem();

            m_world.AddRootSystem(testSystem);

            Assert.IsTrue(testSystem.OnAwakeWasCalled);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        #endregion

        #region HasSystem / GetSystem / TryGetSystem Tests

        [Test]
        public void HasSystem_ReturnsFalse_WhenSystemNotAdded()
        {
            Assert.IsFalse(m_world.HasSystem<TestCallbackSystem>());
        }

        [Test]
        public void GetSystem_ThrowsKeyNotFound_WhenSystemNotAdded()
        {
            Assert.Throws<KeyNotFoundException>(() => m_world.GetSystem<TestCallbackSystem>());
        }

        [Test]
        public void TryGetSystem_ReturnsFalse_WhenSystemNotAdded()
        {
            var result = m_world.TryGetSystem<TestCallbackSystem>(out var system);

            Assert.IsFalse(result);
            Assert.IsNull(system);
        }

        [Test]
        public void TryGetSystem_ReturnsTrue_WhenSystemExists()
        {
            var testSystem = new TestCallbackSystem();
            m_world.AddRootSystem(testSystem);

            var result = m_world.TryGetSystem<TestCallbackSystem>(out var system);

            Assert.IsTrue(result);
            Assert.AreSame(testSystem, system);

            // Cleanup
            m_world.RemoveRootSystem(testSystem);
        }

        #endregion

        #region Test System Classes

        private class TestCallbackSystem : SystemBase
        {
        }

        private class OrderTrackingSystem : SystemBase
        {
            private readonly List<string> m_callOrder;

            public OrderTrackingSystem(List<string> callOrder)
            {
                m_callOrder = callOrder;
            }

            public override void OnAwake()
            {
                m_callOrder.Add("onAwake");
            }
        }

        private class DestroyTrackingSystem : SystemBase
        {
            public bool OnDestroyWasCalled { get; private set; }

            public override void OnDestroy()
            {
                OnDestroyWasCalled = true;
            }
        }

        private class AwakeTrackingSystem : SystemBase
        {
            public bool OnAwakeWasCalled { get; private set; }

            public override void OnAwake()
            {
                OnAwakeWasCalled = true;
            }
        }

        #endregion
    }
}
