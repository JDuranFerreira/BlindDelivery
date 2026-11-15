using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlindDelivery.EditorTools
{
    /// <summary>
    /// Headless-safe URP migration for Blind Delivery.
    /// Creates the pipeline + renderer assets through the URP API (never hand-authored YAML),
    /// then points GraphicsSettings and QualitySettings at them.
    /// </summary>
    public static class URPMigrationTool
    {
        const string Folder       = "Assets/URP";
        const string RendererPath = "Assets/URP/BlindDeliveryURP_Renderer.asset";
        const string AssetPath    = "Assets/URP/BlindDeliveryURP.asset";

        // The earlier hand-written asset is not loadable; remove it so it stops logging errors.
        const string LegacyBadAsset = "Assets/URP/UniversalRenderPipelineAsset.asset";

        [MenuItem("Tools/Blind Delivery/URP/1. Create Pipeline Assets")]
        public static void CreatePipelineAssets()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "URP");

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(LegacyBadAsset) != null)
            {
                AssetDatabase.DeleteAsset(LegacyBadAsset);
                Debug.Log("[URP] Removed malformed hand-written asset.");
            }

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "BlindDeliveryURP_Renderer";
            AssetDatabase.CreateAsset(rendererData, RendererPath);

            var urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
            urpAsset.name = "BlindDeliveryURP";
            AssetDatabase.CreateAsset(urpAsset, AssetPath);

            // Bind the renderer explicitly; Create() leaves the list empty in some point releases.
            var so = new SerializedObject(urpAsset);
            var renderers = so.FindProperty("m_RendererDataList");
            if (renderers != null)
            {
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }
            var defaultIdx = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIdx != null) defaultIdx.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            ConfigureForMobile2D(urpAsset);

            EditorUtility.SetDirty(urpAsset);
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[URP] Created {AssetPath} + {RendererPath}");
        }

        /// <summary>Low-end Android profile: no HDR, no MSAA, no realtime shadows, no depth/colour textures.</summary>
        static void ConfigureForMobile2D(UniversalRenderPipelineAsset a)
        {
            a.renderScale = 1.0f;

            a.supportsHDR = false;
            a.msaaSampleCount = 1;

            a.supportsCameraDepthTexture = false;
            a.supportsCameraOpaqueTexture = false;

            a.shadowDistance = 0f;
            a.shadowCascadeCount = 1;

            // URP 17: the shadow and dynamic-batching toggles are serialized fields
            // behind read-only properties — set them through SerializedObject.
            // (supportsTerrainShadows never existed on the URP asset; removed.)
            var so = new SerializedObject(a);
            var mainShadows = so.FindProperty("m_MainLightShadowsSupported");
            if (mainShadows != null) mainShadows.boolValue = false;
            var addShadows = so.FindProperty("m_AdditionalLightsShadowsSupported");
            if (addShadows != null) addShadows.boolValue = false;
            var softShadows = so.FindProperty("m_SoftShadowsSupported");
            if (softShadows != null) softShadows.boolValue = false;
            var dynBatching = so.FindProperty("m_SupportsDynamicBatching");
            // Dynamic batching was REMOVED from Unity 6 — enabling it logs
            // "Dynamic Batching has been removed and no longer has any effect".
            // The SRP Batcher above is the replacement.
            if (dynBatching != null) dynBatching.boolValue = false;

            // SRP Batcher is a serialized field, not a property, in URP 17.
            var batcher = so.FindProperty("m_UseSRPBatcher");
            if (batcher != null) batcher.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Tools/Blind Delivery/URP/2. Assign to Graphics + Quality")]
        public static void AssignToGraphicsSettings()
        {
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (urpAsset == null)
            {
                Debug.LogError($"[URP] No asset at {AssetPath}. Run step 1 first.");
                return;
            }

            // Assets under ProjectSettings/ are NOT in the AssetDatabase — go through the API.
            var gs = GraphicsSettings.GetGraphicsSettings();
            var gsSo = new SerializedObject(gs);
            var customRP = gsSo.FindProperty("m_CustomRenderPipeline");
            if (customRP == null)
            {
                Debug.LogError("[URP] m_CustomRenderPipeline not found on GraphicsSettings.");
                return;
            }
            customRP.objectReferenceValue = urpAsset;
            gsSo.ApplyModifiedPropertiesWithoutUndo();

            var qs = QualitySettings.GetQualitySettings();
            if (qs != null)
            {
                var qSo = new SerializedObject(qs);
                var qCustom = qSo.FindProperty("m_CustomRenderPipeline");
                if (qCustom != null)
                {
                    qCustom.objectReferenceValue = urpAsset;
                    qSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(gs);
            AssetDatabase.SaveAssets();
            Debug.Log("[URP] GraphicsSettings + QualitySettings now point at URP.");
        }

        [MenuItem("Tools/Blind Delivery/URP/3. Validate Migration")]
        public static void ValidateMigration()
        {
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            var gs = GraphicsSettings.GetGraphicsSettings();
            var gsSo = new SerializedObject(gs);
            var assigned = gsSo.FindProperty("m_CustomRenderPipeline")?.objectReferenceValue
                           as UniversalRenderPipelineAsset;

            if (urpAsset == null)
                Debug.LogError($"[URP] FAIL: no URP asset at {AssetPath}");
            else
                Debug.Log($"[URP] OK: renderScale={urpAsset.renderScale} HDR={urpAsset.supportsHDR} " +
                          $"MSAA={urpAsset.msaaSampleCount} mainLightShadows={urpAsset.supportsMainLightShadows}");

            if (assigned == null)          Debug.LogError("[URP] FAIL: GraphicsSettings has no pipeline assigned.");
            else if (assigned != urpAsset) Debug.LogError("[URP] FAIL: GraphicsSettings points at a different asset.");
            else                           Debug.Log("[URP] OK: GraphicsSettings resolves to the URP asset.");

            var rendererCount = -1;
            if (urpAsset != null)
            {
                // Reflection on rendererCount returned null in Unity 6 — read the
                // serialized renderer list directly instead.
                var urpSo = new SerializedObject(urpAsset);
                var rendererList = urpSo.FindProperty("m_RendererDataList");
                if (rendererList != null) rendererCount = rendererList.arraySize;
            }
            Debug.Log($"[URP] rendererCount={rendererCount} (must be >= 1)");

            Debug.Log($"[URP] currentRenderPipeline = " +
                      $"{(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "<none / Built-in>")}");
        }
    }
}