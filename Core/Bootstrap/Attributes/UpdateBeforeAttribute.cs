using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateBeforeAttribute : Attribute
    {
        public UpdateBeforeAttribute(params Type[] systemTypes)
        {
            SystemTypes = systemTypes;
        }

        public Type[] SystemTypes { get; }
    }
}