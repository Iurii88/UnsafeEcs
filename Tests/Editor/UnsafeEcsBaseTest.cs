// EntityManagerTestBase.cs

using NUnit.Framework;

namespace UnsafeEcs.Tests.Editor
{
    public class UnsafeEcsBaseTest
    {
        [SetUp]
        public virtual void SetUp()
        {
            CustomBootstrap.Initialize();
        }

        [TearDown]
        public virtual void TearDown()
        {
            CustomBootstrap.ShutDown();
        }
    }
}