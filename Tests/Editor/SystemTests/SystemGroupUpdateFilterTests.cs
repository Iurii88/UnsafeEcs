using NUnit.Framework;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.SystemTests
{
    [TestFixture]
    public class SystemGroupUpdateFilterTests
    {
        private World m_world;
        private TestSystemGroup m_group;

        [SetUp]
        public void SetUp()
        {
            m_world = new World();
            m_group = new TestSystemGroup();
            m_world.AddRootSystem(m_group);
        }

        [TearDown]
        public void TearDown()
        {
            m_world.Dispose();
        }

        #region Update Filtering Tests

        [Test]
        public void OnUpdate_OnlyCallsSystemsWithUpdateMask()
        {
            var updateSystem = new UpdateOnlySystem();
            var lateUpdateSystem = new LateUpdateOnlySystem();
            var fixedUpdateSystem = new FixedUpdateOnlySystem();

            m_group.AddSystem(updateSystem);
            m_group.AddSystem(lateUpdateSystem);
            m_group.AddSystem(fixedUpdateSystem);

            m_group.OnUpdate();

            Assert.AreEqual(1, updateSystem.UpdateCallCount);
            Assert.AreEqual(0, lateUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, fixedUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void OnLateUpdate_OnlyCallsSystemsWithLateUpdateMask()
        {
            var updateSystem = new UpdateOnlySystem();
            var lateUpdateSystem = new LateUpdateOnlySystem();
            var fixedUpdateSystem = new FixedUpdateOnlySystem();

            m_group.AddSystem(updateSystem);
            m_group.AddSystem(lateUpdateSystem);
            m_group.AddSystem(fixedUpdateSystem);

            m_group.OnLateUpdate();

            Assert.AreEqual(0, updateSystem.UpdateCallCount);
            Assert.AreEqual(1, lateUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, fixedUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void OnFixedUpdate_OnlyCallsSystemsWithFixedUpdateMask()
        {
            var updateSystem = new UpdateOnlySystem();
            var lateUpdateSystem = new LateUpdateOnlySystem();
            var fixedUpdateSystem = new FixedUpdateOnlySystem();

            m_group.AddSystem(updateSystem);
            m_group.AddSystem(lateUpdateSystem);
            m_group.AddSystem(fixedUpdateSystem);

            m_group.OnFixedUpdate();

            Assert.AreEqual(0, updateSystem.UpdateCallCount);
            Assert.AreEqual(0, lateUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(1, fixedUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void SystemWithAllMasks_IsCalledInAllUpdateTypes()
        {
            var allUpdatesSystem = new AllUpdatesSystem();

            m_group.AddSystem(allUpdatesSystem);

            m_group.OnUpdate();
            m_group.OnLateUpdate();
            m_group.OnFixedUpdate();

            Assert.AreEqual(1, allUpdatesSystem.UpdateCallCount);
            Assert.AreEqual(1, allUpdatesSystem.LateUpdateCallCount);
            Assert.AreEqual(1, allUpdatesSystem.FixedUpdateCallCount);
        }

        [Test]
        public void SystemWithNoMask_IsNeverCalled()
        {
            var noUpdateSystem = new NoUpdateSystem();

            m_group.AddSystem(noUpdateSystem);

            m_group.OnUpdate();
            m_group.OnLateUpdate();
            m_group.OnFixedUpdate();

            Assert.AreEqual(0, noUpdateSystem.UpdateCallCount);
            Assert.AreEqual(0, noUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, noUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void SystemWithUpdateAndLateUpdateMask_IsCalledInBoth()
        {
            var updateAndLateSystem = new UpdateAndLateUpdateSystem();

            m_group.AddSystem(updateAndLateSystem);

            m_group.OnUpdate();
            m_group.OnLateUpdate();
            m_group.OnFixedUpdate();

            Assert.AreEqual(1, updateAndLateSystem.UpdateCallCount);
            Assert.AreEqual(1, updateAndLateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, updateAndLateSystem.FixedUpdateCallCount);
        }

        #endregion

        #region Add/Remove System Tests

        [Test]
        public void RemoveSystem_SystemNoLongerCalled()
        {
            var updateSystem = new UpdateOnlySystem();
            m_group.AddSystem(updateSystem);

            m_group.OnUpdate();
            Assert.AreEqual(1, updateSystem.UpdateCallCount);

            m_group.RemoveSystem(updateSystem);

            m_group.OnUpdate();
            Assert.AreEqual(1, updateSystem.UpdateCallCount); // Should not increment
        }

        [Test]
        public void AddMultipleSystems_AllCalledInCorrectOrder()
        {
            var system1 = new OrderTrackingUpdateSystem("System1");
            var system2 = new OrderTrackingUpdateSystem("System2");
            var system3 = new OrderTrackingUpdateSystem("System3");

            OrderTrackingUpdateSystem.ExecutionOrder.Clear();

            m_group.AddSystem(system1);
            m_group.AddSystem(system2);
            m_group.AddSystem(system3);

            m_group.OnUpdate();

            Assert.AreEqual(3, OrderTrackingUpdateSystem.ExecutionOrder.Count);
            Assert.AreEqual("System1", OrderTrackingUpdateSystem.ExecutionOrder[0]);
            Assert.AreEqual("System2", OrderTrackingUpdateSystem.ExecutionOrder[1]);
            Assert.AreEqual("System3", OrderTrackingUpdateSystem.ExecutionOrder[2]);
        }

        [Test]
        public void RemoveMiddleSystem_OthersStillCalled()
        {
            var system1 = new OrderTrackingUpdateSystem("System1");
            var system2 = new OrderTrackingUpdateSystem("System2");
            var system3 = new OrderTrackingUpdateSystem("System3");

            m_group.AddSystem(system1);
            m_group.AddSystem(system2);
            m_group.AddSystem(system3);

            m_group.RemoveSystem(system2);

            OrderTrackingUpdateSystem.ExecutionOrder.Clear();
            m_group.OnUpdate();

            Assert.AreEqual(2, OrderTrackingUpdateSystem.ExecutionOrder.Count);
            Assert.AreEqual("System1", OrderTrackingUpdateSystem.ExecutionOrder[0]);
            Assert.AreEqual("System3", OrderTrackingUpdateSystem.ExecutionOrder[1]);
        }

        #endregion

        #region Mixed Update Types Tests

        [Test]
        public void MixedSystems_EachUpdateTypeCallsCorrectSystems()
        {
            var update1 = new UpdateOnlySystem();
            var update2 = new UpdateOnlySystem();
            var late1 = new LateUpdateOnlySystem();
            var fixed1 = new FixedUpdateOnlySystem();
            var fixed2 = new FixedUpdateOnlySystem();
            var all1 = new AllUpdatesSystem();

            m_group.AddSystem(update1);
            m_group.AddSystem(late1);
            m_group.AddSystem(fixed1);
            m_group.AddSystem(update2);
            m_group.AddSystem(all1);
            m_group.AddSystem(fixed2);

            // Run Update
            m_group.OnUpdate();
            Assert.AreEqual(1, update1.UpdateCallCount);
            Assert.AreEqual(1, update2.UpdateCallCount);
            Assert.AreEqual(1, all1.UpdateCallCount);
            Assert.AreEqual(0, late1.LateUpdateCallCount);
            Assert.AreEqual(0, fixed1.FixedUpdateCallCount);

            // Run LateUpdate
            m_group.OnLateUpdate();
            Assert.AreEqual(1, late1.LateUpdateCallCount);
            Assert.AreEqual(1, all1.LateUpdateCallCount);

            // Run FixedUpdate
            m_group.OnFixedUpdate();
            Assert.AreEqual(1, fixed1.FixedUpdateCallCount);
            Assert.AreEqual(1, fixed2.FixedUpdateCallCount);
            Assert.AreEqual(1, all1.FixedUpdateCallCount);
        }

        [Test]
        public void MultipleUpdateCalls_CountsAccumulate()
        {
            var updateSystem = new UpdateOnlySystem();
            m_group.AddSystem(updateSystem);

            m_group.OnUpdate();
            m_group.OnUpdate();
            m_group.OnUpdate();

            Assert.AreEqual(3, updateSystem.UpdateCallCount);
        }

        #endregion

        #region Test System Classes

        private class TestSystemGroup : SystemGroup
        {
        }

        private class UpdateOnlySystem : SystemBase
        {
            public int UpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update;

            public override void OnUpdate()
            {
                UpdateCallCount++;
            }
        }

        private class LateUpdateOnlySystem : SystemBase
        {
            public int LateUpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.LateUpdate;

            public override void OnLateUpdate()
            {
                LateUpdateCallCount++;
            }
        }

        private class FixedUpdateOnlySystem : SystemBase
        {
            public int FixedUpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.FixedUpdate;

            public override void OnFixedUpdate()
            {
                FixedUpdateCallCount++;
            }
        }

        private class AllUpdatesSystem : SystemBase
        {
            public int UpdateCallCount { get; private set; }
            public int LateUpdateCallCount { get; private set; }
            public int FixedUpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.All;

            public override void OnUpdate()
            {
                UpdateCallCount++;
            }

            public override void OnLateUpdate()
            {
                LateUpdateCallCount++;
            }

            public override void OnFixedUpdate()
            {
                FixedUpdateCallCount++;
            }
        }

        private class NoUpdateSystem : SystemBase
        {
            public int UpdateCallCount { get; private set; }
            public int LateUpdateCallCount { get; private set; }
            public int FixedUpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.None;

            public override void OnUpdate()
            {
                UpdateCallCount++;
            }

            public override void OnLateUpdate()
            {
                LateUpdateCallCount++;
            }

            public override void OnFixedUpdate()
            {
                FixedUpdateCallCount++;
            }
        }

        private class UpdateAndLateUpdateSystem : SystemBase
        {
            public int UpdateCallCount { get; private set; }
            public int LateUpdateCallCount { get; private set; }
            public int FixedUpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update | SystemUpdateMask.LateUpdate;

            public override void OnUpdate()
            {
                UpdateCallCount++;
            }

            public override void OnLateUpdate()
            {
                LateUpdateCallCount++;
            }

            public override void OnFixedUpdate()
            {
                FixedUpdateCallCount++;
            }
        }

        private class OrderTrackingUpdateSystem : SystemBase
        {
            public static readonly System.Collections.Generic.List<string> ExecutionOrder = new();
            private readonly string m_name;

            public OrderTrackingUpdateSystem(string name)
            {
                m_name = name;
            }

            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update;

            public override void OnUpdate()
            {
                ExecutionOrder.Add(m_name);
            }
        }

        #endregion
    }
}
