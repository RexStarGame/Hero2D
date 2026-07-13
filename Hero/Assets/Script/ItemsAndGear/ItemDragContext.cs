public static class ItemDragContext
{
    public static InventoryItemSlotUI InventorySource { get; private set; }
    public static EquipmentSlotUI EquipmentSource { get; private set; }
    public static bool IsDragging => InventorySource != null || EquipmentSource != null;
    public static void Begin(InventoryItemSlotUI source) { InventorySource = source; EquipmentSource = null; ItemTooltipUI.Instance?.Hide(); }
    public static void Begin(EquipmentSlotUI source) { EquipmentSource = source; InventorySource = null; ItemTooltipUI.Instance?.Hide(); }
    public static void Clear() { InventorySource = null; EquipmentSource = null; }
}
