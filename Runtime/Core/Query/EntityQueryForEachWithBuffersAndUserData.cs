using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityQuery
    {
        // Delegates with user data and buffers
        public delegate void ForEachWithBufferActionWithUserData<in TUserData, TB>(TUserData userData, ref Entity entity, DynamicBuffer<TB> buffer)
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWithUserData<in TUserData, T1, TB>(TUserData userData, ref Entity entity, ref T1 c1, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWithUserData<in TUserData, T1, T2, TB>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWithUserData<in TUserData, T1, T2, T3, TB>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWithUserData<in TUserData, T1, T2, T3, T4, TB>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4,
            DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersActionWithUserData<in TUserData, TB1, TB2>(TUserData userData, ref Entity entity, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersActionWithUserData<in TUserData, T1, TB1, TB2>(TUserData userData, ref Entity entity, ref T1 c1, DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersActionWithUserData<in TUserData, T1, T2, TB1, TB2>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, DynamicBuffer<TB1> buffer1,
            DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersActionWithUserData<in TUserData, T1, T2, T3, TB1, TB2>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3,
            DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        public delegate void ForEachWithBuffersActionWithUserData<in TUserData, T1, T2, T3, T4, TB1, TB2>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4,
            DynamicBuffer<TB1> buffer1, DynamicBuffer<TB2> buffer2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement;

        // ForEach methods with user data and buffers
        public void ForEach<TUserData, TB>(TUserData userData, ForEachWithBufferActionWithUserData<TUserData, TB> action)
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(userData, ref entity, buffer);
            }
        }

        public void ForEach<TUserData, T1, TB>(TUserData userData, ForEachWithBufferActionWithUserData<TUserData, T1, TB> action)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(userData, ref entity, ref c1, buffer);
            }
        }

        public void ForEach<TUserData, T1, T2, TB>(TUserData userData, ForEachWithBufferActionWithUserData<TUserData, T1, T2, TB> action)
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
                action(userData, ref entity, ref c1, ref c2, buffer);
            }
        }

        public void ForEach<TUserData, T1, T2, T3, TB>(TUserData userData, ForEachWithBufferActionWithUserData<TUserData, T1, T2, T3, TB> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3, buffer);
            }
        }

        public void ForEach<TUserData, T1, T2, T3, T4, TB>(TUserData userData, ForEachWithBufferActionWithUserData<TUserData, T1, T2, T3, T4, TB> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3, ref c4, buffer);
            }
        }

        public void ForEach<TUserData, TB1, TB2>(TUserData userData, ForEachWithBuffersActionWithUserData<TUserData, TB1, TB2> action)
            where TB1 : unmanaged, IBufferElement
            where TB2 : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                var buffer1 = m_manager->GetBuffer<TB1>(entity);
                var buffer2 = m_manager->GetBuffer<TB2>(entity);
                action(userData, ref entity, buffer1, buffer2);
            }
        }

        public void ForEach<TUserData, T1, TB1, TB2>(TUserData userData, ForEachWithBuffersActionWithUserData<TUserData, T1, TB1, TB2> action)
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
                action(userData, ref entity, ref c1, buffer1, buffer2);
            }
        }

        public void ForEach<TUserData, T1, T2, TB1, TB2>(TUserData userData, ForEachWithBuffersActionWithUserData<TUserData, T1, T2, TB1, TB2> action)
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
                action(userData, ref entity, ref c1, ref c2, buffer1, buffer2);
            }
        }

        public void ForEach<TUserData, T1, T2, T3, TB1, TB2>(TUserData userData, ForEachWithBuffersActionWithUserData<TUserData, T1, T2, T3, TB1, TB2> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3, buffer1, buffer2);
            }
        }

        public void ForEach<TUserData, T1, T2, T3, T4, TB1, TB2>(TUserData userData, ForEachWithBuffersActionWithUserData<TUserData, T1, T2, T3, T4, TB1, TB2> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3, ref c4, buffer1, buffer2);
            }
        }

        // ==================== TWO USER DATA PARAMETERS WITH BUFFERS ====================

        // Delegates with 2 user data and buffers
        public delegate void ForEachWithBufferActionWith2UserData<in TUserData1, in TUserData2, TB>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, DynamicBuffer<TB> buffer)
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWith2UserData<in TUserData1, in TUserData2, T1, TB>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1,
            DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWith2UserData<in TUserData1, in TUserData2, T1, T2, TB>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1, ref T2 c2,
            DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWith2UserData<in TUserData1, in TUserData2, T1, T2, T3, TB>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1, ref T2 c2,
            ref T3 c3, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        // ForEach methods with 2 user data and buffers
        public void ForEach<TUserData1, TUserData2, TB>(TUserData1 userData1, TUserData2 userData2, ForEachWithBufferActionWith2UserData<TUserData1, TUserData2, TB> action)
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(userData1, userData2, ref entity, buffer);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, TB>(TUserData1 userData1, TUserData2 userData2, ForEachWithBufferActionWith2UserData<TUserData1, TUserData2, T1, TB> action)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(userData1, userData2, ref entity, ref c1, buffer);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, T2, TB>(TUserData1 userData1, TUserData2 userData2, ForEachWithBufferActionWith2UserData<TUserData1, TUserData2, T1, T2, TB> action)
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
                action(userData1, userData2, ref entity, ref c1, ref c2, buffer);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, T2, T3, TB>(TUserData1 userData1, TUserData2 userData2, ForEachWithBufferActionWith2UserData<TUserData1, TUserData2, T1, T2, T3, TB> action)
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
                action(userData1, userData2, ref entity, ref c1, ref c2, ref c3, buffer);
            }
        }

        // ==================== THREE USER DATA PARAMETERS WITH BUFFERS ====================

        // Delegates with 3 user data and buffers
        public delegate void ForEachWithBufferActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity,
            DynamicBuffer<TB> buffer)
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ref Entity entity, ref T1 c1, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, T2, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ref Entity entity, ref T1 c1, ref T2 c2, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        public delegate void ForEachWithBufferActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, T2, T3, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, DynamicBuffer<TB> buffer)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement;

        // ForEach methods with 3 user data and buffers
        public void ForEach<TUserData1, TUserData2, TUserData3, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ForEachWithBufferActionWith3UserData<TUserData1, TUserData2, TUserData3, TB> action)
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(userData1, userData2, userData3, ref entity, buffer);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ForEachWithBufferActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, TB> action)
            where T1 : unmanaged, IComponent
            where TB : unmanaged, IBufferElement
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                var buffer = m_manager->GetBuffer<TB>(entity);
                action(userData1, userData2, userData3, ref entity, ref c1, buffer);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, T2, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ForEachWithBufferActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, T2, TB> action)
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
                action(userData1, userData2, userData3, ref entity, ref c1, ref c2, buffer);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, T2, T3, TB>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3,
            ForEachWithBufferActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, T2, T3, TB> action)
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
                action(userData1, userData2, userData3, ref entity, ref c1, ref c2, ref c3, buffer);
            }
        }
    }
}