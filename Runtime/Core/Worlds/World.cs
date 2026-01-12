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

            for (var index = 0; index < rootSystems.Count; index++)
            {
                var system = rootSystems[index];
                if ((system.UpdateMask & SystemUpdateMask.Update) == 0)
                    continue;
                
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

            return dependency;
        }

        public JobHandle LateUpdate(float dt, JobHandle inputDependency = default)
        {
            var dependency = inputDependency;

            for (var index = 0; index < rootSystems.Count; index++)
            {
                var system = rootSystems[index];
                if ((system.UpdateMask & SystemUpdateMask.LateUpdate) == 0)
                    continue;
                
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

            return dependency;
        }

        public JobHandle FixedUpdate(float dt, JobHandle inputDependency = default)
        {
            fixedDeltaTime = dt;
            elapsedFixedDeltaTime += dt;
            var dependency = inputDependency;

            for (var index = 0; index < rootSystems.Count; index++)
            {
                var system = rootSystems[index];
                if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) == 0)
                    continue;
                
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

            return dependency;
        }

        public void AddRootSystem(SystemBase system)
        {
            rootSystems.Add(system);
            systemByType[system.GetType()] = system;
            system.world = this;
            onSystemAdded?.Invoke(system);
            system.OnAwake();
        }

        public void RemoveRootSystem(SystemBase system)
        {
            rootSystems.Remove(system);
            systemByType.Remove(system.GetType());
            system.world = null;
            system.OnDestroy();
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