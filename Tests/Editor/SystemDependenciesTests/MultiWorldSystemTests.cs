using System.Linq;
using NUnit.Framework;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.SystemDependenciesTests
{
    [TestFixture]
    public class MultiWorldSystemTests : UnsafeEcsBaseTest
    {
        [Test]
        public void MultiWorldRootGroup_ExistsInBothWorlds()
        {
            // Verify the group exists in world 0
            var groupInWorld0 = WorldManager.Worlds[0].GetSystem<MultiWorldRootGroup>();
            Assert.IsNotNull(groupInWorld0, "MultiWorldRootGroup should exist in world 0");

            // Verify the group exists in world 1
            var groupInWorld1 = WorldManager.Worlds[1].GetSystem<MultiWorldRootGroup>();
            Assert.IsNotNull(groupInWorld1, "MultiWorldRootGroup should exist in world 1");

            // Verify these are different instances
            Assert.AreNotSame(groupInWorld0, groupInWorld1, "Groups should be different instances");
        }

        [Test]
        public void MultiWorldChildSystem_ExistsInBothWorlds()
        {
            // Get parent groups in each world
            var groupInWorld0 = WorldManager.Worlds[0].GetSystem<MultiWorldRootGroup>();
            var groupInWorld1 = WorldManager.Worlds[1].GetSystem<MultiWorldRootGroup>();

            // Check child system exists in world 0's group
            var childInWorld0 = groupInWorld0.systems.FirstOrDefault(s => s is MultiWorldChildSystem);
            Assert.IsNotNull(childInWorld0, "MultiWorldChildSystem should exist in world 0");

            // Check child system exists in world 1's group
            var childInWorld1 = groupInWorld1.systems.FirstOrDefault(s => s is MultiWorldChildSystem);
            Assert.IsNotNull(childInWorld1, "MultiWorldChildSystem should exist in world 1");

            // Verify these are different instances
            Assert.AreNotSame(childInWorld0, childInWorld1, "Child systems should be different instances");
        }

        [Test]
        public void ExplicitMultiWorldSystem_ExistsInBothWorlds()
        {
            // The system should be a root system in both worlds
            var systemInWorld0 = WorldManager.Worlds[0].GetSystem<ExplicitMultiWorldSystem>();
            Assert.IsNotNull(systemInWorld0, "ExplicitMultiWorldSystem should exist in world 0");

            var systemInWorld1 = WorldManager.Worlds[1].GetSystem<ExplicitMultiWorldSystem>();
            Assert.IsNotNull(systemInWorld1, "ExplicitMultiWorldSystem should exist in world 1");

            // Verify these are different instances
            Assert.AreNotSame(systemInWorld0, systemInWorld1, "Systems should be different instances");
        }

        [Test]
        public void GlobalSystem_ExistsInAllWorlds()
        {
            // The system should exist in all worlds
            foreach (var world in WorldManager.Worlds)
            {
                var system = world.GetSystem<GlobalSystem>();
                Assert.IsNotNull(system, $"GlobalSystem should exist in world #{WorldManager.Worlds.IndexOf(world)}");
            }

            // Verify these are different instances
            var system0 = WorldManager.Worlds[0].GetSystem<GlobalSystem>();
            var system1 = WorldManager.Worlds[1].GetSystem<GlobalSystem>();
            Assert.AreNotSame(system0, system1, "Global systems should be different instances");
        }

        [Test]
        public void GlobalGroup_ExistsInAllWorlds()
        {
            // The group should exist in all worlds
            foreach (var world in WorldManager.Worlds)
            {
                var group = world.GetSystem<GlobalGroup>();
                Assert.IsNotNull(group, $"GlobalGroup should exist in world #{WorldManager.Worlds.IndexOf(world)}");

                // Check child system exists in this world's group
                var childSystem = group.systems.FirstOrDefault(s => s is GlobalGroupChildSystem);
                Assert.IsNotNull(childSystem, $"GlobalGroupChildSystem should exist in world #{WorldManager.Worlds.IndexOf(world)}");
            }
        }

        [Test]
        public void DependentMultiWorldGroup_ExistsInCorrectWorlds()
        {
            // Should exist in worlds 0 and 2, but not 1
            var groupInWorld0 = WorldManager.Worlds[0].HasSystem<DependentMultiWorldGroup>();
            Assert.IsTrue(groupInWorld0, "DependentMultiWorldGroup should exist in world 0");

            var groupInWorld1 = WorldManager.Worlds[1].HasSystem<DependentMultiWorldGroup>();
            Assert.IsFalse(groupInWorld1, "DependentMultiWorldGroup should not exist in world 1");
        }
    }
}