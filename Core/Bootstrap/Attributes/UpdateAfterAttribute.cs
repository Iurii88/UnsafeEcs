using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateAfterAttribute : Attribute
    {
        public UpdateAfterAttribute(params Type[] systemTypes)
        {
            SystemTypes = systemTypes;
        }

        public Type[] SystemTypes { get; }
    }
}