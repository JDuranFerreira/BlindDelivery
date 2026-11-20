using System;
using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Levels;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BlindDelivery.Editor
{
    /// <summary>
    /// Level authoring: the level's item list is a list of ItemDefinition assets. Designers add,
    /// remove, reorder and duplicate definitions here; the runtime factory is built from the
    /// player-tool entries, while level-only fixtures never reach the HUD.
    /// </summary>
    [CustomEditor(typeof(LevelData))]
    public sealed class LevelDataEditor : UnityEditor.Editor
    {
        const float RowHeight = 58f;

        SerializedProperty tools;
        SerializedProperty fixtures;
        ReorderableList list;
        ReorderableList fixtureList;

        void OnEnable()
        {
            tools = serializedObject.FindProperty("tools");
            fixtures = serializedObject.FindProperty("fixtures");
            list = new ReorderableList(serializedObject, tools, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Guidance Objects — shown in HUD (drag to reorder)"),
                drawElementCallback = (rect, index, active, focused) => DrawElement(rect, tools, index, active, focused, true),
                elementHeightCallback = _ => RowHeight,
                onAddCallback = _ => AddElement(tools),
            };
            fixtureList = new ReorderableList(serializedObject, fixtures, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Level Objects — never in HUD"),
                drawElementCallback = (rect, index, active, focused) => DrawElement(rect, fixtures, index, active, focused, false),
                elementHeightCallback = _ => 20f,
                onAddCallback = _ => AddElement(fixtures),
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "tools" || property.name == "fixtures") continue;
                    EditorGUILayout.PropertyField(property, true);
                }
                while (property.NextVisible(false));
            }

            EditorGUILayout.Space(8f);
            list.DoList(EditorGUILayout.GetControlRect(false, list.GetHeight()));
            EditorGUILayout.Space(8f);
            fixtureList.DoList(EditorGUILayout.GetControlRect(false, fixtureList.GetHeight()));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Item Definition")) CreateDefinition();
            if (GUILayout.Button("Validate list")) Validate();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Guidance Objects appear in the HUD and can be dragged by the player. " +
                "Level Objects (wind breaks, spikes, etc.) belong in the second list: they are " +
                "authored into the level and can never be selected by the player.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawElement(Rect rect, SerializedProperty listProperty, int index, bool active, bool focused, bool allowDuplicate)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            ItemDefinitionAsset definition = element.objectReferenceValue as ItemDefinitionAsset;

            Rect field = new Rect(rect.x, rect.y + 2f, rect.width - 74f, 18f);
            element.objectReferenceValue = EditorGUI.ObjectField(
                field, "Definition", definition, typeof(ItemDefinitionAsset), false);

            if (definition != null)
            {
                Texture2D preview = definition.Prefab != null ? AssetPreview.GetAssetPreview(definition.Prefab) : null;
                if (preview != null) GUI.DrawTexture(new Rect(rect.x + 4f, rect.y + 23f, 28f, 28f), preview);

                Rect info = new Rect(rect.x + 38f, rect.y + 24f, rect.width - 118f, 16f);
                EditorGUI.LabelField(info,
                    definition.Type + "  •  " + definition.Usage +
                    "  •  cost " + definition.ToData().MoveCost +
                    "  •  cd " + definition.ToData().CooldownSeconds.ToString("0.0") + "s");

                if (definition.Usage == ItemUsage.Guidance && definition.Prefab == null)
                {
                    EditorGUI.LabelField(new Rect(rect.x + 38f, rect.y + 40f, rect.width - 118f, 14f),
                        "Player tool needs a prefab", EditorStyles.helpBox);
                }
            }
            else
            {
                EditorGUI.LabelField(new Rect(rect.x + 4f, rect.y + 26f, rect.width - 80f, 16f),
                    "Drop an Item Definition asset here", EditorStyles.helpBox);
            }

            if (allowDuplicate)
            {
                Rect copy = new Rect(rect.x + rect.width - 70f, rect.y + 24f, 66f, 20f);
                using (new EditorGUI.DisabledScope(definition == null))
                {
                    if (GUI.Button(copy, "Copy")) DuplicateDefinition(index);
                }
            }
        }

        void AddElement(SerializedProperty property)
        {
            property.InsertArrayElementAtIndex(property.arraySize);
            property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = null;
        }

        void DuplicateDefinition(int index)
        {
            var owner = (LevelData)target;
            ItemDefinitionAsset source = tools.GetArrayElementAtIndex(index).objectReferenceValue as ItemDefinitionAsset;
            if (source == null) return;

            string ownerPath = AssetDatabase.GetAssetPath(owner);
            string folder = ownerPath.Substring(0, ownerPath.LastIndexOf('/'));
            string name = owner.name + "_" + source.Type;
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");

            ItemDefinitionAsset copy = UnityEngine.Object.Instantiate(source);
            copy.name = name;
            AssetDatabase.CreateAsset(copy, path);
            AssetDatabase.SaveAssets();

            tools.GetArrayElementAtIndex(index).objectReferenceValue = copy;
            serializedObject.ApplyModifiedProperties();
            Debug.Log("[LevelData] Created level-specific definition: " + path);
        }

        void CreateDefinition()
        {
            var owner = (LevelData)target;
            string ownerPath = AssetDatabase.GetAssetPath(owner);
            string folder = ownerPath.Substring(0, ownerPath.LastIndexOf('/'));
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/ItemDefinition.asset");

            ItemDefinitionAsset definition = CreateInstance<ItemDefinitionAsset>();
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = definition;
            Debug.Log("[LevelData] Created Item Definition: " + path);
        }

        void Validate()
        {
            var seen = new HashSet<ItemType>();
            int issues = 0;
            ValidateList(tools, seen, "Player item", ref issues);
            ValidateList(fixtures, seen, "Fixture", ref issues);

            Debug.Log(issues == 0
                ? "[LevelData] " + target.name + ": item lists are valid."
                : "[LevelData] " + target.name + ": " + issues + " issue(s) found.");
        }

        void ValidateList(SerializedProperty property, HashSet<ItemType> seen, string label, ref int issues)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                ItemDefinitionAsset definition =
                    property.GetArrayElementAtIndex(i).objectReferenceValue as ItemDefinitionAsset;
                if (definition == null)
                {
                    Debug.LogWarning("[LevelData] " + label + " slot " + (i + 1) + " is empty.", target);
                    issues++;
                    continue;
                }

                if (definition.Usage == ItemUsage.Guidance && definition.Prefab == null)
                {
                    Debug.LogWarning("[LevelData] " + definition.name + " is a Guidance Object but has no prefab.", target);
                    issues++;
                }

                if (!seen.Add(definition.Type))
                {
                    Debug.LogWarning("[LevelData] Duplicate item type: " + definition.Type + ".", target);
                    issues++;
                }
            }
        }
    }
}
