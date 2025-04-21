using System;

namespace UnsafeEcs.Core.Bootstrap.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateBeforeAttribute : Attribute
    {
        public Type[] SystemTypes { get; }

        public UpdateBeforeAttribute(params Type[] systemTypes)
        {
            SystemTypes = systemTypes;
        }
    }
}