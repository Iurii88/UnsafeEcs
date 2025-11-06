using UnsafeEcs.Core.Bootstrap;
using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Systems;

namespace UnsafeEcs.Additions.Groups
{
    [UpdateInWorld(WorldBootstrap.AllWorldsIndex)]
    [UpdateAfter(typeof(SimulationSystemGroup))]
    public class RenderingSystemGroup : SystemGroup
    {
    }
}