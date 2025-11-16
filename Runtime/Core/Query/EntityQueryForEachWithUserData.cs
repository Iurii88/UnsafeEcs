using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityQuery
    {
        // Delegates with user data parameter
        public delegate void ForEachActionWithUserData<in TUserData>(TUserData userData, ref Entity entity);

        public delegate void ForEachActionWithUserData<in TUserData, T1>(TUserData userData, ref Entity entity, ref T1 c1)
            where T1 : unmanaged, IComponent;

        public delegate void ForEachActionWithUserData<in TUserData, T1, T2>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent;

        public delegate void ForEachActionWithUserData<in TUserData, T1, T2, T3>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent;

        public delegate void ForEachActionWithUserData<in TUserData, T1, T2, T3, T4>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent;

        public delegate void ForEachActionWithUserData<in TUserData, T1, T2, T3, T4, T5>(TUserData userData, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent;

        // ForEach methods with user data parameter
        public void ForEach<TUserData>(TUserData userData, ForEachActionWithUserData<TUserData> action)
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                action(userData, ref entity);
            }
        }

        public void ForEach<TUserData, T1>(TUserData userData, ForEachActionWithUserData<TUserData, T1> action)
            where T1 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                action(userData, ref entity, ref c1);
            }
        }

        public void ForEach<TUserData, T1, T2>(TUserData userData, ForEachActionWithUserData<TUserData, T1, T2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                action(userData, ref entity, ref c1, ref c2);
            }
        }

        public void ForEach<TUserData, T1, T2, T3>(TUserData userData, ForEachActionWithUserData<TUserData, T1, T2, T3> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3);
            }
        }

        public void ForEach<TUserData, T1, T2, T3, T4>(TUserData userData, ForEachActionWithUserData<TUserData, T1, T2, T3, T4> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3, ref c4);
            }
        }

        public void ForEach<TUserData, T1, T2, T3, T4, T5>(TUserData userData, ForEachActionWithUserData<TUserData, T1, T2, T3, T4, T5> action)
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
                action(userData, ref entity, ref c1, ref c2, ref c3, ref c4, ref c5);
            }
        }

        // ==================== TWO USER DATA PARAMETERS ====================

        // Delegates with 2 user data parameters
        public delegate void ForEachActionWith2UserData<in TUserData1, in TUserData2>(TUserData1 userData1, TUserData2 userData2, ref Entity entity);

        public delegate void ForEachActionWith2UserData<in TUserData1, in TUserData2, T1>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1)
            where T1 : unmanaged, IComponent;

        public delegate void ForEachActionWith2UserData<in TUserData1, in TUserData2, T1, T2>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1, ref T2 c2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent;

        public delegate void ForEachActionWith2UserData<in TUserData1, in TUserData2, T1, T2, T3>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent;

        public delegate void ForEachActionWith2UserData<in TUserData1, in TUserData2, T1, T2, T3, T4>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent;

        public delegate void ForEachActionWith2UserData<in TUserData1, in TUserData2, T1, T2, T3, T4, T5>(TUserData1 userData1, TUserData2 userData2, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent;

        // ForEach methods with 2 user data parameters
        public void ForEach<TUserData1, TUserData2>(TUserData1 userData1, TUserData2 userData2, ForEachActionWith2UserData<TUserData1, TUserData2> action)
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                action(userData1, userData2, ref entity);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1>(TUserData1 userData1, TUserData2 userData2, ForEachActionWith2UserData<TUserData1, TUserData2, T1> action)
            where T1 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                action(userData1, userData2, ref entity, ref c1);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, T2>(TUserData1 userData1, TUserData2 userData2, ForEachActionWith2UserData<TUserData1, TUserData2, T1, T2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                action(userData1, userData2, ref entity, ref c1, ref c2);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, T2, T3>(TUserData1 userData1, TUserData2 userData2, ForEachActionWith2UserData<TUserData1, TUserData2, T1, T2, T3> action)
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
                action(userData1, userData2, ref entity, ref c1, ref c2, ref c3);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, T2, T3, T4>(TUserData1 userData1, TUserData2 userData2, ForEachActionWith2UserData<TUserData1, TUserData2, T1, T2, T3, T4> action)
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
                action(userData1, userData2, ref entity, ref c1, ref c2, ref c3, ref c4);
            }
        }

        public void ForEach<TUserData1, TUserData2, T1, T2, T3, T4, T5>(TUserData1 userData1, TUserData2 userData2, ForEachActionWith2UserData<TUserData1, TUserData2, T1, T2, T3, T4, T5> action)
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
                action(userData1, userData2, ref entity, ref c1, ref c2, ref c3, ref c4, ref c5);
            }
        }

        // ==================== THREE USER DATA PARAMETERS ====================

        // Delegates with 3 user data parameters
        public delegate void ForEachActionWith3UserData<in TUserData1, in TUserData2, in TUserData3>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity);

        public delegate void ForEachActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity, ref T1 c1)
            where T1 : unmanaged, IComponent;

        public delegate void ForEachActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, T2>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity, ref T1 c1, ref T2 c2)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent;

        public delegate void ForEachActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, T2, T3>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent;

        public delegate void ForEachActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, T2, T3, T4>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent;

        public delegate void ForEachActionWith3UserData<in TUserData1, in TUserData2, in TUserData3, T1, T2, T3, T4, T5>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ref Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent;

        // ForEach methods with 3 user data parameters
        public void ForEach<TUserData1, TUserData2, TUserData3>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ForEachActionWith3UserData<TUserData1, TUserData2, TUserData3> action)
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                action(userData1, userData2, userData3, ref entity);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ForEachActionWith3UserData<TUserData1, TUserData2, TUserData3, T1> action)
            where T1 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                action(userData1, userData2, userData3, ref entity, ref c1);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, T2>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ForEachActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, T2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            var entities = m_manager->QueryEntities(ref this);
            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                ref var c1 = ref m_manager->GetComponent<T1>(entity);
                ref var c2 = ref m_manager->GetComponent<T2>(entity);
                action(userData1, userData2, userData3, ref entity, ref c1, ref c2);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, T2, T3>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ForEachActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, T2, T3> action)
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
                action(userData1, userData2, userData3, ref entity, ref c1, ref c2, ref c3);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, T2, T3, T4>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ForEachActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, T2, T3, T4> action)
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
                action(userData1, userData2, userData3, ref entity, ref c1, ref c2, ref c3, ref c4);
            }
        }

        public void ForEach<TUserData1, TUserData2, TUserData3, T1, T2, T3, T4, T5>(TUserData1 userData1, TUserData2 userData2, TUserData3 userData3, ForEachActionWith3UserData<TUserData1, TUserData2, TUserData3, T1, T2, T3, T4, T5> action)
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
                action(userData1, userData2, userData3, ref entity, ref c1, ref c2, ref c3, ref c4, ref c5);
            }
        }
    }
}
