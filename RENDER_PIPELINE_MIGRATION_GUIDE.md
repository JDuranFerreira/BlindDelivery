# Built-in Render Pipeline to URP Migration Guide for Blind Delivery Project

## Issue Summary
The project currently uses the **Built-in Render Pipeline** (deprecated) and needs to migrate to **Universal Render Pipeline (URP)** or **High-Definition Render Pipeline (HDRP)**.

## Current Status
- ✅ **Project Structure**: Unity project with multiple assemblies
- ❌ **Render Pipeline**: Using Built-in Render Pipeline (`m_DefaultRenderingPath: 1`)
- 📋 **Action Required**: Migration to URP/HDRP

## Migration Steps

### Step 1: Choose Target Render Pipeline

**Recommendation**: Use **Universal Render Pipeline (URP)** because:
- Mobile-optimized (project targets mobile)
- Faster iteration
- Good balance of features and performance
- Industry standard for mobile games

### Step 2: Install URP Package
```bash
# In Unity Project window
Assets > Rendering > Universal Render Pipeline > Install URP

# Or via Package Manager
Edit > Project Settings > Package Manager
- Click "+" > Add package from git URL
- URL: "com.unity.render-pipelines.universal"
```

### Step 3: Configure URP Settings
Create/Update `Assets/URP/DefaultAsset.asset`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RenderPipelineAsset xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <name>Default Render Pipeline Asset</name>
    <renderPipelineType>UniversalRenderPipeline</renderPipelineType>
    
    <!-- Camera Settings -->
    <cameraType>Camera</cameraType>
    <clearDepth>1</clearDepth>
    <clearColor>0</clearColor>
    < cullingMask>1677721599</cullingMask>
    
    <!-- Lighting Settings -->
    <mainLightMode>1</mainLightMode>
    <additionalLightsMode>1</additionalLightsMode>
    <additionalLightsPerObjectLimit>4</additionalLightsPerObjectLimit>
    
    <!-- Shadow Settings -->
    <shadowQuality>1</shadowQuality>
    <shadowDistance>20</shadowDistance>
    <shadowNearPlaneOffset>0.05</shadowNearPlaneOffset>
    <shadowCascade2Split>0.33</shadowCascade2Split>
    
    <!-- Post-processing -->
    <postProcessingSettings>
        <upscaleAmount>2</upscaleAmount>
        <finalBlendMode>0</finalBlendMode>
        příští>
    </postProcessingSettings>
    
    <!-- Debug Settings -->
    <debugLevel>0</debugLevel>
</RenderPipelineAsset>
```

### Step 4: Update Graphics Settings

Create `Assets/URP/URPAsset.asset`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<UniversalRenderPipelineAsset xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <name>Blind Delivery URP Asset</name>
    
    <!-- Shader Graph -->
    <shaderIncludePath>Packages/com.unity.render-pipelines.universal/Runtime/"</shaderIncludePath>
    
    <!-- Lighting -->
    <defaultMaterial>Textures/Material</defaultMaterial>
    <defaultShader>Universal Render Pipeline/Lit</defaultShader>
    
    <!-- Camera -->
    <defaultCamera>Cameras/UniversalCamera</defaultCamera>
    
    <!-- Shadows -->
    <shadowFilterMode>1</shadowFilterMode>
    <shadowResolution>512</shadowResolution>
    
    <!-- Anti-aliasing -->
    <antialiasingMode>1</antialiasingMode>
    
    <!-- Batching -->
    <enableBatching>1</enableBatching>
    
    <!-- SRPs -->
    <srpBatcher>1</srpBatcher>
    <srpDefaultMaterial>{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}</srpDefaultMaterial>
</UniversalRenderPipelineAsset>
```

### Step 5: Update Project Settings

Update `ProjectSettings/GraphicsSettings.asset`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<GraphicsSettings xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <!-- Remove built-in render pipeline references -->
    <m_AlwaysIncludedShaders>
        <!-- URP-specific shaders -->
        {fileID: 10753, guid: 0000000000000000f000000000000000, type: 0} <!-- URP/Lit -->
        {fileID: 10770, guid: 0000000000000000f000000000000000, type: 0} <!-- URP/Unlit -->
        {fileID: 10783, guid: 0000000000000000f000000000000000, type: 0} <!-- URP/2D -->
    </m_AlwaysIncludedShaders>
    
    <!-- Set URP asset reference -->
    <m_RenderPipelineGlobalSettingsMap>
        UniversalRenderPipeline: {fileID: 11400000, guid: your-guid-here, type: 2}
    </m_RenderPipelineGlobalSettingsMap>
    
    <!-- Update rendering path -->
    <m_DefaultRenderingPath>2</m_DefaultRenderingPath>
    <m_DefaultMobileRenderingPath>2</m_DefaultMobileRenderingPath>
    
    <!-- Custom render pipeline reference -->
    <m_CustomRenderPipeline>{fileID: 11400000, guid: your-urp-asset-guid, type: 2}</m_CustomRenderPipeline>
</GraphicsSettings>
```

### Step 6: Update Camera Setup

Create `Assets/URP/UniversalCamera.prefab`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Prefab xmlns="http://schemas.unity3d.com/2020/01/UnityEngine.Universal">
    <GameObject>
        <name>Universal Camera</name>
        <m_ObjectHideFlags>0</m_ObjectHideFlags>
        
        <!-- Camera Component -->
        <Camera>
            <m_CullingMask>1677721599</m_CullingMask>
            <m_UseOcclusionCulling>0</m_UseOcclusionCulling>
            <m_StoreActionsIntermediate>0</m_StoreActionsIntermediate>
            <m_NetworkTransmissionMode>0</m_NetworkTransmissionMode>
            <m_AllowDynamicResolution>0</m_AllowDynamicResolution>
            <m_UseOcclusionCulling>0</m_UseOcclusionCulling>
        </Camera>
        
        <!-- Universal Additional Camera Component -->
        <UniversalAdditionalCameraData>
            <m_RenderType>0</m_RenderType>
            <m_VolumeLayer>0</m_VolumeLayer>
            <m_RenderPostProcessing>1</m_RenderPostProcessing>
            <m_Antialiasing>1</m_Antialiasing>
            <m_AntialiasingQuality>0</m_AntialiasingQuality>
        </UniversalAdditionalCameraData>
        
        <!-- URP Camera Component -->
        <UniversalCamera>
            <m_AllowXRAutoBlitToBackbuffer>1</m_AllowXRAutoBlitToBackbuffer>
            <m_TopoOrdering>1</m_TopoOrdering>
        </UniversalCamera>
    </GameObject>
</Prefab>
```

### Step 7: Update Materials and Shaders

Update existing materials to use URP shaders:

```csharp
// Update material shader
void UpdateMaterialsToURP()
{
    Material[] materials = GameObject.FindObjectsOfType<Material>();
    
    foreach (Material mat in materials)
    {
        if (mat.shader.name.Contains("Standard") || mat.shader.name.Contains("Built-in"))
        {
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            Debug.Log($"Updated material {mat.name} to URP shader");
        }
    }
}
```

### Step 8: Create URP Configuration Script

Create `Assets/URP/URPConfiguration.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPConfiguration
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        // Configure URP asset if not set
        if (GraphicsSettings.currentRenderPipeline == null)
        {
            // Find or create URP asset
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                // Create default URP configuration
                Debug.Log("URP not configured - please set up URP asset in project settings");
            }
        }
    }
}
```

### Step 9: Update Lighting Configuration

Create `Assets/URP/URPLightingConfiguration.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class URPLightingConfiguration : MonoBehaviour
{
    [Header("URP Lighting Configuration")]
    [SerializeField] private bool autoConfigure = true;
    [SerializeField] private bool useDynamicBatching = true;
    [SerializeField] private int maxAdditionalLights = 4;
    
    void Start()
    {
        if (autoConfigure)
        {
            ConfigureURPLighting();
        }
    }
    
    void ConfigureURPLighting()
    {
        // Find URP asset
        var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.additionalLightsPerObjectLimit = maxAdditionalLights;
            urpAsset.enableDynamicBatching = useDynamicBatching;
        }
    }
}
```

### Step 10: Update Graphics Settings Script

Create `Assets/Editor/URPMigrationTool.cs`:

```csharp
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class URPMigrationTool
{
    [MenuItem("Tools/URP/Migrate Blind Delivery to URP")]
    public static void MigrateToURP()
    {
        // Create URP Asset if not exists
        string urpAssetPath = "Assets/URP/DefaultAsset.asset";
        if (!System.IO.File.Exists(urpAssetPath))
        {
            var urpAsset = ScriptableObject.CreateInstance&lt;UniversalRenderPipelineAsset&gt;();
            AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created URP Asset at: " + urpAssetPath);
        }
        
        // Update Graphics Settings
        UpdateGraphicsSettings(urpAssetPath);
        
        // Update Quality Settings
        UpdateQualitySettings();
        
        // Update URP Asset defaults
        UpdateURPAssetDefaults(urpAssetPath);
        
        Debug.Log("URP migration completed for Blind Delivery!");
    }
    
    static void UpdateGraphicsSettings(string urpAssetPath)
    {
        // Load existing GraphicsSettings
        var settings = UnityEditor.AssetDatabase.LoadAssetAtPath&lt;GraphicsSettings&gt;(
            "ProjectSettings/GraphicsSettings.asset");
        
        if (settings != null)
        {
            // Update to URP
            settings.m_DefaultRenderingPath = 2;
            settings.m_DefaultMobileRenderingPath = 2;
            
            // Set URP asset reference
            var urpAsset = UnityEditor.AssetDatabase.LoadAssetAtPath&lt;UniversalRenderPipelineAsset&gt;(urpAssetPath);
            if (urpAsset != null)
            {
                settings.m_CustomRenderPipeline = urpAsset;
            }
        }
    }
    
    static void UpdateQualitySettings()
    {
        // Update quality settings to support URP
        QualitySettings.renderPipeline = UniversalRenderPipelineAsset.Find()
            ? UniversalRenderPipelineAsset.Find()
            : null;
        
        // Set appropriate quality levels for mobile
        QualitySettings.SetQualityLevel(2, true); // Medium quality
    }
    
    static void UpdateURPAssetDefaults(string urpAssetPath)
    {
        var urpAsset = UnityEditor.AssetDatabase.LoadAssetAtPath&lt;UniversalRenderPipelineAsset&gt;(urpAssetPath);
        if (urpAsset != null)
        {
            // Configure for mobile
            urpAsset.allowHDR = false;
            urpAsset.allowMSAA = false;
            urpAsset.allowLightCookies = false;
            
            // Set appropriate shadow resolution
            urpAsset.shadowResolution = UniversalRenderPipelineAsset.ShadowResolution.Low;
        }
    }
}
```

### Step 11: Update Test and Validation

Update test suite to verify URP functionality:

```csharp
[UnityTest]
public IEnumerator TestURPRendering()
{
    // Find URP asset
    var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
    Assert.IsNotNull(urpAsset, "URP asset not found");
    
    // Test URP configuration
    Assert.IsTrue(urpAsset.isDefault, "URP should be configured as default");
    
    yield return null;
}
```

## Testing and Validation

### Visual Tests
```csharp
[UnityTest]
public IEnumerator TestURPVisuals()
{
    // Test that URP shaders render correctly
    Renderer renderer = GetComponent&lt;Renderer&gt;();
    Material originalMaterial = renderer.material;
    
    // Change to URP shader
    Material urpMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    renderer.material = urpMaterial;
    
    // Verify shader change
    Assert.AreEqual("Universal Render Pipeline/Lit", renderer.material.shader.name);
    
    // Restore original
    renderer.material = originalMaterial;
    
    yield return null;
}
```

### Performance Tests
```csharp
[UnityTest]
public IEnumerator TestURPPerformance()
{
    // Measure URP performance
    float frameTime = 0f;
    int frameCount = 0;
    
    for (int i = 0; i < 60; i++)
    {
        frameTime += Time.deltaTime;
        frameCount++;
        yield return new WaitForEndOfFrame();
    }
    
    float avgFrameTime = frameTime / frameCount;
    Debug.Log($"Average frame time with URP: {avgFrameTime * 1000:F2}ms");
    
    // Verify performance meets mobile standards
    Assert.IsTrue(avgFrameTime < 0.033f, "Frame time too high for URP on mobile");
}
```

## Required Code Changes

### Update Graphics Configuration
```csharp
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class UPRGraphicsConfiguration : MonoBehaviour
{
    private UniversalAdditionalCameraData cameraData;
    private UniversalRenderPipelineAsset urpAsset;
    
    void Start()
    {
        // Get URP asset
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        
        // Configure camera for URP
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraData = mainCamera.GetComponent&lt;UniversalAdditionalCameraData&gt;();
            if (cameraData == null)
            {
                cameraData = mainCamera.gameObject.AddComponent&lt;UniversalAdditionalCameraData&gt;();
            }
            
            // Configure for mobile
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
    }
}
```

## Expected Timeline

1. **Week 1**: Install URP package, create URP Asset
2. **Week 2**: Update Graphics Settings, configure URP
3. **Week 3**: Update materials and shaders, test visuals
4. **Week 4**: Update lighting configuration, performance testing
5. **Week 5**: Integration testing, fix remaining issues

## Verification Checklist

- [ ] URP package installed successfully
- [ ] URP Asset configured and assigned
- [ ] Graphics Settings updated
- [ ] Materials migrated to URP shaders
- [ ] Lighting configuration optimized for mobile
- [ ] Camera setup updated for URP
- [ ] Performance tests pass
- [ ] Visual tests pass
- [ ] Built-in render pipeline references removed

## Benefits of URP Migration

1. **Mobile Optimization**: URP is specifically designed for mobile platforms
2. **Performance**: Better performance on mobile devices
3. **Future-Proof**: Ongoing development and support
4. **Feature Set**: Modern rendering features
5. **Compatibility**: Better integration with Unity tools

## Next Steps

1. Run the URP migration tool
2. Update all shader and material references
3. Configure URP for mobile optimization
4. Test performance and visuals
5. Update documentation
6. Commit the changes

This migration will improve the project's performance on mobile devices while providing better rendering quality and future compatibility.