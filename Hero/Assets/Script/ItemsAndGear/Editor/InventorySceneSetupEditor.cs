#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class InventorySceneSetupEditor
{
    static InventorySceneSetupEditor()
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

        InventoryPanelUI panel = FindSceneComponent<InventoryPanelUI>(scene);
        if (panel == null || FindChildRecursive(panel.transform, "EquipmentSlots") != null)
            return;

        CompleteSetup();
    }

    private const string SlotPrefabPath = "Assets/Script/ItemsAndGear/InventoryItemSlot.prefab";
    private const string DatabasePath = "Assets/Script/ItemsAndGear/Item Database.asset";

    [MenuItem("Hero2D/Setup/Complete Inventory Equipment UI")]
    public static void CompleteSetup()
    {
        Scene scene = SceneManager.GetActiveScene();
        InventoryPanelUI panel = FindSceneComponent<InventoryPanelUI>(scene);
        PlayerInventory inventory = FindSceneComponent<PlayerInventory>(scene);
        PlayerEquipment equipment = FindSceneComponent<PlayerEquipment>(scene);
        InventorySaveSystem saveSystem = FindSceneComponent<InventorySaveSystem>(scene);

        if (panel == null || inventory == null || equipment == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Inventory Setup",
                "The active scene must contain InventoryPanelUI, PlayerInventory and PlayerEquipment.",
                "OK");
            return;
        }

        RectTransform inventoryRoot = panel.transform as RectTransform;
        Canvas canvas = panel.GetComponentInParent<Canvas>();

        if (inventoryRoot == null || canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Inventory Setup",
                "InventoryPanelUI must be on a UI object underneath a Canvas.",
                "OK");
            return;
        }

        InventoryItemSlotUI slotPrefab =
            AssetDatabase.LoadAssetAtPath<InventoryItemSlotUI>(SlotPrefabPath);

        if (slotPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Hero2D Inventory Setup",
                "InventoryItemSlot.prefab could not be found.",
                "OK");
            return;
        }

        SetObject(panel, "inventory", inventory);
        SetObject(panel, "slotPrefab", slotPrefab);

        CreateEquipmentSlots(inventoryRoot, inventory, equipment);
        CreateDragIcon(canvas.transform);
        CreateTooltip(canvas.transform, equipment);
        CreateFeedback(canvas.transform, equipment);

        ItemDatabase database = CreateOrUpdateDatabase();
        if (saveSystem != null && database != null)
        {
            SetObject(saveSystem, "inventory", inventory);
            SetObject(saveSystem, "equipment", equipment);
            SetObject(saveSystem, "database", database);
        }

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(inventory);
        EditorUtility.SetDirty(equipment);
        if (saveSystem != null) EditorUtility.SetDirty(saveSystem);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Hero2D Inventory Setup",
            "Inventory equipment slots, drag icon, tooltip, feedback and item database are connected. The scene has been saved.",
            "Done");
    }

    private static void CreateEquipmentSlots(
        RectTransform inventoryRoot,
        PlayerInventory inventory,
        PlayerEquipment equipment)
    {
        RectTransform container = GetOrCreateUIObject("EquipmentSlots", inventoryRoot);
        Stretch(container);

        CreateEquipmentSlot(container, "HelmetSlot", EquipmentSlotType.Helmet, 0,
            new Vector2(520f, 300f), "HELMET", inventory, equipment);
        CreateEquipmentSlot(container, "NecklaceSlot", EquipmentSlotType.Necklace, 0,
            new Vector2(320f, 185f), "NECKLACE", inventory, equipment);
        CreateEquipmentSlot(container, "ChestSlot", EquipmentSlotType.Chest, 0,
            new Vector2(520f, 125f), "CHEST", inventory, equipment);
        CreateEquipmentSlot(container, "GlovesSlot", EquipmentSlotType.Gloves, 0,
            new Vector2(720f, 185f), "GLOVES", inventory, equipment);
        CreateEquipmentSlot(container, "WeaponSlot", EquipmentSlotType.Weapon, 0,
            new Vector2(320f, 25f), "WEAPON", inventory, equipment);
        CreateEquipmentSlot(container, "RingSlot1", EquipmentSlotType.Ring, 0,
            new Vector2(720f, 25f), "RING I", inventory, equipment);
        CreateEquipmentSlot(container, "BootsSlot", EquipmentSlotType.Boots, 0,
            new Vector2(520f, -175f), "BOOTS", inventory, equipment);
        CreateEquipmentSlot(container, "RingSlot2", EquipmentSlotType.Ring, 1,
            new Vector2(720f, -135f), "RING II", inventory, equipment);
    }

    private static void CreateEquipmentSlot(
        RectTransform parent,
        string objectName,
        EquipmentSlotType type,
        int number,
        Vector2 position,
        string label,
        PlayerInventory inventory,
        PlayerEquipment equipment)
    {
        RectTransform root = GetOrCreateUIObject(objectName, parent);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = position;
        root.sizeDelta = new Vector2(120f, 120f);
        root.localScale = Vector3.one;

        Image background = EnsureComponent<Image>(root.gameObject);
        background.color = new Color(0.08f, 0.11f, 0.14f, 0.08f);
        background.raycastTarget = true;

        CanvasGroup group = EnsureComponent<CanvasGroup>(root.gameObject);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        ItemHoverSource hover = EnsureComponent<ItemHoverSource>(root.gameObject);
        EquipmentSlotUI slot = EnsureComponent<EquipmentSlotUI>(root.gameObject);

        RectTransform highlightRect = GetOrCreateUIObject("ValidHighlight", root);
        Stretch(highlightRect);
        Image highlight = EnsureComponent<Image>(highlightRect.gameObject);
        highlight.color = new Color(0.25f, 0.95f, 0.45f, 0.28f);
        highlight.raycastTarget = false;
        highlight.enabled = false;

        RectTransform iconRect = GetOrCreateUIObject("Icon", root);
        Stretch(iconRect, 5f);
        Image icon = EnsureComponent<Image>(iconRect.gameObject);
        icon.sprite = null;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        RectTransform labelRect = GetOrCreateUIObject("EmptyLabel", root);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 5f);
        labelRect.sizeDelta = new Vector2(0f, 22f);

        TextMeshProUGUI emptyLabel = EnsureComponent<TextMeshProUGUI>(labelRect.gameObject);
        emptyLabel.text = label;
        emptyLabel.fontSize = 15f;
        emptyLabel.alignment = TextAlignmentOptions.Center;
        emptyLabel.color = new Color(0.78f, 0.82f, 0.9f, 0.7f);
        emptyLabel.raycastTarget = false;

        SerializedObject serializedSlot = new SerializedObject(slot);
        serializedSlot.FindProperty("slotType").enumValueIndex = (int)type;
        serializedSlot.FindProperty("slotNumber").intValue = number;
        serializedSlot.FindProperty("equipment").objectReferenceValue = equipment;
        serializedSlot.FindProperty("inventory").objectReferenceValue = inventory;
        serializedSlot.FindProperty("icon").objectReferenceValue = icon;
        serializedSlot.FindProperty("validHighlight").objectReferenceValue = highlight;
        serializedSlot.FindProperty("emptyLabel").objectReferenceValue = emptyLabel;
        serializedSlot.FindProperty("canvasGroup").objectReferenceValue = group;
        serializedSlot.FindProperty("hover").objectReferenceValue = hover;
        serializedSlot.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(slot);
    }

    private static void CreateDragIcon(Transform canvasRoot)
    {
        RectTransform rect = GetOrCreateUIObject("InventoryDragIcon", canvasRoot);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(76f, 76f);
        rect.SetAsLastSibling();

        Image image = EnsureComponent<Image>(rect.gameObject);
        image.sprite = null;
        image.preserveAspect = true;
        image.raycastTarget = false;

        CanvasGroup group = EnsureComponent<CanvasGroup>(rect.gameObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        ItemDragVisualUI visual = EnsureComponent<ItemDragVisualUI>(rect.gameObject);
        SetObject(visual, "rectTransform", rect);
        SetObject(visual, "icon", image);
        SetObject(visual, "canvasGroup", group);
    }

    private static void CreateTooltip(Transform canvasRoot, PlayerEquipment equipment)
    {
        RectTransform managerRect = GetOrCreateUIObject("ItemTooltipManager", canvasRoot);
        Stretch(managerRect);
        managerRect.SetAsLastSibling();

        ItemTooltipUI tooltip = EnsureComponent<ItemTooltipUI>(managerRect.gameObject);

        RectTransform panelRect = GetOrCreateUIObject("TooltipPanel", managerRect);
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(340f, 300f);

        Image panelImage = EnsureComponent<Image>(panelRect.gameObject);
        panelImage.color = new Color(0.035f, 0.045f, 0.06f, 0.97f);
        panelImage.raycastTarget = false;

        CanvasGroup panelGroup = EnsureComponent<CanvasGroup>(panelRect.gameObject);
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;

        RectTransform titleRect = GetOrCreateUIObject("TooltipTitle", panelRect);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        titleRect.sizeDelta = new Vector2(-28f, 34f);

        TextMeshProUGUI title = EnsureComponent<TextMeshProUGUI>(titleRect.gameObject);
        title.text = "ITEM";
        title.fontSize = 24f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.TopLeft;
        title.raycastTarget = false;

        RectTransform detailsRect = GetOrCreateUIObject("TooltipDetails", panelRect);
        detailsRect.anchorMin = new Vector2(0f, 0f);
        detailsRect.anchorMax = new Vector2(1f, 1f);
        detailsRect.offsetMin = new Vector2(14f, 14f);
        detailsRect.offsetMax = new Vector2(-14f, -55f);

        TextMeshProUGUI details = EnsureComponent<TextMeshProUGUI>(detailsRect.gameObject);
        details.text = string.Empty;
        details.fontSize = 17f;
        details.alignment = TextAlignmentOptions.TopLeft;
        details.enableWordWrapping = true;
        details.overflowMode = TextOverflowModes.Overflow;
        details.color = new Color(0.82f, 0.85f, 0.92f);
        details.raycastTarget = false;

        SetObject(tooltip, "panel", panelRect);
        SetObject(tooltip, "titleText", title);
        SetObject(tooltip, "detailsText", details);
        SetObject(tooltip, "canvasGroup", panelGroup);
        SetObject(tooltip, "equipment", equipment);
    }

    private static void CreateFeedback(Transform canvasRoot, PlayerEquipment equipment)
    {
        RectTransform rect = GetOrCreateUIObject("EquipmentFeedback", canvasRoot);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -55f);
        rect.sizeDelta = new Vector2(520f, 42f);
        rect.SetAsLastSibling();

        CanvasGroup group = EnsureComponent<CanvasGroup>(rect.gameObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        TextMeshProUGUI message = EnsureComponent<TextMeshProUGUI>(rect.gameObject);
        message.text = string.Empty;
        message.fontSize = 22f;
        message.fontStyle = FontStyles.Bold;
        message.alignment = TextAlignmentOptions.Center;
        message.raycastTarget = false;

        EquipmentFeedbackUI feedback = EnsureComponent<EquipmentFeedbackUI>(rect.gameObject);
        SetObject(feedback, "equipment", equipment);
        SetObject(feedback, "messageText", message);
        SetObject(feedback, "group", group);
    }

    private static ItemDatabase CreateOrUpdateDatabase()
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition");
        System.Array.Sort(itemGuids, System.StringComparer.Ordinal);

        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty items = serializedDatabase.FindProperty("items");
        items.ClearArray();

        System.Collections.Generic.HashSet<string> usedIDs =
            new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

        int itemIndex = 0;
        foreach (string guid in itemGuids)
        {
            string itemPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.ItemID))
            {
                Debug.LogError(
                    $"[ItemDatabase] '{item.name}' has an empty Item ID and cannot be saved. " +
                    "Give it a permanent unique Item ID in the Inspector.",
                    item);
            }
            else if (!usedIDs.Add(item.ItemID))
            {
                Debug.LogError(
                    $"[ItemDatabase] Duplicate Item ID '{item.ItemID}' found on '{item.name}'. " +
                    "Every item asset needs a different permanent Item ID.",
                    item);
            }

            items.InsertArrayElementAtIndex(itemIndex);
            items.GetArrayElementAtIndex(itemIndex).objectReferenceValue = item;
            itemIndex++;
        }

        serializedDatabase.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        return database;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        T[] objects = Resources.FindObjectsOfTypeAll<T>();
        foreach (T item in objects)
        {
            if (item != null && item.gameObject.scene == scene)
                return item;
        }

        return null;
    }

    private static RectTransform GetOrCreateUIObject(string objectName, Transform parent)
    {
        Transform existing = FindChildRecursive(parent, objectName);
        if (existing != null)
            return existing as RectTransform;

        GameObject created = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, "Create " + objectName);
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName)
                return child;

            Transform nested = FindChildRecursive(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
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

        if (property != null)
        {
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
