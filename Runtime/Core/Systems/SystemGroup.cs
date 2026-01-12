using System.Collections.Generic;
using Unity.Jobs;

namespace UnsafeEcs.Core.Systems
{
    public abstract class SystemGroup : SystemBase
    {
        public readonly List<SystemBase> systems = new();

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
            }
        }

        public void RemoveSystem(SystemBase system)
        {
            system.OnDestroy();
            systems.Remove(system);
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
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if ((system.UpdateMask & SystemUpdateMask.Update) == 0)
                    continue;

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
            for (var index = 0; index < systems.Count; index++)
            {
                var system = systems[index];
                if ((system.UpdateMask & SystemUpdateMask.LateUpdate) == 0)
                    continue;
                
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
            for (var index = 0; index < systems.Count; index++)
            {
                var system = systems[index];
                if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) == 0)
                    continue;
                
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