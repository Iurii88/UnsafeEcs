using System;
using System.Collections.Generic;
using Unity.Jobs;
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

            m_worldManagerGo = new GameObject
            {
                name = "UnsafeEcs World Manager (Test)"
            };
            var worldUpdater = m_worldManagerGo.AddComponent<WorldUpdater>();
            worldUpdater.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            m_dontDestroyOnLoadPrivate = false;
        }

        public static World CreateWorld(int initialCapacity = 0, string name = null)
        {
            var world = new World(initialCapacity);
            if (!string.IsNullOrEmpty(name))
                world.SetName(name);
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

        /// <summary>
        /// Bakes all registered worlds for optimal update performance.
        /// Call this after all systems have been added to all worlds.
        /// </summary>
        public static void BakeAllWorlds()
        {
            for (var i = 0; i < Worlds.Count; i++)
            {
                Worlds[i].Bake();
            }
        }

        public static void Destroy()
        {
            if (m_worldManagerGo == null)
                return;
            
            // Use DestroyImmediate in edit mode, Destroy in play mode
            if (Application.isPlaying)
                Object.Destroy(m_worldManagerGo);
            else
                Object.DestroyImmediate(m_worldManagerGo);

            m_worldManagerGo = null;
        }

        public static void OnDestroy()
        {
            foreach (var world in Worlds)
                world.Dispose();

            Worlds.Clear();
            TypeManager.Dispose();
            ManagedTypeManager.Dispose();

            if (m_worldManagerGo != null && !m_dontDestroyOnLoadPrivate)
            {
                // Use DestroyImmediate in edit mode (tests), Destroy in play mode
                if (Application.isPlaying)
                    Object.Destroy(m_worldManagerGo);
                else
                    Object.DestroyImmediate(m_worldManagerGo);

                m_worldManagerGo = null;
            }
        }

        public static void Update(float deltaTime)
        {
            // Schedule all worlds (jobs run in parallel across worker threads)
            var combinedHandle = default(JobHandle);
            for (var index = 0; index < Worlds.Count; index++)
            {
                var world = Worlds[index];
                var worldHandle = world.Update(deltaTime);
                combinedHandle = JobHandle.CombineDependencies(combinedHandle, worldHandle);
            }

            // Wait for all worlds to complete
            combinedHandle.Complete();
        }

        public static void LateUpdate(float deltaTime)
        {
            var combinedHandle = default(JobHandle);
            for (var index = 0; index < Worlds.Count; index++)
            {
                var world = Worlds[index];
                var worldHandle = world.LateUpdate(deltaTime);
                combinedHandle = JobHandle.CombineDependencies(combinedHandle, worldHandle);
            }

            combinedHandle.Complete();
        }

        public static void FixedUpdate(float deltaTime)
        {
            var combinedHandle = default(JobHandle);
            for (var index = 0; index < Worlds.Count; index++)
            {
                var world = Worlds[index];
                var worldHandle = world.FixedUpdate(deltaTime);
                combinedHandle = JobHandle.CombineDependencies(combinedHandle, worldHandle);
            }

            combinedHandle.Complete();
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