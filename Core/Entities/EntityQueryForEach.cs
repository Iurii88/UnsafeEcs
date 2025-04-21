using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityQuery
    {
        public delegate void ForEachAction(ref Entity entity);

        public delegate void ForEachAction<T1>(ref Entity entity, ref T1 c1) where T1 : unmanaged, IComponent;

        public delegate void ForEachAction<T1, T2>(ref Entity entity, ref T1 c1, ref T2 c2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent;

        public delegate void ForEachAction<T1, T2, T3>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent;

        public delegate void ForEachAction<T1, T2, T3, T4>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent;

        public delegate void ForEachAction<T1, T2, T3, T4, T5>(ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent;

        public void ForEach(ForEachAction action)
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                action(ref entity);
            }
        }

        public void ForEach<T1>(ForEachAction<T1> action)
            where T1 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                action(ref entity, ref c1);
            }
        }

        public void ForEach<T1, T2>(ForEachAction<T1, T2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                action(ref entity, ref c1, ref c2);
            }
        }

        public void ForEach<T1, T2, T3>(ForEachAction<T1, T2, T3> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                action(ref entity, ref c1, ref c2, ref c3);
            }
        }

        public void ForEach<T1, T2, T3, T4>(ForEachAction<T1, T2, T3, T4> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                ref var c4 = ref m_manager->GetComponent<T4>(entity);
                action(ref entity, ref c1, ref c2, ref c3, ref c4);
            }
        }

        public void ForEach<T1, T2, T3, T4, T5>(ForEachAction<T1, T2, T3, T4, T5> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                ref var c3 = ref m_manager->GetComponent<T3>(entity);
                ref var c4 = ref m_manager->GetComponent<T4>(entity);
                ref var c5 = ref m_manager->GetComponent<T5>(entity);
                action(ref entity, ref c1, ref c2, ref c3, ref c4, ref c5);
            }
        }
    }
}