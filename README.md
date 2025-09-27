# UnsafeEcs-0.7.1 preview

![GitHub](https://img.shields.io/badge/license-MIT-blue.svg)
![GitHub](https://img.shields.io/badge/unsafe-C%23-important)
![Unity](https://img.shields.io/badge/unity-compatible-brightgreen.svg)
![Performance](https://img.shields.io/badge/performance-high-success.svg)

> **Benchmark System**: 12th Gen Intel® Core™ i5-12600KF @ 3.70 GHz  
> [Jump to Performance Benchmarks](#performance-benchmark-analysis)

⚠️ PREVIEW VERSION NOTICE ⚠️  
This is a pre-release preview version of UnsafeEcs. Many features and APIs are subject to change. This documentation is provided for evaluation purposes only and is not representative of the final stable release.

## Table of Contents
- [Introduction](#introduction)
- [Features](#features)
- [Cross-Platform Support](#full-cross-platform-support)
- [Mobile Performance](#mobile-performance-considerations)
- [Installation](#installation)
- [Thread Safety](#critical-note-thread-safety)
- [Getting Started](#getting-started)
- [World Management](#world-management)
- [Query System](#query-system)
- [Examples](#examples)
- [Buffers](#buffers)
- [World Serialization & Deserialization](#world-serialization--deserialization)
- [Performance Benchmark Analysis](#performance-benchmark-analysis)

## Introduction

UnsafeEcs is a high-performance Entity-Component-System (ECS) library for Unity written in unsafe C# code. It enables direct, Burst-compatible operations on entities, allowing you to create/delete entities, add/remove components in burst-jobs, and perform other entity manipulations with exceptional speed and efficiency. Designed for Unity game development scenarios where performance is critical, such as complex simulations and games with thousands of objects.

## Features

- ⚡ **Burst-compatible** unsafe operations
- 🧩 Automatic system ordering via attributes
- 🔍 Optimized query caching
- 🧵 Jobified parallel processing
- 💾 Full world serialization/deserialization in Burst jobs
- 🔄 Entity migration between worlds

## Full Cross-Platform Support

UnsafeEcs is designed to be a fully cross-platform ECS solution that works seamlessly across:

| Platform | Device Type | Support Status |
|----------|-------------|----------------|
| **Windows** | Desktop | ✅ Fully Tested |
| **macOS** | Desktop | ✅ Fully Tested |
| **Linux** | Desktop | ✅ Fully Tested |
| **iOS** | Mobile | ✅ Fully Tested |
| **Android** | Mobile | ✅ Fully Tested |
| **WebGL** | Browser | ✅ Fully Tested |
| **PlayStation** | Console | 🟡 Supported* |
| **Xbox** | Console | 🟡 Supported* |
| **Nintendo Switch** | Console | 🟡 Supported* |

The library leverages Unity's Burst compiler and low-level memory management to ensure consistent performance across all platforms while maintaining full compatibility with platform-specific features.

## Mobile Performance Considerations

UnsafeECS runs exceptionally well on mobile devices—exactly where you need maximum performance.

- 📉 **Low memory footprint**: Minimal overhead for resource-constrained devices
- ⚡ **Efficient CPU usage**: Optimized for mobile processors
- 🔋 **Battery-friendly**: Designed to minimize power consumption

## Installation

### Option 1: Unity Package Manager (Git URL)

Open Package Manager (Window → Package Manager), click "+" → "Add package from git URL..." and enter:
```
https://github.com/Iurii88/UnsafeEcs.git
```

Or add directly to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.iurii88.unsafeecs": "https://github.com/Iurii88/UnsafeEcs.git"
  }
}
```

To install a specific version, use tags:
```json
"com.iurii88.unsafeecs": "https://github.com/Iurii88/UnsafeEcs.git#v0.1.0"
```

### Option 2: Git Submodule

Add as a submodule to your Assets folder:
```bash
git submodule add https://github.com/Iurii88/UnsafeEcs.git Assets/Scripts/UnsafeEcs
git submodule update --init --recursive
```

### Requirements

- Unity 2021.3 or newer
- Enable "Allow 'unsafe' code" in Player Settings (Edit → Project Settings → Player → Other Settings)

### Verify Installation

After installation, you should be able to use the library:
```csharp
using UnsafeEcs;

public class TestSystem : MonoBehaviour 
{
    private EcsWorld world;
    
    void Start() 
    {
        world = new EcsWorld();
    }
}
```

## Critical Note: Thread Safety

### ⚠️ Important Thread Safety Considerations

UnsafeEcs is fully compatible with Burst jobs and enables high-performance ECS operations in parallel contexts. However, it is **not thread-safe** for concurrent structural changes to the ECS architecture. This has important implications for your code:

- ❌ **You cannot safely modify the EntityManager from multiple jobs in parallel**
- ❌ **Concurrent create/delete entities or add/remove components operations will cause race conditions**
- ❌ **Parallel structural changes lead to undefined behavior and data corruption**

### 🔒 Safe Usage Patterns

All structural changes (entity/component modifications) must be performed using one of these approaches:

1. **Sequentially (single-threaded)** - Process structural changes on the main thread
2. **From a single job** - Use IJob instead of IJobParallelFor for structural modifications
3. **Isolate mutations** - Ensure only one thread can modify entities at any given time

### ✅ What Remains Safe for Parallel Processing

- **Component data access and modifications** - Reading/writing component values in parallel jobs
- **Performance-critical iterations** - Processing component data with IJobParallelFor

This distinction is critical: you can transform data in parallel, but changes to the ECS structure itself must be properly isolated to avoid corruption of the entity management system.

## Getting Started

### World Initialization

UnsafeEcs requires initialization of worlds before use. There are two approaches:

#### Automatic Initialization (Recommended)

The automatic approach uses attributes similar to Unity DOTS to configure systems. Create a Bootstrap class:

```csharp
public class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var gameAssembly = Assembly.GetExecutingAssembly();
        var ecsAssembly = Assembly.Load("UnsafeEcs");
        var assemblies = new[]
        {
            gameAssembly,
            ecsAssembly
        };
        WorldBootstrap.Initialize(assemblies);
    }
}
```

This approach supports the following attributes for system ordering:

- `UpdateInWorldAttribute`: Assigns a system to a specific world
  ```csharp
   [UpdateInWorld(0)] //use WorldBootstrap.AllWorldsIndex to add system to all worlds
   public class SimulationSystemGroup : SystemGroup {}
  ```
- `UpdateInGroupAttribute`: Assigns a system to a specific group
  ```csharp
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  public class MyMiddleSystem : SystemBase { ... }
  ```
- `UpdateBeforeAttribute`: Specifies that a system should update before another system
  ```csharp
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  [UpdateBefore(typeof(MyMiddleSystem))]
  public class MyEarlySystem : SystemBase { ... }
  ```
- `UpdateAfterAttribute`: Specifies that a system should update after another system
  ```csharp
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  [UpdateAfter(typeof(MyMiddleSystem))]
  public class MyLateSystem : SystemBase { ... }
  ```

#### Manual Initialization

For more control, you can manually set up worlds and systems:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
public static void ManualInitialize()
{
    var world = WorldManager.CreateWorld();
    
    var initializationSystemGroup = new InitializationSystemGroup();
    world.AddRootSystem(initializationSystemGroup);

    var system = new Examples.CreateEntitiesSystem();
    initializationSystemGroup.AddSystem(system);
}
```

### System Lifecycle

Systems in UnsafeEcs follow a standard lifecycle with these virtual methods:

```csharp
public virtual void OnAwake() {}     // Called when the system is first initialized
public virtual void OnUpdate() {}     // Called each frame during normal updates
public virtual void OnFixedUpdate() {} // Called during physics updates
public virtual void OnDestroy() {}    // Called when the system is being destroyed
```

## World Management

### Accessing Worlds

You can access all available worlds using the WorldManager:

```csharp
// Get all worlds
var allWorlds = WorldManager.Worlds;

// Access a specific world by index
var gameWorld = WorldManager.Worlds[0];
```

## Query System

UnsafeEcs features a high-performance query system that:

- Executes inside jobs for maximum performance
- Automatically caches query results
- Updates only when component changes are detected
- Provides efficient filtering with `With<>`, `Without<>` and `WithAny<>` operators

### Custom Query Filtering

In addition to the standard component-based filtering, UnsafeEcs provides a custom filtering mechanism that allows for more precise entity selection based on component values or other criteria. This is implemented through the `IQueryFilter` interface:

```csharp
public interface IQueryFilter
{
    public bool Validate(Entity entity);
}
```

#### Creating Custom Filters

To create a custom filter, first define your component:

```csharp
public struct IntValueComponent : IComponent
{
    public int value;
}
```

Then implement the `IQueryFilter` interface for your specific filtering needs:

```csharp
public struct IntValueFilter : IQueryFilter
{
    public ComponentArray<IntValueComponent> componentArray;
    public int filterValue; 
    
    public bool Validate(Entity entity)
    {
        ref var component = ref componentArray.Get(entity);
        return component.value == filterValue;
    }
}
```

#### Using Custom Filters in Queries

Once defined, the custom filter can be applied during query execution:

```csharp
var filteredEntities = CreateQuery().With<IntValueComponent>().Fetch(new IntValueFilter
{
    componentArray = GetComponentArray<IntValueComponent>(),
    filterValue = 1
});
```

## Examples

### Creating Entities One by One

Here's an example of creating 1 million entities with a Transform component:

```csharp
public struct Transform : IComponent
{
    public float3 position;
    public quaternion rotation;
    public float3 scale;
}
```

```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class CreateEntitiesSystem : SystemBase
{
    private const int EntitiesCount = 1000000; // 1 million

    public override void OnAwake()
    {
        base.OnAwake();
        
        var jobStopwatch = Stopwatch.StartNew();
        var jobHandle = new CreateEntitiesJob
        {
            entityManagerWrapper = entityManagerWrapper
        }.Schedule();
        jobHandle.Complete();

        jobStopwatch.Stop();
        UnityEngine.Debug.Log($"Created {EntitiesCount}, ticks:{jobStopwatch.ElapsedTicks}, {jobStopwatch.ElapsedMilliseconds} ms");
    }
    
    [BurstCompile]
    private struct CreateEntitiesJob : IJob
    {
        public ReferenceWrapper<EntityManager> entityManagerWrapper;
        public void Execute()
        {
            for (var i = 0; i < EntitiesCount; i++)
            {
                var entity = entityManagerWrapper.Value.CreateEntity();
                //any editing operations are available right in burst-job
                entity.AddComponent(new Transform(float3.zero, quaternion.identity));
                //entity.RemoveComponent<Transform>();
                //entity.Destroy();
            }
        }
    }
}
```

**Performance Results:**
```
Created 1000000, ticks:2000649, 200 ms
```

### Bulk Entity Creation (Recommended)

For significantly improved performance when creating many entities of the same archetype, use the bulk creation method:

```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public class BulkCreationSystem : SystemBase
{
    private const int EntityCount = 1000000;

    public override void OnAwake()
    {
        base.OnAwake();
        
        var jobStopwatch = Stopwatch.StartNew();

        new CreateEntitiesBulkJob
        {
            entityManagerWrapper = world.entityManagerWrapper,
            entityCount = EntityCount
        }.Schedule().Complete();

        jobStopwatch.Stop();
        Debug.Log($"Created {EntityCount}, ticks:{jobStopwatch.ElapsedTicks}, {jobStopwatch.ElapsedMilliseconds} ms");
    }
    
    [BurstCompile]
    private struct CreateEntitiesBulkJob : IJob
    {
        public ReferenceWrapper<EntityManager> entityManagerWrapper;
        public int entityCount;

        public void Execute()
        {
            var unitArchetype = new EntityArchetypeBuilder()
                .With<Transform>()
                .Build();

            var entities = entityManagerWrapper.Value.CreateEntities(unitArchetype, entityCount, Allocator.TempJob);
            var transforms = entityManagerWrapper.Value.GetComponentArray<Transform>();

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];

                ref var transform = ref transforms.Get(entity);
                transform.position = float3.zero;
                transform.rotation = quaternion.identity;
                transform.scale = 1f;
            }

            entities.Dispose();
        }
    }
}
```

**Performance Results:**
```
Created 1000000, ticks:230481, 23 ms
```

### Updating Entities

This example shows how to process entities in parallel with query:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class MovementSystem : SystemBase
{
    private Query query;
    
    public override void OnAwake()
    {
        query = CreateQuery()
            .With<Transform>()
            .Without<Destroy>();
    }

    public override void OnUpdate()
    {
        var entities = query.Fetch();
        var transforms = GetComponentArray<Transform>();

        new MovementJobParallel
            {
                entities = entities,
                transforms = transforms,
                deltaTime = world.deltaTime
            }
            .Schedule(entities.Length, 512).Complete();
    }

    [BurstCompile]
    private struct MovementJobParallel : IJobParallelFor
    {
        [ReadOnly] public UnsafeList<Entity> entities;
        [ReadOnly] public ComponentArray<Transform> transforms;
        public float deltaTime;

        public void Execute(int index)
        {
            var entity = entities[index];
            ref var transform = ref transforms.Get(entity);
            transform.Translate(new float3(0, 0, 1) * deltaTime);
        }
    }
}
```

**Performance Results:**
```
Entities: 1000000, ticks:14848, 1 ms
```

### ForEach Processing

UnsafeEcs also supports DOTS-like ForEach syntax for more concise code:

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class ForEachMovementSystem : SystemBase
{
    private Query query;
    
    public override void OnAwake()
    {
        // Cache the query during initialization, but it's also very fast to do it right in the update
        query = CreateQuery()
            .With<Transform>()
            .Without<Destroy>();
    }

    public override void OnUpdate()
    {
        // Using ForEach pattern
        // NOTE: This will cause allocations due to world.deltaTime capture
        // To avoid allocations, use static variables or job structs
        query.ForEach((ref Entity entity, ref Transform transform) =>
        {
            transform.Translate(new float3(0, 0, 1) * world.deltaTime);
        });
    }
}
```

## Buffers

Buffers provide a dynamic array-like structure for storing collections of data on entities. Similar to components, buffers can be attached to entities but allow for variable-sized data collections rather than fixed structures.

### Buffer Definition

To use buffers, you need to define a buffer element type:

```csharp
public struct BufferElement : IBufferElement
{
    public float3 point;
}
```

The buffer element must implement the `IBufferElement` interface, which marks it as a valid buffer type for the ECS system.

### Creating Archetypes with Buffers

When defining an archetype that includes buffers, use the `WithBuffer<>()` method:

```csharp
var unitArchetype = new EntityArchetypeBuilder()
    .With<Transform, Unit, Renderable>()
    .WithBuffer<BufferElement>()
    .Build();
```

### Adding Buffers to Entities

You can add a buffer to an entity and populate it with elements:

```csharp
// Create a new buffer on an entity
var buffer = entity.AddBuffer<BufferElement>();

// Add a single element
buffer.Add(new BufferElement
{
    point = new float3(0, 0, 0)
});

// Add multiple elements
for (var j = 0; j < 10; j++)
{
    buffer.Add(new BufferElement
    {
        point = new float3(j, j, j)
    });
}
```

### Using ForEach with Buffers

You can query and process entities with buffers using the ForEach API:

```csharp
// Process entities with BufferElement buffers
CreateQuery().With<BufferElement>().ForEach((ref Entity _, DynamicBuffer<BufferElement> buffer) =>
{
    // Process each buffer
    for (int i = 0; i < buffer.Length; i++)
    {
        ref var element = ref buffer[i];
        element.point += new float3(0, 1, 0);
    }
});
```

### Buffer Queries in Jobs

Buffers can be used in job systems for parallel processing:

```csharp
// Fetch entities with buffers
var entities = CreateQuery().With<BufferElement>().Fetch();
var buffers = GetBufferArray<BufferElement>();

// Process buffers in parallel job
new ProcessBuffersJob
{
    entities = entities,
    buffers = buffers,
    deltaTime = Time.deltaTime
}.Schedule(entities.Length, 64).Complete();
```

```csharp
[BurstCompile]
private struct ProcessBuffersJob : IJobParallelFor
{
    [ReadOnly] public UnsafeList<Entity> entities;
    public BufferArray<BufferElement> buffers;
    public float deltaTime;

    public void Execute(int index)
    {
        var entity = entities[index];
        var buffer = buffers.Get(entity);
        
        for (int i = 0; i < buffer.Length; i++)
        {
            ref var point = ref buffer[i].point;
            point += new float3(0, 0, 1);
        }
    }
}
```

## World Serialization & Deserialization

UnsafeEcs provides a powerful, high-performance serialization system that enables:

- 💾 **Complete world state preservation**
- 🚀 **Burst-compatible serialization jobs**
- ⏱️ **Ultra-fast save/load operations**
- 🔄 **Seamless migration between runtime and saved states**

### Simple API, Powerful Results

```csharp
// SAVE: Serialize all worlds in a single line
byte[] worldData = EcsSerializer.Serialize();

// LOAD: Restore entire ECS state including all worlds, entities, and components
EcsSerializer.Deserialize(worldData);
```

### Performance Benchmarks

The serialization system achieves exceptional performance even with massive entity counts:

| Entity Count | Components | Data Size | Serialization | Deserialization |
|-------------|-----------|----------|--------------|----------------|
| 1,000,000   | Transform | 88.88MB  | 40ms         | 15ms           |

### Compression Integration

While UnsafeEcs doesn't include compression functionality, integrating a compression solution is strongly recommended as ECS data typically achieves excellent compression ratios (often 95-98% size reduction) due to its structured nature.

```csharp
// Serialize and compress in one operation
byte[] worldData = EcsSerializer.Serialize();
byte[] compressedData = YourCompressionLibrary.Compress(worldData);
// Typical compression results: 84.88MB → 3.68MB (96% reduction)

// Save to disk
File.WriteAllBytes("game_save.dat", compressedData);

// Load and restore
byte[] loadedData = File.ReadAllBytes("game_save.dat");
byte[] decompressedData = YourCompressionLibrary.Decompress(loadedData);
EcsSerializer.Deserialize(decompressedData);
```

### Entity Migration Between Worlds

You can migrate entities between different worlds:

```csharp
var sourceWorld = WorldManager.GetWorld(0);
var destinationWorld = WorldManager.GetWorld(1);
var migratedEntity = EntityMigration.MigrateEntity(sourceWorld, destinationWorld, entity);
```

## Performance Benchmark Analysis

### Overview

This benchmark analysis evaluates the library's performance across different operation types and execution modes using 500,000 entities.

### Test Environment

- **Hardware**: 12th Gen Intel® Core™ i5-12600KF @ 3.70 GHz
- **Entity Count**: 500,000
- **Components**: Four test components (TestComponent0-3)
- **Memory Usage**: ~57 MB allocated

### Test Scenarios

The benchmark evaluates three key scenarios in both single-threaded (Update) and multi-threaded (Jobs) execution modes:

1. **Component Access & Modification**: Incrementing values in all four components
2. **Single Component Structural Changes**: Removing and re-adding a single component type
3. **Multiple Component Structural Changes**: Removing and re-adding three component types

### Performance Results

#### Test 1: Component Access & Modification

| Execution Mode | FPS    | Frame Time | Performance Comparison |
|---------------|--------|------------|------------------------|
| Update        | 325 fps | 3.0 ms     | Baseline               |
| Jobs          | 1097 fps | 0.9 ms     | 3.4× faster            |

**Code Implementation:**

```csharp
// Update mode (single-threaded)
private unsafe void ExecuteTest1Update()
{
    var entities = m_filter3.Fetch();
    for (var i = 0; i < entities.m_length; i++)
    {
        ref var entity = ref entities.Ptr[i];
        m_stash0.Get(entity).Test++;
        m_stash1.Get(entity).Test++;
        m_stash2.Get(entity).Test++;
        m_stash3.Get(entity).Test++;
    }
}

// Jobs mode (multi-threaded)
[BurstCompile]
private struct Test1Job : IJobParallelFor
{
    [ReadOnly] public UnsafeList<Entity> entities;
    [ReadOnly] public ComponentArray<TestComponent0> stash0;
    [ReadOnly] public ComponentArray<TestComponent1> stash1;
    [ReadOnly] public ComponentArray<TestComponent2> stash2;
    [ReadOnly] public ComponentArray<TestComponent3> stash3;

    public unsafe void Execute(int index)
    {
        ref var entity = ref entities.Ptr[index];
        stash0.Get(entity).Test++;
        stash1.Get(entity).Test++;
        stash2.Get(entity).Test++;
        stash3.Get(entity).Test++;
    }
}
```

#### Test 2: Single Component Structural Changes

| Execution Mode | FPS    | Frame Time | Performance Comparison |
|---------------|--------|------------|------------------------|
| Update        | 152 fps | 6.5 ms     | Baseline               |
| Jobs          | 176 fps | 5.6 ms     | 1.16× faster           |

**Code Implementation:**

```csharp
// Update mode (single-threaded)
private unsafe void ExecuteTest2Update()
{
    var entities = m_filter3.Fetch();
    for (var i = 0; i < entities.m_length; i++)
        m_stash3.Remove(entities.Ptr[i]);

    entities = m_filter0.Fetch();
    for (var i = 0; i < entities.m_length; i++)
        m_stash3.Add(entities.Ptr[i]);
}

// Jobs mode (single job)
[BurstCompile]
private struct Test2Job : IJob
{
    [ReadOnly] public EntityQuery filter3;
    [ReadOnly] public EntityQuery filter0;
    [ReadOnly] public ComponentArray<TestComponent3> stash3;

    public unsafe void Execute()
    {
        var entities = filter3.FetchWithoutJob();
        for (var i = 0; i < entities.m_length; i++)
            stash3.Remove(entities.Ptr[i]);

        entities = filter0.FetchWithoutJob();
        for (var i = 0; i < entities.m_length; i++)
            stash3.Add(entities.Ptr[i]);
    }
}
```

#### Test 3: Multiple Component Structural Changes

| Execution Mode | FPS    | Frame Time | Performance Comparison |
|---------------|--------|------------|------------------------|
| Update        | 76 fps  | 13.1 ms    | Baseline               |
| Jobs          | 97 fps  | 10.3 ms    | 1.28× faster           |

**Code Implementation:**

```csharp
// Update mode (single-threaded)
private unsafe void ExecuteTest3Update()
{
    var entities = m_filter3.Fetch();
    for (var i = 0; i < entities.m_length; i++)
    {
        m_stash1.Remove(entities.Ptr[i]);
        m_stash2.Remove(entities.Ptr[i]);
        m_stash3.Remove(entities.Ptr[i]);
    }

    entities = m_filter0.Fetch();
    for (var i = 0; i < entities.m_length; i++)
    {
        m_stash1.Add(entities.Ptr[i]);
        m_stash2.Add(entities.Ptr[i]);
        m_stash3.Add(entities.Ptr[i]);
    }
}

// Jobs mode (single job)
[BurstCompile]
private struct Test3Job : IJob
{
    [ReadOnly] public EntityQuery filter3;
    [ReadOnly] public EntityQuery filter0;
    [ReadOnly] public ComponentArray<TestComponent1> stash1;
    [ReadOnly] public ComponentArray<TestComponent2> stash2;
    [ReadOnly] public ComponentArray<TestComponent3> stash3;

    public unsafe void Execute()
    {
        var entities = filter3.FetchWithoutJob();
        for (var i = 0; i < entities.m_length; i++)
        {
            stash1.Remove(entities.Ptr[i]);
            stash2.Remove(entities.Ptr[i]);
            stash3.Remove(entities.Ptr[i]);
        }

        entities = filter0.FetchWithoutJob();
        for (var i = 0; i < entities.m_length; i++)
        {
            stash1.Add(entities.Ptr[i]);
            stash2.Add(entities.Ptr[i]);
            stash3.Add(entities.Ptr[i]);
        }
    }
}
```
