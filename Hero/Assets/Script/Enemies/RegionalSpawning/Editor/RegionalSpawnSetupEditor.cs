#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RegionalSpawnSetupEditor
{
    private const string MenuRoot = "Hero2D/Enemies/Regional Spawning/";

    [MenuItem(MenuRoot + "Create Regional Spawn Director")]
    private static void CreateDirector()
    {
        RegionalSpawnDirector existing =
            Object.FindAnyObjectByType<RegionalSpawnDirector>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        GameObject directorObject = new GameObject("RegionalSpawnDirector");
        Undo.RegisterCreatedObjectUndo(
            directorObject,
            "Create Regional Spawn Director");
        directorObject.AddComponent<RegionalSpawnDirector>();
        Selection.activeGameObject = directorObject;
        EditorGUIUtility.PingObject(directorObject);
    }

    [MenuItem(MenuRoot + "Add Spawn Zone To Selected Simulation Root")]
    private static void AddSpawnZone()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Regional Spawn Setup",
                "Select the WorldChunk Simulation root first.",
                "OK");
            return;
        }

        GameObject zoneObject = new GameObject("EnemySpawnZone");
        Undo.RegisterCreatedObjectUndo(zoneObject, "Create Enemy Spawn Zone");
        Undo.SetTransformParent(
            zoneObject.transform,
            selected.transform,
            "Parent Enemy Spawn Zone");
        zoneObject.transform.localPosition = Vector3.zero;

        EnemySpawnZone2D zone =
            Undo.AddComponent<EnemySpawnZone2D>(zoneObject);

        GameObject dynamicRoot = new GameObject("DynamicEnemies");
        Undo.RegisterCreatedObjectUndo(dynamicRoot, "Create Dynamic Enemies Root");
        Undo.SetTransformParent(
            dynamicRoot.transform,
            zoneObject.transform,
            "Parent Dynamic Enemies Root");
        dynamicRoot.transform.localPosition = Vector3.zero;

        SerializedObject serializedZone = new SerializedObject(zone);
        serializedZone.FindProperty("dynamicEnemiesRoot").objectReferenceValue =
            dynamicRoot.transform;
        serializedZone.ApplyModifiedProperties();

        Selection.activeGameObject = zoneObject;
        EditorGUIUtility.PingObject(zoneObject);
    }

    [MenuItem(MenuRoot + "Add Enemy Entry Area To Selected Zone")]
    private static void AddEntryArea()
    {
        EnemySpawnZone2D zone = GetSelectedZone();
        if (zone == null)
        {
            EditorUtility.DisplayDialog(
                "Regional Spawn Setup",
                "Select an EnemySpawnZone object first.",
                "OK");
            return;
        }

        SerializedObject serializedZone = new SerializedObject(zone);
        SerializedProperty entries = serializedZone.FindProperty("enemies");
        int entryIndex = entries.arraySize;
        entries.InsertArrayElementAtIndex(entryIndex);

        SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
        SetString(entry, "displayName", $"Enemy {entryIndex + 1}");
        entry.FindPropertyRelative("enemyPrefab").objectReferenceValue = null;
        SetInt(entry, "desiredPopulation", 3);
        SetFloat(entry, "initialSpawnDelay", 0.5f);
        SetFloat(entry, "minimumRespawnTime", 8f);
        SetFloat(entry, "maximumRespawnTime", 14f);
        SetFloat(entry, "failedPointRetryTime", 1.5f);
        SetFloat(entry, "minimumDistanceFromPlayers", 7f);
        SetFloat(entry, "maximumDistanceFromPlayers", 28f);
        SetBool(entry, "requireOffscreen", true);
        SetFloat(entry, "offscreenPadding", 1.25f);
        SetBool(entry, "requireAllowedGround", false);
        entry.FindPropertyRelative("allowedGroundLayers").intValue = 0;
        entry.FindPropertyRelative("blockedEnvironmentLayers").intValue = 0;
        entry.FindPropertyRelative("blockedTags").arraySize = 0;
        SetFloat(entry, "enemyClearanceRadius", 0.5f);
        SetInt(entry, "spawnPointSearchAttempts", 32);

        GameObject areaObject = new GameObject(
            $"Enemy {entryIndex + 1} Spawn Area");
        Undo.RegisterCreatedObjectUndo(areaObject, "Create Enemy Spawn Area");
        Undo.SetTransformParent(
            areaObject.transform,
            zone.transform,
            "Parent Enemy Spawn Area");
        areaObject.transform.localPosition = Vector3.zero;

        BoxCollider2D area = Undo.AddComponent<BoxCollider2D>(areaObject);
        area.isTrigger = true;
        area.size = new Vector2(12f, 12f);
        entry.FindPropertyRelative("spawnArea").objectReferenceValue = area;

        serializedZone.ApplyModifiedProperties();
        EditorUtility.SetDirty(zone);
        Selection.activeGameObject = areaObject;
        EditorGUIUtility.PingObject(areaObject);
    }

    [MenuItem(
        MenuRoot + "Add Enemy Entry Area To Selected Zone",
        true)]
    private static bool ValidateAddEntryArea()
    {
        return GetSelectedZone() != null;
    }

    private static EnemySpawnZone2D GetSelectedZone()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return null;

        EnemySpawnZone2D zone = selected.GetComponent<EnemySpawnZone2D>();
        return zone != null
            ? zone
            : selected.GetComponentInParent<EnemySpawnZone2D>();
    }

    private static void SetString(
        SerializedProperty parent,
        string name,
        string value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null) property.stringValue = value;
    }

    private static void SetInt(
        SerializedProperty parent,
        string name,
        int value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null) property.intValue = value;
    }

    private static void SetFloat(
        SerializedProperty parent,
        string name,
        float value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null) property.floatValue = value;
    }

    private static void SetBool(
        SerializedProperty parent,
        string name,
        bool value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null) property.boolValue = value;
    }
}
#endif
