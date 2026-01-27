using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ProjectIssueScanner : EditorWindow
{
    private Vector2 scroll;
    private string pathFilter = "Assets";
    private bool onlyShowWarnings = true;

    private readonly List<Result> results = new List<Result>(512);
    private bool includeEditorScripts = false;

    private enum Severity { Info, Warning, High }
    private struct Result
    {
        public Severity severity;
        public string scriptName;
        public string assetPath;
        public string message;
    }

    [MenuItem("Tools/Project Issue Scanner")]
    public static void Open()
    {
        GetWindow<ProjectIssueScanner>("Issue Scanner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Heuristic scanner (not perfect). Flags common performance/memory smells.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(6);
        includeEditorScripts = GUILayout.Toggle(includeEditorScripts, "Include Editor", GUILayout.Width(110));

        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label("Scan Root:", GUILayout.Width(70));
            pathFilter = GUILayout.TextField(pathFilter);
            onlyShowWarnings = GUILayout.Toggle(onlyShowWarnings, "Only Warnings", GUILayout.Width(110));
            if (GUILayout.Button("Scan Project", GUILayout.Width(110)))
                Scan();
        }

        GUILayout.Space(8);

        using (var scope = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scope.scrollPosition;

            if (results.Count == 0)
            {
                GUILayout.Label("No results yet. Click Scan Project.", EditorStyles.helpBox);
                return;
            }

            foreach (var r in results)
            {
                if (onlyShowWarnings && r.severity == Severity.Info) continue;

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label($"[{r.severity}] {r.scriptName}", EditorStyles.boldLabel);
                    GUILayout.Label(r.message, EditorStyles.wordWrappedLabel);

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(r.assetPath, EditorStyles.miniLabel);

                        if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(r.assetPath);
                            EditorGUIUtility.PingObject(obj);
                            Selection.activeObject = obj;
                        }
                    }
                }
            }
        }
    }

    private void Scan()
    {
        results.Clear();

        // Find all scripts in Assets (or your chosen root)
        string[] guids = AssetDatabase.FindAssets("t:Script", new[] { pathFilter });

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;

            // Skip Editor scripts by default
            if (!includeEditorScripts && assetPath.Contains("/Editor/"))
                continue;

            // Never scan the scanner itself (prevents self-false-positives)
            if (assetPath.EndsWith("/ProjectIssueScanner.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            string text;
            try { text = File.ReadAllText(assetPath); }
            catch { continue; }

            string scriptName = Path.GetFileNameWithoutExtension(assetPath);

            // Global smells (anywhere)
            if (ContainsAny(text, "Resources.Load(", "Resources.LoadAll("))
                Add(Severity.Warning, scriptName, assetPath, "Uses Resources.Load*. Consider Addressables or careful unload strategy. This can cause memory pressure if misused.");

            if (ContainsAny(text, "DontDestroyOnLoad("))
                Add(Severity.Info, scriptName, assetPath, "Uses DontDestroyOnLoad. Verify you don't create duplicates across scenes.");

            if (ContainsAny(text, "static List<", "static Dictionary<", "static HashSet<"))
                Add(Severity.Info, scriptName, assetPath, "Contains static collections. Make sure they are cleared when appropriate (common source of 'leaks by reference').");

            // Event subscription heuristic: OnEnable has += but file lacks -= in OnDisable
            bool hasOnEnable = text.Contains("OnEnable");
            bool hasOnDisable = text.Contains("OnDisable");
            bool hasPlusEq = text.Contains("+=");
            bool hasMinusEq = text.Contains("-=");
            if (hasOnEnable && hasPlusEq && (!hasOnDisable || !hasMinusEq))
                Add(Severity.Warning, scriptName, assetPath, "Possible event leak: OnEnable uses += but missing matching -= in OnDisable (heuristic).");

            // Per-frame methods check
            CheckHotMethod(text, scriptName, assetPath, "Update");
            CheckHotMethod(text, scriptName, assetPath, "FixedUpdate");
            CheckHotMethod(text, scriptName, assetPath, "LateUpdate");
            CheckHotMethod(text, scriptName, assetPath, "OnGUI");
        }

        // Sort: High first
        results.Sort((a, b) => b.severity.CompareTo(a.severity));
        Repaint();
    }

    private void CheckHotMethod(string text, string scriptName, string assetPath, string methodName)
    {
        string body = ExtractMethodBody(text, methodName);
        if (string.IsNullOrEmpty(body)) return;

        // Expensive finds
        if (ContainsAny(body, "FindObjectOfType", "FindAnyObjectByType", "GameObject.Find", "FindWithTag", "FindGameObjectsWithTag"))
            Add(Severity.High, scriptName, assetPath, $"{methodName} contains Find* calls. These are expensive and can cause spikes. Cache references in Start/Awake.");

        // Component searches
        if (ContainsAny(body, "GetComponentsInChildren", "GetComponents", "GetComponentInParent"))
            Add(Severity.Warning, scriptName, assetPath, $"{methodName} contains GetComponents* searches. Might allocate / be expensive if done often. Cache or limit frequency.");

        // Instantiate in hot path
        if (ContainsAny(body, "Instantiate("))
            Add(Severity.High, scriptName, assetPath, $"{methodName} calls Instantiate(). Consider pooling if this happens frequently.");

        // LINQ in hot path
        if (ContainsAny(body, ".Select(", ".Where(", ".ToList(", ".ToArray(", ".OrderBy(", ".Any(", ".FirstOrDefault("))
            Add(Severity.Warning, scriptName, assetPath, $"{methodName} appears to use LINQ. LINQ often allocates garbage; avoid in per-frame loops.");

        // Coroutine spam
        if (ContainsAny(body, "StartCoroutine("))
            Add(Severity.Warning, scriptName, assetPath, $"{methodName} starts a coroutine. Ensure it isn't started every frame by mistake.");

        // String concat / interpolation in hot path
        if (ContainsAny(body, " + \"", "$\""))
            Add(Severity.Info, scriptName, assetPath, $"{methodName} may allocate strings (concat/interpolation). If frequent, use StringBuilder or cache formatted strings.");
    }

    // Very small brace-matching extractor (best-effort)
    private string ExtractMethodBody(string text, string methodName)
    {
        // Find "void MethodName(" occurrence
        int idx = text.IndexOf("void " + methodName, StringComparison.Ordinal);
        if (idx < 0) return null;

        int braceOpen = text.IndexOf('{', idx);
        if (braceOpen < 0) return null;

        int depth = 0;
        for (int i = braceOpen; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{') depth++;
            else if (c == '}') depth--;

            if (depth == 0)
            {
                // body between braceOpen..i
                int start = braceOpen + 1;
                int len = i - start;
                if (len <= 0) return "";
                return text.Substring(start, len);
            }
        }
        return null;
    }

    private bool ContainsAny(string text, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
            if (text.IndexOf(needles[i], StringComparison.Ordinal) >= 0)
                return true;
        return false;
    }

    private void Add(Severity severity, string scriptName, string assetPath, string message)
    {
        results.Add(new Result
        {
            severity = severity,
            scriptName = scriptName,
            assetPath = assetPath,
            message = message
        });
    }
}
