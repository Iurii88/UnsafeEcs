using NUnit.Framework;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.WorldTests
{
    [TestFixture]
    public class WorldUpdateFilterTests
    {
        private World m_world;

        [SetUp]
        public void SetUp()
        {
            m_world = new World();
        }

        [TearDown]
        public void TearDown()
        {
            m_world.Dispose();
        }

        #region Update Filtering Tests

        [Test]
        public void Update_OnlyCallsSystemsWithUpdateMask()
        {
            var updateSystem = new UpdateOnlySystem();
            var lateUpdateSystem = new LateUpdateOnlySystem();
            var fixedUpdateSystem = new FixedUpdateOnlySystem();

            m_world.AddRootSystem(updateSystem);
            m_world.AddRootSystem(lateUpdateSystem);
            m_world.AddRootSystem(fixedUpdateSystem);

            m_world.Update(0.016f);

            Assert.AreEqual(1, updateSystem.UpdateCallCount);
            Assert.AreEqual(0, lateUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, fixedUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void LateUpdate_OnlyCallsSystemsWithLateUpdateMask()
        {
            var updateSystem = new UpdateOnlySystem();
            var lateUpdateSystem = new LateUpdateOnlySystem();
            var fixedUpdateSystem = new FixedUpdateOnlySystem();

            m_world.AddRootSystem(updateSystem);
            m_world.AddRootSystem(lateUpdateSystem);
            m_world.AddRootSystem(fixedUpdateSystem);

            m_world.LateUpdate(0.016f);

            Assert.AreEqual(0, updateSystem.UpdateCallCount);
            Assert.AreEqual(1, lateUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, fixedUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void FixedUpdate_OnlyCallsSystemsWithFixedUpdateMask()
        {
            var updateSystem = new UpdateOnlySystem();
            var lateUpdateSystem = new LateUpdateOnlySystem();
            var fixedUpdateSystem = new FixedUpdateOnlySystem();

            m_world.AddRootSystem(updateSystem);
            m_world.AddRootSystem(lateUpdateSystem);
            m_world.AddRootSystem(fixedUpdateSystem);

            m_world.FixedUpdate(0.02f);

            Assert.AreEqual(0, updateSystem.UpdateCallCount);
            Assert.AreEqual(0, lateUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(1, fixedUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void SystemWithAllMasks_IsCalledInAllUpdateTypes()
        {
            var allUpdatesSystem = new AllUpdatesSystem();

            m_world.AddRootSystem(allUpdatesSystem);

            m_world.Update(0.016f);
            m_world.LateUpdate(0.016f);
            m_world.FixedUpdate(0.02f);

            Assert.AreEqual(1, allUpdatesSystem.UpdateCallCount);
            Assert.AreEqual(1, allUpdatesSystem.LateUpdateCallCount);
            Assert.AreEqual(1, allUpdatesSystem.FixedUpdateCallCount);
        }

        [Test]
        public void SystemWithNoMask_IsNeverCalled()
        {
            var noUpdateSystem = new NoUpdateSystem();

            m_world.AddRootSystem(noUpdateSystem);

            m_world.Update(0.016f);
            m_world.LateUpdate(0.016f);
            m_world.FixedUpdate(0.02f);

            Assert.AreEqual(0, noUpdateSystem.UpdateCallCount);
            Assert.AreEqual(0, noUpdateSystem.LateUpdateCallCount);
            Assert.AreEqual(0, noUpdateSystem.FixedUpdateCallCount);
        }

        [Test]
        public void SystemWithUpdateAndFixedMask_IsCalledInBoth()
        {
            var updateAndFixedSystem = new UpdateAndFixedUpdateSystem();

            m_world.AddRootSystem(updateAndFixedSystem);

            m_world.Update(0.016f);
            m_world.LateUpdate(0.016f);
            m_world.FixedUpdate(0.02f);

            Assert.AreEqual(1, updateAndFixedSystem.UpdateCallCount);
            Assert.AreEqual(0, updateAndFixedSystem.LateUpdateCallCount);
            Assert.AreEqual(1, updateAndFixedSystem.FixedUpdateCallCount);
        }

        #endregion

        #region Add/Remove System Tests

        [Test]
        public void RemoveRootSystem_SystemNoLongerCalled()
        {
            var updateSystem = new UpdateOnlySystem();
            m_world.AddRootSystem(updateSystem);

            m_world.Update(0.016f);
            Assert.AreEqual(1, updateSystem.UpdateCallCount);

            m_world.RemoveRootSystem(updateSystem);

            m_world.Update(0.016f);
            Assert.AreEqual(1, updateSystem.UpdateCallCount); // Should not increment
        }

        [Test]
        public void AddMultipleSystems_AllCalledInCorrectOrder()
        {
            var system1 = new OrderTrackingUpdateSystem("System1");
            var system2 = new OrderTrackingUpdateSystem("System2");
            var system3 = new OrderTrackingUpdateSystem("System3");

            OrderTrackingUpdateSystem.ExecutionOrder.Clear();

            m_world.AddRootSystem(system1);
            m_world.AddRootSystem(system2);
            m_world.AddRootSystem(system3);

            m_world.Update(0.016f);

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

            m_world.AddRootSystem(system1);
            m_world.AddRootSystem(system2);
            m_world.AddRootSystem(system3);

            m_world.RemoveRootSystem(system2);

            OrderTrackingUpdateSystem.ExecutionOrder.Clear();
            m_world.Update(0.016f);

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

            m_world.AddRootSystem(update1);
            m_world.AddRootSystem(late1);
            m_world.AddRootSystem(fixed1);
            m_world.AddRootSystem(update2);
            m_world.AddRootSystem(all1);
            m_world.AddRootSystem(fixed2);

            // Run Update
            m_world.Update(0.016f);
            Assert.AreEqual(1, update1.UpdateCallCount);
            Assert.AreEqual(1, update2.UpdateCallCount);
            Assert.AreEqual(1, all1.UpdateCallCount);
            Assert.AreEqual(0, late1.LateUpdateCallCount);
            Assert.AreEqual(0, fixed1.FixedUpdateCallCount);

            // Run LateUpdate
            m_world.LateUpdate(0.016f);
            Assert.AreEqual(1, late1.LateUpdateCallCount);
            Assert.AreEqual(1, all1.LateUpdateCallCount);

            // Run FixedUpdate
            m_world.FixedUpdate(0.02f);
            Assert.AreEqual(1, fixed1.FixedUpdateCallCount);
            Assert.AreEqual(1, fixed2.FixedUpdateCallCount);
            Assert.AreEqual(1, all1.FixedUpdateCallCount);
        }

        [Test]
        public void MultipleUpdateCalls_CountsAccumulate()
        {
            var updateSystem = new UpdateOnlySystem();
            m_world.AddRootSystem(updateSystem);

            m_world.Update(0.016f);
            m_world.Update(0.016f);
            m_world.Update(0.016f);

            Assert.AreEqual(3, updateSystem.UpdateCallCount);
        }

        [Test]
        public void DeltaTimeAndElapsedTime_UpdatedCorrectly()
        {
            var system = new UpdateOnlySystem();
            m_world.AddRootSystem(system);

            m_world.Update(0.016f);
            Assert.AreEqual(0.016f, m_world.deltaTime, 0.0001f);
            Assert.AreEqual(0.016f, m_world.elapsedDeltaTime, 0.0001f);

            m_world.Update(0.033f);
            Assert.AreEqual(0.033f, m_world.deltaTime, 0.0001f);
            Assert.AreEqual(0.049f, m_world.elapsedDeltaTime, 0.0001f);
        }

        [Test]
        public void FixedDeltaTimeAndElapsedTime_UpdatedCorrectly()
        {
            var system = new FixedUpdateOnlySystem();
            m_world.AddRootSystem(system);

            m_world.FixedUpdate(0.02f);
            Assert.AreEqual(0.02f, m_world.fixedDeltaTime, 0.0001f);
            Assert.AreEqual(0.02f, m_world.elapsedFixedDeltaTime, 0.0001f);

            m_world.FixedUpdate(0.02f);
            Assert.AreEqual(0.02f, m_world.fixedDeltaTime, 0.0001f);
            Assert.AreEqual(0.04f, m_world.elapsedFixedDeltaTime, 0.0001f);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void EmptyWorld_UpdateDoesNotThrow()
        {
            Assert.DoesNotThrow(() => m_world.Update(0.016f));
            Assert.DoesNotThrow(() => m_world.LateUpdate(0.016f));
            Assert.DoesNotThrow(() => m_world.FixedUpdate(0.02f));
        }

        [Test]
        public void AddAndRemoveSameSystem_NoErrors()
        {
            var system = new UpdateOnlySystem();

            m_world.AddRootSystem(system);
            m_world.Update(0.016f);
            Assert.AreEqual(1, system.UpdateCallCount);

            m_world.RemoveRootSystem(system);
            m_world.Update(0.016f);
            Assert.AreEqual(1, system.UpdateCallCount);

            m_world.AddRootSystem(system);
            m_world.Update(0.016f);
            Assert.AreEqual(2, system.UpdateCallCount);
        }

        [Test]
        public void LargeNumberOfSystems_AllExecuteCorrectly()
        {
            const int systemCount = 100;
            var updateSystems = new UpdateOnlySystem[systemCount];
            var lateUpdateSystems = new LateUpdateOnlySystem[systemCount];

            for (var i = 0; i < systemCount; i++)
            {
                updateSystems[i] = new UpdateOnlySystem();
                lateUpdateSystems[i] = new LateUpdateOnlySystem();
                m_world.AddRootSystem(updateSystems[i]);
                m_world.AddRootSystem(lateUpdateSystems[i]);
            }

            m_world.Update(0.016f);
            m_world.LateUpdate(0.016f);

            for (var i = 0; i < systemCount; i++)
            {
                Assert.AreEqual(1, updateSystems[i].UpdateCallCount, $"Update system {i} should be called once");
                Assert.AreEqual(1, lateUpdateSystems[i].LateUpdateCallCount, $"LateUpdate system {i} should be called once");
            }
        }

        #endregion

        #region Test System Classes

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

        private class UpdateAndFixedUpdateSystem : SystemBase
        {
            public int UpdateCallCount { get; private set; }
            public int LateUpdateCallCount { get; private set; }
            public int FixedUpdateCallCount { get; private set; }
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update | SystemUpdateMask.FixedUpdate;

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
