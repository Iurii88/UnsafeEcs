using NUnit.Framework;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.SystemTests
{
    [TestFixture]
    public class SystemProfilingTests : UnsafeEcsBaseTest
    {
        private World m_world;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_world = WorldManager.Worlds[0];
        }

#if UNITY_EDITOR
        #region BeginProfiling / EndProfiling Tests

        [Test]
        public void BeginProfiling_EndProfiling_SetsLastExecutionTimeMs()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            system.BeginProfiling();
            // Do some minimal work
            for (var i = 0; i < 1000; i++) { }
            system.EndProfiling();

            Assert.GreaterOrEqual(system.LastExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void LastExecutionTimeMs_IsZero_BeforeProfiling()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            Assert.AreEqual(0, system.LastExecutionTimeMs);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void AverageExecutionTimeMs_IsZero_BeforeProfiling()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            Assert.AreEqual(0, system.AverageExecutionTimeMs);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void AverageExecutionTimeMs_EqualsLastExecutionTimeMs_AfterFirstSample()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            system.BeginProfiling();
            System.Threading.Thread.Sleep(1); // Ensure some measurable time
            system.EndProfiling();

            // After first sample, average should equal last
            Assert.AreEqual(system.LastExecutionTimeMs, system.AverageExecutionTimeMs, 0.001);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void AverageExecutionTimeMs_SmoothsOverMultipleSamples()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            // Take multiple samples
            for (var i = 0; i < 5; i++)
            {
                system.BeginProfiling();
                System.Threading.Thread.Sleep(1);
                system.EndProfiling();
            }

            // Average should be calculated (not zero, not equal to last necessarily)
            Assert.Greater(system.AverageExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void AverageExecutionTimeMs_UsesExponentialMovingAverage_AfterSmoothingFactor()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            // Take more than SmoothingFactor (10) samples
            for (var i = 0; i < 15; i++)
            {
                system.BeginProfiling();
                // Minimal work
                system.EndProfiling();
            }

            // Average should be non-negative after many samples
            Assert.GreaterOrEqual(system.AverageExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void Profiling_CanBeCalledMultipleTimes()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            // Should not throw when called multiple times
            Assert.DoesNotThrow(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    system.BeginProfiling();
                    system.EndProfiling();
                }
            });

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void Profiling_EndWithoutBegin_DoesNotThrow()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            // Calling EndProfiling without BeginProfiling should not throw
            // (though it will give incorrect values)
            Assert.DoesNotThrow(() => system.EndProfiling());

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void Profiling_MultipleBegins_LastOneIsUsed()
        {
            var system = new ProfilingTestSystem();
            m_world.AddRootSystem(system);

            // First measure: single begin/end with 50ms sleep
            system.BeginProfiling();
            System.Threading.Thread.Sleep(50);
            system.EndProfiling();
            var singleSleepTime = system.LastExecutionTimeMs;

            // Second measure: begin, sleep 50ms, begin again (restart), then end immediately
            system.BeginProfiling();
            System.Threading.Thread.Sleep(50);
            system.BeginProfiling(); // Restart timing - the 50ms above should be discarded
            system.EndProfiling();
            var restartedTime = system.LastExecutionTimeMs;

            // The restarted time should be significantly less than the single sleep time
            // because the second BeginProfiling restarts the stopwatch
            Assert.Less(restartedTime, singleSleepTime * 0.5,
                $"Second BeginProfiling should restart timer. Single: {singleSleepTime}ms, Restarted: {restartedTime}ms");

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        #endregion

        #region World Update Profiling Integration Tests

        [Test]
        public void WorldUpdate_ProfilesSystems_LastExecutionTimeIsSet()
        {
            var system = new SlowUpdateSystem();
            m_world.AddRootSystem(system);

            // LastExecutionTimeMs should be 0 initially
            Assert.AreEqual(0, system.LastExecutionTimeMs);

            // Trigger a world update - World.Update wraps OnUpdate with profiling
            m_world.Update(0.016f);

            // After update, LastExecutionTimeMs should be set (greater than 0 due to sleep)
            Assert.Greater(system.LastExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void WorldLateUpdate_ProfilesSystems_LastExecutionTimeIsSet()
        {
            var system = new SlowLateUpdateSystem();
            m_world.AddRootSystem(system);

            Assert.AreEqual(0, system.LastExecutionTimeMs);

            m_world.LateUpdate(0.016f);

            Assert.Greater(system.LastExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void WorldFixedUpdate_ProfilesSystems_LastExecutionTimeIsSet()
        {
            var system = new SlowFixedUpdateSystem();
            m_world.AddRootSystem(system);

            Assert.AreEqual(0, system.LastExecutionTimeMs);

            m_world.FixedUpdate(0.02f);

            Assert.Greater(system.LastExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        [Test]
        public void WorldUpdate_MultipleUpdates_AverageIsCalculated()
        {
            var system = new SlowUpdateSystem();
            m_world.AddRootSystem(system);

            // Multiple updates to get average
            for (var i = 0; i < 5; i++)
            {
                m_world.Update(0.016f);
            }

            // Both last and average should be set
            Assert.Greater(system.LastExecutionTimeMs, 0);
            Assert.Greater(system.AverageExecutionTimeMs, 0);

            // Cleanup
            m_world.RemoveRootSystem(system);
        }

        #endregion

        #region Test System Classes

        private class ProfilingTestSystem : SystemBase
        {
        }

        private class SlowUpdateSystem : SystemBase
        {
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update;

            public override void OnUpdate()
            {
                System.Threading.Thread.Sleep(1); // Ensure measurable time
            }
        }

        private class SlowLateUpdateSystem : SystemBase
        {
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.LateUpdate;

            public override void OnLateUpdate()
            {
                System.Threading.Thread.Sleep(1);
            }
        }

        private class SlowFixedUpdateSystem : SystemBase
        {
            public override SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.FixedUpdate;

            public override void OnFixedUpdate()
            {
                System.Threading.Thread.Sleep(1);
            }
        }

        #endregion
#endif
    }
}
