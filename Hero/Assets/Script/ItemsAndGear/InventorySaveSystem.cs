using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventorySaveSystem : MonoBehaviour
{
    [Serializable] private class SlotData { public EquipmentSlotType type; public int number; public string itemID; }
    [Serializable] private class SaveData { public List<string> inventory = new List<string>(); public List<SlotData> equipment = new List<SlotData>(); }
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private ItemDatabase database;
    [SerializeField] private string fileName = "equipment.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (equipment == null) equipment = GetComponent<PlayerEquipment>();
    }
    public void Save()
    {
        if (inventory == null || equipment == null) return;
        SaveData data = new SaveData();
        foreach (ItemDefinition item in inventory.Items) if (item != null) data.inventory.Add(item.ItemID);
        foreach (PlayerEquipment.EquippedSlot slot in equipment.Slots)
            data.equipment.Add(new SlotData { type = slot.slotType, number = slot.slotNumber, itemID = slot.item == null ? "" : slot.item.ItemID });
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
    public void Load()
    {
        if (database == null || !File.Exists(SavePath)) return;
        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        List<ItemDefinition> restored = new List<ItemDefinition>();
        foreach (string id in data.inventory) { ItemDefinition item = database.Find(id); if (item != null) restored.Add(item); }
        inventory.ReplaceContents(restored);
        foreach (SlotData slot in data.equipment)
            equipment.RestoreSlot(slot.type, slot.number, database.Find(slot.itemID) as EquippableItemDefinition);
        equipment.NotifyRestored();
    }
    private void OnApplicationQuit() => Save();
}
