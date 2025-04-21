using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityQuery
    {
        public delegate void ForEachWithBufferAction<TB>(ref Entity entity, DynamicBuffer<TB> buffer)
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferAction<T1, TB>(ref Entity entity, ref T1 c1, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferAction<T1, T2, TB>(ref Entity entity, ref T1 c1, ref T2 c2, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferAction<T1, T2, T3, TB>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersAction<TB1, TB2>(ref Entity entity, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersAction<T1, TB1, TB2>(ref Entity entity, ref T1 c1, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferAction<T1, T2, T3, T4, TB>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersAction<T1, T2, TB1, TB2>(ref Entity entity, ref T1 c1, ref T2 c2, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersAction<T1, T2, T3, TB1, TB2>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersAction<T1, T2, T3, T4, TB1, TB2>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public void ForEach<TB>(ForEachWithBufferAction<TB> action)
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(ref entity, buffer);
            }
        }

        public void ForEach<T1, TB>(ForEachWithBufferAction<T1, TB> action)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(ref entity, ref c1, buffer);
            }
        }

        public void ForEach<T1, T2, TB>(ForEachWithBufferAction<T1, T2, TB> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(ref entity, ref c1, ref c2, buffer);
            }
        }

        public void ForEach<T1, T2, T3, TB>(ForEachWithBufferAction<T1, T2, T3, TB> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(ref entity, ref c1, ref c2, ref c3, buffer);
            }
        }

        public void ForEach<TB1, TB2>(ForEachWithBuffersAction<TB1, TB2> action)
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                var buffer1 = m_manager->GetBuffer<TB1>(entity);
                var buffer2 = m_manager->GetBuffer<TB2>(entity);
                action(ref entity, buffer1, buffer2);
            }
        }

        public void ForEach<T1, TB1, TB2>(ForEachWithBuffersAction<T1, TB1, TB2> action)
            where T1 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                var buffer1 = m_manager->GetBuffer<TB1>(entity);
                var buffer2 = m_manager->GetBuffer<TB2>(entity);
                action(ref entity, ref c1, buffer1, buffer2);
            }
        }

        public void ForEach<T1, T2, TB1, TB2>(ForEachWithBuffersAction<T1, T2, TB1, TB2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                var buffer1 = m_manager->GetBuffer<TB1>(entity);
                var buffer2 = m_manager->GetBuffer<TB2>(entity);
                action(ref entity, ref c1, ref c2, buffer1, buffer2);
            }
        }

        public void ForEach<T1, T2, T3, TB1, TB2>(ForEachWithBuffersAction<T1, T2, T3, TB1, TB2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                var buffer1 = m_manager->GetBuffer<TB1>(entity);
                var buffer2 = m_manager->GetBuffer<TB2>(entity);
                action(ref entity, ref c1, ref c2, ref c3, buffer1, buffer2);
            }
        }

        public void ForEach<T1, T2, T3, T4, TB>(ForEachWithBufferAction<T1, T2, T3, T4, TB> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                ref var c4 = ref m_manager->GetComponent<T4>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(ref entity, ref c1, ref c2, ref c3, ref c4, buffer);
            }
        }

        public void ForEach<T1, T2, T3, T4, TB1, TB2>(ForEachWithBuffersAction<T1, T2, T3, T4, TB1, TB2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                ref var c4 = ref m_manager->GetComponent<T4>(entity);
                var buffer1 = m_manager->GetBuffer<TB1>(entity);
                var buffer2 = m_manager->GetBuffer<TB2>(entity);
                action(ref entity, ref c1, ref c2, ref c3, ref c4, buffer1, buffer2);
            }
        }
    }
}