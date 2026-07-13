using UnityEngine;

public class RingClass : MonoBehaviour
{
    class Ring
    {
        public string ringName;
        public string ringDescription;
        public int ringLevel;
        public int ringRarity;
        public int ringValue;
        public Ring(string name, string description, int level, int rarity, int value)
        {
            ringName = name;
            ringDescription = description;
            ringLevel = level;
            ringRarity = rarity;
            ringValue = value;
        }
    }
    enum RingRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
