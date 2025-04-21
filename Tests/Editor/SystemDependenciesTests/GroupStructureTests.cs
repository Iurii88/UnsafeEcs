using System.Linq;
using NUnit.Framework;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.SystemDependenciesTests
{
    [TestFixture]
    public class GroupStructureTests : UnsafeEcsBaseTest
    {
        [Test]
        public void World0Group1_HasCorrectSystemsAndOrder()
        {
            var group = WorldManager.Worlds[0].GetSystem<World0Group1>();

            Assert.AreEqual(4, group.systems.Count);
            Assert.IsTrue(group.systems.Any(s => s is World0System1), "Missing World0System1");
            Assert.IsTrue(group.systems.Any(s => s is World0System2), "Missing World0System2");
            Assert.IsTrue(group.systems.Any(s => s is World0System3), "Missing World0System3");
            Assert.IsTrue(group.systems.Any(s => s is World0System6), "Missing World0System6");

            var systems = group.systems;
            Assert.IsInstanceOf<World0System3>(systems[0], "First should be World0System3");
            Assert.IsInstanceOf<World0System1>(systems[1], "Second should be World0System1");
            Assert.IsInstanceOf<World0System2>(systems[2], "Third should be World0System2");
            Assert.IsInstanceOf<World0System6>(systems[3], "Fourth should be World0System6");
        }

        [Test]
        public void World0Group3_HasCorrectSystemsAndOrder()
        {
            var group = WorldManager.Worlds[0].GetSystem<World0Group3>();

            Assert.AreEqual(2, group.systems.Count);
            Assert.IsTrue(group.systems.Any(s => s is World0System4), "Missing World0System4");
            Assert.IsTrue(group.systems.Any(s => s is World0System5), "Missing World0System5");

            var systems = group.systems;
            Assert.IsInstanceOf<World0System5>(systems[0], "First should be World0System5");
            Assert.IsInstanceOf<World0System4>(systems[1], "Second should be World0System4");
        }

        [Test]
        public void World1Group1_HasCorrectSystemsAndOrder()
        {
            var group = WorldManager.Worlds[1].GetSystem<World1Group1>();

            Assert.AreEqual(2, group.systems.Count);
            Assert.IsTrue(group.systems.Any(s => s is World1System1), "Missing World1System1");
            Assert.IsTrue(group.systems.Any(s => s is World1System2), "Missing World1System2");

            var systems = group.systems;
            Assert.IsInstanceOf<World1System1>(systems[0], "First should be World1System1");
            Assert.IsInstanceOf<World1System2>(systems[1], "Second should be World1System2");
        }
    }
}