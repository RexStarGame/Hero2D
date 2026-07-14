public static class ItemDragContext
{
    public static InventoryItemSlotUI InventorySource { get; private set; }
    public static EquipmentSlotUI EquipmentSource { get; private set; }
    public static ItemDefinition DraggedItem { get; private set; }
    public static bool DropHandled { get; private set; }
    public static bool IsDragging => InventorySource != null || EquipmentSource != null;

    public static void Begin(InventoryItemSlotUI source)
    {
        InventorySource = source;
        EquipmentSource = null;
        DraggedItem = source == null ? null : source.Item;
        DropHandled = false;
        ItemTooltipUI.Instance?.Hide();
    }

    public static void Begin(EquipmentSlotUI source)
    {
        EquipmentSource = source;
        InventorySource = null;
        DraggedItem = source == null ? null : source.Item;
        DropHandled = false;
        ItemTooltipUI.Instance?.Hide();
    }

    public static void MarkDropHandled()
    {
        DropHandled = true;
    }

    public static void Clear()
    {
        InventorySource = null;
        EquipmentSource = null;
        DraggedItem = null;
        DropHandled = false;
    }
}
