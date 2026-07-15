#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WorldStreamingSetupEditor
{
    [MenuItem("Tools/Hero2D/World Streaming/Setup Manager And Selected Player")]
    private static void SetupManagerAndPlayer()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "World Streaming",
                "Select the Player GameObject first, then run this command again.",
                "OK");
            return;
        }

        WorldStreamingTarget target = selected.GetComponent<WorldStreamingTarget>();
        if (target == null)
            target = Undo.AddComponent<WorldStreamingTarget>(selected);

        WorldChunkStreamer streamer = Object.FindObjectOfType<WorldChunkStreamer>();
        if (streamer == null)
        {
            GameObject manager = new GameObject("WorldChunkStreamer");
            Undo.RegisterCreatedObjectUndo(manager, "Create World Chunk Streamer");
            streamer = manager.AddComponent<WorldChunkStreamer>();
        }

        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(streamer);
        Selection.activeGameObject = streamer.gameObject;

        EditorUtility.DisplayDialog(
            "World Streaming Ready",
            "Added a streaming target to the selected Player and ensured that one WorldChunkStreamer manager exists. " +
            "Next, divide the map into chunk roots and add WorldChunk to each one.",
            "OK");
    }

    [MenuItem("Tools/Hero2D/World Streaming/Add WorldChunk To Selected Root")]
    private static void AddChunkToSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("World Streaming", "Select a chunk root first.", "OK");
            return;
        }

        WorldChunk chunk = selected.GetComponent<WorldChunk>();
        if (chunk == null)
            chunk = Undo.AddComponent<WorldChunk>(selected);

        EditorUtility.SetDirty(chunk);
        Selection.activeGameObject = selected;
    }
}
#endif
