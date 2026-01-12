using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace UnsafeEcs.Core.Systems
{
    public abstract class SystemGroup : SystemBase
    {
        public readonly List<SystemBase> systems = new();

        private SystemBase[] m_updateSystems = Array.Empty<SystemBase>();
        private SystemBase[] m_lateUpdateSystems = Array.Empty<SystemBase>();
        private SystemBase[] m_fixedUpdateSystems = Array.Empty<SystemBase>();

        public override SystemUpdateMask UpdateMask => SystemUpdateMask.All;

        public void AddSystem(SystemBase system)
        {
            systems.Add(system);
            if (world != null)
            {
                system.world = world;
                world.systemByType[system.GetType()] = system;
                world.onSystemAdded?.Invoke(system);
                system.OnAwake();
                Bake();
            }
        }

        public void Bake()
        {
            var updateCount = 0;
            var lateUpdateCount = 0;
            var fixedUpdateCount = 0;

            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if ((system.UpdateMask & SystemUpdateMask.Update) != 0) updateCount++;
                if ((system.UpdateMask & SystemUpdateMask.LateUpdate) != 0) lateUpdateCount++;
                if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) != 0) fixedUpdateCount++;
            }

            m_updateSystems = new SystemBase[updateCount];
            m_lateUpdateSystems = new SystemBase[lateUpdateCount];
            m_fixedUpdateSystems = new SystemBase[fixedUpdateCount];

            var updateIdx = 0;
            var lateUpdateIdx = 0;
            var fixedUpdateIdx = 0;

            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];

                if ((system.UpdateMask & SystemUpdateMask.Update) != 0)
                    m_updateSystems[updateIdx++] = system;

                if ((system.UpdateMask & SystemUpdateMask.LateUpdate) != 0)
                    m_lateUpdateSystems[lateUpdateIdx++] = system;

                if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) != 0)
                    m_fixedUpdateSystems[fixedUpdateIdx++] = system;

                if (system is SystemGroup childGroup)
                    childGroup.Bake();
            }
        }

        public void RemoveSystem(SystemBase system)
        {
            system.OnDestroy();
            systems.Remove(system);
            world.systemByType.Remove(system.GetType());
            system.world = null;
            Bake();
        }

        public override void OnAwake()
        {
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if (system.world == null)
                {
                    system.world = world;
                    world.systemByType[system.GetType()] = system;
                    world.onSystemAdded?.Invoke(system);
                }
                system.OnAwake();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void OnUpdate()
        {
            var dep = default(JobHandle);
            var arr = m_updateSystems;
            var len = arr.Length;

            for (var i = 0; i < len; i++)
            {
                var s = arr[i];
                s.dependency = dep;
#if UNITY_EDITOR
                s.BeginProfiling();
#endif
                s.OnUpdate();
#if UNITY_EDITOR
                s.EndProfiling();
#endif
                dep = s.dependency;
            }

            dep.Complete();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void OnLateUpdate()
        {
            var dep = default(JobHandle);
            var arr = m_lateUpdateSystems;
            var len = arr.Length;

            for (var i = 0; i < len; i++)
            {
                var s = arr[i];
                s.dependency = dep;
#if UNITY_EDITOR
                s.BeginProfiling();
#endif
                s.OnLateUpdate();
#if UNITY_EDITOR
                s.EndProfiling();
#endif
                dep = s.dependency;
            }

            dep.Complete();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void OnFixedUpdate()
        {
            var dep = default(JobHandle);
            var arr = m_fixedUpdateSystems;
            var len = arr.Length;

            for (var i = 0; i < len; i++)
            {
                var s = arr[i];
                s.dependency = dep;
#if UNITY_EDITOR
                s.BeginProfiling();
#endif
                s.OnFixedUpdate();
#if UNITY_EDITOR
                s.EndProfiling();
#endif
                dep = s.dependency;
            }

            dep.Complete();
        }

        public override void OnDestroy()
        {
            for (var i = 0; i < systems.Count; i++)
                systems[i].OnDestroy();
        }
    }
}