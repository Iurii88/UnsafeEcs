using System.Collections.Generic;
using Unity.Jobs;

namespace UnsafeEcs.Core.Systems
{
    public abstract class SystemGroup : SystemBase
    {
        public readonly List<SystemBase> systems = new();

        // Pre-filtered lists for each update type to avoid checking UpdateMask every frame
        private readonly List<SystemBase> m_updateSystems = new();
        private readonly List<SystemBase> m_lateUpdateSystems = new();
        private readonly List<SystemBase> m_fixedUpdateSystems = new();

        public override SystemUpdateMask UpdateMask => SystemUpdateMask.All;

        public void AddSystem(SystemBase system)
        {
            systems.Add(system);
            RegisterSystemForUpdates(system);
            if (world != null)
            {
                system.world = world;
                world.systemByType[system.GetType()] = system;
                world.onSystemAdded?.Invoke(system);
                system.OnAwake();
            }
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

        public void RemoveSystem(SystemBase system)
        {
            system.OnDestroy();
            systems.Remove(system);
            UnregisterSystemFromUpdates(system);
            world.systemByType.Remove(system.GetType());
            system.world = null;
        }

        public override void OnAwake()
        {
            for (var index = 0; index < systems.Count; index++)
            {
                var system = systems[index];
                if (system.world == null)
                {
                    system.world = world;
                    world.systemByType[system.GetType()] = system;
                    world.onSystemAdded?.Invoke(system);
                }

                system.OnAwake();
            }
        }

        public override void OnUpdate()
        {
            var groupDependency = default(JobHandle);
            for (var i = 0; i < m_updateSystems.Count; i++)
            {
                var system = m_updateSystems[i];
                system.dependency = groupDependency;
#if UNITY_EDITOR
                system.BeginProfiling();
#endif
                system.OnUpdate();
#if UNITY_EDITOR
                system.EndProfiling();
#endif
                groupDependency = system.dependency;
            }

            groupDependency.Complete();
        }

        public override void OnLateUpdate()
        {
            var groupDependency = default(JobHandle);
            for (var i = 0; i < m_lateUpdateSystems.Count; i++)
            {
                var system = m_lateUpdateSystems[i];
                system.dependency = groupDependency;
#if UNITY_EDITOR
                system.BeginProfiling();
#endif
                system.OnLateUpdate();
#if UNITY_EDITOR
                system.EndProfiling();
#endif
                groupDependency = system.dependency;
            }

            groupDependency.Complete();
        }

        public override void OnFixedUpdate()
        {
            var groupDependency = default(JobHandle);
            for (var i = 0; i < m_fixedUpdateSystems.Count; i++)
            {
                var system = m_fixedUpdateSystems[i];
                system.dependency = groupDependency;
#if UNITY_EDITOR
                system.BeginProfiling();
#endif
                system.OnFixedUpdate();
#if UNITY_EDITOR
                system.EndProfiling();
#endif
                groupDependency = system.dependency;
            }

            groupDependency.Complete();
        }

        public override void OnDestroy()
        {
            for (var index = 0; index < systems.Count; index++)
            {
                var system = systems[index];
                system.OnDestroy();
            }
        }
    }
}