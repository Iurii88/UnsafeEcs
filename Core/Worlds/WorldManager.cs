using System.Collections.Generic;
using UnityEngine;
using UnsafeEcs.Core.Components;
using Object = UnityEngine.Object;

namespace UnsafeEcs.Core.Worlds
{
    public static class WorldManager
    {
        public static readonly List<World> Worlds = new();

        public static void Initialize()
        {
            ComponentTypeManager.Initialize();
            Worlds.Clear();

            var go = new GameObject();
            var worldUpdater = go.AddComponent<WorldUpdater>();
            worldUpdater.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            Object.DontDestroyOnLoad(go);
        }
        
        public static void InitializeForTests()
        {
            ComponentTypeManager.Initialize();
            Worlds.Clear();

            var go = new GameObject();
            var worldUpdater = go.AddComponent<WorldUpdater>();
            worldUpdater.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        }

        public static World CreateWorld(int initialCapacity = 0)
        {
            var world = new World(initialCapacity);
            Worlds.Add(world);
            return world;
        }

        public static void DestroyWorld(ref World world)
        {
            for (var i = 0; i < Worlds.Count; i++)
            {
                var currentWorld = Worlds[i];
                if (!currentWorld.Equals(world))
                    continue;

                Worlds.Remove(world);
                world.Dispose();
                break;
            }
        }

        public static void DestroyAllWorlds()
        {
            for (var i = 0; i < Worlds.Count; i++)
            {
                var world = Worlds[i];
                world.Dispose();
            }

            Worlds.Clear();
        }

        public static void Update(float deltaTime)
        {
            foreach (var world in Worlds)
                world.Update(deltaTime);
        }

        public static void FixedUpdate(float deltaTime)
        {
            foreach (var world in Worlds)
                world.FixedUpdate(deltaTime);
        }

        public static void OnDestroy()
        {
            foreach (var world in Worlds)
                world.Dispose();
            ComponentTypeManager.Dispose();
        }
    }

    internal class WorldUpdater : MonoBehaviour
    {
        private void OnDestroy()
        {
            WorldManager.OnDestroy();
        }

        private void Update()
        {
            WorldManager.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            WorldManager.FixedUpdate(Time.fixedDeltaTime);
        }
    }
}