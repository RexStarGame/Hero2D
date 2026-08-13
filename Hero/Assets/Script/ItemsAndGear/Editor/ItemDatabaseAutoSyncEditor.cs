#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ItemDatabaseAutoSyncEditor : AssetPostprocessor
{
    private const string DatabasePath =
        "Assets/Script/ItemsAndGear/Item Database.asset";

    private static bool syncQueued;

    [InitializeOnLoadMethod]
    private static void QueueInitialSync()
    {
        QueueSync();
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsRelevantAsset(importedAssets) ||
            ContainsRelevantAsset(deletedAssets) ||
            ContainsRelevantAsset(movedAssets) ||
            ContainsRelevantAsset(movedFromAssetPaths))
        {
            QueueSync();
        }
    }

    private static bool ContainsRelevantAsset(string[] paths)
    {
        if (paths == null)
            return false;

        foreach (string path in paths)
        {
            if (path == DatabasePath)
                continue;

            if (path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void QueueSync()
    {
        if (syncQueued)
            return;

        syncQueued = true;
        EditorApplication.delayCall += Sync;
    }

    private static void Sync()
    {
        syncQueued = false;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        ItemDatabase database =
            InventorySceneSetupEditor.CreateOrUpdateDatabase();

        if (database != null)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[ItemDatabase] All item assets are registered for saving and loading.");
        }
    }
}
#endif
