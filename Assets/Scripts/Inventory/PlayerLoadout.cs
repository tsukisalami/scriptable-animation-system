using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Demo.Scripts.Runtime.Item;
using Demo.Scripts.Runtime.Character;
using System.Collections;

public class PlayerLoadout : MonoBehaviour
{
    [System.Serializable]
    public class LoadoutCategory
    {
        public string categoryName;
        public List<FPSItem> items = new List<FPSItem>();
        public int currentIndex = 0;
        
        // For consumable items
        public bool isConsumable;
        public int[] itemCounts; // Parallel array with items for consumable categories
        
        public FPSItem GetCurrentItem()
        {
            if (items.Count == 0) return null;
            return items[currentIndex];
        }

        public FPSItem CycleNext()
        {
            if (items.Count == 0) return null;
            
            do
            {
                currentIndex = (currentIndex + 1) % items.Count;
                // For consumables, skip if count is 0
                if (isConsumable && itemCounts[currentIndex] <= 0)
                    continue;
                return GetCurrentItem();
            } while (isConsumable && currentIndex != 0); // Prevent infinite loop
            
            return null;
        }

        public bool HasAvailableItems()
        {
            if (!isConsumable) return items.Count > 0;
            return items.Count > 0 && itemCounts.Any(count => count > 0);
        }
    }

    [Header("Equipment Categories")]
    public LoadoutCategory primaryWeapons = new() { categoryName = "Primary" };
    public LoadoutCategory secondaryWeapons = new() { categoryName = "Secondary" };
    public LoadoutCategory throwables = new() { categoryName = "Throwables", isConsumable = true };
    public LoadoutCategory specialEquipment = new() { categoryName = "Special", isConsumable = true };
    public LoadoutCategory medicalEquipment = new() { categoryName = "Medical", isConsumable = true };
    public LoadoutCategory tools = new() { categoryName = "Tools" };

    private FPSController fpsController;
    public int currentCategoryIndex = -1;

    // Add this dictionary to track category start indices
    private Dictionary<int, int> categoryStartIndices = new Dictionary<int, int>();

    private void Start()
    {
        fpsController = GetComponent<FPSController>();
        if (fpsController == null)
        {
            Debug.LogError("PlayerLoadout: No FPSController found!");
            enabled = false;
            return;
        }
    }

    public void SelectCategory(int index)
    {
        // Check if FPSController has an active action
        if (fpsController._actionState != FPSActionState.None)
        {
            return;
        }
        
        LoadoutCategory category = GetCategory(index);
        
        if (category == null || !category.HasAvailableItems())
        {
            return;
        }

        if (currentCategoryIndex == index)
        {
            FPSItem nextItem = category.CycleNext();
            if (nextItem != null)
            {
                EquipItem(nextItem);
            }
        }
        else
        {
            currentCategoryIndex = index;
            var currentItem = category.GetCurrentItem();
            EquipItem(currentItem);
        }
    }

    public LoadoutCategory GetCategory(int index)
    {
        return index switch
        {
            0 => primaryWeapons,
            1 => secondaryWeapons,
            2 => throwables,
            3 => specialEquipment,
            4 => medicalEquipment,
            5 => tools,
            _ => null
        };
    }

    private void EquipItem(FPSItem item)
    {
        if (item == null) return;
        
        int itemIndexInCategory = GetCategory(currentCategoryIndex).items.FindIndex(w => w.GetType() == item.GetType());
        
        if (itemIndexInCategory != -1)
        {
            int newIndex = categoryStartIndices[currentCategoryIndex] + itemIndexInCategory;
            Debug.Log($"Switched to {item.gameObject.name} as {GetCategory(currentCategoryIndex).categoryName}");
            fpsController.StartWeaponChange(newIndex);
        }
    }

    public void ConsumeCurrentItem()
    {
        LoadoutCategory category = GetCategory(currentCategoryIndex);
        if (category == null || !category.isConsumable) return;

        category.itemCounts[category.currentIndex]--;
        
        // If current item is depleted
        if (category.itemCounts[category.currentIndex] <= 0)
        {
            FPSItem nextItem = category.CycleNext();
            if (nextItem != null)
            {
                // If we found another item with available count, equip it
                EquipItem(nextItem);
            }
            else
            {
                // If no more items available in category, switch to primary weapon
                SelectCategory(0); // 0 is the primary weapon category
            }
        }
    }

    // Helper method to check if current item has uses remaining
    public bool HasCurrentItemUses()
    {
        LoadoutCategory category = GetCategory(currentCategoryIndex);
        if (category == null || !category.isConsumable) return true;
        
        return category.itemCounts[category.currentIndex] > 0;
    }

    // Helper method to add items to categories
    public void AddItemToCategory(LoadoutCategory category, FPSItem item, int count = 1)
    {
        category.items.Add(item);
        if (category.isConsumable)
        {
            System.Array.Resize(ref category.itemCounts, category.items.Count);
            category.itemCounts[category.items.Count - 1] = count;
        }
    }

    // Modify GetAllItems to track indices
    public List<FPSItem> GetAllItems()
    {
        var allItems = new List<FPSItem>();
        
        // Track starting index for each category
        categoryStartIndices[0] = 0;
        categoryStartIndices[1] = allItems.Count;
        allItems.AddRange(primaryWeapons.items);
        
        categoryStartIndices[1] = allItems.Count;
        allItems.AddRange(secondaryWeapons.items);
        
        categoryStartIndices[2] = allItems.Count;
        allItems.AddRange(throwables.items);
        
        categoryStartIndices[3] = allItems.Count;
        allItems.AddRange(specialEquipment.items);
        
        categoryStartIndices[4] = allItems.Count;
        allItems.AddRange(medicalEquipment.items);
        
        categoryStartIndices[5] = allItems.Count;
        allItems.AddRange(tools.items);
        
        return allItems;
    }
} 