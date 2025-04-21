using NUnit.Framework;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.SystemDependenciesTests
{
    [TestFixture]
    public class WorldStructureTests : UnsafeEcsBaseTest
    {
        [Test]
        public void Worlds_AreCreated_Correctly()
        {
            // We now expect 3 worlds (0, 1, 2) based on the multi-world systems
            Assert.AreEqual(3, WorldManager.Worlds.Count);
            Assert.IsNotNull(WorldManager.Worlds[0], "World 0 not created");
            Assert.IsNotNull(WorldManager.Worlds[1], "World 1 not created");
            Assert.IsNotNull(WorldManager.Worlds[2], "World 2 not created");
        }

        [Test]
        public void World0_HasCorrectSystemStructure()
        {
            var world0 = WorldManager.Worlds[0];

            // Original root systems (4):
            // - World0Group1
            // - World0Group2
            // - World0Group3
            // - World0RootSystem1

            // Multi-world root systems (5):
            // - MultiWorldRootGroup (worlds 0,1)
            // - ExplicitMultiWorldSystem (worlds 0,1)
            // - GlobalSystem (all worlds)
            // - GlobalGroup (all worlds)
            // - DependentMultiWorldGroup (worlds 0,2)

            // Total root systems: 4 (original) + 5 (multi-world) = 9
            Assert.AreEqual(9, world0.rootSystems.Count);

            // Verify all root systems
            Assert.IsTrue(world0.HasSystem<World0Group1>(), "Missing World0Group1");
            Assert.IsTrue(world0.HasSystem<World0Group2>(), "Missing World0Group2");
            Assert.IsTrue(world0.HasSystem<World0Group3>(), "Missing World0Group3");
            Assert.IsTrue(world0.HasSystem<World0RootSystem1>(), "Missing World0RootSystem1");

            Assert.IsTrue(world0.HasSystem<MultiWorldRootGroup>(), "Missing MultiWorldRootGroup");
            Assert.IsTrue(world0.HasSystem<ExplicitMultiWorldSystem>(), "Missing ExplicitMultiWorldSystem");
            Assert.IsTrue(world0.HasSystem<GlobalSystem>(), "Missing GlobalSystem");
            Assert.IsTrue(world0.HasSystem<GlobalGroup>(), "Missing GlobalGroup");
            Assert.IsTrue(world0.HasSystem<DependentMultiWorldGroup>(), "Missing DependentMultiWorldGroup");

            // Check child systems (not root systems)
            Assert.IsTrue(world0.HasSystem<MultiWorldChildSystem>(), "Missing MultiWorldChildSystem");
            Assert.IsTrue(world0.HasSystem<GlobalGroupChildSystem>(), "Missing GlobalGroupChildSystem");
            Assert.IsTrue(world0.HasSystem<DependentChildSystem>(), "Missing DependentChildSystem");
        }

        [Test]
        public void World1_HasCorrectSystemStructure()
        {
            var world1 = WorldManager.Worlds[1];

            // Original root systems (2):
            // - World1Group1
            // - World1RootSystem1

            // Multi-world root systems (4):
            // - MultiWorldRootGroup (worlds 0,1)
            // - ExplicitMultiWorldSystem (worlds 0,1)
            // - GlobalSystem (all worlds)
            // - GlobalGroup (all worlds)

            // Total root systems: 2 (original) + 4 (multi-world) = 6
            Assert.AreEqual(6, world1.rootSystems.Count);

            Assert.IsTrue(world1.HasSystem<World1Group1>(), "Missing World1Group1");
            Assert.IsTrue(world1.HasSystem<World1RootSystem1>(), "Missing World1RootSystem1");

            Assert.IsTrue(world1.HasSystem<MultiWorldRootGroup>(), "Missing MultiWorldRootGroup");
            Assert.IsTrue(world1.HasSystem<ExplicitMultiWorldSystem>(), "Missing ExplicitMultiWorldSystem");
            Assert.IsTrue(world1.HasSystem<GlobalSystem>(), "Missing GlobalSystem");
            Assert.IsTrue(world1.HasSystem<GlobalGroup>(), "Missing GlobalGroup");

            // Check child systems
            Assert.IsTrue(world1.HasSystem<MultiWorldChildSystem>(), "Missing MultiWorldChildSystem");
            Assert.IsTrue(world1.HasSystem<GlobalGroupChildSystem>(), "Missing GlobalGroupChildSystem");
        }

        [Test]
        public void World2_HasCorrectSystemStructure()
        {
            var world2 = WorldManager.Worlds[2];

            // Multi-world root systems (5):
            // - ExplicitMultiWorldSystem (worlds 0,1) - NOT in world 2!
            // - GlobalSystem (all worlds)
            // - GlobalGroup (all worlds)
            // - DependentMultiWorldGroup (worlds 0,2)
            // - MultiWorldSystem1 (world 2)
            // - MultiWorldSystem2 (world 2)

            // Actually, ExplicitMultiWorldSystem is only in worlds 0,1
            // So root systems should be:
            // 1. GlobalSystem
            // 2. GlobalGroup
            // 3. DependentMultiWorldGroup
            // 4. MultiWorldSystem1
            // 5. MultiWorldSystem2

            Assert.AreEqual(5, world2.rootSystems.Count);

            Assert.IsTrue(world2.HasSystem<GlobalSystem>(), "Missing GlobalSystem");
            Assert.IsTrue(world2.HasSystem<GlobalGroup>(), "Missing GlobalGroup");
            Assert.IsTrue(world2.HasSystem<DependentMultiWorldGroup>(), "Missing DependentMultiWorldGroup");
            Assert.IsTrue(world2.HasSystem<MultiWorldSystem1>(), "Missing MultiWorldSystem1");
            Assert.IsTrue(world2.HasSystem<MultiWorldSystem2>(), "Missing MultiWorldSystem2");

            // Check child systems
            Assert.IsTrue(world2.HasSystem<GlobalGroupChildSystem>(), "Missing GlobalGroupChildSystem");
            Assert.IsTrue(world2.HasSystem<DependentChildSystem>(), "Missing DependentChildSystem");
        }
    }
}