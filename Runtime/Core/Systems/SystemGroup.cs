using System;
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

                // Auto-rebake if already baked
                if (m_isBaked)
                    Bake();
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

        /// <summary>
        /// Bakes the system lists into optimized arrays for high-performance iteration.
        /// Eliminates virtual dispatch by pre-binding delegates.
        /// Call this after all systems have been added, or it will auto-rebake on runtime additions.
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

            // Recursively bake child groups
            for (var i = 0; i < systems.Count; i++)
            {
                if (systems[i] is SystemGroup childGroup)
                    childGroup.Bake();
            }

            m_isBaked = true;
        }

        public void RemoveSystem(SystemBase system)
        {
            system.OnDestroy();
            systems.Remove(system);
            UnregisterSystemFromUpdates(system);
            world.systemByType.Remove(system.GetType());
            system.world = null;

            // Auto-rebake if already baked
            if (m_isBaked)
                Bake();
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
                    systemRefs[i].dependency = groupDependency;
#if UNITY_EDITOR
                    beginProfiling[i]();
#endif
                    actions[i]();
#if UNITY_EDITOR
                    endProfiling[i]();
#endif
                    groupDependency = systemRefs[i].dependency;
                }
            }
            else
            {
                // SLOW PATH: Original implementation before baking
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
            }

            groupDependency.Complete();
        }

        public override void OnLateUpdate()
        {
            var groupDependency = default(JobHandle);

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
                    systemRefs[i].dependency = groupDependency;
#if UNITY_EDITOR
                    beginProfiling[i]();
#endif
                    actions[i]();
#if UNITY_EDITOR
                    endProfiling[i]();
#endif
                    groupDependency = systemRefs[i].dependency;
                }
            }
            else
            {
                // SLOW PATH: Original implementation before baking
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
            }

            groupDependency.Complete();
        }

        public override void OnFixedUpdate()
        {
            var groupDependency = default(JobHandle);

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
                    systemRefs[i].dependency = groupDependency;
#if UNITY_EDITOR
                    beginProfiling[i]();
#endif
                    actions[i]();
#if UNITY_EDITOR
                    endProfiling[i]();
#endif
                    groupDependency = systemRefs[i].dependency;
                }
            }
            else
            {
                // SLOW PATH: Original implementation before baking
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