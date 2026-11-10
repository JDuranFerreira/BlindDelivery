using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class SystemsDemoDiagnostics
{
    public static string Diagnose()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[diag] playing=" + Application.isPlaying);
        sb.AppendLine("[diag] scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

        Camera cam = Camera.main;
        if (cam == null)
        {
            foreach (Camera c in Object.FindObjectsOfType<Camera>())
            {
                cam = c;
                if (c.isActiveAndEnabled) { cam = c; break; }
            }
        }
        if (cam != null)
            sb.AppendLine("[diag] camera=" + cam.name + " activeSelf=" + cam.gameObject.activeSelf + " cullingMask=" + cam.cullingMask + " clearFlags=" + cam.clearFlags);
        else
            sb.AppendLine("[diag] camera=NULL");

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        sb.AppendLine("[diag] canvases=" + canvases.Length);
        foreach (Canvas c in canvases)
        {
            sb.AppendLine("[diag] === CANVAS " + c.name
                + " activeInHierarchy=" + c.gameObject.activeInHierarchy + " activeSelf=" + c.gameObject.activeSelf
                + " enabled=" + c.enabled + " layer=" + LayerMask.LayerToName(c.gameObject.layer)
                + " sorting=" + c.sortingOrder + " renderMode=" + c.renderMode);
            Dump(c.transform, 1, sb);
        }

        EventSystem es = Object.FindObjectOfType<EventSystem>();
        if (es != null)
            sb.AppendLine("[diag] eventSystem=" + es.name + " moduleActive=" + (es.currentInputModule != null && es.currentInputModule.IsActive()));
        else
            sb.AppendLine("[diag] eventSystem=NULL");

#if UNITY_EDITOR
        int errCount = 0;
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        System.Type logEntries = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
        if (logEntries != null)
        {
            var getCount = logEntries.GetMethod("GetCount", flags);
            var getEntries = logEntries.GetMethod("GetEntries", flags);
            int count = getCount != null ? (int)getCount.Invoke(null, null) : 0;
            sb.AppendLine("[diag] consoleCount=" + count);
            var entriesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntry");
            if (getEntries != null && entriesType != null)
            {
                int take = Mathf.Min(count, 40);
                object[] entries = (object[])getEntries.Invoke(null, new object[] { null, take });
                flagsProp = entriesType.GetField("flags", flags);
                conditionProp = entriesType.GetField("condition", flags);
                foreach (object e in entries)
                {
                    var f = flagsProp != null ? (int)flagsProp.GetValue(e) : 0;
                    var condition = conditionProp != null ? (string)conditionProp.GetValue(e) : "";
                    // LogEntry flags: Error=0x20?, Assert/Exception bits; print everything non-info
                    if (condition != null && condition.Length > 0)
                    {
                        string kind = ((f & 1 << 9) != 0) ? "ERR" : (((f & 1 << 10) != 0) ? "ASS" : (((f & 1 << 11) != 0) ? "WRN" : "LOG"));
                        if (kind != "LOG")
                        {
                            errCount++;
                            sb.AppendLine("[diag] " + kind + ": " + condition.Substring(0, Mathf.Min(300, condition.Length)));
                        }
                    }
                }
            }
        }
        sb.AppendLine("[diag] nonInfoEntriesShown=" + errCount);
#endif
        return sb.ToString();
    }

    // Rebuild helper: drops the stale DemoCanvas from MainMenu and re-runs the scene builder,
    // then reports where the launcher button landed. Zero shell-quoting: callable via eval as
    // 'return SystemsDemoDiagnostics.RebuildDemoScenes();'
    public static string RebuildDemoScenes()
    {
        var sb = new StringBuilder();
        if (UnityEditor.EditorApplication.isPlaying)
        {
            return "[rebuild] exit Play mode first (playing=true)";
        }
        var mainMenu = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/MainMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        int removed = 0;
        foreach (var root in mainMenu.GetRootGameObjects())
        {
            if (root.name == "DemoCanvas")
            {
                UnityEngine.Object.DestroyImmediate(root);
                removed++;
            }
        }
        sb.AppendLine("[rebuild] removed stale DemoCanvas objects=" + removed);
        // Persist the removal BEFORE BuildAll: it re-opens MainMenu, and an unsaved deletion
        // would be discarded — the builder would then skip ('launcher already present').
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mainMenu);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(mainMenu, "Assets/Scenes/MainMenu.unity");
        Template.ExamplesEditor.SystemsDemoSceneBuilder.BuildAll();
        sb.AppendLine("[rebuild] BuildAll done, activeScene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        var canvas = GameObject.Find("DemoCanvas");
        if (canvas == null)
        {
            sb.AppendLine("[rebuild] DemoCanvas NOT FOUND after build");
        }
        else
        {
            RectTransform safe = canvas.GetComponentInChildren<Template.Services.UI.SafeAreaFitter>() != null
                ? canvas.GetComponentInChildren<Template.Services.UI.SafeAreaFitter>().GetComponent<RectTransform>() : null;
            var button = safe != null ? safe.Find("btn_OpenDemo") as RectTransform : null;
            if (button == null)
            {
                sb.AppendLine("[rebuild] btn_OpenDemo NOT FOUND under SafeArea");
            }
            else
            {
                sb.AppendLine("[rebuild] btn_OpenDemo anchoredPos=" + button.anchoredPosition + " rectSize=" + button.rect.size + " worldPos=" + button.position);
                var btn = button.GetComponent< UnityEngine.UI.Button >();
                sb.AppendLine("[rebuild] DemoMenuButton=" + (btn != null && btn.GetComponent<Template.Examples.DemoMenuButton>() != null));
            }
            var evs = Object.FindObjectOfType<EventSystem>();
            sb.AppendLine("[rebuild] eventSystem=" + (evs != null ? evs.name : "NULL") + " module=" + (evs != null && evs.currentInputModule != null ? evs.currentInputModule.GetType().Name : "NULL"));
        }
        return sb.ToString();
    }

    // Opens MainMenu and enters Play mode ('Play anywhere' boot applies). Returns immediately.
    public static string PlayMainMenu()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            return "[play] already playing";
        }
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/Scenes/MainMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        UnityEditor.EditorApplication.isPlaying = true;
        return "[play] entering Play on MainMenu";
    }

    // Full InputSystem/UI-event dump — why clicks may not register.
    public static string InputDiagnose()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[in] playing=" + Application.isPlaying);
        var systems = Object.FindObjectsOfType<EventSystem>(true);
        sb.AppendLine("[in] eventSystemCount=" + systems.Length);
        foreach (var es in systems)
        {
            sb.AppendLine("[in] es=" + es.name + " enabled=" + es.enabled + " activeInHierarchy=" + es.gameObject.activeInHierarchy
                + " current=" + (EventSystem.current != null ? EventSystem.current.name : "NULL"));
            foreach (var m in es.GetComponents<BaseInputModule>())
                sb.AppendLine("[in]   module=" + m.GetType().Name + " enabled=" + m.enabled
                    + " isActiveAndEnabled=" + m.isActiveAndEnabled + " IsActive=" + m.IsActive());
        }
        sb.AppendLine("[in] keyboardPresent=" + (UnityEngine.InputSystem.Keyboard.current != null));
        return sb.ToString();
    }

    static System.Reflection.FieldInfo flagsProp;
    static System.Reflection.FieldInfo conditionProp;

    // Menu entries so template users can self-check the demo (issue: "I press Play and see"
    // "no buttons") without knowing about these statics. Editor-only: this file lives in Assets/Editor.
    [UnityEditor.MenuItem("Tools/Systems Demo/Diagnose (log to Console)")]
    static void DiagnoseMenu()
    {
        Debug.Log(Diagnose());
    }

    [UnityEditor.MenuItem("Tools/Systems Demo/Input Diagnose (log to Console)")]
    static void InputDiagnoseMenu()
    {
        Debug.Log(InputDiagnose());
    }

    [UnityEditor.MenuItem("Tools/Systems Demo/Rebuild Demo Scenes")]
    static void RebuildMenu()
    {
        Debug.Log(RebuildDemoScenes());
    }

    [UnityEditor.MenuItem("Tools/Systems Demo/Play MainMenu")]
    static void PlayMenu()
    {
        PlayMainMenu();
    }

    static void Dump(Transform t, int depth, StringBuilder sb)
    {
        string indent = new string('-', depth * 2);
        string extra = "";
        RectTransform rt = t as RectTransform;
        if (rt != null)
        {
            extra = " pos=" + rt.anchoredPosition + " size=" + rt.rect.size + " anchoredPos=" + rt.anchoredPosition + " worldPos=" + rt.position;
            if (rt.localScale.sqrMagnitude < 0.001f) extra += " SCALE~0";
        }
        if (!t.gameObject.activeSelf) extra += " INACTIVE_SELF";
        sb.AppendLine("[diag] " + indent + t.name + " [" + (t.GetComponent<Graphic>() != null ? "G:" : "") + string.Join(",", System.Linq.Enumerable.Select(t.GetComponents<Component>(), x => x == null ? "null!" : x.GetType().Name)) + "]" + extra);
        foreach (Transform child in t)
            Dump(child, depth + 1, sb);
    }
}
