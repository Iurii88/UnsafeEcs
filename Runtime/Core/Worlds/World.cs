using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnsafeEcs.Core.Components.Managed;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Utils;

namespace UnsafeEcs.Core.Worlds
{
    public class World : IDisposable
    {
        public Action<SystemBase> onSystemAdded;

        public readonly List<SystemBase> rootSystems = new();
        public readonly Dictionary<Type, SystemBase> systemByType = new();

        // Pre-filtered lists for each update type to avoid checking UpdateMask every frame
        private readonly List<SystemBase> m_updateSystems = new();
        private readonly List<SystemBase> m_lateUpdateSystems = new();
        private readonly List<SystemBase> m_fixedUpdateSystems = new();

        // Baked arrays for maximum performance - eliminates virtual dispatch
        private Action[] m_bakedUpdateActions;
        private Action[] m_bakedLateUpdateActions;
        private Action[] m_bakedFixedUpdateActions;
        private SystemBase[] m_bakedUpdateSystemRefs;
        private SystemBase[] m_bakedLateUpdateSystemRefs;
        private SystemBase[] m_bakedFixedUpdateSystemRefs;
        private int m_bakedUpdateCount;
        private int m_bakedLateUpdateCount;
        private int m_bakedFixedUpdateCount;
        private bool m_isBaked;

#if UNITY_EDITOR
        private Action[] m_bakedUpdateBeginProfiling;
        private Action[] m_bakedUpdateEndProfiling;
        private Action[] m_bakedLateUpdateBeginProfiling;
        private Action[] m_bakedLateUpdateEndProfiling;
        private Action[] m_bakedFixedUpdateBeginProfiling;
        private Action[] m_bakedFixedUpdateEndProfiling;
#endif
        public float deltaTime;
        public float fixedDeltaTime;
        public float elapsedDeltaTime;
        public float elapsedFixedDeltaTime;
        public ReferenceWrapper<EntityManager> entityManagerWrapper;

        public readonly ManagedStorage managedStorage = new();
        public readonly WorldData data = new();
        private EntityManager m_entityManager;

        public World()
        {
            m_entityManager = new EntityManager(this, EntityManager.InitialEntityCapacity);
            m_entityManager.Initialize();
            entityManagerWrapper = new ReferenceWrapper<EntityManager>(ref m_entityManager);
        }

        public World(int initialCapacity = 0)
        {
            m_entityManager = new EntityManager(this, initialCapacity);
            m_entityManager.Initialize();
            entityManagerWrapper = new ReferenceWrapper<EntityManager>(ref m_entityManager);
        }

        public ref EntityManager EntityManager => ref m_entityManager;

        public void Dispose()
        {
            for (var index = 0; index < rootSystems.Count; index++)
            {
                var regularSystem = rootSystems[index];
                regularSystem.OnDestroy();
            }

            m_entityManager.Dispose();
        }

        public JobHandle Update(float dt, JobHandle inputDependency = default)
        {
            deltaTime = dt;
            elapsedDeltaTime += dt;
            var dependency = inputDependency;

            if (m_isBaked)
            {
                // FAST PATH: Use baked arrays - no virtual dispatch
                var actions = m_bakedUpdateActions;
                var systemRefs = m_bakedUpdateSystemRefs;
                var count = m_bakedUpdateCount;
#if UNITY_EDITOR
                var beginProfiling = m_bakedUpdateBeginProfiling;
                var endProfiling = m_bakedUpdateEndProfiling;
#endif

                for (var i = 0; i < count; i++)
                {
                    systemRefs[i].dependency = dependency;
#if UNITY_EDITOR
                    beginProfiling[i]();
#endif
                    actions[i]();
#if UNITY_EDITOR
                    endProfiling[i]();
#endif
                    dependency = systemRefs[i].dependency;
                }
            }
            else
            {
                // SLOW PATH: Original implementation before baking
                for (var i = 0; i < m_updateSystems.Count; i++)
                {
                    var system = m_updateSystems[i];
                    system.dependency = dependency;
#if UNITY_EDITOR
                    system.BeginProfiling();
#endif
                    system.OnUpdate();
#if UNITY_EDITOR
                    system.EndProfiling();
#endif
                    dependency = system.dependency;
                }
            }

            return dependency;
        }

        public JobHandle LateUpdate(float dt, JobHandle inputDependency = default)
        {
            var dependency = inputDependency;

            if (m_isBaked)
            {
                // FAST PATH: Use baked arrays - no virtual dispatch
                var actions = m_bakedLateUpdateActions;
                var systemRefs = m_bakedLateUpdateSystemRefs;
                var count = m_bakedLateUpdateCount;
#if UNITY_EDITOR
                var beginProfiling = m_bakedLateUpdateBeginProfiling;
                var endProfiling = m_bakedLateUpdateEndProfiling;
#endif

                for (var i = 0; i < count; i++)
                {
                    systemRefs[i].dependency = dependency;
#if UNITY_EDITOR
                    beginProfiling[i]();
#endif
                    actions[i]();
#if UNITY_EDITOR
                    endProfiling[i]();
#endif
                    dependency = systemRefs[i].dependency;
                }
            }
            else
            {
                // SLOW PATH: Original implementation before baking
                for (var i = 0; i < m_lateUpdateSystems.Count; i++)
                {
                    var system = m_lateUpdateSystems[i];
                    system.dependency = dependency;
#if UNITY_EDITOR
                    system.BeginProfiling();
#endif
                    system.OnLateUpdate();
#if UNITY_EDITOR
                    system.EndProfiling();
#endif
                    dependency = system.dependency;
                }
            }

            return dependency;
        }

        public JobHandle FixedUpdate(float dt, JobHandle inputDependency = default)
        {
            fixedDeltaTime = dt;
            elapsedFixedDeltaTime += dt;
            var dependency = inputDependency;

            if (m_isBaked)
            {
                // FAST PATH: Use baked arrays - no virtual dispatch
                var actions = m_bakedFixedUpdateActions;
                var systemRefs = m_bakedFixedUpdateSystemRefs;
                var count = m_bakedFixedUpdateCount;
#if UNITY_EDITOR
                var beginProfiling = m_bakedFixedUpdateBeginProfiling;
                var endProfiling = m_bakedFixedUpdateEndProfiling;
#endif

                for (var i = 0; i < count; i++)
                {
                    systemRefs[i].dependency = dependency;
#if UNITY_EDITOR
                    beginProfiling[i]();
#endif
                    actions[i]();
#if UNITY_EDITOR
                    endProfiling[i]();
#endif
                    dependency = systemRefs[i].dependency;
                }
            }
            else
            {
                // SLOW PATH: Original implementation before baking
                for (var i = 0; i < m_fixedUpdateSystems.Count; i++)
                {
                    var system = m_fixedUpdateSystems[i];
                    system.dependency = dependency;
#if UNITY_EDITOR
                    system.BeginProfiling();
#endif
                    system.OnFixedUpdate();
#if UNITY_EDITOR
                    system.EndProfiling();
#endif
                    dependency = system.dependency;
                }
            }

            return dependency;
        }

        public void AddRootSystem(SystemBase system)
        {
            rootSystems.Add(system);
            RegisterSystemForUpdates(system);
            systemByType[system.GetType()] = system;
            system.world = this;
            onSystemAdded?.Invoke(system);
            system.OnAwake();

            // Auto-rebake if already baked
            if (m_isBaked)
                Bake();
        }

        public void RemoveRootSystem(SystemBase system)
        {
            rootSystems.Remove(system);
            UnregisterSystemFromUpdates(system);
            systemByType.Remove(system.GetType());
            system.world = null;
            system.OnDestroy();

            // Auto-rebake if already baked
            if (m_isBaked)
                Bake();
        }

        private void RegisterSystemForUpdates(SystemBase system)
        {
            if ((system.UpdateMask & SystemUpdateMask.Update) != 0)
                m_updateSystems.Add(system);
            if ((system.UpdateMask & SystemUpdateMask.LateUpdate) != 0)
                m_lateUpdateSystems.Add(system);
            if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) != 0)
                m_fixedUpdateSystems.Add(system);
        }

        private void UnregisterSystemFromUpdates(SystemBase system)
        {
            m_updateSystems.Remove(system);
            m_lateUpdateSystems.Remove(system);
            m_fixedUpdateSystems.Remove(system);
        }

        /// <summary>
        /// Bakes all system lists into optimized arrays for high-performance iteration.
        /// Eliminates virtual dispatch by pre-binding delegates.
        /// Call this after all systems have been added (typically after bootstrap).
        /// </summary>
        public void Bake()
        {
            // Bake Update systems
            m_bakedUpdateCount = m_updateSystems.Count;
            m_bakedUpdateActions = new Action[m_bakedUpdateCount];
            m_bakedUpdateSystemRefs = new SystemBase[m_bakedUpdateCount];
#if UNITY_EDITOR
            m_bakedUpdateBeginProfiling = new Action[m_bakedUpdateCount];
            m_bakedUpdateEndProfiling = new Action[m_bakedUpdateCount];
#endif

            for (var i = 0; i < m_bakedUpdateCount; i++)
            {
                var system = m_updateSystems[i];
                m_bakedUpdateSystemRefs[i] = system;
                m_bakedUpdateActions[i] = system.OnUpdate;
#if UNITY_EDITOR
                m_bakedUpdateBeginProfiling[i] = system.BeginProfiling;
                m_bakedUpdateEndProfiling[i] = system.EndProfiling;
#endif
            }

            // Bake LateUpdate systems
            m_bakedLateUpdateCount = m_lateUpdateSystems.Count;
            m_bakedLateUpdateActions = new Action[m_bakedLateUpdateCount];
            m_bakedLateUpdateSystemRefs = new SystemBase[m_bakedLateUpdateCount];
#if UNITY_EDITOR
            m_bakedLateUpdateBeginProfiling = new Action[m_bakedLateUpdateCount];
            m_bakedLateUpdateEndProfiling = new Action[m_bakedLateUpdateCount];
#endif

            for (var i = 0; i < m_bakedLateUpdateCount; i++)
            {
                var system = m_lateUpdateSystems[i];
                m_bakedLateUpdateSystemRefs[i] = system;
                m_bakedLateUpdateActions[i] = system.OnLateUpdate;
#if UNITY_EDITOR
                m_bakedLateUpdateBeginProfiling[i] = system.BeginProfiling;
                m_bakedLateUpdateEndProfiling[i] = system.EndProfiling;
#endif
            }

            // Bake FixedUpdate systems
            m_bakedFixedUpdateCount = m_fixedUpdateSystems.Count;
            m_bakedFixedUpdateActions = new Action[m_bakedFixedUpdateCount];
            m_bakedFixedUpdateSystemRefs = new SystemBase[m_bakedFixedUpdateCount];
#if UNITY_EDITOR
            m_bakedFixedUpdateBeginProfiling = new Action[m_bakedFixedUpdateCount];
            m_bakedFixedUpdateEndProfiling = new Action[m_bakedFixedUpdateCount];
#endif

            for (var i = 0; i < m_bakedFixedUpdateCount; i++)
            {
                var system = m_fixedUpdateSystems[i];
                m_bakedFixedUpdateSystemRefs[i] = system;
                m_bakedFixedUpdateActions[i] = system.OnFixedUpdate;
#if UNITY_EDITOR
                m_bakedFixedUpdateBeginProfiling[i] = system.BeginProfiling;
                m_bakedFixedUpdateEndProfiling[i] = system.EndProfiling;
#endif
            }

            // Recursively bake child SystemGroups
            for (var i = 0; i < rootSystems.Count; i++)
            {
                if (rootSystems[i] is SystemGroup group)
                    group.Bake();
            }

            m_isBaked = true;
        }

        public bool HasSystem<T>()
        {
            return systemByType.ContainsKey(typeof(T));
        }

        public T GetSystem<T>() where T : SystemBase
        {
            return (T)systemByType[typeof(T)];
        }

        public bool TryGetSystem<T>(out T system) where T : SystemBase
        {
            var hasSystem = systemByType.TryGetValue(typeof(T), out var outSystem);
            system = (T)outSystem;
            return hasSystem;
        }

        public Entity CreateEntity()
        {
            return EntityManager.CreateEntity();
        }

        public Entity CreateEntity(EntityArchetype archetype)
        {
            return EntityManager.CreateEntity(archetype);
        }

        public UnsafeList<Entity> CreateEntities(EntityArchetype archetype, int count, Allocator allocator)
        {
            return EntityManager.CreateEntities(archetype, count, allocator);
        }

        public void DestroyEntity(Entity entity)
        {
            EntityManager.DestroyEntity(entity);
        }

        #region WorldData convenience methods
        
        public void SetData<T>(T value) => data.SetData(value);
        public T GetData<T>() => data.GetData<T>();
        public bool TryGetData<T>(out T value) => data.TryGetData(out value);
        public bool HasData<T>() => data.HasData<T>();
        public bool RemoveData<T>() => data.RemoveData<T>();
        public T GetOrCreateData<T>(Func<T> factory) => data.GetOrCreateData(factory);
        public T GetOrCreateData<T>() where T : new() => data.GetOrCreateData<T>();

        #endregion
    }
}