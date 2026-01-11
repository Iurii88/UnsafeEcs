using Unity.Jobs;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Utils;
using UnsafeEcs.Core.Worlds;
#if UNITY_EDITOR
using System.Diagnostics;
#endif

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
        public ref EntityManager entityManager => ref world.EntityManager;

        public virtual SystemUpdateMask UpdateMask { get; set; } = SystemUpdateMask.Update;

#if UNITY_EDITOR
        // Performance tracking (editor only)
        public double LastExecutionTimeMs { get; private set; }
        public double AverageExecutionTimeMs { get; private set; }

        private readonly Stopwatch m_stopwatch = new();
        private const int SmoothingFactor = 10;
        private int m_sampleCount;

        public void BeginProfiling()
        {
            m_stopwatch.Restart();
        }

        public void EndProfiling()
        {
            m_stopwatch.Stop();
            LastExecutionTimeMs = m_stopwatch.Elapsed.TotalMilliseconds;

            if (m_sampleCount < SmoothingFactor)
            {
                m_sampleCount++;
                AverageExecutionTimeMs = ((AverageExecutionTimeMs * (m_sampleCount - 1)) + LastExecutionTimeMs) / m_sampleCount;
            }
            else
            {
                AverageExecutionTimeMs = ((AverageExecutionTimeMs * (SmoothingFactor - 1)) + LastExecutionTimeMs) / SmoothingFactor;
            }
        }
#endif

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