using UnityEngine;
using UnityEditor;

public class WorldSnapperWindow : EditorWindow
{
    private enum SnapMode { GridSize, SpriteSize }

    private SnapMode mode = SnapMode.SpriteSize;

    private float gridSize = 1f;

    private Vector2 origin = Vector2.zero;
    private bool useFirstSelectedAsOrigin = true;

    private bool includeChildren = false;

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

        useFirstSelectedAsOrigin = EditorGUILayout.Toggle("Use First Selected as Origin", useFirstSelectedAsOrigin);
        using (new EditorGUI.DisabledScope(useFirstSelectedAsOrigin))
        {
            origin = EditorGUILayout.Vector2Field("Origin", origin);
        }

        if (mode == SnapMode.GridSize)
        {
            gridSize = EditorGUILayout.FloatField("Grid Size (world units)", gridSize);
            if (gridSize <= 0f) gridSize = 0.01f;
        }
        else
        {
            EditorGUILayout.HelpBox(
                "SpriteSize snaps each object to multiples of its SpriteRenderer bounds size.\n" +
                "Best for background pieces that should touch perfectly.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Snap Selected"))
        {
            SnapSelected();
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "TIP: If you still see seams, it may be camera sub-pixel or filtering.\n" +
            "Set sprites to Point filter / Pixel Perfect camera for pixel art.",
            MessageType.None
        );
    }

    private void SnapSelected()
    {
        var selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[WorldSnapper] No objects selected.");
            return;
        }

        Vector2 usedOrigin = origin;
        if (useFirstSelectedAsOrigin && selected[0] != null)
        {
            usedOrigin = selected[0].position;
        }

        Undo.RecordObjects(selected, "World Snapper");

        foreach (var t in selected)
        {
            if (t == null) continue;

            if (includeChildren)
            {
                foreach (var child in t.GetComponentsInChildren<Transform>(true))
                    SnapTransform(child, usedOrigin);
            }
            else
            {
                SnapTransform(t, usedOrigin);
            }
        }
    }

    private void SnapTransform(Transform t, Vector2 usedOrigin)
    {
        if (t == null) return;

        Vector3 p = t.position;

        if (mode == SnapMode.GridSize)
        {
            float gx = gridSize;
            float gy = gridSize;

            float x = usedOrigin.x + Mathf.Round((p.x - usedOrigin.x) / gx) * gx;
            float y = usedOrigin.y + Mathf.Round((p.y - usedOrigin.y) / gy) * gy;

            t.position = new Vector3(x, y, p.z);
            EditorUtility.SetDirty(t);
            return;
        }

        // SpriteSize mode
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            // fallback: do nothing if no sprite
            return;
        }

        // bounds size in world units (takes PPU & scaling into account)
        float w = sr.bounds.size.x;
        float h = sr.bounds.size.y;

        if (w <= 0.0001f || h <= 0.0001f) return;

        float sx = usedOrigin.x + Mathf.Round((p.x - usedOrigin.x) / w) * w;
        float sy = usedOrigin.y + Mathf.Round((p.y - usedOrigin.y) / h) * h;

        t.position = new Vector3(sx, sy, p.z);
        EditorUtility.SetDirty(t);
    }
}
