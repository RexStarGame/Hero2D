using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Database", menuName = "Hero2D/Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();
    public ItemDefinition Find(string id) => string.IsNullOrWhiteSpace(id) ? null : items.Find(i => i != null && i.ItemID == id);
}
