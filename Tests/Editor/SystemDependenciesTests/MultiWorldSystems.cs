using UnsafeEcs.Core.Bootstrap;
using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Systems;

namespace UnsafeEcs.Tests.Editor.SystemDependenciesTests
{
    // Multi-world root group - will be added to worlds 0 and 1
    [UpdateInWorld(0)]
    [UpdateInWorld(1)]
    public class MultiWorldRootGroup : SystemGroup
    {
    }

    // System that will be in all worlds because it's in a multi-world group
    [UpdateInGroup(typeof(MultiWorldRootGroup))]
    public class MultiWorldChildSystem : SystemBase
    {
    }

    // System that explicitly targets multiple worlds
    [UpdateInWorld(0)]
    [UpdateInWorld(1)]
    public class ExplicitMultiWorldSystem : SystemBase
    {
    }

    // System that will be in all worlds
    [UpdateInWorld(WorldBootstrap.AllWorldsIndex)]
    public class GlobalSystem : SystemBase
    {
    }

    // Root group that will be in all worlds
    [UpdateInWorld(WorldBootstrap.AllWorldsIndex)]
    public class GlobalGroup : SystemGroup
    {
    }

    // Child system of global group - will be in all worlds
    [UpdateInGroup(typeof(GlobalGroup))]
    public class GlobalGroupChildSystem : SystemBase
    {
    }

    // Multi-world group that depends on another system
    [UpdateInWorld(0)]
    [UpdateInWorld(2)]
    [UpdateAfter(typeof(GlobalSystem))]
    public class DependentMultiWorldGroup : SystemGroup
    {
    }

    // Child system of multi-world dependent group
    [UpdateInGroup(typeof(DependentMultiWorldGroup))]
    public class DependentChildSystem : SystemBase
    {
    }

    // Systems to verify that multi-world and regular systems can coexist
    [UpdateInGroup(typeof(World0Group1))]
    [UpdateInWorld(2)]
    public class MultiWorldSystem1 : SystemBase
    {
    }

    [UpdateInGroup(typeof(World0Group1))]
    [UpdateInWorld(2)]
    [UpdateAfter(typeof(MultiWorldSystem1))]
    public class MultiWorldSystem2 : SystemBase
    {
    }
}