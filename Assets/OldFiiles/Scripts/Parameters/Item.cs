using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item")]
public class Item : ScriptableObject
{
    [Header("Naming")]
    public string fileName;
    public string itemName;
    public string abbreviation;

    [Header("Type & Rarity")]
    public ItemType type;
    public ItemRarity rarity;

    [Header("Description")]
    [TextArea]
    public string description;

    [Header("Stats")]
    public int width;  // Width in grid cells
    public int height; // Height in grid cells
    public float weight;
    public float value;

    public enum ItemType 
    { 
        Consumables, 
        Equipment, 
        Miscellaneous 
    } 

    public enum ItemRarity 
    { 
        Common, 
        Uncommon, 
        Rare, 
        Epic, 
        Legendary 
    } 

    [Header("Files")]
    public Sprite sprite;
    public GameObject prefab;
    public AudioClip pickupSound;
    public AudioClip useSound;
    //public Event eventToTrigger;
}
