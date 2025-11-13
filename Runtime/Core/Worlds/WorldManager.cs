using System;
using System.Collections.Generic;
using UnityEngine;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Components.Managed;
using Object = UnityEngine.Object;

namespace UnsafeEcs.Core.Worlds
{
    public static class WorldManager
    {
        public static readonly List<World> Worlds = new();

        private static GameObject m_worldManagerGo;
        private static bool m_dontDestroyOnLoadPrivate;

        public static void Initialize(bool dontDestroyOnLoad = true)
        {
            m_dontDestroyOnLoadPrivate = dontDestroyOnLoad;
            TypeManager.Initialize();
            ManagedTypeManager.Initialize();

            m_worldManagerGo = new GameObject
            {
                name = "UnsafeEcs World Manager"
            };
            var worldUpdater = m_worldManagerGo.AddComponent<WorldUpdater>();
            
            if (dontDestroyOnLoad)
            {
                worldUpdater.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                Object.DontDestroyOnLoad(m_worldManagerGo);
            }
        }

        public static void InitializeForTests()
        {
            TypeManager.Initialize();
            ManagedTypeManager.Initialize();

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

        public static void DestroyWorld(World world)
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
                try
                {
                    world.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            Worlds.Clear();
        }
        
        public static void OnDestroy()
        {
            foreach (var world in Worlds)
                world.Dispose();

            Worlds.Clear();
            TypeManager.Dispose();
            ManagedTypeManager.Dispose();

            if (!m_dontDestroyOnLoadPrivate)
                Object.Destroy(m_worldManagerGo);
        }

        public static void Update(float deltaTime)
        {
            foreach (var world in Worlds)
                world.Update(deltaTime);
        }

        public static void LateUpdate(float deltaTime)
        {
            foreach (var world in Worlds)
                world.LateUpdate(deltaTime);
        }

        public static void FixedUpdate(float deltaTime)
        {
            foreach (var world in Worlds)
                world.FixedUpdate(deltaTime);
        }
    }

    internal class WorldUpdater : MonoBehaviour
    {
        private void Update()
        {
            WorldManager.Update(Time.deltaTime);
        }

        private void LateUpdate()
        {
            WorldManager.LateUpdate(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            WorldManager.FixedUpdate(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            WorldManager.OnDestroy();
        }
    }
}