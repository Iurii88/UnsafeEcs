using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Utils;

namespace UnsafeEcs.Core.Worlds
{
    public class World : IDisposable
    {
        public readonly List<SystemBase> rootSystems = new();
        public readonly Dictionary<Type, SystemBase> systemByType = new();
        public float deltaTime;
        public float fixedDeltaTime;
        public float elapsedDeltaTime;
        public float elapsedFixedDeltaTime;
        public ReferenceWrapper<EntityManager> entityManagerWrapper;

        private EntityManager m_entityManager;

        public World()
        {
            m_entityManager = new EntityManager(EntityManager.InitialEntityCapacity);
            m_entityManager.Initialize();
            entityManagerWrapper = new ReferenceWrapper<EntityManager>(ref m_entityManager);
        }

        public World(int initialCapacity = 0)
        {
            m_entityManager = new EntityManager(initialCapacity);
            m_entityManager.Initialize();
            entityManagerWrapper = new ReferenceWrapper<EntityManager>(ref m_entityManager);
        }

        public ref EntityManager EntityManager => ref m_entityManager;

        public void Dispose()
        {
            foreach (var regularSystem in rootSystems)
                regularSystem.OnDestroy();

            m_entityManager.Dispose();
        }

        public void Update(float dt)
        {
            deltaTime = dt;
            elapsedDeltaTime += dt;
            var dependency = default(JobHandle);

            foreach (var system in rootSystems)
            {
                if ((system.UpdateMask & SystemUpdateMask.Update) != 0)
                {
                    system.dependency = dependency;
                    system.OnUpdate();
                    dependency = system.dependency;
                }
            }

            dependency.Complete();
        }

        public void LateUpdate(float dt)
        {
            var dependency = default(JobHandle);

            foreach (var system in rootSystems)
            {
                if ((system.UpdateMask & SystemUpdateMask.LateUpdate) != 0)
                {
                    system.dependency = dependency;
                    system.OnLateUpdate();
                    dependency = system.dependency;
                }
            }

            dependency.Complete();
        }

        public void FixedUpdate(float dt)
        {
            fixedDeltaTime = dt;
            elapsedFixedDeltaTime += dt;
            var dependency = default(JobHandle);

            foreach (var system in rootSystems)
            {
                if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) != 0)
                {
                    system.dependency = dependency;
                    system.OnFixedUpdate();
                    dependency = system.dependency;
                }
            }

            dependency.Complete();
        }

        public void AddRootSystem(SystemBase system)
        {
            rootSystems.Add(system);
            systemByType[system.GetType()] = system;
            system.world = this;
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
    }
}