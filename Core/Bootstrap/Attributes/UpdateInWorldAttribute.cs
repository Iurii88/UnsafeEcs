using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateInWorldAttribute : Attribute
    {
        public UpdateInWorldAttribute(int worldIndex)
        {
            WorldIndex = worldIndex;
        }

        public int WorldIndex { get; }
    }
}