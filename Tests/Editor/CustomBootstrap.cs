using System.Reflection;
using UnsafeEcs.Core.Bootstrap;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Tests.Editor
{
    public static class CustomBootstrap
    {
        public static void Initialize()
        {
            var gameAssembly = Assembly.Load("UnsafeEcs.EditorTests");
            var assemblies = new[]
            {
                gameAssembly
            };
            WorldBootstrap.InitializeForTests(assemblies, WorldBootstrap.LogLevel.Minimal);
        }

        public static void ShutDown()
        {
            WorldManager.OnDestroy();
        }
    }
}