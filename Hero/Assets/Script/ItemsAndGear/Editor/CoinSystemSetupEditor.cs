#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CoinSystemSetupEditor
{
    static CoinSystemSetupEditor()
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
        if (!scene.IsValid() || !scene.isLoaded ||
            scene.path != "Assets/Scenes/SampleScene.unity")
            return;

        if (FindSceneComponent<CoinCounterUI>(scene) != null)
            return;

        CompleteCoinSystemSetup();
    }

    [MenuItem("Hero2D/Setup/Complete Enemy Coins And HUD")]
    public static void CompleteCoinSystemSetup()
    {
        Scene scene = SceneManager.GetActiveScene();
        PlayerInventory inventory = FindSceneComponent<PlayerInventory>(scene);
        InventorySaveSystem saveSystem = FindSceneComponent<InventorySaveSystem>(scene);
        Canvas canvas = FindSceneComponent<Canvas>(scene);

        if (inventory == null || canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Coin Setup",
                "The active scene needs PlayerInventory and a Canvas.",
                "OK");
            return;
        }

        PlayerWallet wallet = inventory.GetComponent<PlayerWallet>();
        if (wallet == null)
            wallet = Undo.AddComponent<PlayerWallet>(inventory.gameObject);

        CoinCounterUI counter = CreateCoinCounter(canvas.transform, wallet);

        if (saveSystem != null)
            SetObject(saveSystem, "wallet", wallet);

        int updatedPrefabs = AddEnemyCoinsToAllPrefabs();

        EditorUtility.SetDirty(wallet);
        EditorUtility.SetDirty(counter);
        if (saveSystem != null) EditorUtility.SetDirty(saveSystem);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[Coin Setup] Gameplay coin counter connected. EnemyCoins added to " +
            $"{updatedPrefabs} enemy prefab(s). Adjust Min Coins and Max Coins on each prefab.");
    }

    [MenuItem("Hero2D/Setup/Add EnemyCoins To All Enemy Prefabs")]
    public static int AddEnemyCoinsToAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab", new[] { "Assets/Script/Enemies" });

        int changed = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool prefabChanged = false;

            try
            {
                EnemyHealth enemy = root.GetComponent<EnemyHealth>();
                BossHealth boss = root.GetComponent<BossHealth>();

                if (enemy == null && boss == null)
                    continue;

                EnemyCoins coins = root.GetComponent<EnemyCoins>();
                if (coins == null)
                {
                    coins = root.AddComponent<EnemyCoins>();
                    SerializedObject serializedCoins = new SerializedObject(coins);
                    serializedCoins.FindProperty("minCoins").intValue = 1;
                    serializedCoins.FindProperty("maxCoins").intValue = 3;
                    serializedCoins.ApplyModifiedPropertiesWithoutUndo();
                    prefabChanged = true;
                }

                if (prefabChanged)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        return changed;
    }

    private static CoinCounterUI CreateCoinCounter(
        Transform canvasRoot, PlayerWallet wallet)
    {
        Transform existing = canvasRoot.Find("CoinCounterHUD");
        GameObject target;

        if (existing != null)
        {
            target = existing.gameObject;
        }
        else
        {
            target = new GameObject(
                "CoinCounterHUD",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(target, "Create Coin Counter HUD");
            target.transform.SetParent(canvasRoot, false);
        }

        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -18f);
        rect.sizeDelta = new Vector2(260f, 48f);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();

        TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
        text.text = "Coins: 0";
        text.fontSize = 27f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.color = new Color(0.92f, 0.94f, 1f);
        text.raycastTarget = false;
        text.richText = true;

        CoinCounterUI counter = target.GetComponent<CoinCounterUI>();
        if (counter == null)
            counter = Undo.AddComponent<CoinCounterUI>(target);

        SetObject(counter, "wallet", wallet);
        SetObject(counter, "coinText", text);
        return counter;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null && component.gameObject.scene == scene)
                return component;
        }

        return null;
    }

    private static void SetObject(
        Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
#endif
