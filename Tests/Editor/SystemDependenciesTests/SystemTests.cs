using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Systems;

namespace UnsafeEcs.Tests.Editor.SystemDependenciesTests
{
    // Systems/World0/World0Groups.cs
    [UpdateInWorld(0)]
    public class World0Group1 : SystemGroup
    {
    }

    [UpdateInWorld(0)]
    public class World0Group2 : SystemGroup
    {
    }

    [UpdateInWorld(0)]
    public class World0Group3 : SystemGroup
    {
    }

    [UpdateInWorld(0)]
    public class World0RootSystem1 : SystemBase
    {
    }

// Systems/World0/Group1Systems.cs
    [UpdateInGroup(typeof(World0Group1))]
    public class World0System1 : SystemBase
    {
    }

    [UpdateInGroup(typeof(World0Group1))]
    [UpdateAfter(typeof(World0System1))]
    public class World0System2 : SystemBase
    {
    }

    [UpdateInGroup(typeof(World0Group1))]
    [UpdateBefore(typeof(World0System1))]
    public class World0System3 : SystemBase
    {
    }

    [UpdateInGroup(typeof(World0Group1))]
    [UpdateAfter(typeof(World0System2))]
    public class World0System6 : SystemBase
    {
    }

// Systems/World0/Group3Systems.cs
    [UpdateInGroup(typeof(World0Group3))]
    public class World0System4 : SystemBase
    {
    }

    [UpdateInGroup(typeof(World0Group3))]
    [UpdateBefore(typeof(World0System4))]
    public class World0System5 : SystemBase
    {
    }

// Systems/World1/World1Groups.cs
    [UpdateInWorld(1)]
    public class World1Group1 : SystemGroup
    {
    }

    [UpdateInWorld(1)]
    public class World1RootSystem1 : SystemBase
    {
    }

// Systems/World1/Group1Systems.cs
    [UpdateInGroup(typeof(World1Group1))]
    public class World1System1 : SystemBase
    {
    }

    [UpdateInGroup(typeof(World1Group1))]
    [UpdateAfter(typeof(World1System1))]
    public class World1System2 : SystemBase
    {
    }
}