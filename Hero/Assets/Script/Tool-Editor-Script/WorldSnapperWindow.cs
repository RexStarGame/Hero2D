#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldSnapperWindow : EditorWindow
{
    private enum SnapMode { GridSize, SpriteSize }

    private SnapMode mode = SnapMode.SpriteSize;
    private float gridSize = 1f;
    private Vector2 origin = Vector2.zero;
    private bool useFirstSelectedAsOrigin = true;
    private bool includeChildren;

    private bool preventPixelSeams = true;
    private float seamOverlapPixels = 0.25f;
    private bool preparePixelArtTextures = true;

    [MenuItem("Tools/World Snapper")]
    public static void ShowWindow()
    {
        GetWindow<WorldSnapperWindow>("World Snapper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Snap Selected Objects", EditorStyles.boldLabel);

        mode = (SnapMode)EditorGUILayout.EnumPopup("Mode", mode);
        includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);

        EditorGUILayout.Space(8);

        useFirstSelectedAsOrigin = EditorGUILayout.Toggle("Use Active Selected as Origin", useFirstSelectedAsOrigin);
        using (new EditorGUI.DisabledScope(useFirstSelectedAsOrigin))
            origin = EditorGUILayout.Vector2Field("Origin", origin);

        if (mode == SnapMode.GridSize)
        {
            gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size (world units)", gridSize));
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Sprite Size uses each sprite's rendered world size as its grid cell. " +
                "The optional overlap closes sub-pixel seams without permanently scaling the sprites.",
                MessageType.Info);

            preventPixelSeams = EditorGUILayout.Toggle("Prevent Pixel Seams", preventPixelSeams);
            using (new EditorGUI.DisabledScope(!preventPixelSeams))
            {
                seamOverlapPixels = Mathf.Clamp(
                    EditorGUILayout.FloatField("Seam Overlap (pixels)", seamOverlapPixels),
                    0f,
                    2f);
            }

            preparePixelArtTextures = EditorGUILayout.Toggle(
                new GUIContent(
                    "Prepare Pixel Textures",
                    "Sets selected sprite textures to Point, Clamp, no mipmaps and no compression."),
                preparePixelArtTextures);
        }

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Snap Selected Seamlessly", GUILayout.Height(30f)))
            SnapSelected();

        EditorGUILayout.Space(8);
        GUILayout.Label("Camera", EditorStyles.boldLabel);

        Camera mainCamera = Camera.main;
        if (mainCamera != null && !mainCamera.orthographic)
        {
            EditorGUILayout.HelpBox(
                "The Main Camera is Perspective. A moving perspective camera can make perfectly snapped " +
                "2D sprites shimmer at their seams.",
                MessageType.Warning);

            if (GUILayout.Button("Set Main Camera To Orthographic 2D"))
                SetMainCameraOrthographic(mainCamera);
        }
        else if (mainCamera != null)
        {
            EditorGUILayout.HelpBox("Main Camera is Orthographic.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No enabled camera tagged MainCamera was found.", MessageType.None);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Recommended starting value: 0.25 pixel overlap. Increase to 0.5 only if a line is still visible. " +
            "Do not place exact duplicate sprites at the same position and sorting order.",
            MessageType.None);
    }

    private void SnapSelected()
    {
        Transform[] roots = Selection.transforms;
        if (roots == null || roots.Length == 0)
        {
            Debug.LogWarning("[WorldSnapper] No objects selected.");
            return;
        }

        List<Transform> targets = CollectTargets(roots);
        if (targets.Count == 0) return;

        Vector2 usedOrigin = origin;
        Transform activeTransform = Selection.activeTransform;
        if (useFirstSelectedAsOrigin && activeTransform != null)
            usedOrigin = activeTransform.position;

        if (mode == SnapMode.SpriteSize && preparePixelArtTextures)
            PrepareTextures(targets);

        Object[] undoTargets = new Object[targets.Count];
        for (int i = 0; i < targets.Count; i++)
            undoTargets[i] = targets[i];

        Undo.RecordObjects(undoTargets, "World Snapper - Seamless Snap");

        int snapped = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (SnapTransform(targets[i], usedOrigin))
                snapped++;
        }

        SceneView.RepaintAll();
        Debug.Log($"[WorldSnapper] Seamlessly snapped {snapped} object(s). " +
                  $"Overlap: {(preventPixelSeams ? seamOverlapPixels : 0f):0.##} source pixel(s).");
    }

    private List<Transform> CollectTargets(Transform[] roots)
    {
        List<Transform> targets = new List<Transform>();
        HashSet<Transform> seen = new HashSet<Transform>();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform root = roots[i];
            if (root == null) continue;

            if (!includeChildren)
            {
                if (seen.Add(root)) targets.Add(root);
                continue;
            }

            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < descendants.Length; j++)
            {
                Transform descendant = descendants[j];
                if (descendant != null && seen.Add(descendant))
                    targets.Add(descendant);
            }
        }

        return targets;
    }

    private bool SnapTransform(Transform target, Vector2 usedOrigin)
    {
        if (target == null) return false;

        Vector3 position = target.position;
        float stepX;
        float stepY;

        if (mode == SnapMode.GridSize)
        {
            stepX = gridSize;
            stepY = gridSize;
        }
        else
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return false;

            stepX = renderer.bounds.size.x;
            stepY = renderer.bounds.size.y;

            if (preventPixelSeams && seamOverlapPixels > 0f)
            {
                float pixelsPerUnit = Mathf.Max(1f, renderer.sprite.pixelsPerUnit);
                Vector3 scale = target.lossyScale;
                float overlapX = seamOverlapPixels * Mathf.Abs(scale.x) / pixelsPerUnit;
                float overlapY = seamOverlapPixels * Mathf.Abs(scale.y) / pixelsPerUnit;

                stepX -= overlapX;
                stepY -= overlapY;
            }
        }

        if (stepX <= 0.0001f || stepY <= 0.0001f)
            return false;

        float x = usedOrigin.x + Mathf.Round((position.x - usedOrigin.x) / stepX) * stepX;
        float y = usedOrigin.y + Mathf.Round((position.y - usedOrigin.y) / stepY) * stepY;

        target.position = new Vector3(x, y, position.z);
        EditorUtility.SetDirty(target);
        return true;
    }

    private static void PrepareTextures(List<Transform> targets)
    {
        HashSet<string> paths = new HashSet<string>();

        for (int i = 0; i < targets.Count; i++)
        {
            SpriteRenderer renderer = targets[i] != null
                ? targets[i].GetComponent<SpriteRenderer>()
                : null;

            if (renderer == null || renderer.sprite == null) continue;

            string path = AssetDatabase.GetAssetPath(renderer.sprite.texture);
            if (!string.IsNullOrEmpty(path)) paths.Add(path);
        }

        foreach (string path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            changed |= SetIfDifferent(importer.filterMode, FilterMode.Point, value => importer.filterMode = value);
            changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(
                importer.textureCompression,
                TextureImporterCompression.Uncompressed,
                value => importer.textureCompression = value);

            if (changed)
                importer.SaveAndReimport();
        }
    }

    private static bool SetIfDifferent<T>(T current, T desired, System.Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, desired)) return false;
        setter(desired);
        return true;
    }

    private static void SetMainCameraOrthographic(Camera mainCamera)
    {
        if (mainCamera == null) return;

        Undo.RecordObject(mainCamera, "Set Main Camera To Orthographic 2D");
        mainCamera.orthographic = true;
        EditorUtility.SetDirty(mainCamera);

        Debug.Log(
            "[WorldSnapper] Main Camera changed to Orthographic. " +
            "If Cinemachine controls the lens, keep its Lens Mode Override inherited or set it to Orthographic too.",
            mainCamera);
    }
}
#endif
