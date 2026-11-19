using BlindDelivery.Core.Items;
using BlindDelivery.Gameplay.Items;
using UnityEditor;
using UnityEngine;

namespace BlindDelivery.Editor
{
    /// <summary>
    /// Inspector for one authored item: what it is, which prefab the factory instantiates and
    /// how it is used in a level. Player tools must have a GuidanceItem prefab; level fixtures
    /// may be config-only (e.g. a wind zone) and need no prefab.
    /// </summary>
    [CustomEditor(typeof(ItemDefinitionAsset))]
    public sealed class ItemDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var definition = (ItemDefinitionAsset)target;

            if (definition.Prefab != null)
            {
                Texture2D preview = AssetPreview.GetAssetPreview(definition.Prefab);
                if (preview != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(preview, GUILayout.Width(64f), GUILayout.Height(64f));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }

            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (definition.Usage == ItemUsage.PlayerTool)
            {
                if (definition.Prefab == null)
                {
                    EditorGUILayout.HelpBox("A player tool needs a prefab with a GuidanceItem component.", MessageType.Warning);
                }
                else if (definition.Prefab.GetComponent<GuidanceItem>() == null)
                {
                    EditorGUILayout.HelpBox("This prefab has no GuidanceItem component; the factory cannot create it.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Player tool: appears in the HUD and can be placed and dragged.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Level fixture: authored into the level for design only (e.g. wind). " +
                    "It is excluded from the player HUD and from the item factory.",
                    MessageType.Info);
            }
        }
    }
}
