using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;
using Debug = UnityEngine.Debug;

namespace UnsafeEcs.Core.Bootstrap
{
    public static class WorldBootstrap
    {
        // Log levels
        public enum LogLevel
        {
            None, // No logging at all
            Minimal, // Only critical information and errors
            Normal, // Default level with important events
            Verbose, // More detailed logging of system operations
            Diagnostic // Everything including timing and diagnostics
        }

        // Special index indicating a system should be added to all worlds
        public const int AllWorldsIndex = int.MaxValue;

        // Color constants for Unity console
        private const string ColorSystem = "#4EC9B0"; // Light blue
        private const string ColorGroup = "#569CD6"; // Blue
        private const string ColorWorld = "#B5CEA8"; // Green
        private const string ColorWarning = "#CE9178"; // Orange
        private const string ColorError = "#F44747"; // Red
        private const string ColorHighlight = "#DCDCAA"; // Yellow

        private static LogLevel m_currentLogLevel;

        private static readonly Dictionary<Type, List<int>> ResolvedWorldIndices = new();
        private static readonly Dictionary<int, World> CreatedWorlds = new();
        private static BootstrapCache m_cache;
        private static Stopwatch m_stopwatch;

        public static Action<SystemBase> onSystemCreated;

        // Original method for backwards compatibility
        public static void Initialize(Assembly[] assemblies, WorldBootstrapOptions options)
        {
            m_currentLogLevel = options.logLevel;
            WorldManager.Initialize(options.dontDestroyOnLoad);
            InitializeWorlds(assemblies);
        }

        // New method for using predefined worlds
        public static void Initialize(Assembly[] assemblies, Dictionary<int, World> worlds, WorldBootstrapOptions options)
        {
            m_currentLogLevel = options.logLevel;
            WorldManager.Initialize(options.dontDestroyOnLoad);
            InitializeWorlds(assemblies, worlds);
        }

        // Alternative overload with world list (indices will be auto-assigned starting from 0)
        public static void Initialize(Assembly[] assemblies, List<World> worlds, WorldBootstrapOptions options)
        {
            m_currentLogLevel = options.logLevel;
            WorldManager.Initialize(options.dontDestroyOnLoad);

            // Convert list to dictionary with auto-assigned indices
            var worldDict = new Dictionary<int, World>();
            for (var i = 0; i < worlds.Count; i++)
            {
                worldDict[i] = worlds[i];
            }

            InitializeWorlds(assemblies, worldDict);
        }

        // Original test method for backwards compatibility
        public static void InitializeForTests(Assembly[] assemblies, LogLevel logLevel = LogLevel.Normal)
        {
            m_currentLogLevel = logLevel;
            m_cache = null;
            ResolvedWorldIndices.Clear();
            CreatedWorlds.Clear();
            WorldManager.InitializeForTests();
            InitializeWorlds(assemblies);
        }

        // New test method for using predefined worlds
        public static void InitializeForTests(Assembly[] assemblies, Dictionary<int, World> worlds, LogLevel logLevel = LogLevel.Normal)
        {
            m_currentLogLevel = logLevel;
            m_cache = null;
            ResolvedWorldIndices.Clear();
            CreatedWorlds.Clear();
            WorldManager.InitializeForTests();
            InitializeWorlds(assemblies, worlds);
        }

        // Alternative test overload with world list
        public static void InitializeForTests(Assembly[] assemblies, List<World> worlds, LogLevel logLevel = LogLevel.Normal)
        {
            m_currentLogLevel = logLevel;
            m_cache = null;
            ResolvedWorldIndices.Clear();
            CreatedWorlds.Clear();
            WorldManager.InitializeForTests();

            // Convert list to dictionary with auto-assigned indices
            var worldDict = new Dictionary<int, World>();
            for (var i = 0; i < worlds.Count; i++)
            {
                worldDict[i] = worlds[i];
            }

            InitializeWorlds(assemblies, worldDict);
        }

        // Original method (create worlds automatically)
        private static void InitializeWorlds(Assembly[] assemblies)
        {
            InitializeWorlds(assemblies, null);
        }

        // Extended method that supports both automatic world creation and predefined worlds
        private static void InitializeWorlds(Assembly[] assemblies, Dictionary<int, World> predefinedWorlds)
        {
            m_stopwatch = Stopwatch.StartNew();

            // Analyze systems if cache is empty
            if (m_cache == null)
            {
                Log("<b>=== SYSTEM ANALYSIS PHASE ===</b>", LogLevel.Minimal);
                m_cache = AnalyzeSystems(assemblies);
            }

            Log($"Bootstrap analysis completed in: <color={ColorHighlight}>{m_stopwatch.ElapsedMilliseconds}ms</color>", LogLevel.Normal);

            // Prepare world mappings
            ResolvedWorldIndices.Clear();
            foreach (var kv in m_cache.systemWorldIndices)
                ResolvedWorldIndices[kv.Key] = kv.Value;

            // Handle world creation/assignment
            CreatedWorlds.Clear();

            if (predefinedWorlds != null && predefinedWorlds.Count > 0)
            {
                // Use predefined worlds
                Log($"Using {predefinedWorlds.Count} predefined worlds:", LogLevel.Normal);
                foreach (var worldPair in predefinedWorlds.OrderBy(kv => kv.Key))
                {
                    CreatedWorlds[worldPair.Key] = worldPair.Value;
                    Log($"Using predefined world <color={ColorWorld}>#{worldPair.Key}</color>", LogLevel.Normal);
                }

                // Check if we need additional worlds that weren't provided
                var requiredWorldIndices = new HashSet<int>();
                foreach (var worldsList in ResolvedWorldIndices.Values)
                {
                    foreach (var index in worldsList)
                    {
                        if (index != AllWorldsIndex && !predefinedWorlds.ContainsKey(index))
                        {
                            requiredWorldIndices.Add(index);
                        }
                    }
                }

                // Create missing worlds if needed
                if (requiredWorldIndices.Count > 0)
                {
                    Log($"Creating {requiredWorldIndices.Count} additional required worlds:", LogLevel.Normal);
                    foreach (var worldIndex in requiredWorldIndices.OrderBy(i => i))
                    {
                        CreatedWorlds[worldIndex] = WorldManager.CreateWorld();
                        Log($"Created additional world <color={ColorWorld}>#{worldIndex}</color>", LogLevel.Normal);
                    }
                }
            }
            else
            {
                // Original behavior - create worlds automatically
                var worldIndices = new HashSet<int>();

                // Collect all specific world indices
                foreach (var worldsList in ResolvedWorldIndices.Values)
                {
                    foreach (var index in worldsList)
                    {
                        if (index != AllWorldsIndex)
                        {
                            worldIndices.Add(index);
                        }
                    }
                }

                // Create worlds in order
                var sortedIndices = worldIndices.OrderBy(i => i).ToList();
                Log($"Creating {sortedIndices.Count} worlds in order:", LogLevel.Normal);
                foreach (var worldIndex in sortedIndices)
                {
                    CreatedWorlds[worldIndex] = WorldManager.CreateWorld();
                    Log($"Created world <color={ColorWorld}>#{worldIndex}</color>", LogLevel.Normal);
                }
            }

            // Validate world assignments
            ValidateWorldAssignments();

            // Create and organize systems
            Log("<b>=== SYSTEM INITIALIZATION PHASE ===</b>", LogLevel.Minimal);
            CreateSystemHierarchy(m_cache.allSystemTypes, m_cache.groupChildren);

            Log($"Total initialization time: <color={ColorHighlight}>{m_stopwatch.ElapsedMilliseconds}ms</color>", LogLevel.Minimal);
            m_stopwatch.Stop();
        }

        // New method to validate that all required worlds exist
        private static void ValidateWorldAssignments()
        {
            var missingWorlds = new List<int>();
            var systemsWithMissingWorlds = new List<Type>();

            foreach (var systemWorldPair in ResolvedWorldIndices)
            {
                var systemType = systemWorldPair.Key;
                var worldIndices = systemWorldPair.Value;

                foreach (var worldIndex in worldIndices)
                {
                    if (worldIndex != AllWorldsIndex && !CreatedWorlds.ContainsKey(worldIndex))
                    {
                        if (!missingWorlds.Contains(worldIndex))
                            missingWorlds.Add(worldIndex);

                        if (!systemsWithMissingWorlds.Contains(systemType))
                            systemsWithMissingWorlds.Add(systemType);
                    }
                }
            }

            if (missingWorlds.Count > 0)
            {
                var missingWorldsStr = string.Join(", ", missingWorlds.Select(w => $"#{w}"));
                var affectedSystemsStr = string.Join(", ", systemsWithMissingWorlds.Select(t => t.Name));

                LogWarning($"Missing worlds: <color={ColorWorld}>{missingWorldsStr}</color>");
                LogWarning($"Affected systems: <color={ColorSystem}>{affectedSystemsStr}</color>");

                throw new InvalidOperationException(
                    $"Required worlds are missing: {missingWorldsStr}. " +
                    "Either provide these worlds in predefinedWorlds parameter or adjust system world assignments.");
            }
        }

        // Method to get information about world requirements (useful for debugging)
        public static Dictionary<int, List<Type>> GetWorldSystemRequirements(Assembly[] assemblies)
        {
            var tempCache = AnalyzeSystems(assemblies);
            var result = new Dictionary<int, List<Type>>();

            foreach (var systemWorldPair in tempCache.systemWorldIndices)
            {
                var systemType = systemWorldPair.Key;
                var worldIndices = systemWorldPair.Value;

                foreach (var worldIndex in worldIndices)
                {
                    if (worldIndex == AllWorldsIndex)
                        continue; // Skip AllWorldsIndex in requirements

                    if (!result.ContainsKey(worldIndex))
                        result[worldIndex] = new List<Type>();

                    result[worldIndex].Add(systemType);
                }
            }

            return result;
        }

        // Method to get systems that will be added to all worlds
        public static List<Type> GetAllWorldsSystems(Assembly[] assemblies)
        {
            var tempCache = AnalyzeSystems(assemblies);
            var result = new List<Type>();

            foreach (var systemWorldPair in tempCache.systemWorldIndices)
            {
                var systemType = systemWorldPair.Key;
                var worldIndices = systemWorldPair.Value;

                if (worldIndices.Contains(AllWorldsIndex))
                {
                    result.Add(systemType);
                }
            }

            return result;
        }

        private static BootstrapCache AnalyzeSystems(Assembly[] assemblies)
        {
            var result = new BootstrapCache();

            // Find all system types across all assemblies
            result.allSystemTypes = assemblies
                .SelectMany(asm => asm.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(SystemBase)))
                .ToList();

            Log($"Found <color={ColorHighlight}>{result.allSystemTypes.Count}</color> system types", LogLevel.Minimal);

            // Resolve world indices for each system
            foreach (var type in result.allSystemTypes)
            {
                if (!result.systemWorldIndices.ContainsKey(type))
                {
                    var resolvedIndices = ResolveWorldIndices(type, result.allSystemTypes, result.systemWorldIndices);
                    if (resolvedIndices != null && resolvedIndices.Count > 0)
                    {
                        result.systemWorldIndices[type] = resolvedIndices;

                        // Log world assignments
                        if (resolvedIndices.Contains(AllWorldsIndex))
                        {
                            Log($"System <color={ColorSystem}>{type.Name}</color> assigned to <color={ColorWorld}>ALL worlds</color>", LogLevel.Verbose);
                        }
                        else
                        {
                            var worldsStr = string.Join(", ", resolvedIndices.Select(idx => $"<color={ColorWorld}>#{idx}</color>"));
                            Log($"System <color={ColorSystem}>{type.Name}</color> assigned to worlds: {worldsStr}", LogLevel.Verbose);
                        }
                    }
                    else
                    {
                        LogWarning($"System <color={ColorSystem}>{type.Name}</color> has no world assignment!");
                    }
                }
            }

            // Summarize world assignments
            if (m_currentLogLevel >= LogLevel.Normal)
            {
                var worldCounts = new Dictionary<int, int>();
                var multiWorldSystems = 0;
                var allWorldSystems = 0;

                foreach (var worldIndices in result.systemWorldIndices.Values)
                {
                    if (worldIndices.Contains(AllWorldsIndex))
                    {
                        allWorldSystems++;
                        continue;
                    }

                    if (worldIndices.Count > 1)
                    {
                        multiWorldSystems++;
                    }

                    foreach (var worldIndex in worldIndices)
                    {
                        if (!worldCounts.ContainsKey(worldIndex))
                            worldCounts[worldIndex] = 0;
                        worldCounts[worldIndex]++;
                    }
                }

                Log("<b>World Assignments Summary:</b>", LogLevel.Normal);
                foreach (var worldKv in worldCounts.OrderBy(kv => kv.Key))
                {
                    Log($"World <color={ColorWorld}>#{worldKv.Key}</color>: {worldKv.Value} systems", LogLevel.Normal);
                }

                if (allWorldSystems > 0)
                {
                    Log($"Systems assigned to <color={ColorWorld}>ALL worlds</color>: {allWorldSystems}", LogLevel.Normal);
                }

                if (multiWorldSystems > 0)
                {
                    Log($"Systems assigned to <color={ColorWorld}>multiple specific worlds</color>: {multiWorldSystems}", LogLevel.Normal);
                }
            }

            // Build group hierarchy
            foreach (var type in result.allSystemTypes)
            {
                var groupAttrs = type.GetCustomAttributes<UpdateInGroupAttribute>();
                foreach (var attr in groupAttrs)
                {
                    if (!result.groupChildren.TryGetValue(attr.GroupType, out var children))
                    {
                        result.groupChildren[attr.GroupType] = children = new List<Type>();
                    }

                    children.Add(type);
                    Log($"System <color={ColorSystem}>{type.Name}</color> added to group <color={ColorGroup}>{attr.GroupType.Name}</color>", LogLevel.Verbose);
                }
            }

            // Log group hierarchy if verbose
            if (m_currentLogLevel >= LogLevel.Verbose)
            {
                Log("<b>Group Hierarchy:</b>", LogLevel.Verbose);
                var groupTypes = result.groupChildren.Keys.ToList();
                foreach (var groupType in groupTypes)
                {
                    var childCount = result.groupChildren[groupType].Count;
                    Log($"Group <color={ColorGroup}>{groupType.Name}</color>: {childCount} children", LogLevel.Verbose);
                }
            }

            return result;
        }

        private static List<int> ResolveWorldIndices(Type type, List<Type> allTypes, Dictionary<Type, List<int>> resolvedIndices)
        {
            // Check cache first
            if (resolvedIndices.TryGetValue(type, out var cachedIndices))
                return cachedIndices;

            // Check for direct world attributes (can have multiple)
            var worldAttrs = type.GetCustomAttributes<UpdateInWorldAttribute>().ToList();
            if (worldAttrs.Count > 0)
            {
                var indices = new List<int>();
                foreach (var attr in worldAttrs)
                {
                    if (attr.WorldIndex == AllWorldsIndex || !indices.Contains(attr.WorldIndex))
                    {
                        indices.Add(attr.WorldIndex);
                    }
                }

                // If AllWorldsIndex is in the list, it's the only one that matters
                if (indices.Contains(AllWorldsIndex))
                {
                    return new List<int> { AllWorldsIndex };
                }

                return indices;
            }

            // Check parent groups for world assignment
            var groupAttrs = type.GetCustomAttributes<UpdateInGroupAttribute>();
            var parentIndices = new List<int>();

            foreach (var groupAttr in groupAttrs)
            {
                if (allTypes.Contains(groupAttr.GroupType))
                {
                    var groupIndices = ResolveWorldIndices(groupAttr.GroupType, allTypes, resolvedIndices);
                    if (groupIndices != null && groupIndices.Count > 0)
                    {
                        // If any parent group is in all worlds, this system is in all worlds
                        if (groupIndices.Contains(AllWorldsIndex))
                        {
                            return new List<int> { AllWorldsIndex };
                        }

                        // Add any new world indices from the parent
                        foreach (var idx in groupIndices)
                        {
                            if (!parentIndices.Contains(idx))
                            {
                                parentIndices.Add(idx);
                            }
                        }
                    }
                }
            }

            return parentIndices.Count > 0 ? parentIndices : null;
        }

        private static void CreateSystemHierarchy(List<Type> allTypes, Dictionary<Type, List<Type>> groupMap)
        {
            // Track system instances by type and world
            var systemInstancesByWorld = new Dictionary<Type, Dictionary<int, SystemBase>>();

            // First, instantiate all systems for their assigned worlds
            foreach (var type in allTypes)
            {
                if (ResolvedWorldIndices.TryGetValue(type, out var worldIndices) && worldIndices.Count > 0)
                {
                    systemInstancesByWorld[type] = new Dictionary<int, SystemBase>();

                    // Handle AllWorldsIndex case
                    if (worldIndices.Contains(AllWorldsIndex))
                    {
                        foreach (var worldEntry in CreatedWorlds)
                        {
                            var instance = (SystemBase)Activator.CreateInstance(type);
                            onSystemCreated?.Invoke(instance);
                            systemInstancesByWorld[type][worldEntry.Key] = instance;
                            Log($"Instantiated system <color={ColorSystem}>{type.Name}</color> in world <color={ColorWorld}>#{worldEntry.Key}</color> (all worlds mode)", LogLevel.Verbose);
                        }
                    }
                    else
                    {
                        // Handle specific world indices
                        foreach (var worldIndex in worldIndices)
                        {
                            if (CreatedWorlds.TryGetValue(worldIndex, out _))
                            {
                                var instance = (SystemBase)Activator.CreateInstance(type);
                                onSystemCreated?.Invoke(instance);
                                systemInstancesByWorld[type][worldIndex] = instance;
                                Log($"Instantiated system <color={ColorSystem}>{type.Name}</color> in world <color={ColorWorld}>#{worldIndex}</color>", LogLevel.Verbose);
                            }
                        }
                    }
                }
            }

            // Find root systems (not in any group) for each world
            var rootSystemsByWorld = new Dictionary<int, List<SystemBase>>();
            var processedSystems = new Dictionary<int, HashSet<Type>>();

            // Initialize tracking collections for each world
            foreach (var worldId in CreatedWorlds.Keys)
            {
                rootSystemsByWorld[worldId] = new List<SystemBase>();
                processedSystems[worldId] = new HashSet<Type>();
            }

            // Identify root systems for each world
            foreach (var systemEntry in systemInstancesByWorld)
            {
                var systemType = systemEntry.Key;
                var isChild = groupMap.Values.Any(children => children.Contains(systemType));

                if (!isChild)
                {
                    foreach (var worldInstance in systemEntry.Value)
                    {
                        var worldIndex = worldInstance.Key;
                        rootSystemsByWorld[worldIndex].Add(worldInstance.Value);
                        Log($"Root system found: <color={ColorSystem}>{systemType.Name}</color> in world <color={ColorWorld}>#{worldIndex}</color>", LogLevel.Verbose);
                    }
                }
            }

            // Log summary of root systems by world
            if (m_currentLogLevel >= LogLevel.Normal)
            {
                Log("<b>Root Systems by World:</b>", LogLevel.Normal);
                foreach (var worldKv in rootSystemsByWorld.OrderBy(kv => kv.Key))
                {
                    Log($"World <color={ColorWorld}>#{worldKv.Key}</color>: {worldKv.Value.Count} root systems", LogLevel.Normal);
                }
            }

            // Process root systems and build hierarchy for each world
            foreach (var worldEntry in CreatedWorlds)
            {
                var worldIndex = worldEntry.Key;
                var world = worldEntry.Value;
                var rootSystems = rootSystemsByWorld[worldIndex];

                Log($"Processing world <color={ColorWorld}>#{worldIndex}</color>", LogLevel.Normal);

                var sortedSystems = SortSystemsWithDependencies(rootSystems);
                foreach (var system in sortedSystems)
                {
                    if (system is SystemGroup group)
                    {
                        PopulateSystemGroup(group, groupMap, systemInstancesByWorld, processedSystems, worldIndex);
                    }

                    processedSystems[worldIndex].Add(system.GetType());
                    world.AddRootSystem(system);
                    Log($"Added root system <color={ColorSystem}>{system.GetType().Name}</color> to world <color={ColorWorld}>#{worldIndex}</color>", LogLevel.Normal);
                }
            }

            // Handle orphaned systems (in world but not in hierarchy)
            var orphanedCount = 0;
            foreach (var systemEntry in systemInstancesByWorld)
            {
                var systemType = systemEntry.Key;

                foreach (var worldInstancePair in systemEntry.Value)
                {
                    var worldIndex = worldInstancePair.Key;
                    var systemInstance = worldInstancePair.Value;

                    if (!processedSystems.TryGetValue(worldIndex, out var processed) || !processed.Contains(systemType))
                    {
                        if (CreatedWorlds.TryGetValue(worldIndex, out var world))
                        {
                            world.AddRootSystem(systemInstance);
                            orphanedCount++;
                            LogWarning($"Orphaned system added directly to world: <color={ColorSystem}>{systemType.Name}</color> in world <color={ColorWorld}>#{worldIndex}</color>");
                        }
                    }
                }
            }

            if (orphanedCount > 0)
            {
                LogWarning($"Found {orphanedCount} orphaned systems. Check your system hierarchy setup.");
            }
        }

        private static void PopulateSystemGroup(
            SystemGroup group,
            Dictionary<Type, List<Type>> groupMap,
            Dictionary<Type, Dictionary<int, SystemBase>> systemInstancesByWorld,
            Dictionary<int, HashSet<Type>> processedSystems,
            int worldIndex)
        {
            var groupType = group.GetType();
            if (!groupMap.TryGetValue(groupType, out var childTypes))
                return;

            var children = new List<SystemBase>();
            foreach (var childType in childTypes)
            {
                if (systemInstancesByWorld.TryGetValue(childType, out var worldInstances) &&
                    worldInstances.TryGetValue(worldIndex, out var childSystem))
                {
                    children.Add(childSystem);
                }
            }

            var sortedChildren = SortSystemsWithDependencies(children);

            Log($"Group <color={ColorGroup}>{groupType.Name}</color> in world <color={ColorWorld}>#{worldIndex}</color>: adding {sortedChildren.Count} child systems", LogLevel.Normal);

            foreach (var child in sortedChildren)
            {
                if (child is SystemGroup childGroup)
                {
                    PopulateSystemGroup(childGroup, groupMap, systemInstancesByWorld, processedSystems, worldIndex);
                }

                if (!processedSystems.TryGetValue(worldIndex, out var processed))
                {
                    processedSystems[worldIndex] = processed = new HashSet<Type>();
                }

                processed.Add(child.GetType());
                group.AddSystem(child);
                Log($"Added child system <color={ColorSystem}>{child.GetType().Name}</color> to group <color={ColorGroup}>{groupType.Name}</color> in world <color={ColorWorld}>#{worldIndex}</color>",
                    LogLevel.Verbose);
            }
        }

        private static List<SystemBase> SortSystemsWithDependencies(List<SystemBase> systems)
        {
            if (systems.Count <= 1)
                return systems;

            Log($"Sorting {systems.Count} systems with dependencies", LogLevel.Verbose);

            // Build dependency graph
            var dependencyGraph = BuildDependencyGraph(systems);

            // Perform topological sort
            var visited = new HashSet<SystemBase>();
            var tempMarked = new HashSet<SystemBase>();
            var result = new List<SystemBase>();
            var cyclePath = new Stack<SystemBase>();

            foreach (var system in systems)
            {
                if (!visited.Contains(system))
                {
                    VisitSystemForSorting(
                        system,
                        dependencyGraph,
                        visited,
                        tempMarked,
                        result,
                        cyclePath);
                }
            }

            // Log final execution order
            if (m_currentLogLevel >= LogLevel.Diagnostic)
            {
                Log("<b>Final execution order:</b>", LogLevel.Diagnostic);
                for (var i = 0; i < result.Count; i++)
                {
                    Log($"  {i + 1}. <color={ColorSystem}>{result[i].GetType().Name}</color>", LogLevel.Diagnostic);
                }
            }

            return result;
        }

        private static void VisitSystemForSorting(
            SystemBase system,
            Dictionary<SystemBase, List<SystemBase>> graph,
            HashSet<SystemBase> visited,
            HashSet<SystemBase> tempMarked,
            List<SystemBase> result,
            Stack<SystemBase> currentPath)
        {
            if (tempMarked.Contains(system))
            {
                // Build cycle description properly
                var cycleList = new List<SystemBase>();

                // Find the starting point of cycle in the path
                var pathArray = currentPath.ToArray();
                var startIndex = Array.LastIndexOf(pathArray, system);

                // Add systems from the cycle
                for (var i = startIndex; i >= 0; i--)
                {
                    cycleList.Add(pathArray[i]);
                }

                // Complete the cycle
                cycleList.Add(system);

                // Format cycle for display
                var cycleNames = cycleList.Select(s => s.GetType().Name).ToList();

                throw new InvalidOperationException(
                    $"<color={ColorError}>Cyclic dependency detected!</color>\n" +
                    $"Path: <color={ColorSystem}>{string.Join($"</color> → <color={ColorSystem}>", cycleNames)}</color>");
            }

            if (visited.Contains(system))
                return;

            tempMarked.Add(system);
            currentPath.Push(system);

            if (graph.TryGetValue(system, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    VisitSystemForSorting(dep, graph, visited, tempMarked, result, currentPath);
                }
            }

            currentPath.Pop();
            tempMarked.Remove(system);
            visited.Add(system);
            result.Add(system);
        }

        private static Dictionary<SystemBase, List<SystemBase>> BuildDependencyGraph(List<SystemBase> systems)
        {
            var graph = new Dictionary<SystemBase, List<SystemBase>>();

            // First pass: Initialize all systems in the graph
            foreach (var system in systems)
            {
                graph[system] = new List<SystemBase>();
            }

            // Second pass: Process all dependencies
            foreach (var system in systems)
            {
                var systemType = system.GetType();

                // Process UpdateAfter attributes
                foreach (var attr in systemType.GetCustomAttributes<UpdateAfterAttribute>())
                {
                    foreach (var depType in attr.SystemTypes)
                    {
                        var dependency = systems.FirstOrDefault(s => depType.IsInstanceOfType(s));
                        if (dependency != null)
                        {
                            graph[system].Add(dependency);
                            Log($"Dependency: <color={ColorSystem}>{systemType.Name}</color> runs after <color={ColorSystem}>{dependency.GetType().Name}</color>", LogLevel.Diagnostic);
                        }
                    }
                }

                // Process UpdateBefore attributes
                foreach (var attr in systemType.GetCustomAttributes<UpdateBeforeAttribute>())
                {
                    foreach (var targetType in attr.SystemTypes)
                    {
                        var target = systems.FirstOrDefault(s => targetType.IsInstanceOfType(s));
                        if (target != null)
                        {
                            graph[target].Add(system);
                            Log($"Dependency: <color={ColorSystem}>{target.GetType().Name}</color> runs after <color={ColorSystem}>{systemType.Name}</color>", LogLevel.Diagnostic);
                        }
                    }
                }
            }

            return graph;
        }

        private class BootstrapCache
        {
            public readonly Dictionary<Type, List<Type>> groupChildren = new();
            public readonly Dictionary<Type, List<int>> systemWorldIndices = new();
            public List<Type> allSystemTypes = new(128);
        }

        #region Logging Utilities

        private static void Log(string message, LogLevel level)
        {
            if (m_currentLogLevel >= level)
            {
                Debug.Log($"[Bootstrap] {message}");
            }
        }

        private static void LogWarning(string message)
        {
            if (m_currentLogLevel >= LogLevel.Minimal)
            {
                Debug.LogWarning($"[Bootstrap] <color={ColorWarning}>{message}</color>");
            }
        }

        #endregion
    }
}