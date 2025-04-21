// EntityQueryTestBase.cs

using System;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Tests.Editor.EntityManagerTests.Query
{
    // Test components
    public struct ComponentA : IComponent
    {
    }

    public struct ComponentB : IComponent
    {
    }

    public struct ComponentC : IComponent
    {
    }

    public struct ComponentD : IComponent
    {
    }

    public struct ComponentE : IComponent
    {
    }

    public struct BufferElement : IBufferElement
    {
        public int value;
    }

    public class EntityQueryTest : UnsafeEcsQueryBaseTest
    {
        protected EntityQuery CreateTestQuery()
        {
            return entityManager.CreateQuery();
        }

        protected Entity CreateEntityWithComponents(params Type[] componentTypes)
        {
            var entity = entityManager.CreateEntity();

            foreach (var type in componentTypes)
            {
                if (type == typeof(ComponentA))
                    entityManager.AddComponent<ComponentA>(entity);
                else if (type == typeof(ComponentB))
                    entityManager.AddComponent<ComponentB>(entity);
                else if (type == typeof(ComponentC))
                    entityManager.AddComponent<ComponentC>(entity);
                else if (type == typeof(ComponentD))
                    entityManager.AddComponent<ComponentD>(entity);
                else if (type == typeof(ComponentE))
                    entityManager.AddComponent<ComponentE>(entity);
                else if (type == typeof(BufferElement))
                    entityManager.AddBuffer<BufferElement>(entity);
            }

            return entity;
        }
    }
}