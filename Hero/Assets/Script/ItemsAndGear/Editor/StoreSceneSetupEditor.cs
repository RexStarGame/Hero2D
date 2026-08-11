#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StoreSceneSetupEditor
{
    static StoreSceneSetupEditor()
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

        GameObject storeObject = FindSceneObject(scene, "Store");
        if (storeObject == null || storeObject.GetComponent<StorePanelUI>() != null)
            return;

        CompleteStoreSetup();
    }

    [MenuItem("Hero2D/Setup/Complete Store UI")]
    public static void CompleteStoreSetup()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject storeObject = FindSceneObject(scene, "Store");
        PlayerInventory inventory = FindSceneComponent<PlayerInventory>(scene);
        PlayerXP playerXP = FindSceneComponent<PlayerXP>(scene);
        InventorySaveSystem saveSystem = FindSceneComponent<InventorySaveSystem>(scene);

        if (storeObject == null || inventory == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Store Setup",
                "The active scene needs a UI GameObject named 'Store' and a PlayerInventory.",
                "OK");
            return;
        }

        RectTransform storeRect = storeObject.transform as RectTransform;
        Canvas canvas = storeObject.GetComponentInParent<Canvas>();
        if (storeRect == null || canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Store Setup",
                "Store must be a UI object underneath a Canvas.",
                "OK");
            return;
        }

        PlayerWallet wallet = inventory.GetComponent<PlayerWallet>();
        if (wallet == null)
            wallet = Undo.AddComponent<PlayerWallet>(inventory.gameObject);

        CanvasGroup group = EnsureComponent<CanvasGroup>(storeObject);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        StorePanelUI store = EnsureComponent<StorePanelUI>(storeObject);

        RectTransform overlay = GetOrCreateUIObject("StoreDynamicUI", storeRect);
        Stretch(overlay);
        overlay.SetAsLastSibling();

        TMP_Text header = CreateText(
            "StoreTitle", overlay, "RONIN SUPPLIES", 25f,
            new Vector2(0.06f, 0.84f), new Vector2(0.60f, 0.96f),
            TextAlignmentOptions.MidlineLeft, new Color(1f, 0.82f, 0.40f));

        TMP_Text balance = CreateText(
            "StoreBalance", overlay, "Gold: 0", 20f,
            new Vector2(0.64f, 0.84f), new Vector2(0.93f, 0.96f),
            TextAlignmentOptions.MidlineRight, new Color(0.88f, 0.9f, 0.95f));

        RectTransform gridRoot = GetOrCreateUIObject("StoreGrid", overlay);
        SetAnchors(gridRoot, new Vector2(0.055f, 0.10f), new Vector2(0.60f, 0.79f));

        GridLayoutGroup grid = EnsureComponent<GridLayoutGroup>(gridRoot.gameObject);
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.spacing = new Vector2(5f, 5f);
        grid.cellSize = new Vector2(72f, 72f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        StoreItemSlotUI template = CreateSlotTemplate(gridRoot);

        RectTransform detailsPanel = GetOrCreateUIObject("StoreDetails", overlay);
        SetAnchors(detailsPanel, new Vector2(0.635f, 0.34f), new Vector2(0.94f, 0.78f));

        Image preview = CreateImage(
            "PreviewIcon", detailsPanel,
            new Vector2(0.05f, 0.48f), new Vector2(0.37f, 0.95f));
        preview.enabled = false;
        preview.preserveAspect = true;
        preview.raycastTarget = false;

        TMP_Text title = CreateText(
            "SelectedItemName", detailsPanel, "Select an item", 23f,
            new Vector2(0.40f, 0.78f), new Vector2(0.97f, 0.96f),
            TextAlignmentOptions.TopLeft, new Color(1f, 0.82f, 0.40f));
        title.fontStyle = FontStyles.Bold;

        TMP_Text details = CreateText(
            "SelectedItemDetails", detailsPanel, string.Empty, 16f,
            new Vector2(0.40f, 0.05f), new Vector2(0.97f, 0.78f),
            TextAlignmentOptions.TopLeft, new Color(0.82f, 0.85f, 0.92f));
        details.enableWordWrapping = true;
        details.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text price = CreateText(
            "StorePrice", overlay, string.Empty, 20f,
            new Vector2(0.65f, 0.21f), new Vector2(0.91f, 0.30f),
            TextAlignmentOptions.MidlineLeft, Color.white);

        TMP_Text feedback = CreateText(
            "StoreFeedback", overlay, string.Empty, 16f,
            new Vector2(0.64f, 0.145f), new Vector2(0.94f, 0.21f),
            TextAlignmentOptions.Center, Color.white);

        Button buy = CreateButton(
            "BuyButton", overlay, "BUY",
            new Vector2(0.65f, 0.06f), new Vector2(0.91f, 0.145f));
        buy.interactable = false;

        Button close = CreateButton(
            "StoreCloseButton", overlay, "X",
            new Vector2(0.935f, 0.875f), new Vector2(0.975f, 0.95f));
        close.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(close.onClick, store.Close);

        SetObject(store, "inventory", inventory);
        SetObject(store, "wallet", wallet);
        SetObject(store, "playerXP", playerXP);
        SetObject(store, "inventorySaveSystem", saveSystem);
        SetObject(store, "panelGroup", group);
        SetObject(store, "gridRoot", gridRoot);
        SetObject(store, "slotTemplate", template);
        SetObject(store, "previewIcon", preview);
        SetObject(store, "titleText", title);
        SetObject(store, "detailsText", details);
        SetObject(store, "priceText", price);
        SetObject(store, "balanceText", balance);
        SetObject(store, "feedbackText", feedback);
        SetObject(store, "buyButton", buy);

        if (saveSystem != null)
            SetObject(saveSystem, "wallet", wallet);

        EditorUtility.SetDirty(store);
        EditorUtility.SetDirty(wallet);
        EditorUtility.SetDirty(group);
        if (saveSystem != null) EditorUtility.SetDirty(saveSystem);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Store Setup] Store UI, wallet, inventory purchase flow and saving are connected. " +
            "The Store stock list was intentionally left empty.",
            store);
    }

    private static StoreItemSlotUI CreateSlotTemplate(RectTransform parent)
    {
        RectTransform root = GetOrCreateUIObject("StoreSlotTemplate", parent);
        root.sizeDelta = new Vector2(72f, 72f);

        Image background = EnsureComponent<Image>(root.gameObject);
        background.color = new Color(0.035f, 0.045f, 0.06f, 0.12f);
        background.raycastTarget = true;

        RectTransform selectionRect = GetOrCreateUIObject("SelectionHighlight", root);
        Stretch(selectionRect, 1f);
        Image selection = EnsureComponent<Image>(selectionRect.gameObject);
        selection.color = new Color(1f, 0.72f, 0.16f, 0.35f);
        selection.raycastTarget = false;
        selection.enabled = false;

        RectTransform iconRect = GetOrCreateUIObject("Icon", root);
        Stretch(iconRect, 3f);
        Image icon = EnsureComponent<Image>(iconRect.gameObject);
        icon.sprite = null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        StoreItemSlotUI slot = EnsureComponent<StoreItemSlotUI>(root.gameObject);
        SetObject(slot, "icon", icon);
        SetObject(slot, "selectionHighlight", selection);

        root.gameObject.SetActive(false);
        return slot;
    }

    private static Button CreateButton(
        string name, RectTransform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = GetOrCreateUIObject(name, parent);
        SetAnchors(rect, anchorMin, anchorMax);

        Image image = EnsureComponent<Image>(rect.gameObject);
        image.color = new Color(0.18f, 0.13f, 0.06f, 0.9f);
        image.raycastTarget = true;

        Button button = EnsureComponent<Button>(rect.gameObject);
        button.targetGraphic = image;

        TMP_Text text = CreateText(
            "Label", rect, label, 20f, Vector2.zero, Vector2.one,
            TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.40f));
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static TMP_Text CreateText(
        string name, RectTransform parent, string value, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax,
        TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = GetOrCreateUIObject(name, parent);
        SetAnchors(rect, anchorMin, anchorMax);

        TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(rect.gameObject);
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    private static Image CreateImage(
        string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = GetOrCreateUIObject(name, parent);
        SetAnchors(rect, anchorMin, anchorMax);
        return EnsureComponent<Image>(rect.gameObject);
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
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

    private static RectTransform GetOrCreateUIObject(string name, Transform parent)
    {
        Transform existing = FindDirectChild(parent, name);
        if (existing != null)
            return existing as RectTransform;

        GameObject created = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, "Create " + name);
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name)
                return child;

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void SetAnchors(
        RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, float padding = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        rect.localScale = Vector3.one;
    }

    private static void SetObject(Object target, string propertyName, Object value)
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
