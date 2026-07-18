#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DifficultySceneSetupEditor
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static DifficultySceneSetupEditor()
    {
        EditorApplication.delayCall += TryAutomaticSetup;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += TryAutomaticSetup;
    }

    private static void TryAutomaticSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            return;

        if (FindSceneObject(scene, "DifficultyPanel") != null)
            return;

        CompleteDifficultySetup();
    }

    [MenuItem("Hero2D/Setup/Complete Difficulty System")]
    public static void CompleteDifficultySetup()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject pauseMenu = FindSceneObject(scene, "PauseMenu");
        if (pauseMenu == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Difficulty Setup",
                "Open SampleScene and make sure it contains a PauseMenu object.",
                "OK");
            return;
        }

        AddProfilesToEnemyPrefabs();
        CreateDifficultyUI(pauseMenu.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[Difficulty Setup] Added the Pause Menu difficulty selector and " +
            "EnemyDifficultyProfile to enemy/boss prefabs. Easy preserves the current baseline.");
    }

    [MenuItem("Hero2D/Setup/Add Difficulty Profiles To Enemy Prefabs")]
    public static void AddProfilesToEnemyPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab", new[] { "Assets/Script/Enemies" });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                bool isEnemy = root.GetComponentInChildren<EnemyHealth>(true) != null;
                bool isBoss = root.GetComponentInChildren<BossHealth>(true) != null;
                if ((!isEnemy && !isBoss) ||
                    root.GetComponent<EnemyDifficultyProfile>() != null)
                    continue;

                root.AddComponent<EnemyDifficultyProfile>();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void CreateDifficultyUI(Transform pauseMenu)
    {
        Transform existing = pauseMenu.Find("DifficultyPanel");
        if (existing != null)
            return;

        Vector3 normalizedScale = new Vector3(
            SafeInverse(pauseMenu.localScale.x),
            SafeInverse(pauseMenu.localScale.y),
            1f);

        Button openButton = CreateButton(
            "DifficultyButton", pauseMenu, "DIFFICULTY", new Vector2(0f, -16f));
        openButton.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 42f);
        openButton.transform.localScale = normalizedScale;

        GameObject panel = CreatePanel(
            "DifficultyPanel", pauseMenu, new Vector2(610f, 370f),
            new Color(0.035f, 0.045f, 0.055f, 0.98f));
        panel.transform.localScale = normalizedScale;
        panel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        DifficultyMenuUI menu = Undo.AddComponent<DifficultyMenuUI>(panel);
        TMP_Text title = CreateText(
            "Title", panel.transform, "DIFFICULTY", 30f,
            new Vector2(0f, 145f), new Vector2(520f, 44f));
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.76f, 0.20f);

        TMP_Text current = CreateText(
            "CurrentDifficulty", panel.transform, "Current: Easy", 21f,
            new Vector2(0f, 105f), new Vector2(520f, 34f));

        Button easy = CreateButton(
            "Easy", panel.transform, "EASY", new Vector2(-210f, 52f));
        Button normal = CreateButton(
            "Normal", panel.transform, "NORMAL", new Vector2(-70f, 52f));
        Button hard = CreateButton(
            "Hard", panel.transform, "HARD", new Vector2(70f, 52f));
        Button nightmare = CreateButton(
            "Nightmare", panel.transform, "NIGHTMARE", new Vector2(210f, 52f));

        TMP_Text summary = CreateText(
            "DifficultySummary", panel.transform,
            "Only enemy/boss health, enemy damage, and normal-enemy spawn limits change.",
            17f, new Vector2(0f, -10f), new Vector2(540f, 62f));
        summary.color = new Color(0.75f, 0.79f, 0.86f);

        Button close = CreateButton(
            "Close", panel.transform, "BACK", new Vector2(0f, -138f));

        GameObject confirmation = CreatePanel(
            "Confirmation", panel.transform, new Vector2(560f, 235f),
            new Color(0.06f, 0.07f, 0.085f, 1f));
        confirmation.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -20f);

        TMP_Text warning = CreateText(
            "Warning", confirmation.transform, "Confirm difficulty change", 18f,
            new Vector2(0f, 38f), new Vector2(500f, 125f));
        warning.color = new Color(1f, 0.83f, 0.45f);

        Button confirm = CreateButton(
            "Confirm", confirmation.transform, "CONFIRM", new Vector2(-85f, -76f));
        Button cancel = CreateButton(
            "Cancel", confirmation.transform, "CANCEL", new Vector2(85f, -76f));

        menu.Configure(
            easy, normal, hard, nightmare, current,
            confirmation, warning, confirm, cancel);

        UnityEventTools.AddPersistentListener(openButton.onClick, menu.ShowMenu);
        UnityEventTools.AddPersistentListener(close.onClick, menu.HideMenu);

        confirmation.SetActive(false);
        panel.SetActive(false);
        EditorUtility.SetDirty(menu);
    }

    private static GameObject CreatePanel(
        string name, Transform parent, Vector2 size, Color color)
    {
        GameObject go = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    private static Button CreateButton(
        string name, Transform parent, string label, Vector2 position)
    {
        GameObject go = CreatePanel(
            name, parent, new Vector2(126f, 42f),
            new Color(0.16f, 0.18f, 0.21f, 1f));
        go.GetComponent<RectTransform>().anchoredPosition = position;

        Button button = Undo.AddComponent<Button>(go);
        button.targetGraphic = go.GetComponent<Image>();

        TMP_Text text = CreateText(
            "Label", go.transform, label, 18f, Vector2.zero,
            new Vector2(118f, 38f));
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject go = new GameObject(
            name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = new Color(0.92f, 0.94f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                    return transforms[i].gameObject;
            }
        }

        return null;
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
    }
}
#endif
