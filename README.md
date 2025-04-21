# UnsafeEcs

![GitHub](https://img.shields.io/badge/license-MIT-blue.svg)
![GitHub](https://img.shields.io/badge/unsafe-C%23-important)
![Unity](https://img.shields.io/badge/unity-compatible-brightgreen.svg)
![Performance](https://img.shields.io/badge/performance-high-success.svg)

> **Benchmark System**: 12th Gen Intel® Core™ i5-12600KF @ 3.70 GHz

## Introduction

UnsafeEcs is a high-performance Entity-Component-System (ECS) library for Unity written in unsafe C# code. It enables direct, Burst-compatible operations on entities, allowing you to create/delete entities, add/remove components in burst-jobs, and perform other entity manipulations with exceptional speed and efficiency. Designed for Unity game development scenarios where performance is critical, such as complex simulations and games with thousands of objects.

## Features

- ⚡ **Burst-compatible** unsafe operations
- 🧩 Automatic system ordering via attributes
- 🔍 Optimized query caching
- 🧵 Jobified parallel processing
- 💾 Full world serialization/deserialization in Burst jobs
- 🔄 Entity migration between worlds

## Installation

### As a Git Submodule (Recommended)

```
git submodule add git@github.com:Iurii88/UnsafeEcs.git Assets/Plugins/UnsafeEcs
```

### Manual Installation

Clone or download the repository and place it in your Unity project's Assets folder:

```
git clone git@github.com:Iurii88/UnsafeEcs.git Assets/Plugins/UnsafeEcs
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
- **Query operations** - Fetching and filtering entities in parallel contexts

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
   [UpdateInWorld(0)]
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

## Examples

### Creating Entities

Here's an example of creating 1 million entities with a Transform component:

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
Created 1000000, ticks:2203989, 220 ms
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
Entities: 1000000, ticks:16350, 1 ms
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

# Buffers

Buffers provide a dynamic array-like structure for storing collections of data on entities. Similar to components, buffers can be attached to entities but allow for variable-sized data collections rather than fixed structures.

## Buffer Definition

To use buffers, you need to define a buffer element type:

```csharp
public struct BufferElement : IBufferElement
{
    public float3 point;
}
```

The buffer element must implement the `IBufferElement` interface, which marks it as a valid buffer type for the ECS system.

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
        buffer[i] = new BufferElement 
        { 
            point = buffer[i].point + new float3(0, 1, 0) 
        };
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

# World Serialization & Deserialization

UnsafeEcs supports full serialization and deserialization of all worlds and their data in Burst jobs through the `EcsSerializer` interface. This provides a simple yet powerful mechanism to save and restore your entire ECS state.

## Serialization API

The serialization system has been simplified with a centralized API that handles both serialization and deserialization of all worlds:

```csharp
// Serialize all worlds to a single byte array
var ecsData = EcsSerializer.Serialize();

// Deserialize all worlds from the byte array
// This will create any necessary worlds and restore all entity data
EcsSerializer.Deserialize(ecsData);
```

## Performance

The serialization system is highly optimized for performance:

```
[Serialization] Serialized ECS data (Size: 84.88MB) in 41ms (1m entities with transform component)

[ECS Data] ECS data deserialized in 35ms (all worlds and entities are created already)
```

## Data Compression

The framework itself doesn't provide built-in compression, but implementing compression is highly recommended as it can dramatically reduce storage requirements:

```csharp
// Serialize all worlds
var ecsData = EcsSerializer.Serialize();

// Use your preferred compression library 
// (not included in UnsafeEcs but strongly recommended)
var compressedData = YourCompressionLibrary.Compress(ecsData);
// Typical compression results: Original: 84.88MB → Compressed: 3.68MB (4.3% of original size)

// Save compressed data
File.WriteAllBytes("game_save.dat", compressedData);

// Later, load and decompress
var loadedData = File.ReadAllBytes("game_save.dat");
var decompressedData = YourCompressionLibrary.Decompress(loadedData);

// Deserialize all worlds
EcsSerializer.Deserialize(decompressedData);
```

While UnsafeEcs doesn't include compression functionality, integrating a compression solution is strongly recommended as ECS data typically achieves excellent compression ratios (often 95-98% size reduction) due to its structured nature.

### Entity Migration Between Worlds

You can migrate entities between different worlds:

```csharp
var sourceWorld = WorldManager.GetWorld(0);
var destinationWorld = WorldManager.GetWorld(1);
var migratedEntity = EntityMigration.MigrateEntity(sourceWorld, destinationWorld, entity);
```
