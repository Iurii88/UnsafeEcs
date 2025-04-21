using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInGroupAttribute : Attribute
    {
        public Type GroupType { get; }

        public UpdateInGroupAttribute(Type groupType)
        {
            GroupType = groupType;
        }
    }
}