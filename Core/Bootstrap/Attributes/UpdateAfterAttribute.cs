using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateAfterAttribute : Attribute
    {
        public Type[] SystemTypes { get; }

        public UpdateAfterAttribute(params Type[] systemTypes)
        {
            SystemTypes = systemTypes;
        }
    }
}