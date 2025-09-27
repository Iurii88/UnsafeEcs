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
            foreach (var system in systems)
            {
                if (system.world == null)
                {
                    system.world = world;
                    world.systemByType[system.GetType()] = system;
                }

                system.OnAwake();
            }
        }

        public override void OnUpdate()
        {
            var groupDependency = default(JobHandle);
            foreach (var system in systems)
            {
                if ((system.UpdateMask & SystemUpdateMask.Update) != 0)
                {
                    system.dependency = groupDependency;
                    system.OnUpdate();
                    groupDependency = system.dependency;
                }
            }

            groupDependency.Complete();
        }

        public override void OnLateUpdate()
        {
            var groupDependency = default(JobHandle);
            foreach (var system in systems)
            {
                if ((system.UpdateMask & SystemUpdateMask.LateUpdate) != 0)
                {
                    system.dependency = groupDependency;
                    system.OnLateUpdate();
                    groupDependency = system.dependency;
                }
            }

            groupDependency.Complete();
        }

        public override void OnFixedUpdate()
        {
            var groupDependency = default(JobHandle);
            foreach (var system in systems)
            {
                if ((system.UpdateMask & SystemUpdateMask.FixedUpdate) != 0)
                {
                    system.dependency = groupDependency;
                    system.OnFixedUpdate();
                    groupDependency = system.dependency;
                }
            }

            groupDependency.Complete();
        }

        public override void OnDestroy()
        {
            foreach (var system in systems)
                system.OnDestroy();
        }
    }
}