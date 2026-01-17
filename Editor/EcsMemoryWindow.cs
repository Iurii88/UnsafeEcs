using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Editor
{
    public class EcsMemoryWindow : EditorWindow
    {
        private const float RefreshInterval = 0.5f;
        private const float LeftPanelWidth = 340f;

        private Vector2 m_typeListScrollPos;
        private Vector2 m_detailsScrollPos;
        private bool m_autoRefresh = true;
        private double m_lastRefreshTime;

        private SearchField m_searchField;
        private string m_searchString = "";

        private readonly List<TypeInfo> m_typeCache = new();
        private readonly List<TypeInfo> m_filteredTypes = new();

        private int m_selectedTypeIndex = -1;
        private int m_selectedWorldIndex = -1; // -1 = All Worlds

        // Global stats
        private long m_totalMemoryBytes;
        private int m_totalEntities;
        private int m_totalComponents;

        private enum SortMode
        {
            ByIndex,
            ByName,
            BySize,
            ByMemory,
            ByCount
        }

        private SortMode m_sortMode = SortMode.ByIndex;
        private bool m_sortAscending = true;

        private struct TypeInfo
        {
            public int Index;
            public long Hash;
            public Type Type;
            public string Name;
            public string FullName;
            public string Namespace;
            public string Assembly;
            public int ComponentSize;
            public bool IsBuffer;
            public bool IsComponent;
            public bool IsManagedRef;

            // Per-world chunk info (aggregated when "All Worlds" selected)
            public int ComponentCount;
            public int ChunkCapacity;
            public long ChunkMemoryBytes;
            public long TotalMemoryBytes; // Including entity mappings
            public bool HasChunk;
        }

        // Colors (matching project style)
        private static readonly Color SelectedColor = new(0.17f, 0.36f, 0.53f);
        private static readonly Color HoverColor = new(0.25f, 0.25f, 0.25f);
        private static readonly Color DarkBgColor = new(0.18f, 0.18f, 0.18f);
        private static readonly Color LightBgColor = new(0.2f, 0.2f, 0.2f);
        private static readonly Color SeparatorColor = new(0.1f, 0.1f, 0.1f);
        private static readonly Color AccentColor = new(0.35f, 0.65f, 1f);
        private static readonly Color DisabledColor = new(0.5f, 0.5f, 0.5f);
        private static readonly Color ComponentColor = new(0.4f, 0.8f, 0.4f);
        private static readonly Color BufferColor = new(0.9f, 0.7f, 0.3f);
        private static readonly Color ManagedRefColor = new(0.6f, 0.6f, 0.9f);
        private static readonly Color UnknownColor = new(0.6f, 0.4f, 0.4f);
        private static readonly Color MemoryHighColor = new(0.9f, 0.4f, 0.4f);
        private static readonly Color MemoryMediumColor = new(0.9f, 0.7f, 0.3f);
        private static readonly Color MemoryLowColor = new(0.4f, 0.7f, 0.4f);

        [MenuItem("Window/UnsafeEcs/ECS Memory %#m")]
        public static void ShowWindow()
        {
            var window = GetWindow<EcsMemoryWindow>();
            window.titleContent = new GUIContent("ECS Memory");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            m_searchField = new SearchField();
            RefreshTypeCache();
        }

        private void OnDisable()
        {
            m_typeCache.Clear();
            m_filteredTypes.Clear();
        }

        private void Update()
        {
            if (m_autoRefresh && EditorApplication.timeSinceStartup - m_lastRefreshTime > RefreshInterval)
            {
                m_lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshTypeCache();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                DrawCenteredMessage("Enter Play Mode to view memory usage", "d_PlayButton@2x");
                return;
            }

            if (m_typeCache.Count == 0)
            {
                DrawCenteredMessage("No types registered in TypeManager", "d_console.infoicon");
                return;
            }

            // Global stats bar
            DrawGlobalStats();

            EditorGUILayout.BeginHorizontal();

            // Left panel - type list
            DrawTypeList();

            // Vertical separator
            DrawVerticalSeparator();

            // Right panel - type details
            DrawTypeDetails();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCenteredMessage(string message, string iconName)
        {
            var rect = new Rect(0, EditorStyles.toolbar.fixedHeight, position.width,
                position.height - EditorStyles.toolbar.fixedHeight);
            EditorGUI.DrawRect(rect, DarkBgColor);

            var centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            var iconContent = EditorGUIUtility.IconContent(iconName);
            var totalHeight = 64 + 8 + 20;
            var startY = rect.y + (rect.height - totalHeight) / 2;

            var iconRect = new Rect((position.width - 64) / 2, startY, 64, 64);
            GUI.Label(iconRect, iconContent);

            var labelRect = new Rect(0, startY + 72, position.width, 20);
            GUI.Label(labelRect, message, centeredStyle);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var refreshContent = new GUIContent(" Auto", EditorGUIUtility.IconContent("d_Refresh").image);
            m_autoRefresh = GUILayout.Toggle(m_autoRefresh, refreshContent, EditorStyles.toolbarButton,
                GUILayout.Width(60));

            if (!m_autoRefresh)
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    RefreshTypeCache();
            }

            GUILayout.Space(8);

            // World filter - only show when playing and worlds exist
            if (Application.isPlaying && WorldManager.Worlds.Count > 0)
            {
                GUILayout.Label("World:", EditorStyles.miniLabel);

                var worldNames = new string[WorldManager.Worlds.Count + 1];
                worldNames[0] = "All Worlds";
                for (var i = 0; i < WorldManager.Worlds.Count; i++)
                {
                    worldNames[i + 1] = WorldManager.Worlds[i].GetDisplayName(i);
                }

                var currentIndex = m_selectedWorldIndex + 1; // -1 becomes 0 (All Worlds)
                if (currentIndex < 0 || currentIndex >= worldNames.Length)
                    currentIndex = 0;

                var newWorldIndex = EditorGUILayout.Popup(currentIndex, worldNames, EditorStyles.toolbarPopup,
                    GUILayout.Width(140));

                if (newWorldIndex != currentIndex)
                {
                    m_selectedWorldIndex = newWorldIndex - 1; // 0 becomes -1 (All Worlds)
                    RefreshTypeCache();
                }

                GUILayout.Space(8);
            }

            var totalCount = m_typeCache.Count;
            var filteredCount = m_filteredTypes.Count;
            var countText = string.IsNullOrEmpty(m_searchString)
                ? $"{totalCount} types"
                : $"{filteredCount} / {totalCount} types";
            GUILayout.Label(countText, EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            // Sort dropdown
            GUILayout.Label("Sort:", EditorStyles.miniLabel);
            var sortNames = new[] { "Index", "Name", "Size", "Memory", "Count" };
            var newSortMode = (SortMode)EditorGUILayout.Popup((int)m_sortMode, sortNames, EditorStyles.toolbarPopup,
                GUILayout.Width(70));
            if (newSortMode != m_sortMode)
            {
                m_sortMode = newSortMode;
                SortTypes();
            }

            if (GUILayout.Button(m_sortAscending ? "▲" : "▼", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                m_sortAscending = !m_sortAscending;
                SortTypes();
            }

            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            m_searchString = m_searchField.OnToolbarGUI(m_searchString, GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck())
                FilterTypes();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGlobalStats()
        {
            var statsRect = EditorGUILayout.GetControlRect(false, 32);
            EditorGUI.DrawRect(statsRect, new Color(0.15f, 0.15f, 0.15f));

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = DisabledColor }
            };
            var valueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            var unitStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            var x = statsRect.x + 16;
            var y = statsRect.y + 4;

            // Total Memory
            EditorGUI.LabelField(new Rect(x, y, 60, 12), "MEMORY", labelStyle);
            var memoryStr = FormatBytes(m_totalMemoryBytes);
            EditorGUI.LabelField(new Rect(x, y + 12, 100, 16), memoryStr, valueStyle);
            x += 110;

            // Separator
            EditorGUI.DrawRect(new Rect(x, y + 2, 1, 24), SeparatorColor);
            x += 16;

            // Entities
            EditorGUI.LabelField(new Rect(x, y, 60, 12), "ENTITIES", labelStyle);
            EditorGUI.LabelField(new Rect(x, y + 12, 80, 16), m_totalEntities.ToString("N0"), valueStyle);
            x += 90;

            // Separator
            EditorGUI.DrawRect(new Rect(x, y + 2, 1, 24), SeparatorColor);
            x += 16;

            // Components
            EditorGUI.LabelField(new Rect(x, y, 80, 12), "COMPONENTS", labelStyle);
            EditorGUI.LabelField(new Rect(x, y + 12, 80, 16), m_totalComponents.ToString("N0"), valueStyle);
            x += 100;

            // Separator
            EditorGUI.DrawRect(new Rect(x, y + 2, 1, 24), SeparatorColor);
            x += 16;

            // Types
            EditorGUI.LabelField(new Rect(x, y, 60, 12), "TYPES", labelStyle);
            EditorGUI.LabelField(new Rect(x, y + 12, 60, 16), m_typeCache.Count.ToString(), valueStyle);

            // World indicator (right side)
            var worldText = m_selectedWorldIndex < 0 ? "All Worlds" :
                (Application.isPlaying && m_selectedWorldIndex < WorldManager.Worlds.Count)
                    ? WorldManager.Worlds[m_selectedWorldIndex].GetDisplayName(m_selectedWorldIndex)
                    : "Unknown";
            var worldStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = AccentColor }
            };
            EditorGUI.LabelField(new Rect(statsRect.xMax - 160, y + 8, 150, 16), worldText, worldStyle);
        }

        private void DrawTypeList()
        {
            var leftPanelRect = EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));
            EditorGUI.DrawRect(leftPanelRect, DarkBgColor);

            // Header
            DrawListHeader();

            m_typeListScrollPos = EditorGUILayout.BeginScrollView(m_typeListScrollPos, GUILayout.ExpandHeight(true));

            var displayList = string.IsNullOrEmpty(m_searchString) ? m_typeCache : m_filteredTypes;

            for (var i = 0; i < displayList.Count; i++)
            {
                DrawTypeItem(displayList[i], i % 2 == 0);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawListHeader()
        {
            var headerRect = EditorGUILayout.GetControlRect(false, 22, GUILayout.Width(LeftPanelWidth));
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f));

            var headerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = DisabledColor }
            };

            var x = headerRect.x + 8;
            var y = headerRect.y;
            var h = headerRect.height;

            EditorGUI.LabelField(new Rect(x, y, 30, h), "IDX", headerStyle);
            x += 30;
            EditorGUI.LabelField(new Rect(x, y, 40, h), "TYPE", headerStyle);
            x += 40;
            EditorGUI.LabelField(new Rect(x, y, 110, h), "NAME", headerStyle);
            EditorGUI.LabelField(new Rect(headerRect.xMax - 115, y, 45, h), "COUNT", headerStyle);
            EditorGUI.LabelField(new Rect(headerRect.xMax - 65, y, 55, h), "MEMORY", headerStyle);

            DrawHorizontalSeparator();
        }

        private void DrawTypeItem(TypeInfo typeInfo, bool isEven)
        {
            var isSelected = m_selectedTypeIndex == typeInfo.Index;
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

            // Background
            if (isSelected)
                EditorGUI.DrawRect(rect, SelectedColor);
            else if (rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, HoverColor);
            else if (!isEven)
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.Space(8);

            // Index
            var indexStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = isSelected ? Color.white : new Color(0.6f, 0.6f, 0.6f) }
            };
            GUILayout.Label(typeInfo.Index.ToString(), indexStyle, GUILayout.Width(25));

            GUILayout.Space(5);

            // Category tag
            Color tagColor;
            string tagText;
            if (typeInfo.Type == null)
            {
                tagColor = UnknownColor;
                tagText = "???";
            }
            else if (typeInfo.IsManagedRef)
            {
                tagColor = ManagedRefColor;
                tagText = "REF";
            }
            else if (typeInfo.IsBuffer)
            {
                tagColor = BufferColor;
                tagText = "BUF";
            }
            else
            {
                tagColor = ComponentColor;
                tagText = "CMP";
            }

            var tagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = isSelected ? Color.white : tagColor }
            };
            GUILayout.Label(tagText, tagStyle, GUILayout.Width(30));

            GUILayout.Space(5);

            // Name
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = isSelected ? Color.white : new Color(0.85f, 0.85f, 0.85f) }
            };
            GUILayout.Label(typeInfo.Name, nameStyle, GUILayout.Width(110));

            GUILayout.FlexibleSpace();

            // Count
            var countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = isSelected ? Color.white : (typeInfo.HasChunk ? new Color(0.7f, 0.7f, 0.7f) : DisabledColor) }
            };
            var countText = typeInfo.HasChunk ? typeInfo.ComponentCount.ToString("N0") : "-";
            GUILayout.Label(countText, countStyle, GUILayout.Width(45));

            // Memory
            var memoryColor = GetMemoryColor(typeInfo.TotalMemoryBytes);
            if (isSelected) memoryColor = Color.white;
            if (!typeInfo.HasChunk) memoryColor = isSelected ? Color.white : DisabledColor;

            var memoryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = memoryColor }
            };
            var memoryText = typeInfo.HasChunk ? FormatBytesShort(typeInfo.TotalMemoryBytes) : "-";
            GUILayout.Label(memoryText, memoryStyle, GUILayout.Width(55));

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            // Handle click
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                m_selectedTypeIndex = typeInfo.Index;

                // Double-click opens script
                if (Event.current.clickCount == 2 && typeInfo.Type != null)
                {
                    EditorScriptUtility.OpenScriptForType(typeInfo.Type);
                }

                Event.current.Use();
            }
        }

        private Color GetMemoryColor(long bytes)
        {
            if (bytes > 1024 * 1024) return MemoryHighColor; // > 1 MB
            if (bytes > 100 * 1024) return MemoryMediumColor; // > 100 KB
            return MemoryLowColor;
        }

        private void DrawTypeDetails()
        {
            var rightPanelRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rightPanelRect, LightBgColor);

            TypeInfo? selectedType = null;
            foreach (var typeInfo in m_typeCache)
            {
                if (typeInfo.Index == m_selectedTypeIndex)
                {
                    selectedType = typeInfo;
                    break;
                }
            }

            if (!selectedType.HasValue || m_selectedTypeIndex < 0)
            {
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 };
                GUILayout.Label("Select a type to view details", style);
                GUILayout.FlexibleSpace();
            }
            else
            {
                m_detailsScrollPos = EditorGUILayout.BeginScrollView(m_detailsScrollPos);
                DrawTypeInfo(selectedType.Value);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTypeInfo(TypeInfo typeInfo)
        {
            GUILayout.Space(8);

            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            GUILayout.Label(typeInfo.Name, headerStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            DrawHorizontalSeparator();
            GUILayout.Space(8);

            // Memory section
            if (typeInfo.HasChunk)
            {
                DrawSection("MEMORY USAGE", () =>
                {
                    DrawPropertyWithColor("Total Memory", FormatBytes(typeInfo.TotalMemoryBytes),
                        GetMemoryColor(typeInfo.TotalMemoryBytes));
                    DrawProperty("Chunk Data", FormatBytes(typeInfo.ChunkMemoryBytes));
                    DrawProperty("Component Size", $"{typeInfo.ComponentSize} bytes");
                    DrawProperty("Components", typeInfo.ComponentCount.ToString("N0"));
                    DrawProperty("Capacity", typeInfo.ChunkCapacity.ToString("N0"));

                    // Utilization
                    var utilization = typeInfo.ChunkCapacity > 0
                        ? (float)typeInfo.ComponentCount / typeInfo.ChunkCapacity * 100f
                        : 0f;
                    var utilizationColor = utilization > 80 ? MemoryLowColor :
                        utilization > 50 ? MemoryMediumColor : MemoryHighColor;
                    DrawPropertyWithColor("Utilization", $"{utilization:F1}%", utilizationColor);
                });
            }
            else
            {
                DrawSection("MEMORY USAGE", () =>
                {
                    var noChunkStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Italic,
                        normal = { textColor = DisabledColor }
                    };
                    GUILayout.Label("No chunk allocated (type not used in selected world)", noChunkStyle);
                });
            }

            // Type info section
            DrawSection("TYPE INFO", () =>
            {
                DrawProperty("Index", typeInfo.Index.ToString());
                DrawProperty("Hash", $"0x{typeInfo.Hash:X16}");
                DrawProperty("Size", $"{typeInfo.ComponentSize} bytes");

                // Category
                string category;
                Color categoryColor;
                if (typeInfo.Type == null)
                {
                    category = "Unknown";
                    categoryColor = UnknownColor;
                }
                else if (typeInfo.IsManagedRef)
                {
                    category = "Managed Reference";
                    categoryColor = ManagedRefColor;
                }
                else if (typeInfo.IsBuffer)
                {
                    category = "Buffer Element";
                    categoryColor = BufferColor;
                }
                else
                {
                    category = "Component";
                    categoryColor = ComponentColor;
                }

                DrawPropertyWithColor("Category", category, categoryColor);
            });

            if (typeInfo.Type != null)
            {
                DrawSection("TYPE DETAILS", () =>
                {
                    DrawProperty("Full Name", typeInfo.FullName);
                    DrawProperty("Namespace", typeInfo.Namespace ?? "(global)");
                    DrawProperty("Assembly", typeInfo.Assembly);
                });

                // Fields
                var fields = typeInfo.Type.GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (fields.Length > 0)
                {
                    DrawSection($"FIELDS ({fields.Length})", () =>
                    {
                        foreach (var field in fields)
                        {
                            var fieldTypeName = GetFriendlyTypeName(field.FieldType);
                            var access = field.IsPublic ? "public" : "private";

                            EditorGUILayout.BeginHorizontal();

                            var accessStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                fontSize = 9,
                                normal = { textColor = field.IsPublic ? ComponentColor : DisabledColor }
                            };
                            GUILayout.Label(access, accessStyle, GUILayout.Width(45));

                            var typeStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                normal = { textColor = AccentColor }
                            };
                            GUILayout.Label(fieldTypeName, typeStyle, GUILayout.Width(100));

                            var nameStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                            };
                            GUILayout.Label(field.Name, nameStyle);

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }
                    });
                }
            }
            else
            {
                DrawSection("WARNING", () =>
                {
                    var warningStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        wordWrap = true,
                        normal = { textColor = UnknownColor }
                    };
                    GUILayout.Label(
                        "Type information not available. The type may not have been registered yet or assembly scanning failed.",
                        warningStyle);
                });
            }

            GUILayout.Space(8);
        }

        private void DrawSection(string title, Action drawContent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal = { textColor = AccentColor }
            };
            GUILayout.Label(title, headerStyle);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            drawContent();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        private void DrawProperty(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = DisabledColor }
            };
            GUILayout.Label(label + ":", labelStyle, GUILayout.Width(100));

            var valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                wordWrap = true
            };
            GUILayout.Label(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPropertyWithColor(string label, string value, Color valueColor)
        {
            EditorGUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = DisabledColor }
            };
            GUILayout.Label(label + ":", labelStyle, GUILayout.Width(100));

            var valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = valueColor }
            };
            GUILayout.Label(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHorizontalSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, SeparatorColor);
        }

        private void DrawVerticalSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, SeparatorColor);
        }

        private unsafe void RefreshTypeCache()
        {
            m_typeCache.Clear();
            m_totalMemoryBytes = 0;
            m_totalEntities = 0;
            m_totalComponents = 0;

            if (!TypeManager.TypeOrder.Data.IsCreated)
                return;

            var typeCount = TypeManager.TypeCount.Data;

            // Get worlds to analyze
            var worldsToAnalyze = new List<World>();
            if (Application.isPlaying)
            {
                if (m_selectedWorldIndex < 0)
                {
                    // All worlds
                    worldsToAnalyze.AddRange(WorldManager.Worlds);
                }
                else if (m_selectedWorldIndex < WorldManager.Worlds.Count)
                {
                    worldsToAnalyze.Add(WorldManager.Worlds[m_selectedWorldIndex]);
                }

                // Count entities
                foreach (var world in worldsToAnalyze)
                {
                    ref var em = ref world.EntityManager;
                    if (!em.entities.IsCreated) continue;

                    for (var i = 0; i < em.entities.m_length; i++)
                    {
                        if (!em.deadEntities.Ptr[i])
                            m_totalEntities++;
                    }
                }
            }

            for (var i = 0; i < typeCount; i++)
            {
                var hash = TypeManager.TypeOrder.Data[i];
                var type = TypeManager.GetTypeFromHash(hash);
                var size = TypeManager.GetTypeSizeByIndex(i);
                var isBuffer = TypeManager.IsBufferType(i);

                var isManagedRef = false;
                if (type != null && type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();
                    isManagedRef = genericDef.Name.StartsWith("ManagedRef");
                }

                // Aggregate chunk info from selected worlds
                var componentCount = 0;
                var chunkCapacity = 0;
                long chunkMemoryBytes = 0;
                long totalMemoryBytes = 0;
                var hasChunk = false;

                foreach (var world in worldsToAnalyze)
                {
                    ref var em = ref world.EntityManager;
                    if (!em.chunks.IsCreated || i >= em.chunks.Length)
                        continue;

                    var chunkUnion = em.chunks.Ptr[i];
                    if (!chunkUnion.IsValid)
                        continue;

                    hasChunk = true;

                    if (isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();
                        if (bufferChunk != null)
                        {
                            componentCount += bufferChunk->length;
                            chunkCapacity += bufferChunk->capacity;

                            // Buffer chunk memory: headers + entity mappings + buffer data
                            var headerMemory = bufferChunk->capacity * bufferChunk->headerSize;
                            var entityIdMemory = bufferChunk->capacity * sizeof(int);
                            var bufferIndicesMemory = (bufferChunk->maxEntityId + 1) * sizeof(int);

                            // Estimate buffer data memory (sum of all buffer capacities)
                            long bufferDataMemory = 0;
                            for (var j = 0; j < bufferChunk->length; j++)
                            {
                                var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);
                                bufferDataMemory += header->capacity * bufferChunk->elementSize;
                            }

                            chunkMemoryBytes += headerMemory + bufferDataMemory;
                            totalMemoryBytes += headerMemory + entityIdMemory + bufferIndicesMemory + bufferDataMemory;
                        }
                    }
                    else
                    {
                        var componentChunk = chunkUnion.AsComponentChunk();
                        if (componentChunk != null)
                        {
                            componentCount += componentChunk->length;
                            chunkCapacity += componentChunk->capacity;

                            // Component chunk memory: data + entity IDs + component indices
                            var dataMemory = componentChunk->capacity * componentChunk->componentSize;
                            var entityIdMemory = componentChunk->capacity * sizeof(int);
                            var componentIndicesMemory = (componentChunk->maxEntityId + 1) * sizeof(int);

                            chunkMemoryBytes += dataMemory;
                            totalMemoryBytes += dataMemory + entityIdMemory + componentIndicesMemory;
                        }
                    }
                }

                m_totalMemoryBytes += totalMemoryBytes;
                m_totalComponents += componentCount;

                m_typeCache.Add(new TypeInfo
                {
                    Index = i,
                    Hash = hash,
                    Type = type,
                    Name = GetTypeName(type, i),
                    FullName = type?.FullName ?? $"Unknown({i})",
                    Namespace = type?.Namespace,
                    Assembly = type?.Assembly.GetName().Name ?? "Unknown",
                    ComponentSize = size,
                    IsBuffer = isBuffer,
                    IsComponent = !isBuffer && !isManagedRef,
                    IsManagedRef = isManagedRef,
                    ComponentCount = componentCount,
                    ChunkCapacity = chunkCapacity,
                    ChunkMemoryBytes = chunkMemoryBytes,
                    TotalMemoryBytes = totalMemoryBytes,
                    HasChunk = hasChunk
                });
            }

            SortTypes();
            FilterTypes();
        }

        private string GetTypeName(Type type, int index)
        {
            if (type == null)
                return $"Unknown({index})";

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();
                var baseName = genericDef.Name;
                var tickIndex = baseName.IndexOf('`');
                if (tickIndex > 0)
                    baseName = baseName.Substring(0, tickIndex);

                return $"{baseName}<{string.Join(", ", Array.ConvertAll(genericArgs, t => t.Name))}>";
            }

            return type.Name;
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();
                var baseName = genericDef.Name;
                var tickIndex = baseName.IndexOf('`');
                if (tickIndex > 0)
                    baseName = baseName.Substring(0, tickIndex);

                return $"{baseName}<{string.Join(", ", Array.ConvertAll(genericArgs, GetFriendlyTypeName))}>";
            }

            return type.Name;
        }

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        private string FormatBytesShort(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1}M";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1}K";
            return $"{bytes}B";
        }

        private void SortTypes()
        {
            switch (m_sortMode)
            {
                case SortMode.ByIndex:
                    m_typeCache.Sort((a, b) => m_sortAscending
                        ? a.Index.CompareTo(b.Index)
                        : b.Index.CompareTo(a.Index));
                    break;
                case SortMode.ByName:
                    m_typeCache.Sort((a, b) => m_sortAscending
                        ? string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
                        : string.Compare(b.Name, a.Name, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.BySize:
                    m_typeCache.Sort((a, b) => m_sortAscending
                        ? a.ComponentSize.CompareTo(b.ComponentSize)
                        : b.ComponentSize.CompareTo(a.ComponentSize));
                    break;
                case SortMode.ByMemory:
                    m_typeCache.Sort((a, b) => m_sortAscending
                        ? a.TotalMemoryBytes.CompareTo(b.TotalMemoryBytes)
                        : b.TotalMemoryBytes.CompareTo(a.TotalMemoryBytes));
                    break;
                case SortMode.ByCount:
                    m_typeCache.Sort((a, b) => m_sortAscending
                        ? a.ComponentCount.CompareTo(b.ComponentCount)
                        : b.ComponentCount.CompareTo(a.ComponentCount));
                    break;
            }

            FilterTypes();
        }

        private void FilterTypes()
        {
            m_filteredTypes.Clear();

            if (string.IsNullOrEmpty(m_searchString))
                return;

            foreach (var typeInfo in m_typeCache)
            {
                if (typeInfo.Name.IndexOf(m_searchString, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (typeInfo.FullName != null &&
                     typeInfo.FullName.IndexOf(m_searchString, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    m_filteredTypes.Add(typeInfo);
                }
            }
        }
    }
}
