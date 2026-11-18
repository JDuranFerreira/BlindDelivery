using System.Collections.Generic;
using BlindDelivery.Gameplay.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlindDelivery.Editor
{
    /// <summary>
    /// Inspector for PlatformBuilder. Generates a horizontal platform from the Start and End
    /// markers: left-end prefab, middle prefabs, right-end prefab (a mirrored copy of the end).
    /// </summary>
    [CustomEditor(typeof(PlatformBuilder))]
    public sealed class PlatformBuilderEditor : UnityEditor.Editor
    {
        const string GeneratedRootName = "GeneratedPlatform";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("Build Platform", GUILayout.Height(24)))
            {
                Build((PlatformBuilder)target);
            }

            if (GUILayout.Button("Clear Generated Platform"))
            {
                Clear((PlatformBuilder)target);
            }
        }

        static string[] PrefabPaths(PlatformType type)
        {
            switch (type)
            {
                case PlatformType.Ground:
                    return new[]
                    {
                        "Assets/Prefabs/Sprites/sprite_ground_End.prefab",
                        "Assets/Prefabs/Sprites/sprite_ground_Middle.prefab",
                        "Assets/Prefabs/Sprites/sprite_ground_EndRight.prefab",
                    };
                default:
                    throw new System.NotSupportedException($"No sprite set for platform type '{type}'.");
            }
        }

        public static void Build(PlatformBuilder builder)
        {
            if (builder == null) return;
            if (builder.start == null || builder.end == null)
            {
                Debug.LogError("[PlatformBuilder] Assign both Start and End GameObjects.", builder);
                return;
            }

            string[] paths = PrefabPaths(builder.type);
            GameObject leftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]);
            GameObject middlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[1]);
            GameObject rightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[2]);
            if (leftPrefab == null || middlePrefab == null || rightPrefab == null)
            {
                Debug.LogError("[PlatformBuilder] A platform sprite prefab is missing.", builder);
                return;
            }

            if (Mathf.Abs(builder.start.position.y - builder.end.position.y) > 0.01f)
            {
                Debug.LogWarning("[PlatformBuilder] Start and End have different heights; using Start's Y.", builder);
            }

            float[] positions = PlatformBuilder.CalculatePositions(
                builder.start.position.x, builder.end.position.x, builder.cellSize);
            Transform root = EnsureRoot(builder);
            ClearChildren(root);

            float y = builder.start.position.y;
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject prefab;
                string name;
                if (positions.Length == 1)
                {
                    prefab = middlePrefab;
                    name = "Platform_Mid_0";
                }
                else if (i == 0)
                {
                    prefab = leftPrefab;
                    name = "Platform_Left";
                }
                else if (i == positions.Length - 1)
                {
                    prefab = rightPrefab;
                    name = "Platform_Right";
                }
                else
                {
                    prefab = middlePrefab;
                    name = "Platform_Mid_" + i;
                }

                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                tile.name = name;
                tile.transform.SetParent(root, false);
                tile.transform.localPosition = root.InverseTransformPoint(new Vector3(positions[i], y, 0f));
            }

            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            Debug.Log($"[PlatformBuilder] Generated {positions.Length} tile(s) for '{builder.type}'.");
        }

        public static void Clear(PlatformBuilder builder)
        {
            if (builder == null || builder.generatedRoot == null) return;
            Object.DestroyImmediate(builder.generatedRoot.gameObject);
            var serialized = new SerializedObject(builder);
            serialized.FindProperty("generatedRoot").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        static Transform EnsureRoot(PlatformBuilder builder)
        {
            if (builder.generatedRoot != null) return builder.generatedRoot;

            var rootObject = new GameObject(GeneratedRootName);
            rootObject.transform.SetParent(builder.transform, false);
            var serialized = new SerializedObject(builder);
            serialized.FindProperty("generatedRoot").objectReferenceValue = rootObject.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rootObject.transform;
        }

        static void ClearChildren(Transform root)
        {
            var children = new List<Transform>();
            foreach (Transform child in root) children.Add(child);
            foreach (Transform child in children) Object.DestroyImmediate(child.gameObject);
        }
    }
}
