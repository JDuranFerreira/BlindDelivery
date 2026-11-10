using System.Collections.Generic;
using System.IO;
using Template.Examples;
using Template.Services.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Template.ExamplesEditor
{
    /// <summary>
    /// One-shot builder for the template's example scenes (Template menu). Idempotent by design:
    /// MainMenu receives its launcher canvas once, SystemsDemo is rebuilt fresh on every run, the
    /// pooled demo-cube prefab is created once, and the demo scene lands in Build Settings after
    /// MainMenu (Bootstrap stays scene 0). Re-run any time the example UI or script list changes.
    /// </summary>
    public static class SystemsDemoSceneBuilder
    {
        const string DemoScenePath = "Assets/Scenes/SystemsDemo.unity";
        const string CubePrefabPath = "Assets/Prefabs/DemoCube.prefab";
        [MenuItem("Template/Build Systems Example Scenes")]
        public static void BuildAll()
        {
            EnsureCubePrefab();
            BuildMainMenu();
            BuildDemoScene();
            AddDemoSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SystemsDemoBuilder] done: prefab, MainMenu launcher, demo scene, Build Settings (Bootstrap, MainMenu, SystemsDemo)");
        }
        // =====================================================================================
        // Cube prefab the PoolFactory entry points at
        // =====================================================================================
        static void EnsureCubePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CubePrefabPath) != null)
            {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(CubePrefabPath));
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DemoCube";
            go.AddComponent<PooledCube>();
            PrefabUtility.SaveAsPrefabAsset(go, CubePrefabPath);
            Object.DestroyImmediate(go);
        }
        // =====================================================================================
        // MainMenu — camera-only today; this adds the launcher canvas
        // =====================================================================================
        static void BuildMainMenu()
        {
            if (!File.Exists("Assets/Scenes/MainMenu.unity"))
            {
                Debug.LogError("[SystemsDemoBuilder] Assets/Scenes/MainMenu.unity is missing.");
                return;
            }
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            if (GameObject.Find("DemoCanvas") != null)
            {
                Debug.Log("[SystemsDemoBuilder] MainMenu already carries the launcher — skipped.");
                return;
            }
            Canvas canvas = CreateCanvas("DemoCanvas");
            RectTransform safe = CreateSafeArea(canvas.transform);
            Text title = CreateStackLabel(safe, "Title", "Unity Mobile Template", 120f, 60, TextAnchor.UpperCenter, -8f);
            title.fontStyle = FontStyle.Bold;
            CreateStackLabel(safe, "Subtitle",
                "Play anywhere: AppBootstrap boots every service before the first scene.\nThen open the example scene.", 110f, 30, TextAnchor.MiddleLeft, -128f);
            GameObject menuButton = CreateButton(safe, "btn_OpenDemo", "Open Systems Demo", -260f, 84f);
            menuButton.AddComponent<DemoMenuButton>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        }
        // =====================================================================================
        // SystemsDemo — the example scene itself
        // =====================================================================================
        static void BuildDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject cameraGo = GameObject.Find("Main Camera");
            if (cameraGo == null)
            {
                cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraGo.tag = "MainCamera";
            }
            Camera cam = cameraGo.GetComponent<Camera>();
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            Canvas canvas = CreateCanvas("DemoCanvas");
            RectTransform safe = CreateSafeArea(canvas.transform);
            Text title = CreateStackLabel(safe, "Title", "Unity Mobile Template — Systems Demo", 110f, 44, TextAnchor.UpperCenter, -8f);
            title.fontStyle = FontStyle.Bold;
            Text status = CreateStackLabel(safe, "Status", "Boot and service events stream here.", 320f, 24, TextAnchor.UpperLeft, -128f);
            Text fps = CreateStackLabel(safe, "FpsText", "fps", 40f, 22, TextAnchor.UpperRight, -12f);
            RectTransform fpsRect = fps.GetComponent<RectTransform>();
            fpsRect.anchorMin = new Vector2(1f, 1f);
            fpsRect.anchorMax = new Vector2(1f, 1f);
            fpsRect.pivot = new Vector2(1f, 1f);
            fpsRect.sizeDelta = new Vector2(420f, 40f);
            fpsRect.anchoredPosition = new Vector2(-16f, -12f);
            RectTransform buttonsHost = CreateTopPanel(safe, "Buttons", new Vector2(0f, -484f), 1420f);
            VerticalLayoutGroup layout = buttonsHost.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            // One button per system: (GameObject name, label)
            (string, string)[] buttons =
            {
                ("btn_LoadMenu", "ISceneLoader: covered load → MainMenu"),
                ("btn_Save", "ISaveService: save (+12 coins, stamps v2)"),
                ("btn_Load", "ISaveService: load back"),
                ("btn_Settings", "ISettingsService: music volume +0.1"),
                ("btn_Audio", "IAudioService: PlaySfx + PlayMusic + Duck"),
                ("btn_Localize", "ILocalizationService: switch locale"),
                ("btn_PoolSpawn", "PoolManager: spawn 12 pooled cubes"),
                ("btn_PoolRelease", "PoolManager: return cubes to the pool"),
                ("btn_UiPush", "UIManager: push the overlay screen"),
                ("btn_UiBack", "UIManager: back"),
                ("btn_UiPopup", "UIManager: queue + pump a popup"),
                ("btn_Consent", "IConsentService: grant / deny"),
                ("btn_Analytics", "IAnalyticsService: LogEvent"),
                ("btn_RemoteConfig", "IRemoteConfig: fetch + typed reads"),
                ("btn_AdsRewarded", "IAdService: ShowRewarded"),
                ("btn_AdsInterstitial", "IAdService: ShowInterstitial"),
                ("btn_AdsBanner", "IAdService: ShowBanner"),
                ("btn_AdsRemove", "IAdService: PurchaseRemoveAds"),
                ("btn_Purchases", "IPurchaseService: PurchaseAsync"),
                ("btn_PurchasesRestore", "IPurchaseService: RestorePurchasesAsync"),
                ("btn_GameServices", "IGameServices: sign-in + ReportScore"),
                ("btn_Notifications", "INotificationService: schedule in 1 min"),
                ("btn_Currency", "CurrencyWallet: grant + spend"),
            };
            foreach ((string name, string label) in buttons)
            {
                GameObject button = CreateButton(buttonsHost, name, label, 0f, 54f);
                LayoutElement element = button.AddComponent<LayoutElement>();
                element.preferredHeight = 54f;
            }
            // Overlay screen — pushed through the UIManager stack (starts hidden)
            GameObject overlay = new GameObject("OverlayScreen", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            overlay.transform.SetParent(canvas.transform, false);
            Stretch(overlay.GetComponent<RectTransform>());
            Image dim = overlay.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);
            //
            GameObject card = CreatePanel(overlay.transform, "Card", new Color(0.14f, 0.42f, 0.25f), 760f, 420f);
            CreateChildLabel(card.transform, "CardText",
                "Screen two, pushed on the UIManager stack.\n\nBack pops it and reveals what was under it —\nthe Android back button calls the same BackAsync.", 30);
            DemoScreen demoScreen = overlay.AddComponent<DemoScreen>();
            demoScreen.SetId("demo_overlay_0");
            overlay.SetActive(false);
            // Popup host — one concrete IPopupHost the demo binds to the UIManager
            GameObject popup = new GameObject("PopupHost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            popup.transform.SetParent(canvas.transform, false);
            Stretch(popup.GetComponent<RectTransform>());
            popup.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            GameObject popupCard = CreatePanel(popup.transform, "Card", new Color(0.18f, 0.2f, 0.28f), 780f, 460f);
            CreateChildLabel(popupCard.transform, "PopupTitle", "Title", 90f, 34);
            Text body = CreateChildLabel(popupCard.transform, "PopupBody", "Body", 240f, 28);
            body.alignment = TextAnchor.UpperLeft;
            GameObject okButton = CreateButton(popupCard.transform, "btn_PopupOk", "OK", 0f, 64f);
            Move(okButton.GetComponent<RectTransform>(), new Vector2(0f, -180f), new Vector2(320f, 64f));
            DemoPopupHost host = popup.AddComponent<DemoPopupHost>();
            okButton.GetComponent<Button>().onClick.AddListener(host.Confirm);
            popup.SetActive(false);
            // The runner: finds services through AppBootstrap.Instance, owns no template service itself
            GameObject runnerGo = new GameObject("SystemsDemoRunner", typeof(SystemsDemoRunner));
            SystemsDemoRunner runner = runnerGo.GetComponent<SystemsDemoRunner>();
            SerializedObject so = new SerializedObject(runner);
            so.FindProperty("statusLabel").objectReferenceValue = status;
            so.FindProperty("fpsLabel").objectReferenceValue = fps;
            so.FindProperty("pooledPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CubePrefabPath);
            so.FindProperty("overlayScreen").objectReferenceValue = demoScreen;
            so.FindProperty("popupHost").objectReferenceValue = host;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, DemoScenePath);
        }
        static void AddDemoSceneToBuildSettings()
        {
            bool present = false;
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.path == DemoScenePath)
                {
                    present = true;
                    break;
                }
            }
            if (!present)
            {
                EditorBuildSettings.scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
                {
                    new EditorBuildSettingsScene(DemoScenePath, true),
                }.ToArray();
                Debug.Log("[SystemsDemoBuilder] SystemsDemo appended to Build Settings.");
            }
        }
        // =====================================================================================
        // UI construction helpers (legacy uGUI: no TMP essentials dependency in the template yet)
        // =====================================================================================
        static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
            return canvas;
        }
        static RectTransform CreateSafeArea(Transform canvas)
        {
            GameObject go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            go.transform.SetParent(canvas, false);
            Stretch(go.GetComponent<RectTransform>());
            return go.GetComponent<RectTransform>();
        }
        static RectTransform CreateTopPanel(Transform parent, string name, Vector2 position, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-16f, height);
            rect.anchoredPosition = position;
            return rect;
        }
        static GameObject CreatePanel(Transform parent, string name, Color color, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            return go;
        }
        static GameObject CreateButton(Transform parent, string name, string label, float topOffset, float height)
        {
            GameObject go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.16f, 0.2f, 0.28f);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-48f, height);
            rect.anchoredPosition = new Vector2(0f, topOffset);
            CreateChildLabel(go.transform, "Label", label, height, 26);
            return go;
        }
        /// <summary>A wide label pinned under the top edge: the demo's stacked readouts.</summary>
        static Text CreateStackLabel(Transform parent, string name, string text, float height, int fontSize, TextAnchor alignment, float topOffset)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-48f, height);
            rect.anchoredPosition = new Vector2(24f, topOffset);
            return label;
        }
        /// <summary>A label that fills its parent: button captions and popup text.</summary>
        static Text CreateChildLabel(Transform parent, string name, string text, int fontSize)
        {
            return CreateChildLabel(parent, name, text, 0f, fontSize);
        }
        static Text CreateChildLabel(Transform parent, string name, string text, float height, int fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            Stretch(go.GetComponent<RectTransform>());
            return label;
        }
        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        static void Move(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
