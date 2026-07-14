using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventorySaveSystem : MonoBehaviour
{
    [Serializable]
    private class SlotData
    {
        public EquipmentSlotType type;
        public int number;
        public string itemID;
    }

    [Serializable]
    private class SaveData
    {
        public int saveVersion = 2;
        public int gold;
        public List<string> inventory = new List<string>();
        public List<SlotData> equipment = new List<SlotData>();
    }

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private ItemDatabase database;
    [SerializeField] private string fileName = "equipment.json";
    [SerializeField] private bool loadOnStart = true;

    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        if (equipment == null)
            equipment = GetComponent<PlayerEquipment>();

        if (wallet == null)
            wallet = GetComponent<PlayerWallet>();
    }

    private void Start()
    {
        if (loadOnStart)
            Load();
    }

    public void Save()
    {
        if (inventory == null || equipment == null)
            return;

        SaveData data = new SaveData
        {
            gold = wallet == null ? 0 : wallet.Gold
        };

        foreach (ItemDefinition item in inventory.Items)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.ItemID))
            {
                Debug.LogError(
                    $"[InventorySaveSystem] '{item.name}' has no Item ID and cannot be saved.",
                    item);
                continue;
            }

            data.inventory.Add(item.ItemID);
        }

        foreach (PlayerEquipment.EquippedSlot slot in equipment.Slots)
        {
            data.equipment.Add(new SlotData
            {
                type = slot.slotType,
                number = slot.slotNumber,
                itemID = slot.item == null ? string.Empty : slot.item.ItemID
            });
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public void Load()
    {
        if (database == null || inventory == null || equipment == null || !File.Exists(SavePath))
            return;

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data == null)
            return;

        List<ItemDefinition> restored = new List<ItemDefinition>();

        if (data.inventory != null)
        {
            foreach (string id in data.inventory)
            {
                ItemDefinition item = database.Find(id);
                if (item != null)
                {
                    restored.Add(item);
                }
                else
                {
                    Debug.LogWarning(
                        $"[InventorySaveSystem] Saved item ID '{id}' is not registered in the Item Database.");
                }
            }
        }

        inventory.ReplaceContents(restored);

        // Older save files did not contain wallet data, so keep startingGold for them.
        if (wallet != null && data.saveVersion >= 2)
            wallet.RestoreGold(data.gold);

        if (data.equipment != null)
        {
            foreach (SlotData slot in data.equipment)
            {
                EquippableItemDefinition restoredItem =
                    database.Find(slot.itemID) as EquippableItemDefinition;

                if (!string.IsNullOrWhiteSpace(slot.itemID) && restoredItem == null)
                {
                    Debug.LogWarning(
                        $"[InventorySaveSystem] Equipped item ID '{slot.itemID}' is not registered in the Item Database.");
                }

                equipment.RestoreSlot(slot.type, slot.number, restoredItem);
            }
        }

        equipment.NotifyRestored();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
