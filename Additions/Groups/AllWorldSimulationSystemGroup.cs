using UnsafeEcs.Core.Bootstrap;
using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Systems;

namespace UnsafeEcs.Additions.Groups
{
    [UpdateInWorld(WorldBootstrap.AllWorldsIndex)]
    [UpdateAfter(typeof(AllWorldInitializationSystemGroup))]
    public class AllWorldSimulationSystemGroup : SystemGroup
    {
    }
}