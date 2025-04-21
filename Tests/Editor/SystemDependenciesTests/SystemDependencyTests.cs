using NUnit.Framework;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.SystemDependenciesTests
{
    [TestFixture]
    public class SystemDependencyTests: UnsafeEcsBaseTest
    {
        [Test]
        public void RootSystems_AreInCorrectWorlds()
        {
            Assert.IsTrue(WorldManager.Worlds[0].HasSystem<World0RootSystem1>(),
                "World0RootSystem1 not in World 0");

            Assert.IsTrue(WorldManager.Worlds[1].HasSystem<World1RootSystem1>(),
                "World1RootSystem1 not in World 1");
        }

        [Test]
        public void AllSystems_HaveCorrectWorldReferences()
        {
            foreach (var world in WorldManager.Worlds)
            {
                foreach (var system in world.rootSystems)
                {
                    Assert.AreEqual(world, system.world,
                        $"Group {system.GetType().Name} has incorrect world reference");

                    if (system is SystemGroup group)
                    {
                        foreach (var childSystem in group.systems)
                        {
                            Assert.AreEqual(world, childSystem.world,
                                $"System {childSystem.GetType().Name} has incorrect world reference");
                        }
                    }
                }
            }
        }

        [Test]
        public void ComplexDependencies_AreResolvedCorrectly()
        {
            var group = WorldManager.Worlds[0].GetSystem<World0Group1>();
            var systems = group.systems;

            Assert.IsInstanceOf<World0System3>(systems[0], "Position 0 incorrect");
            Assert.IsInstanceOf<World0System1>(systems[1], "Position 1 incorrect");
            Assert.IsInstanceOf<World0System2>(systems[2], "Position 2 incorrect");
            Assert.IsInstanceOf<World0System6>(systems[3], "Position 3 incorrect");
        }
    }
}