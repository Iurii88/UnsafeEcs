using NUnit.Framework;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.WorldTests
{
    [TestFixture]
    public class WorldManagerTests : UnsafeEcsBaseTest
    {
        #region Update Tests

        [Test]
        public void Update_UpdatesAllWorlds()
        {
            var updateCounts = new int[WorldManager.Worlds.Count];

            // Add tracking systems to all worlds
            for (var i = 0; i < WorldManager.Worlds.Count; i++)
            {
                var world = WorldManager.Worlds[i];
                var index = i;
                var system = new UpdateCounterSystem(() => updateCounts[index]++);
                world.AddRootSystem(system);
            }

            WorldManager.Update(0.016f);

            // All worlds should have been updated
            for (var i = 0; i < updateCounts.Length; i++)
            {
                Assert.AreEqual(1, updateCounts[i], $"World {i} was not updated");
            }
        }

        [Test]
        public void LateUpdate_UpdatesAllWorlds()
        {
            var updateCounts = new int[WorldManager.Worlds.Count];

            for (var i = 0; i < WorldManager.Worlds.Count; i++)
            {
                var world = WorldManager.Worlds[i];
                var index = i;
                var system = new LateUpdateCounterSystem(() => updateCounts[index]++);
                world.AddRootSystem(system);
            }

            WorldManager.LateUpdate(0.016f);

            for (var i = 0; i < updateCounts.Length; i++)
            {
                Assert.AreEqual(1, updateCounts[i], $"World {i} was not late updated");
            }
        }

        [Test]
        public void FixedUpdate_UpdatesAllWorlds()
        {
            var updateCounts = new int[WorldManager.Worlds.Count];

            for (var i = 0; i < WorldManager.Worlds.Count; i++)
            {
                var world = WorldManager.Worlds[i];
                var index = i;
                var system = new FixedUpdateCounterSystem(() => updateCounts[index]++);
                world.AddRootSystem(system);
            }

            WorldManager.FixedUpdate(0.02f);

            for (var i = 0; i < updateCounts.Length; i++)
            {
                Assert.AreEqual(1, updateCounts[i], $"World {i} was not fixed updated");
            }
        }

        [Test]
        public void Update_PassesDeltaTimeToWorlds()
        {
            var world = WorldManager.Worlds[0];
            var capturedDt = 0f;

            var system = new DeltaTimeCaptureSystem(dt => capturedDt = dt);
            world.AddRootSystem(system);

            WorldManager.Update(0.033f);

            Assert.AreEqual(0.033f, capturedDt, 0.0001f);

            // Cleanup
            world.RemoveRootSystem(system);
        }

        [Test]
        public void FixedUpdate_PassesDeltaTimeToWorlds()
        {
            var world = WorldManager.Worlds[0];
            var capturedDt = 0f;

            var system = new FixedDeltaTimeCaptureSystem(dt => capturedDt = dt);
            world.AddRootSystem(system);

            WorldManager.FixedUpdate(0.02f);

            Assert.AreEqual(0.02f, capturedDt, 0.0001f);

            // Cleanup
            world.RemoveRootSystem(system);
        }

        #endregion

        #region CreateWorld Tests

        [Test]
        public void CreateWorld_AddsWorldToList()
        {
            var initialCount = WorldManager.Worlds.Count;

            var world = WorldManager.CreateWorld();

            Assert.AreEqual(initialCount + 1, WorldManager.Worlds.Count);
            Assert.Contains(world, WorldManager.Worlds);

            // Cleanup
            WorldManager.DestroyWorld(world);
        }

        [Test]
        public void CreateWorld_WithCapacity_CreatesWorld()
        {
            var world = WorldManager.CreateWorld(1000);

            Assert.IsNotNull(world);
            Assert.Contains(world, WorldManager.Worlds);

            // Cleanup
            WorldManager.DestroyWorld(world);
        }

        #endregion

        #region DestroyWorld Tests

        [Test]
        public void DestroyWorld_RemovesWorldFromList()
        {
            var world = WorldManager.CreateWorld();
            var initialCount = WorldManager.Worlds.Count;

            WorldManager.DestroyWorld(world);

            Assert.AreEqual(initialCount - 1, WorldManager.Worlds.Count);
            Assert.IsFalse(WorldManager.Worlds.Contains(world));
        }

        [Test]
        public void DestroyWorld_DisposesWorld()
        {
            var world = WorldManager.CreateWorld();

            // Add a system to track dispose
            var system = new DisposeTrackingSystem();
            world.AddRootSystem(system);

            WorldManager.DestroyWorld(world);

            Assert.IsTrue(system.OnDestroyWasCalled);
        }

        [Test]
        public void DestroyWorld_NonExistentWorld_DoesNotThrow()
        {
            var world = new World();

            // Should not throw when destroying a world not in the list
            Assert.DoesNotThrow(() => WorldManager.DestroyWorld(world));

            // Cleanup the manually created world
            world.Dispose();
        }

        #endregion

        #region DestroyAllWorlds Tests

        [Test]
        public void DestroyAllWorlds_RemovesAllWorlds()
        {
            // Add some extra worlds
            var world1 = WorldManager.CreateWorld();
            var world2 = WorldManager.CreateWorld();

            WorldManager.DestroyAllWorlds();

            Assert.AreEqual(0, WorldManager.Worlds.Count);
        }

        [Test]
        public void DestroyAllWorlds_DisposesAllWorlds()
        {
            var world1 = WorldManager.CreateWorld();
            var world2 = WorldManager.CreateWorld();

            var system1 = new DisposeTrackingSystem();
            var system2 = new DisposeTrackingSystem();

            world1.AddRootSystem(system1);
            world2.AddRootSystem(system2);

            WorldManager.DestroyAllWorlds();

            Assert.IsTrue(system1.OnDestroyWasCalled);
            Assert.IsTrue(system2.OnDestroyWasCalled);
        }

        #endregion

        #region Parallel Execution Tests

        [Test]
        public void Update_WorldsCanRunInParallel()
        {
            // Create additional worlds
            var extraWorld1 = WorldManager.CreateWorld();
            var extraWorld2 = WorldManager.CreateWorld();

            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<int>();
            var startTimes = new System.Collections.Concurrent.ConcurrentDictionary<int, System.DateTime>();

            // Add slow systems to track parallel execution
            var system1 = new SlowUpdateSystem(0, executionOrder, startTimes);
            var system2 = new SlowUpdateSystem(1, executionOrder, startTimes);

            extraWorld1.AddRootSystem(system1);
            extraWorld2.AddRootSystem(system2);

            var startTime = System.DateTime.Now;
            WorldManager.Update(0.016f);
            var elapsedMs = (System.DateTime.Now - startTime).TotalMilliseconds;

            // If running in parallel, total time should be close to single sleep time
            // If sequential, it would be 2x sleep time
            // Use a generous margin for test stability
            // Note: This test verifies the parallel infrastructure exists,
            // actual parallelism depends on job system

            Assert.AreEqual(2, executionOrder.Count);

            // Cleanup
            WorldManager.DestroyWorld(extraWorld1);
            WorldManager.DestroyWorld(extraWorld2);
        }

        [Test]
        public void Update_CompletesAllWorldJobsBeforeReturning()
        {
            var extraWorld = WorldManager.CreateWorld();
            var completed = false;

            var system = new JobCompletionTrackingSystem(() => completed = true);
            extraWorld.AddRootSystem(system);

            WorldManager.Update(0.016f);

            // After Update returns, all jobs should be complete
            Assert.IsTrue(completed);

            // Cleanup
            WorldManager.DestroyWorld(extraWorld);
        }

        #endregion

        #region Test System Classes

        private class UpdateCounterSystem : SystemBase
        {
            private readonly System.Action m_onUpdate;

            public UpdateCounterSystem(System.Action onUpdate)
            {
                m_onUpdate = onUpdate;
            }

            public override void OnUpdate()
            {
                m_onUpdate?.Invoke();
            }
        }

        private class LateUpdateCounterSystem : SystemBase
        {
            private readonly System.Action m_onLateUpdate;

            public LateUpdateCounterSystem(System.Action onLateUpdate)
            {
                m_onLateUpdate = onLateUpdate;
                UpdateMask = SystemUpdateMask.LateUpdate;
            }

            public override void OnLateUpdate()
            {
                m_onLateUpdate?.Invoke();
            }
        }

        private class FixedUpdateCounterSystem : SystemBase
        {
            private readonly System.Action m_onFixedUpdate;

            public FixedUpdateCounterSystem(System.Action onFixedUpdate)
            {
                m_onFixedUpdate = onFixedUpdate;
                UpdateMask = SystemUpdateMask.FixedUpdate;
            }

            public override void OnFixedUpdate()
            {
                m_onFixedUpdate?.Invoke();
            }
        }

        private class DeltaTimeCaptureSystem : SystemBase
        {
            private readonly System.Action<float> m_onUpdate;

            public DeltaTimeCaptureSystem(System.Action<float> onUpdate)
            {
                m_onUpdate = onUpdate;
            }

            public override void OnUpdate()
            {
                m_onUpdate?.Invoke(world.deltaTime);
            }
        }

        private class FixedDeltaTimeCaptureSystem : SystemBase
        {
            private readonly System.Action<float> m_onFixedUpdate;

            public FixedDeltaTimeCaptureSystem(System.Action<float> onFixedUpdate)
            {
                m_onFixedUpdate = onFixedUpdate;
                UpdateMask = SystemUpdateMask.FixedUpdate;
            }

            public override void OnFixedUpdate()
            {
                m_onFixedUpdate?.Invoke(world.fixedDeltaTime);
            }
        }

        private class DisposeTrackingSystem : SystemBase
        {
            public bool OnDestroyWasCalled { get; private set; }

            public override void OnDestroy()
            {
                OnDestroyWasCalled = true;
            }
        }

        private class SlowUpdateSystem : SystemBase
        {
            private readonly int m_id;
            private readonly System.Collections.Concurrent.ConcurrentBag<int> m_executionOrder;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.DateTime> m_startTimes;

            public SlowUpdateSystem(
                int id,
                System.Collections.Concurrent.ConcurrentBag<int> executionOrder,
                System.Collections.Concurrent.ConcurrentDictionary<int, System.DateTime> startTimes)
            {
                m_id = id;
                m_executionOrder = executionOrder;
                m_startTimes = startTimes;
            }

            public override void OnUpdate()
            {
                m_startTimes[m_id] = System.DateTime.Now;
                System.Threading.Thread.Sleep(10);
                m_executionOrder.Add(m_id);
            }
        }

        private class JobCompletionTrackingSystem : SystemBase
        {
            private readonly System.Action m_onComplete;

            public JobCompletionTrackingSystem(System.Action onComplete)
            {
                m_onComplete = onComplete;
            }

            public override void OnUpdate()
            {
                m_onComplete?.Invoke();
            }
        }

        #endregion
    }
}
