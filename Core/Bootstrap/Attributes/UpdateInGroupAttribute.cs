using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInGroupAttribute : Attribute
    {
        public UpdateInGroupAttribute(Type groupType)
        {
            GroupType = groupType;
        }

        public Type GroupType { get; }
    }
}