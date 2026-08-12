#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class WorldTerrainPainterWindow : EditorWindow
{
    private enum PaintMode { Paint, Erase, Rectangle, Fill }

    private const int MaxFloodCells = 250000;
    private const string UndoLabel = "World Terrain Painter";

    [SerializeField] private WorldTerrainPainterProfile profile;
    [SerializeField] private WorldTerrainPainterTargets targets = new WorldTerrainPainterTargets();
    [SerializeField] private PaintMode mode;
    [SerializeField] private int terrainIndex;
    [SerializeField] private int brushSize = 1;
    [SerializeField] private bool circularBrush;
    [SerializeField] private bool scatterDecorations = true;
    [SerializeField] private bool paintCollision = true;
    [SerializeField] private bool showGridPreview = true;
    [SerializeField] private int randomSeed = 1731;
    [SerializeField] private Texture2D colorMap;
    [SerializeField] private Vector2Int colorMapOrigin;
    [SerializeField] private bool flipColorMapY = true;
    [SerializeField] private bool clearMatchedCellsBeforeImport;

    private SerializedObject serializedWindow;
    private SerializedProperty targetsProperty;
    private Vector2 scroll;
    private bool scenePaintingEnabled;
    private bool strokeActive;
    private Vector3Int rectangleStart;
    private Vector3Int hoverCell;
    private readonly HashSet<Vector3Int> paintedThisStroke = new HashSet<Vector3Int>();
    private System.Random random;
    private int undoGroup = -1;

    [MenuItem("Tools/Hero2D/World Terrain Painter %#t")]
    public static void Open()
    {
        GetWindow<WorldTerrainPainterWindow>("Terrain Painter");
    }

    private void OnEnable()
    {
        serializedWindow = new SerializedObject(this);
        targetsProperty = serializedWindow.FindProperty("targets");
        random = new System.Random(randomSeed);
        SceneView.duringSceneGui += DuringSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        EndStroke();
        SceneView.duringSceneGui -= DuringSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        Repaint();
        SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        serializedWindow.Update();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();
        DrawProfile();
        DrawTargets();
        DrawBrush();
        DrawColorMapImporter();

        EditorGUILayout.EndScrollView();
        serializedWindow.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Hero2D World Terrain Painter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Paint Tilemaps directly in the Scene view. Nothing changes until Scene Painting is enabled. " +
            "Rule Tiles can be assigned as paint tiles for automatic edges and corners.",
            MessageType.Info);

        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = scenePaintingEnabled ? new Color(0.55f, 1f, 0.55f) : Color.white;
        if (GUILayout.Button(scenePaintingEnabled ? "Scene Painting: ON" : "Enable Scene Painting", GUILayout.Height(32f)))
        {
            scenePaintingEnabled = !scenePaintingEnabled;
            EndStroke();
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = previous;
    }

    private void DrawProfile()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Terrain Palette", EditorStyles.boldLabel);
        profile = (WorldTerrainPainterProfile)EditorGUILayout.ObjectField("Profile", profile, typeof(WorldTerrainPainterProfile), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Profile"))
                CreateProfileAsset();

            using (new EditorGUI.DisabledScope(profile == null))
            {
                if (GUILayout.Button("Select Profile"))
                    Selection.activeObject = profile;
            }
        }

        if (profile == null)
        {
            EditorGUILayout.HelpBox("Create or assign a profile, then add terrain types and Tile/Rule Tile assets in its Inspector.", MessageType.Warning);
            return;
        }

        string[] terrainNames = GetTerrainNames();
        if (terrainNames.Length == 0)
        {
            EditorGUILayout.HelpBox("The profile has no terrain types yet. Select it and add Grass, Sand, Road, Water, and any other terrain you need.", MessageType.Warning);
            return;
        }

        terrainIndex = Mathf.Clamp(terrainIndex, 0, terrainNames.Length - 1);
        terrainIndex = EditorGUILayout.Popup("Terrain", terrainIndex, terrainNames);
    }

    private void DrawTargets()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Scene Tilemap Layers", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetsProperty, true);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Layered Tilemaps"))
                CreateLayeredTilemaps();

            if (GUILayout.Button("Find In Scene"))
                FindTargetsInScene();

            if (GUILayout.Button("Clear"))
                targets.Clear();
        }

        if (!targets.HasAnyTilemap())
            EditorGUILayout.HelpBox("Assign existing Tilemaps or create a safe layered setup. This never replaces existing SpriteRenderer ground.", MessageType.None);
    }

    private void DrawBrush()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
        mode = (PaintMode)GUILayout.Toolbar((int)mode, new[] { "Paint", "Erase", "Rectangle", "Fill" });

        if (mode == PaintMode.Paint || mode == PaintMode.Erase)
        {
            brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 25);
            circularBrush = EditorGUILayout.Toggle("Circular Brush", circularBrush);
        }

        scatterDecorations = EditorGUILayout.Toggle("Scatter Decorations", scatterDecorations);
        paintCollision = EditorGUILayout.Toggle("Paint Collision", paintCollision);
        showGridPreview = EditorGUILayout.Toggle("Show Brush Preview", showGridPreview);

        int newSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
        if (newSeed != randomSeed)
        {
            randomSeed = newSeed;
            random = new System.Random(randomSeed);
        }

        EditorGUILayout.HelpBox(
            "Left mouse paints. Drag for continuous strokes. Shift + left mouse temporarily erases. " +
            "Rectangle: drag between corners. Fill: click a connected area. Ctrl+Z fully restores each stroke.",
            MessageType.None);
    }

    private void DrawColorMapImporter()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Optional Color-Map Import", EditorStyles.boldLabel);
        colorMap = (Texture2D)EditorGUILayout.ObjectField("Color Map", colorMap, typeof(Texture2D), false);
        colorMapOrigin = EditorGUILayout.Vector2IntField("Origin Cell", colorMapOrigin);
        flipColorMapY = EditorGUILayout.Toggle("Flip Y", flipColorMapY);
        clearMatchedCellsBeforeImport = EditorGUILayout.Toggle("Clear Matched Cells First", clearMatchedCellsBeforeImport);

        using (new EditorGUI.DisabledScope(profile == null || colorMap == null || !targets.HasAnyTilemap()))
        {
            if (GUILayout.Button("Import Color Map", GUILayout.Height(26f)))
                ImportColorMap();
        }

        EditorGUILayout.HelpBox(
            "Each opaque image pixel becomes one cell. Its RGB color is matched to a terrain type's Map Color. " +
            "The importer reads non-readable textures safely without changing their import settings.",
            MessageType.None);
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (!scenePaintingEnabled || profile == null || profile.terrains.Count == 0)
            return;

        WorldTerrainType terrain = CurrentTerrain;
        Tilemap target = GetTargetForMode(terrain, Event.current.shift);
        if (terrain == null || target == null)
            return;

        Event current = Event.current;
        if (!TryGetCellUnderMouse(current.mousePosition, target, out hoverCell))
            return;

        if (showGridPreview)
            DrawPreview(target, terrain, current.shift);

        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
        {
            EndStroke();
            GUIUtility.hotControl = 0;
            current.Use();
            return;
        }

        if (current.alt || current.button != 0)
            return;

        if (current.type == EventType.MouseDown)
        {
            GUIUtility.hotControl = controlId;
            BeginStroke();

            if (mode == PaintMode.Rectangle)
                rectangleStart = hoverCell;
            else if (mode == PaintMode.Fill)
            {
                FloodFill(target, hoverCell, terrain, current.shift);
                EndStroke();
            }
            else
                ApplyBrush(target, hoverCell, terrain, current.shift || mode == PaintMode.Erase);

            current.Use();
        }
        else if (current.type == EventType.MouseDrag && strokeActive)
        {
            if (mode == PaintMode.Paint || mode == PaintMode.Erase)
                ApplyBrush(target, hoverCell, terrain, current.shift || mode == PaintMode.Erase);

            current.Use();
        }
        else if (current.type == EventType.MouseUp && strokeActive)
        {
            if (mode == PaintMode.Rectangle)
                ApplyRectangle(target, rectangleStart, hoverCell, terrain, current.shift);

            EndStroke();
            GUIUtility.hotControl = 0;
            current.Use();
        }

        sceneView.Repaint();
    }

    private void BeginStroke()
    {
        if (strokeActive) return;

        strokeActive = true;
        paintedThisStroke.Clear();
        Undo.IncrementCurrentGroup();
        undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoLabel);
        RegisterTargetUndo();
    }

    private void RegisterTargetUndo()
    {
        List<UnityEngine.Object> maps = new List<UnityEngine.Object>();
        foreach (WorldTerrainLayer layer in Enum.GetValues(typeof(WorldTerrainLayer)))
        {
            Tilemap map = targets.Get(layer);
            if (map != null && !maps.Contains(map)) maps.Add(map);
        }

        if (maps.Count > 0)
            Undo.RegisterCompleteObjectUndo(maps.ToArray(), UndoLabel);
    }

    private void EndStroke()
    {
        if (!strokeActive) return;

        strokeActive = false;
        paintedThisStroke.Clear();
        MarkTargetsDirty();

        if (undoGroup >= 0)
            Undo.CollapseUndoOperations(undoGroup);

        undoGroup = -1;
    }

    private void ApplyBrush(Tilemap target, Vector3Int center, WorldTerrainType terrain, bool erase)
    {
        int min = -(brushSize - 1) / 2;
        int max = brushSize / 2;
        float radius = brushSize * 0.5f;

        for (int y = min; y <= max; y++)
        {
            for (int x = min; x <= max; x++)
            {
                if (circularBrush && new Vector2(x, y).magnitude > radius)
                    continue;

                PaintCell(target, center + new Vector3Int(x, y, 0), terrain, erase);
            }
        }
    }

    private void ApplyRectangle(Tilemap target, Vector3Int from, Vector3Int to, WorldTerrainType terrain, bool erase)
    {
        int minX = Mathf.Min(from.x, to.x);
        int maxX = Mathf.Max(from.x, to.x);
        int minY = Mathf.Min(from.y, to.y);
        int maxY = Mathf.Max(from.y, to.y);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
            PaintCell(target, new Vector3Int(x, y, 0), terrain, erase);
    }

    private void FloodFill(Tilemap target, Vector3Int start, WorldTerrainType terrain, bool erase)
    {
        TileBase source = target.GetTile(start);
        if (erase && source == null) return;

        BoundsInt bounds = target.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            PaintCell(target, start, terrain, erase);
            return;
        }

        bounds.xMin -= 1;
        bounds.xMax += 1;
        bounds.yMin -= 1;
        bounds.yMax += 1;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        queue.Enqueue(start);

        while (queue.Count > 0 && visited.Count < MaxFloodCells)
        {
            Vector3Int cell = queue.Dequeue();
            if (!visited.Add(cell) || !bounds.Contains(cell)) continue;
            if (target.GetTile(cell) != source) continue;

            PaintCell(target, cell, terrain, erase);
            queue.Enqueue(cell + Vector3Int.right);
            queue.Enqueue(cell + Vector3Int.left);
            queue.Enqueue(cell + Vector3Int.up);
            queue.Enqueue(cell + Vector3Int.down);
        }

        if (visited.Count >= MaxFloodCells)
            Debug.LogWarning($"[WorldTerrainPainter] Fill stopped at the safety limit of {MaxFloodCells:N0} cells.");
    }

    private void PaintCell(Tilemap target, Vector3Int cell, WorldTerrainType terrain, bool erase)
    {
        if (!paintedThisStroke.Add(cell)) return;

        if (erase)
        {
            target.SetTile(cell, null);
            ClearDecoration(cell);
            if (targets.collision != null) targets.collision.SetTile(cell, null);
            return;
        }

        TileBase tile = terrain.PickPaintTile(random);
        if (tile == null) return;

        target.SetTile(cell, tile);

        if (scatterDecorations && targets.decorations != null && terrain.decorationChance > 0f)
        {
            ClearDecoration(cell);
            if (random.NextDouble() <= terrain.decorationChance)
            {
                TileBase decoration = terrain.PickDecorationTile(random);
                if (decoration != null)
                    SetDecoration(cell, decoration, terrain.PickDecorationScale(random));
            }
        }

        if (paintCollision && targets.collision != null && terrain.paintCollision)
            targets.collision.SetTile(cell, terrain.collisionTile != null ? terrain.collisionTile : tile);
    }

    private void SetDecoration(Vector3Int cell, TileBase decoration, float uniformScale)
    {
        Tilemap decorationMap = targets.decorations;
        if (decorationMap == null) return;

        decorationMap.SetTile(cell, decoration);
        decorationMap.RemoveTileFlags(cell, TileFlags.LockTransform);
        decorationMap.SetTransformMatrix(
            cell,
            Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(uniformScale, uniformScale, 1f)));
    }

    private void ClearDecoration(Vector3Int cell)
    {
        Tilemap decorationMap = targets.decorations;
        if (decorationMap == null) return;

        decorationMap.SetTile(cell, null);
        decorationMap.SetTransformMatrix(cell, Matrix4x4.identity);
    }

    private void DrawPreview(Tilemap target, WorldTerrainType terrain, bool temporaryErase)
    {
        Color color = temporaryErase || mode == PaintMode.Erase
            ? new Color(1f, 0.25f, 0.25f, 0.9f)
            : new Color(0.2f, 1f, 0.55f, 0.9f);
        Handles.color = color;

        if (mode == PaintMode.Rectangle && strokeActive)
        {
            Vector3Int min = Vector3Int.Min(rectangleStart, hoverCell);
            Vector3Int max = Vector3Int.Max(rectangleStart, hoverCell) + new Vector3Int(1, 1, 0);
            Vector3 worldMin = target.CellToWorld(min);
            Vector3 worldMax = target.CellToWorld(max);
            Vector3 size = worldMax - worldMin;
            Handles.DrawWireCube(worldMin + size * 0.5f, size);
            return;
        }

        int previewSize = mode == PaintMode.Fill ? 1 : brushSize;
        int minOffset = -(previewSize - 1) / 2;
        int maxOffset = previewSize / 2;
        for (int y = minOffset; y <= maxOffset; y++)
        for (int x = minOffset; x <= maxOffset; x++)
        {
            if (circularBrush && mode != PaintMode.Fill && new Vector2(x, y).magnitude > previewSize * 0.5f)
                continue;

            Vector3Int cell = hoverCell + new Vector3Int(x, y, 0);
            Vector3 center = target.GetCellCenterWorld(cell);
            Vector3 size = target.layoutGrid != null ? target.layoutGrid.cellSize : Vector3.one;
            Handles.DrawWireCube(center, size);
        }
    }

    private bool TryGetCellUnderMouse(Vector2 mousePosition, Tilemap target, out Vector3Int cell)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, target.transform.position.z));
        if (plane.Raycast(ray, out float distance))
        {
            cell = target.WorldToCell(ray.GetPoint(distance));
            return true;
        }

        cell = default;
        return false;
    }

    private Tilemap GetTargetForMode(WorldTerrainType terrain, bool shiftErase)
    {
        if (terrain == null) return null;
        return targets.Get(terrain.targetLayer);
    }

    private WorldTerrainType CurrentTerrain
    {
        get
        {
            if (profile == null || profile.terrains == null || profile.terrains.Count == 0) return null;
            terrainIndex = Mathf.Clamp(terrainIndex, 0, profile.terrains.Count - 1);
            return profile.terrains[terrainIndex];
        }
    }

    private string[] GetTerrainNames()
    {
        if (profile == null || profile.terrains == null) return Array.Empty<string>();
        string[] names = new string[profile.terrains.Count];
        for (int i = 0; i < names.Length; i++)
        {
            WorldTerrainType terrain = profile.terrains[i];
            names[i] = terrain == null || string.IsNullOrWhiteSpace(terrain.displayName)
                ? $"Terrain {i + 1}"
                : terrain.displayName;
        }
        return names;
    }

    private void CreateLayeredTilemaps()
    {
        if (!EditorSceneManager.EnsureUntitledSceneHasBeenSaved("Save the scene before creating terrain Tilemaps."))
            return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Hero2D Terrain Tilemaps");

        GameObject root = new GameObject("World Terrain Tilemaps", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(root, "Create Hero2D Terrain Tilemaps");
        targets.grid = root.GetComponent<Grid>();
        targets.grid.cellSize = profile != null
            ? new Vector3(profile.cellSize.x, profile.cellSize.y, 0f)
            : new Vector3(0.32f, 0.32f, 0f);

        targets.ground = CreateTilemap(root.transform, "Ground", -20, false);
        targets.paths = CreateTilemap(root.transform, "Paths", -19, false);
        targets.water = CreateTilemap(root.transform, "Water", -18, false);
        targets.decorations = CreateTilemap(root.transform, "Decorations", -10, false);
        targets.obstacles = CreateTilemap(root.transform, "Obstacles", -5, false);
        targets.collision = CreateTilemap(root.transform, "Terrain Collision", -30, true);

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        serializedWindow.Update();
        Repaint();
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int order, bool collider)
    {
        GameObject child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        Undo.RegisterCreatedObjectUndo(child, "Create Terrain Tilemap");
        child.transform.SetParent(parent, false);

        Tilemap tilemap = child.GetComponent<Tilemap>();
        TilemapRenderer renderer = child.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = order;

        if (collider)
        {
            renderer.enabled = false;
            TilemapCollider2D tilemapCollider = Undo.AddComponent<TilemapCollider2D>(child);
            CompositeCollider2D composite = Undo.AddComponent<CompositeCollider2D>(child);
            Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(child);
            body.bodyType = RigidbodyType2D.Static;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        return tilemap;
    }

    private void FindTargetsInScene()
    {
        Tilemap[] maps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < maps.Length; i++)
        {
            Tilemap map = maps[i];
            string value = map.name.ToLowerInvariant();
            if (targets.ground == null && value.Contains("ground")) targets.ground = map;
            else if (targets.paths == null && (value.Contains("path") || value.Contains("road"))) targets.paths = map;
            else if (targets.water == null && value.Contains("water")) targets.water = map;
            else if (targets.decorations == null && value.Contains("decor")) targets.decorations = map;
            else if (targets.obstacles == null && (value.Contains("obstacle") || value.Contains("wall"))) targets.obstacles = map;
            else if (targets.collision == null && value.Contains("collision")) targets.collision = map;
        }

        if (targets.grid == null)
            targets.grid = FindFirstObjectByType<Grid>(FindObjectsInactive.Include);

        Repaint();
    }

    private void CreateProfileAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create World Terrain Profile",
            "World Terrain Profile",
            "asset",
            "Choose where the reusable terrain palette should be saved.");
        if (string.IsNullOrEmpty(path)) return;

        WorldTerrainPainterProfile asset = CreateInstance<WorldTerrainPainterProfile>();
        asset.terrains.Add(new WorldTerrainType { displayName = "Grass", mapColor = Color.green });
        asset.terrains.Add(new WorldTerrainType { displayName = "Sand", mapColor = new Color(0.92f, 0.75f, 0.28f) });
        asset.terrains.Add(new WorldTerrainType { displayName = "Road", mapColor = new Color(0.42f, 0.25f, 0.12f), targetLayer = WorldTerrainLayer.Paths });
        asset.terrains.Add(new WorldTerrainType { displayName = "Water", mapColor = Color.blue, targetLayer = WorldTerrainLayer.Water });
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        profile = asset;
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    private void ImportColorMap()
    {
        if (profile == null || colorMap == null) return;

        Color32[] pixels;
        try
        {
            pixels = ReadPixelsWithoutChangingImporter(colorMap);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[WorldTerrainPainter] Could not read color map: {exception.Message}");
            return;
        }

        BeginStroke();
        int painted = 0;
        int unmatched = 0;
        int width = colorMap.width;
        int height = colorMap.height;

        for (int sourceY = 0; sourceY < height; sourceY++)
        for (int x = 0; x < width; x++)
        {
            Color32 pixel = pixels[sourceY * width + x];
            if (pixel.a < 16) continue;

            WorldTerrainType terrain = profile.FindClosestTerrain(pixel);
            if (terrain == null)
            {
                unmatched++;
                continue;
            }

            Tilemap target = targets.Get(terrain.targetLayer);
            if (target == null)
            {
                unmatched++;
                continue;
            }

            int y = flipColorMapY ? height - 1 - sourceY : sourceY;
            Vector3Int cell = new Vector3Int(colorMapOrigin.x + x, colorMapOrigin.y + y, 0);
            if (clearMatchedCellsBeforeImport)
            {
                target.SetTile(cell, null);
                ClearDecoration(cell);
                if (targets.collision != null) targets.collision.SetTile(cell, null);
            }

            paintedThisStroke.Remove(cell);
            PaintCell(target, cell, terrain, false);
            painted++;
        }

        EndStroke();
        Debug.Log($"[WorldTerrainPainter] Color map imported: {painted:N0} cells painted, {unmatched:N0} opaque pixels unmatched.");
    }

    private static Color32[] ReadPixelsWithoutChangingImporter(Texture2D source)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();
            Color32[] pixels = readable.GetPixels32();
            DestroyImmediate(readable);
            return pixels;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private void MarkTargetsDirty()
    {
        Scene scene = default;
        foreach (WorldTerrainLayer layer in Enum.GetValues(typeof(WorldTerrainLayer)))
        {
            Tilemap map = targets.Get(layer);
            if (map == null) continue;
            EditorUtility.SetDirty(map);
            if (!scene.IsValid()) scene = map.gameObject.scene;
        }

        if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
    }
}
#endif
