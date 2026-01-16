using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Editor
{
    public class EntitiesWindow : EditorWindow
    {
        private const float WorldPanelHeight = 110f;
        private const float LeftPanelWidth = 280f;
        private const float RefreshInterval = 0.25f;

        private Vector2 m_worldListScrollPos;
        private Vector2 m_entityListScrollPos;
        private Vector2 m_componentScrollPos;

        private World m_selectedWorld;
        private int m_selectedEntityId = -1;
        private bool m_autoRefresh = true;
        private double m_lastRefreshTime;

        private SearchField m_searchField;
        private string m_searchString = "";

        private readonly List<EntityInfo> m_entityCache = new();
        private readonly List<EntityInfo> m_filteredEntities = new();
        private readonly Dictionary<int, bool> m_componentFoldouts = new();
        private ComponentDataReader m_dataReader;

        private struct EntityInfo
        {
            public int Id;
            public uint Version;
            public EntityArchetype Archetype;
            public List<ComponentInfo> Components;
            public string Name; // From EntityName component if present

            public string DisplayName => string.IsNullOrEmpty(Name) ? $"Entity {Id}" : Name;
        }

        private struct ComponentInfo
        {
            public int Index;
            public string Name;
            public string FullName;
            public int Size;
            public bool IsBuffer;
            public bool IsManagedRef;
        }

        [MenuItem("Window/UnsafeEcs/Entities %#e")]
        public static void ShowWindow()
        {
            var window = GetWindow<EntitiesWindow>();
            window.titleContent = new GUIContent("ECS Entities");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            m_searchField = new SearchField();
            ComponentTypeRegistry.Initialize();
        }

        private void OnDisable()
        {
            m_selectedWorld = null;
            m_entityCache.Clear();
            m_filteredEntities.Clear();
            m_componentFoldouts.Clear();
            m_dataReader = null;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (m_autoRefresh && EditorApplication.timeSinceStartup - m_lastRefreshTime > RefreshInterval)
            {
                m_lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshEntityCache();
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                DrawCenteredMessage("Enter Play Mode to inspect entities", "d_PlayButton@2x");
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

            if (!m_autoRefresh)
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    RefreshEntityCache();
            }

            GUILayout.Space(8);

            var totalCount = m_entityCache.Count;
            var filteredCount = m_filteredEntities.Count;
            var countText = string.IsNullOrEmpty(m_searchString)
                ? $"{totalCount} entities"
                : $"{filteredCount} / {totalCount} entities";
            GUILayout.Label(countText, EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            m_searchString = m_searchField.OnToolbarGUI(m_searchString, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
                FilterEntities();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            var leftPanelRect = EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));
            EditorGUI.DrawRect(leftPanelRect, new Color(0.18f, 0.18f, 0.18f));

            // Worlds section
            DrawSectionHeader("WORLDS");

            m_worldListScrollPos = EditorGUILayout.BeginScrollView(m_worldListScrollPos,
                GUILayout.Height(WorldPanelHeight));

            for (var i = 0; i < WorldManager.Worlds.Count; i++)
            {
                var world = WorldManager.Worlds[i];
                var isSelected = m_selectedWorld == world;

                ref var entityManager = ref world.EntityManager;
                var aliveCount = CountAliveEntities(ref entityManager);
                var worldDisplayName = world.GetDisplayName(i);

                DrawWorldItem(worldDisplayName, $"{aliveCount} entities", isSelected, () =>
                {
                    m_selectedWorld = world;
                    m_selectedEntityId = -1;
                    RefreshEntityCache();
                });
            }

            EditorGUILayout.EndScrollView();

            if (m_selectedWorld == null && WorldManager.Worlds.Count > 0)
            {
                m_selectedWorld = WorldManager.Worlds[0];
                RefreshEntityCache();
            }

            DrawHorizontalSeparator();

            // Entities section
            DrawSectionHeader("ENTITIES");

            m_entityListScrollPos = EditorGUILayout.BeginScrollView(m_entityListScrollPos, GUILayout.ExpandHeight(true));

            var displayList = string.IsNullOrEmpty(m_searchString) ? m_entityCache : m_filteredEntities;

            if (displayList.Count == 0)
            {
                GUILayout.Space(20);
                var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 };
                GUILayout.Label(string.IsNullOrEmpty(m_searchString) ? "No entities" : "No matching entities",
                    emptyStyle);
            }
            else
            {
                for (var i = 0; i < displayList.Count; i++)
                {
                    DrawEntityItem(displayList[i], i % 2 == 0);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            var rightPanelRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rightPanelRect, new Color(0.2f, 0.2f, 0.2f));

            if (m_selectedEntityId < 0)
            {
                GUILayout.Space(20);
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 };
                EditorGUILayout.LabelField("Select an entity to view components", style);
            }
            else
            {
                DrawEntityDetails();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEntityDetails()
        {
            EntityInfo? selectedEntity = null;
            foreach (var entityInfo in m_entityCache)
            {
                if (entityInfo.Id == m_selectedEntityId)
                {
                    selectedEntity = entityInfo;
                    break;
                }
            }

            if (!selectedEntity.HasValue)
            {
                m_selectedEntityId = -1;
                return;
            }

            var entity = selectedEntity.Value;

            // Header
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            GUILayout.Label(entity.DisplayName, headerStyle);

            if (!string.IsNullOrEmpty(entity.Name))
            {
                var idStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                GUILayout.Label($"(id: {entity.Id})", idStyle);
            }

            var versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUILayout.Label($"v{entity.Version}", versionStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            DrawHorizontalSeparator();
            GUILayout.Space(8);

            // Components header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            var subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            GUILayout.Label($"COMPONENTS ({entity.Components.Count})", subHeaderStyle);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            m_componentScrollPos = EditorGUILayout.BeginScrollView(m_componentScrollPos);

            foreach (var component in entity.Components)
            {
                DrawComponentCard(component, entity.Id);
            }

            GUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        private void DrawComponentCard(ComponentInfo component, int entityId)
        {
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            var boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(boxRect, new Color(0.22f, 0.22f, 0.22f));

            // Handle double-click to open script
            if (Event.current.type == EventType.MouseDown &&
                Event.current.clickCount == 2 &&
                boxRect.Contains(Event.current.mousePosition))
            {
                var componentType = ComponentTypeRegistry.GetTypeByIndex(component.Index);
                if (componentType != null)
                {
                    EditorScriptUtility.OpenScriptForType(componentType);
                    Event.current.Use();
                }
            }

            // Header row with foldout
            EditorGUILayout.BeginHorizontal();

            // Foldout
            if (!m_componentFoldouts.TryGetValue(component.Index, out var isExpanded))
                m_componentFoldouts[component.Index] = false;

            var foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            m_componentFoldouts[component.Index] = EditorGUILayout.Foldout(isExpanded, "", true, foldoutStyle);

            // Tag
            Color tagColor;
            string tagText;
            if (component.IsBuffer)
            {
                tagColor = new Color(0.9f, 0.7f, 0.3f);
                tagText = "BUFFER";
            }
            else if (component.IsManagedRef)
            {
                tagColor = new Color(0.6f, 0.6f, 0.9f);
                tagText = "MANAGED";
            }
            else
            {
                tagColor = new Color(0.4f, 0.8f, 0.4f);
                tagText = "COMPONENT";
            }

            var tagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = tagColor }
            };
            GUILayout.Label(tagText, tagStyle, GUILayout.Width(70));

            // Name
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUILayout.Label(component.Name, nameStyle);

            GUILayout.FlexibleSpace();

            // Size
            var sizeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUILayout.Label($"{component.Size} bytes", sizeStyle);

            EditorGUILayout.EndHorizontal();

            // Expanded content
            if (m_componentFoldouts[component.Index])
            {
                GUILayout.Space(4);
                DrawHorizontalSeparator();
                GUILayout.Space(4);

                if (component.IsBuffer)
                {
                    DrawBufferContent(entityId, component.Index);
                }
                else if (component.IsManagedRef)
                {
                    DrawManagedRefContent(entityId, component.Index);
                }
                else
                {
                    DrawComponentContent(entityId, component.Index);
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawComponentContent(int entityId, int componentIndex)
        {
            if (m_dataReader == null)
                return;

            var fields = m_dataReader.ReadComponentData(entityId, componentIndex);
            if (fields.Count == 0)
            {
                DrawEmptyMessage("No data available");
                return;
            }

            DrawFieldList(fields, 0);
        }

        private void DrawManagedRefContent(int entityId, int componentIndex)
        {
            if (m_dataReader == null)
                return;

            var refData = m_dataReader.ReadManagedRefData(entityId, componentIndex);
            if (!refData.HasValue)
            {
                DrawEmptyMessage("Unable to read managed reference");
                return;
            }

            var data = refData.Value;
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            var valueStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };

            // Reference info
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.Label("Object ID:", labelStyle, GUILayout.Width(70));
            GUILayout.Label(data.ObjectId.ToString(), valueStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.Label("Version:", labelStyle, GUILayout.Width(70));
            GUILayout.Label(data.Version.ToString(), valueStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.Label("Storage ID:", labelStyle, GUILayout.Width(70));
            GUILayout.Label(data.StorageId.ToString(), valueStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Resolved object
            if (data.ResolvedObject != null)
            {
                GUILayout.Space(4);

                var objHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.9f) }
                };
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Label($"Resolved: {data.TypeName}", objHeaderStyle);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);
                DrawFieldList(data.ObjectFields, 1);
            }
            else
            {
                var nullStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.6f, 0.4f, 0.4f) }
                };
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Label("(object not resolved)", nullStyle);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBufferContent(int entityId, int componentIndex)
        {
            if (m_dataReader == null)
                return;

            var bufferData = m_dataReader.ReadBufferData(entityId, componentIndex);
            if (!bufferData.HasValue)
            {
                DrawEmptyMessage("Unable to read buffer");
                return;
            }

            var data = bufferData.Value;
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            var valueStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };

            // Buffer info
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.Label("Length:", labelStyle, GUILayout.Width(60));
            GUILayout.Label(data.Length.ToString(), valueStyle);
            GUILayout.Space(16);
            GUILayout.Label("Capacity:", labelStyle, GUILayout.Width(60));
            GUILayout.Label(data.Capacity.ToString(), valueStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (data.Elements.Count == 0)
            {
                DrawEmptyMessage("Buffer is empty");
                return;
            }

            GUILayout.Space(4);

            // Elements
            var elementHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.7f, 0.3f) }
            };

            for (var i = 0; i < data.Elements.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Label($"[{i}]", elementHeaderStyle, GUILayout.Width(30));

                if (data.Elements[i].Count > 0)
                {
                    EditorGUILayout.BeginVertical();
                    DrawFieldList(data.Elements[i], 0, true);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();

                if (i < data.Elements.Count - 1)
                    GUILayout.Space(2);
            }

            if (data.Length > data.Elements.Count)
            {
                var moreStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
                };
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                GUILayout.Label($"... and {data.Length - data.Elements.Count} more elements", moreStyle);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFieldList(List<ComponentDataReader.FieldData> fields, int indent, bool compact = false)
        {
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            var valueStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
            var typeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.4f, 0.4f, 0.4f) }
            };

            var indentSpace = 4 + indent * 12;

            foreach (var field in fields)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indentSpace);

                if (!compact)
                {
                    GUILayout.Label(field.TypeName, typeStyle, GUILayout.Width(60));
                }

                GUILayout.Label(field.Name + ":", labelStyle, GUILayout.Width(compact ? 80 : 100));
                GUILayout.Label(field.Value, valueStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Nested fields
                if (field.IsNested && field.NestedFields != null && field.NestedFields.Count > 0)
                {
                    DrawFieldList(field.NestedFields, indent + 1, compact);
                }
            }
        }

        private void DrawEmptyMessage(string message)
        {
            var emptyStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            GUILayout.Label(message, emptyStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWorldItem(string label, string info, bool isSelected, Action onClick)
        {
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));

            if (isSelected)
                EditorGUI.DrawRect(rect, new Color(0.17f, 0.36f, 0.53f));
            else if (rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));

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
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
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

        private void DrawEntityItem(EntityInfo entity, bool isEven)
        {
            var isSelected = m_selectedEntityId == entity.Id;
            var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));

            if (isSelected)
                EditorGUI.DrawRect(rect, new Color(0.17f, 0.36f, 0.53f));
            else if (rect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
            else if (!isEven)
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.Space(8);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = isSelected ? Color.white : new Color(0.85f, 0.85f, 0.85f) }
            };
            GUILayout.Label($"{entity.DisplayName}:{entity.Version}", labelStyle);

            GUILayout.FlexibleSpace();

            var countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUILayout.Label($"[{entity.Components.Count}]", countStyle);
            GUILayout.Space(8);

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                m_selectedEntityId = entity.Id;
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
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };
            GUILayout.Label(title, style);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        private void DrawHorizontalSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
        }

        private void DrawVerticalSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));
        }

        private string GetNamespace(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return "Unknown";

            var lastDot = fullName.LastIndexOf('.');
            return lastDot > 0 ? fullName.Substring(0, lastDot) : "(global)";
        }

        private unsafe void RefreshEntityCache()
        {
            m_entityCache.Clear();

            if (m_selectedWorld == null)
                return;

            // Create or refresh the data reader
            if (m_dataReader == null || m_dataReader.World != m_selectedWorld)
                m_dataReader = new ComponentDataReader(m_selectedWorld);

            ref var entityManager = ref m_selectedWorld.EntityManager;

            // Get EntityName component array for name lookup
            var entityNameArray = entityManager.GetComponentArray<EntityName>();

            for (var i = 0; i < entityManager.entities.m_length; i++)
            {
                if (entityManager.deadEntities.Ptr[i])
                    continue;

                var entity = entityManager.entities.Ptr[i];
                var archetype = entityManager.entityArchetypes.Ptr[i];

                var components = new List<ComponentInfo>();
                foreach (var componentIndex in archetype.componentBits)
                {
                    components.Add(new ComponentInfo
                    {
                        Index = componentIndex,
                        Name = ComponentTypeRegistry.GetTypeNameByIndex(componentIndex),
                        FullName = ComponentTypeRegistry.GetTypeFullNameByIndex(componentIndex),
                        Size = TypeManager.GetTypeSizeByIndex(componentIndex),
                        IsBuffer = ComponentTypeRegistry.IsBufferType(componentIndex),
                        IsManagedRef = ComponentTypeRegistry.IsManagedRefType(componentIndex)
                    });
                }

                // Try to get entity name from EntityName component
                string entityName = null;
                if (entityNameArray.TryGet(entity, out var nameComponent))
                {
                    entityName = nameComponent.Value.ToString();
                }

                m_entityCache.Add(new EntityInfo
                {
                    Id = entity.id,
                    Version = entity.version,
                    Archetype = archetype,
                    Components = components,
                    Name = entityName
                });
            }

            FilterEntities();
        }

        private void FilterEntities()
        {
            m_filteredEntities.Clear();

            if (string.IsNullOrEmpty(m_searchString))
                return;

            foreach (var entity in m_entityCache)
            {
                // Search by entity name first
                if (!string.IsNullOrEmpty(entity.Name) &&
                    entity.Name.IndexOf(m_searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    m_filteredEntities.Add(entity);
                    continue;
                }

                // Search by component names
                foreach (var component in entity.Components)
                {
                    if (component.Name.IndexOf(m_searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        m_filteredEntities.Add(entity);
                        break;
                    }
                }
            }
        }

        private unsafe int CountAliveEntities(ref EntityManager entityManager)
        {
            var count = 0;
            for (var i = 0; i < entityManager.entities.m_length; i++)
            {
                if (!entityManager.deadEntities.Ptr[i])
                    count++;
            }

            return count;
        }
    }
}
