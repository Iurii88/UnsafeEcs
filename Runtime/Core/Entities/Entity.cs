using System;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct Entity : IEquatable<Entity>
    {
        public int id;
        public uint version;

        public EntityManager* managerPtr;

        public static readonly Entity Null = new Entity { id = -1, version = 0, managerPtr = null }; 
        
        public ref EntityManager Manager
        {
            get
            {
                var ptr = &managerPtr[0];
                return ref *ptr;
            }
        }

        public readonly void Destroy()
        {
            if (managerPtr != null)
                managerPtr->DestroyEntity(this);
        }

        public readonly bool IsAlive()
        {
            return managerPtr != null && managerPtr->IsEntityAlive(this);
        }

        public override bool Equals(object obj)
        {
            return obj is Entity other && Equals(other);
        }

        public bool Equals(Entity other)
        {
            return id == other.id && version == other.version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, version);
        }

        // Equality operator
        public static bool operator ==(Entity left, Entity right)
        {
            return left.id == right.id && left.version == right.version;
        }

        // Inequality operator
        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"id:{id} v:{version}";
        }
    }
}