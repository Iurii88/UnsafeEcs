using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnsafeEcs.Core.Systems;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Editor
{
    public class SystemsWindow : EditorWindow
    {
        private const float WorldPanelHeight = 110f;
        private const float LeftPanelWidth = 320f;
        private const float RefreshInterval = 0.25f;
        private const float TreeIndent = 16f;
        private const float ItemHeight = 22f;

        private Vector2 m_worldListScrollPos;
        private Vector2 m_systemListScrollPos;
        private Vector2 m_detailsScrollPos;

        private World m_selectedWorld;
        private SystemBase m_selectedSystem;
        private bool m_autoRefresh = true;
        private double m_lastRefreshTime;

        private SearchField m_searchField;
        private string m_searchString = "";

        private readonly Dictionary<SystemBase, bool> m_foldoutStates = new();
        private int m_totalSystemCount;

        // Colors
        private static readonly Color SelectedColor = new(0.17f, 0.36f, 0.53f);
        private static readonly Color HoverColor = new(0.25f, 0.25f, 0.25f);
        private static readonly Color DarkBgColor = new(0.18f, 0.18f, 0.18f);
        private static readonly Color LightBgColor = new(0.2f, 0.2f, 0.2f);
        private static readonly Color SeparatorColor = new(0.1f, 0.1f, 0.1f);
        private static readonly Color GroupColor = new(0.4f, 0.75f, 0.95f);
        private static readonly Color SystemColor = new(0.85f, 0.85f, 0.85f);
        private static readonly Color EnabledColor = new(0.4f, 0.8f, 0.4f);
        private static readonly Color DisabledColor = new(0.5f, 0.5f, 0.5f);
        private static readonly Color AccentColor = new(0.35f, 0.65f, 1f);

        [MenuItem("Window/UnsafeEcs/Systems %#s")]
        public static void ShowWindow()
        {
            var window = GetWindow<SystemsWindow>();
            window.titleContent = new GUIContent("ECS Systems");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            m_searchField = new SearchField();
        }

        private void OnDisable()
        {
            m_selectedWorld = null;
            m_selectedSystem = null;
            m_foldoutStates.Clear();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (m_autoRefresh && EditorApplication.timeSinceStartup - m_lastRefreshTime > RefreshInterval)
            {
                m_lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                DrawCenteredMessage("Enter Play Mode to inspect systems", "d_PlayButton@2x");
                return;
            }

            if (WorldManager.Worlds.Count == 0)
            {
                DrawCenteredMessage("No ECS worlds exist", "d_console.warnicon");
                return;
            }

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            // Left panel
            DrawLeftPanel();

            // Vertical separator
            DrawVerticalSeparator();

            // Right panel
            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCenteredMessage(string message, string iconName)
        {
            var rect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            var centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };

            var iconContent = EditorGUIUtility.IconContent(iconName);
            var totalHeight = 64 + 8 + 20;
            var startY = (position.height - totalHeight) / 2;

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

            GUILayout.Space(8);

            UpdateSystemCount();
            GUILayout.Label($"{m_totalSystemCount} systems", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(70)))
                SetAllFoldouts(true);

            if (GUILayout.Button("Collapse", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SetAllFoldouts(false);

            GUILayout.Space(8);

            m_searchString = m_searchField.OnToolbarGUI(m_searchString, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            var leftPanelRect = EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));
            EditorGUI.DrawRect(leftPanelRect, DarkBgColor);

            // Worlds section
            DrawSectionHeader("WORLDS");

            m_worldListScrollPos = EditorGUILayout.BeginScrollView(m_worldListScrollPos,
                GUILayout.Height(WorldPanelHeight));

            for (var i = 0; i < WorldManager.Worlds.Count; i++)
            {
                var world = WorldManager.Worlds[i];
                var isSelected = m_selectedWorld == world;
                var systemCount = CountSystems(world);

                DrawWorldItem($"World {i}", $"{systemCount} systems", isSelected, () =>
                {
                    m_selectedWorld = world;
                    m_selectedSystem = null;
                });
            }

            EditorGUILayout.EndScrollView();

            if (m_selectedWorld == null && WorldManager.Worlds.Count > 0)
                m_selectedWorld = WorldManager.Worlds[0];

            DrawHorizontalSeparator();

            // Systems section
            DrawSectionHeader("SYSTEMS");

            m_systemListScrollPos = EditorGUILayout.BeginScrollView(m_systemListScrollPos, GUILayout.ExpandHeight(true));

            if (m_selectedWorld != null && m_selectedWorld.rootSystems.Count > 0)
            {
                foreach (var rootSystem in m_selectedWorld.rootSystems)
                {
                    DrawSystemNode(rootSystem, 0);
                }
            }
            else
            {
                GUILayout.Space(20);
                var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 };
                GUILayout.Label("No systems", emptyStyle);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSystemNode(SystemBase system, int depth)
        {
            var typeName = system.GetType().Name;
            var isGroup = system is SystemGroup;
            var isSelected = m_selectedSystem == system;

            // Filter by search
            if (!string.IsNullOrEmpty(m_searchString))
            {
                var hasMatchingChild = isGroup && HasMatchingChild((SystemGroup)system);
                var selfMatches = typeName.IndexOf(m_searchString, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!selfMatches && !hasMatchingChild)
                    return;
            }

            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(ItemHeight));

            // Background
            if (isSelected)
                EditorGUI.DrawRect(rect, SelectedColor);
            else if (rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, HoverColor);

            var indent = depth * TreeIndent;
            GUILayout.Space(indent + 4);

            // Foldout for groups
            if (isGroup)
            {
                if (!m_foldoutStates.ContainsKey(system))
                    m_foldoutStates[system] = true;

                var isExpanded = m_foldoutStates[system];
                var foldoutRect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14));

                // Draw triangle
                if (Event.current.type == EventType.Repaint)
                {
                    var arrowColor = isSelected ? Color.white : new Color(0.7f, 0.7f, 0.7f);
                    DrawTriangle(foldoutRect.center, isExpanded, arrowColor);
                }

                if (Event.current.type == EventType.MouseDown && foldoutRect.Contains(Event.current.mousePosition))
                {
                    m_foldoutStates[system] = !isExpanded;
                    Event.current.Use();
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }
            else
            {
                GUILayout.Space(14);
            }

            // Icon
            var iconContent = isGroup
                ? EditorGUIUtility.IconContent("d_Folder Icon")
                : EditorGUIUtility.IconContent("d_cs Script Icon");
            GUILayout.Label(iconContent, GUILayout.Width(16), GUILayout.Height(16));

            // System name
            var labelColor = isGroup ? GroupColor : SystemColor;
            if (isSelected) labelColor = Color.white;

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isGroup ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = labelColor }
            };
            GUILayout.Label(typeName, labelStyle);

            // Child count for groups
            if (isGroup)
            {
                var group = (SystemGroup)system;
                var countStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                GUILayout.Label($"({group.systems.Count})", countStyle);
            }

            GUILayout.FlexibleSpace();

            // Update mask badges
            DrawUpdateMaskBadges(system.UpdateMask);

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            // Handle click
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                m_selectedSystem = system;
                Event.current.Use();
            }

            // Draw children if expanded
            if (isGroup && m_foldoutStates.TryGetValue(system, out var expanded) && expanded)
            {
                var group = (SystemGroup)system;
                foreach (var child in group.systems)
                {
                    DrawSystemNode(child, depth + 1);
                }
            }
        }

        private void DrawTriangle(Vector2 center, bool expanded, Color color)
        {
            var size = 4f;
            Vector3[] points;

            if (expanded)
            {
                points = new[]
                {
                    new Vector3(center.x - size, center.y - size * 0.5f, 0),
                    new Vector3(center.x + size, center.y - size * 0.5f, 0),
                    new Vector3(center.x, center.y + size * 0.5f, 0)
                };
            }
            else
            {
                points = new[]
                {
                    new Vector3(center.x - size * 0.5f, center.y - size, 0),
                    new Vector3(center.x + size * 0.5f, center.y, 0),
                    new Vector3(center.x - size * 0.5f, center.y + size, 0)
                };
            }

            Handles.color = color;
            Handles.DrawAAConvexPolygon(points);
        }

        private void DrawUpdateMaskBadges(SystemUpdateMask mask)
        {
            var hasUpdate = (mask & SystemUpdateMask.Update) != 0;
            var hasLateUpdate = (mask & SystemUpdateMask.LateUpdate) != 0;
            var hasFixedUpdate = (mask & SystemUpdateMask.FixedUpdate) != 0;

            DrawBadge("U", hasUpdate, "Update");
            DrawBadge("L", hasLateUpdate, "LateUpdate");
            DrawBadge("F", hasFixedUpdate, "FixedUpdate");
        }

        private void DrawBadge(string text, bool enabled, string tooltip)
        {
            var color = enabled ? EnabledColor : new Color(0.35f, 0.35f, 0.35f);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                fontStyle = enabled ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = color }
            };

            GUILayout.Label(new GUIContent(text, tooltip), style, GUILayout.Width(14));
        }

        private bool HasMatchingChild(SystemGroup group)
        {
            foreach (var child in group.systems)
            {
                var childTypeName = child.GetType().Name;
                if (childTypeName.IndexOf(m_searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (child is SystemGroup childGroup && HasMatchingChild(childGroup))
                    return true;
            }

            return false;
        }

        private void DrawRightPanel()
        {
            var rightPanelRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rightPanelRect, LightBgColor);

            if (m_selectedSystem == null)
            {
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 };
                GUILayout.Label("Select a system to view details", style);
                GUILayout.FlexibleSpace();
            }
            else
            {
                DrawSystemDetails();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSystemDetails()
        {
            var systemType = m_selectedSystem.GetType();
            var isGroup = m_selectedSystem is SystemGroup;

            // Header
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);

            var iconContent = isGroup
                ? EditorGUIUtility.IconContent("d_Folder Icon")
                : EditorGUIUtility.IconContent("d_cs Script Icon");
            GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            GUILayout.Label(systemType.Name, headerStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            DrawHorizontalSeparator();

            m_detailsScrollPos = EditorGUILayout.BeginScrollView(m_detailsScrollPos);
            GUILayout.Space(8);

            // Type information section
            DrawSection("TYPE INFORMATION", () =>
            {
                DrawProperty("Full Name", systemType.FullName);
                DrawProperty("Assembly", systemType.Assembly.GetName().Name);
                DrawProperty("Category", isGroup ? "System Group" : "System");
            });

            // Update mask section
            DrawSection("UPDATE MASK", () =>
            {
                var mask = m_selectedSystem.UpdateMask;
                DrawUpdateMaskProperty("Update", (mask & SystemUpdateMask.Update) != 0);
                DrawUpdateMaskProperty("LateUpdate", (mask & SystemUpdateMask.LateUpdate) != 0);
                DrawUpdateMaskProperty("FixedUpdate", (mask & SystemUpdateMask.FixedUpdate) != 0);
            });

            // Attributes section
            DrawSection("ATTRIBUTES", () => DrawSystemAttributes(systemType));

            // Children section (for groups)
            if (isGroup)
            {
                var group = (SystemGroup)m_selectedSystem;
                DrawSection($"CHILD SYSTEMS ({group.systems.Count})", () =>
                {
                    if (group.systems.Count == 0)
                    {
                        var emptyStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = DisabledColor }
                        };
                        GUILayout.Label("No child systems", emptyStyle);
                    }
                    else
                    {
                        foreach (var child in group.systems)
                        {
                            DrawChildSystemLink(child);
                        }
                    }
                });
            }

            // Inheritance section
            DrawSection("INHERITANCE", () => DrawInheritanceChain(systemType));

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
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
            GUILayout.Label(label + ":", labelStyle, GUILayout.Width(70));

            var valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                wordWrap = true
            };
            GUILayout.Label(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawUpdateMaskProperty(string label, bool enabled)
        {
            EditorGUILayout.BeginHorizontal();
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = DisabledColor }
            };
            GUILayout.Label(label + ":", labelStyle, GUILayout.Width(70));

            var valueColor = enabled ? EnabledColor : new Color(0.6f, 0.3f, 0.3f);
            var valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = valueColor }
            };
            GUILayout.Label(enabled ? "Enabled" : "Disabled", valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSystemAttributes(Type systemType)
        {
            var attributes = systemType.GetCustomAttributes(true);
            var hasRelevantAttribute = false;

            foreach (var attr in attributes)
            {
                var attrType = attr.GetType();
                var attrName = attrType.Name;

                if (attrName.Contains("Update") || attrName.Contains("System") || attrName.Contains("World"))
                {
                    hasRelevantAttribute = true;

                    EditorGUILayout.BeginHorizontal();

                    var attrStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = AccentColor }
                    };
                    GUILayout.Label($"[{attrName}]", attrStyle, GUILayout.Width(140));

                    var properties = attrType.GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.Name == "TypeId") continue;

                        try
                        {
                            var value = prop.GetValue(attr);
                            if (value != null)
                            {
                                var displayValue = value is Type typeValue ? typeValue.Name : value.ToString();
                                var propStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                                };
                                GUILayout.Label($"{prop.Name}: {displayValue}", propStyle);
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (!hasRelevantAttribute)
            {
                var emptyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = DisabledColor }
                };
                GUILayout.Label("No system attributes", emptyStyle);
            }
        }

        private void DrawChildSystemLink(SystemBase child)
        {
            EditorGUILayout.BeginHorizontal();

            var isChildGroup = child is SystemGroup;
            var iconContent = isChildGroup
                ? EditorGUIUtility.IconContent("d_Folder Icon")
                : EditorGUIUtility.IconContent("d_cs Script Icon");

            GUILayout.Label(iconContent, GUILayout.Width(16), GUILayout.Height(16));

            if (GUILayout.Button(child.GetType().Name, EditorStyles.linkLabel))
            {
                m_selectedSystem = child;
            }

            if (isChildGroup)
            {
                var childGroup = (SystemGroup)child;
                var countStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = DisabledColor }
                };
                GUILayout.Label($"({childGroup.systems.Count})", countStyle);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInheritanceChain(Type systemType)
        {
            var types = new List<Type>();
            var currentType = systemType;

            while (currentType != null && currentType != typeof(object))
            {
                types.Add(currentType);
                currentType = currentType.BaseType;
            }

            for (var i = 0; i < types.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(i * 12);

                var arrowText = i > 0 ? "-> " : "";
                var typeColor = i == 0 ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = typeColor },
                    fontStyle = i == 0 ? FontStyle.Bold : FontStyle.Normal
                };

                GUILayout.Label(arrowText + types[i].Name, style);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawWorldItem(string label, string info, bool isSelected, Action onClick)
        {
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));

            if (isSelected)
                EditorGUI.DrawRect(rect, SelectedColor);
            else if (rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, HoverColor);

            GUILayout.Space(12);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = isSelected ? Color.white : new Color(0.8f, 0.8f, 0.8f) }
            };
            GUILayout.Label(label, labelStyle);

            GUILayout.FlexibleSpace();

            var infoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = DisabledColor }
            };
            GUILayout.Label(info, infoStyle);
            GUILayout.Space(8);

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                onClick?.Invoke();
                Event.current.Use();
            }
        }

        private void DrawSectionHeader(string title)
        {
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal = { textColor = DisabledColor }
            };
            GUILayout.Label(title, style);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
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

        private void SetAllFoldouts(bool expanded)
        {
            if (m_selectedWorld == null) return;

            foreach (var rootSystem in m_selectedWorld.rootSystems)
            {
                SetFoldoutsRecursive(rootSystem, expanded);
            }
        }

        private void SetFoldoutsRecursive(SystemBase system, bool expanded)
        {
            if (system is SystemGroup group)
            {
                m_foldoutStates[system] = expanded;
                foreach (var child in group.systems)
                {
                    SetFoldoutsRecursive(child, expanded);
                }
            }
        }

        private void UpdateSystemCount()
        {
            m_totalSystemCount = 0;
            if (m_selectedWorld != null)
            {
                foreach (var rootSystem in m_selectedWorld.rootSystems)
                {
                    m_totalSystemCount += CountSystemsRecursive(rootSystem);
                }
            }
        }

        private int CountSystems(World world)
        {
            var count = 0;
            foreach (var rootSystem in world.rootSystems)
            {
                count += CountSystemsRecursive(rootSystem);
            }

            return count;
        }

        private int CountSystemsRecursive(SystemBase system)
        {
            var count = 1;
            if (system is SystemGroup group)
            {
                foreach (var child in group.systems)
                {
                    count += CountSystemsRecursive(child);
                }
            }

            return count;
        }
    }
}
