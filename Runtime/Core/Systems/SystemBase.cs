using Unity.Jobs;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Utils;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Core.Systems
{
    [System.Flags]
    public enum SystemUpdateMask
    {
        None = 0,
        Update = 1 << 0,
        LateUpdate = 1 << 1,
        FixedUpdate = 1 << 2,
        All = Update | LateUpdate | FixedUpdate
    }

    public abstract class SystemBase
    {
        public JobHandle dependency;
        public World world;
        public ReferenceWrapper<EntityManager> entityManagerWrapper => world.entityManagerWrapper;

        public virtual SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update;

        public virtual void OnAwake()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnLateUpdate()
        {
        }

        public virtual void OnFixedUpdate()
        {
        }

        public virtual void OnDestroy()
        {
        }

        protected EntityQuery CreateQuery()
        {
            return world.EntityManager.CreateQuery();
        }

        protected ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            return world.EntityManager.GetComponentArray<T>();
        }

        protected BufferArray<T> GetBufferArray<T>() where T : unmanaged, IBufferElement
        {
            return world.EntityManager.GetBufferArray<T>();
        }
    }
}