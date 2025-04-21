using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Systems;

namespace UnsafeEcs.Additions.Groups
{
    [UpdateInWorld(0)]
    [UpdateAfter(typeof(RenderingSystemGroup))]
    public class CleanUpSystemGroup : SystemGroup
    {
    }
}