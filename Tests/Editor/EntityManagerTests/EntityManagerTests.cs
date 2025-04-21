// EntityManagerTestBase.cs

using NUnit.Framework;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests
{
    public class UnsafeEcsQueryBaseTest : UnsafeEcsBaseTest
    {
        protected static ref EntityManager entityManager => ref WorldManager.Worlds[0].EntityManager;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }
    }
}