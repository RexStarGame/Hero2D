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

        GameObject existingPanel = FindSceneObject(scene, "DifficultyPanel");
        if (existingPanel != null)
        {
            AddProfilesToEnemyPrefabs();
            if (existingPanel.transform.Find("Extreme") == null)
                UpgradeExistingDifficultyUI(scene, existingPanel);
            return;
        }

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
        Transform existingPanel = pauseMenu.transform.Find("DifficultyPanel");
        if (existingPanel != null && existingPanel.Find("Extreme") == null)
            UpgradeExistingDifficultyUI(scene, existingPanel.gameObject);
        else if (existingPanel == null)
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
                if (!isEnemy && !isBoss)
                    continue;

                bool changed = false;
                EnemyDifficultyProfile profile =
                    root.GetComponent<EnemyDifficultyProfile>();
                if (profile == null)
                {
                    profile = root.AddComponent<EnemyDifficultyProfile>();
                    changed = true;
                }

                SerializedObject serializedProfile =
                    new SerializedObject(profile);
                SerializedProperty version =
                    serializedProfile.FindProperty("profileVersion");
                SerializedProperty automaticMidpoint =
                    serializedProfile.FindProperty("calculateExtremeAsMidpoint");

                if (version != null && version.intValue < 2)
                {
                    if (automaticMidpoint != null)
                        automaticMidpoint.boolValue = true;
                    version.intValue = 2;
                    serializedProfile.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (changed)
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
            "Easy", panel.transform, "EASY", new Vector2(-232f, 52f));
        Button normal = CreateButton(
            "Normal", panel.transform, "NORMAL", new Vector2(-116f, 52f));
        Button hard = CreateButton(
            "Hard", panel.transform, "HARD", new Vector2(0f, 52f));
        Button extreme = CreateButton(
            "Extreme", panel.transform, "EXTREME", new Vector2(116f, 52f));
        Button nightmare = CreateButton(
            "Nightmare", panel.transform, "NIGHTMARE", new Vector2(232f, 52f));

        SetDifficultyButtonWidth(easy);
        SetDifficultyButtonWidth(normal);
        SetDifficultyButtonWidth(hard);
        SetDifficultyButtonWidth(extreme);
        SetDifficultyButtonWidth(nightmare);

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
            easy, normal, hard, extreme, nightmare, current,
            confirmation, warning, confirm, cancel);

        UnityEventTools.AddPersistentListener(openButton.onClick, menu.ShowMenu);
        UnityEventTools.AddPersistentListener(close.onClick, menu.HideMenu);

        confirmation.SetActive(false);
        panel.SetActive(false);
        EditorUtility.SetDirty(menu);
    }

    private static void UpgradeExistingDifficultyUI(
        Scene scene, GameObject panel)
    {
        DifficultyMenuUI menu = panel.GetComponent<DifficultyMenuUI>();
        if (menu == null)
            menu = Undo.AddComponent<DifficultyMenuUI>(panel);

        Button easy = GetButton(panel.transform, "Easy");
        Button normal = GetButton(panel.transform, "Normal");
        Button hard = GetButton(panel.transform, "Hard");
        Button nightmare = GetButton(panel.transform, "Nightmare");

        if (easy == null || normal == null || hard == null || nightmare == null)
        {
            Debug.LogWarning(
                "[Difficulty Setup] Existing DifficultyPanel is missing one of " +
                "the original buttons. Run Complete Difficulty System manually.");
            return;
        }

        SetButtonPosition(easy, -232f);
        SetButtonPosition(normal, -116f);
        SetButtonPosition(hard, 0f);
        SetButtonPosition(nightmare, 232f);

        Button extreme = CreateButton(
            "Extreme", panel.transform, "EXTREME", new Vector2(116f, 52f));

        SetDifficultyButtonWidth(easy);
        SetDifficultyButtonWidth(normal);
        SetDifficultyButtonWidth(hard);
        SetDifficultyButtonWidth(extreme);
        SetDifficultyButtonWidth(nightmare);

        Transform confirmationTransform = panel.transform.Find("Confirmation");
        GameObject confirmation = confirmationTransform != null
            ? confirmationTransform.gameObject
            : null;

        menu.Configure(
            easy,
            normal,
            hard,
            extreme,
            nightmare,
            GetText(panel.transform, "CurrentDifficulty"),
            confirmation,
            confirmationTransform != null
                ? GetText(confirmationTransform, "Warning")
                : null,
            confirmationTransform != null
                ? GetButton(confirmationTransform, "Confirm")
                : null,
            confirmationTransform != null
                ? GetButton(confirmationTransform, "Cancel")
                : null);

        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Difficulty Setup] Existing Pause Menu upgraded with Extreme " +
            "between Hard and Nightmare.");
    }

    private static Button GetButton(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static TMP_Text GetText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static void SetButtonPosition(Button button, float x)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, 52f);
    }

    private static void SetDifficultyButtonWidth(Button button)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(108f, 42f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = 15f;
            label.GetComponent<RectTransform>().sizeDelta = new Vector2(102f, 38f);
        }
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
