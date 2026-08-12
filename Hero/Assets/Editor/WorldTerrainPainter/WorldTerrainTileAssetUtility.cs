#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WorldTerrainTileAssetUtility
{
    [MenuItem("Tools/Hero2D/Terrain Painter/Create Tile Assets From Selected Sprites")]
    private static void CreateTilesFromSelectedSprites()
    {
        List<Sprite> sprites = CollectSelectedSprites();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Create Terrain Tiles",
                "Select one or more Sprite assets, or select a sliced sprite sheet in the Project window.",
                "OK");
            return;
        }

        string folder = EditorUtility.OpenFolderPanel("Choose Tile Asset Folder", "Assets", string.Empty);
        if (string.IsNullOrEmpty(folder)) return;

        string assetsRoot = Path.GetFullPath(Application.dataPath);
        string fullFolder = Path.GetFullPath(folder);
        if (fullFolder != assetsRoot && !fullFolder.StartsWith(assetsRoot + Path.DirectorySeparatorChar))
        {
            EditorUtility.DisplayDialog("Create Terrain Tiles", "Choose a folder inside this project's Assets folder.", "OK");
            return;
        }

        string assetFolder = "Assets" + fullFolder.Substring(Application.dataPath.Length).Replace('\\', '/');
        int created = 0;
        Object lastCreated = null;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite sprite = sprites[i];
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Sprite;
                tile.name = sprite.name;

                string safeName = MakeSafeFileName(sprite.name);
                string path = AssetDatabase.GenerateUniqueAssetPath($"{assetFolder}/{safeName}.asset");
                AssetDatabase.CreateAsset(tile, path);
                lastCreated = tile;
                created++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (lastCreated != null)
        {
            Selection.activeObject = lastCreated;
            EditorGUIUtility.PingObject(lastCreated);
        }

        Debug.Log($"[WorldTerrainPainter] Created {created} Tile asset(s) in {assetFolder}.");
    }

    [MenuItem("Tools/Hero2D/Terrain Painter/Create Tile Assets From Selected Sprites", true)]
    private static bool ValidateCreateTilesFromSelectedSprites()
    {
        return Selection.objects != null && Selection.objects.Length > 0;
    }

    private static List<Sprite> CollectSelectedSprites()
    {
        List<Sprite> sprites = new List<Sprite>();
        HashSet<Sprite> seen = new HashSet<Sprite>();

        Object[] selected = Selection.objects;
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] is Sprite directSprite && seen.Add(directSprite))
                sprites.Add(directSprite);

            string path = AssetDatabase.GetAssetPath(selected[i]);
            if (string.IsNullOrEmpty(path)) continue;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int j = 0; j < assets.Length; j++)
            {
                if (assets[j] is Sprite sprite && seen.Add(sprite))
                    sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites;
    }

    private static string MakeSafeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; i++)
            value = value.Replace(invalid[i], '_');

        return string.IsNullOrWhiteSpace(value) ? "Terrain Tile" : value;
    }
}
#endif
