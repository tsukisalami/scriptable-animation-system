using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Demo.Scripts.Runtime.Item;
using Demo.Scripts.Runtime.Character;
using System.Collections;
using Ballistics;

public enum ItemType
{
    Weapon,
    Consumable,
    Tool
}

[System.Serializable]
public class WeaponMagazines
{
    public FPSItem weaponReference;
    public List<MagazineData> magazineTemplates = new List<MagazineData>();
    public List<List<BulletInfo>> magazineBullets = new List<List<BulletInfo>>();
    
    // Get the magazine with the most bullets
    public (MagazineData, List<BulletInfo>) GetBestMagazine()
    {
        if (magazineTemplates.Count == 0 || magazineBullets.Count == 0) 
            return (null, null);
        
        // Find the first full magazine, or the one with the most bullets
        int bestIndex = 0;
        int bestCount = 0;
        
        for (int i = 0; i < magazineBullets.Count; i++)
        {
            int bulletCount = magazineBullets[i].Count;
            
            // If this is a full magazine, return it immediately
            if (bulletCount == magazineTemplates[i].ammoCount)
            {
                return (magazineTemplates[i], magazineBullets[i]);
            }
            
            // Otherwise, keep track of the magazine with most bullets
            if (bulletCount > bestCount)
            {
                bestCount = bulletCount;
                bestIndex = i;
            }
        }
        
        // Return the magazine with the most bullets (or null if all are empty)
        return bestCount > 0 ? (magazineTemplates[bestIndex], magazineBullets[bestIndex]) : (null, null);
    }
    
    // Check if there's any magazine with bullets
    public bool HasAmmo()
    {
        return magazineBullets.Any(mag => mag.Count > 0);
    }
}

public class PlayerLoadout : MonoBehaviour
{
    // Event triggered when a consumable item's count changes
    public event System.Action OnConsumableUsed;

    [System.Serializable]
    public class LoadoutCategory
    {
        public string categoryName;
        public List<FPSItem> items = new List<FPSItem>();
        public int currentIndex = 0;
        
        // Item types and counts
        [SerializeField] public ItemType[] itemTypes; // Type of each item
        [SerializeField] public int[] itemCounts; // Magazine count or item count
        [HideInInspector] public int[] initialItemCounts; // Track initial/maximum counts for consumables
        
        // Weapon magazines per weapon in this category
        [SerializeField] public List<List<MagazineData>> weaponMagazineTemplates = new List<List<MagazineData>>();
        [HideInInspector] public List<List<List<BulletInfo>>> weaponMagazineBullets = new List<List<List<BulletInfo>>>();
        
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
                if (itemTypes[currentIndex] == ItemType.Consumable && itemCounts[currentIndex] <= 0)
                    continue;
                return GetCurrentItem();
            } while (itemTypes[currentIndex] == ItemType.Consumable && currentIndex != 0); // Prevent infinite loop
            
            return null;
        }

        public bool HasAvailableItems()
        {
            // Check if we have any items
            if (items.Count == 0) return false;
            
            // For categories that might have consumables, check counts
            for (int i = 0; i < items.Count; i++)
            {
                if (itemTypes[i] != ItemType.Consumable || itemCounts[i] > 0)
                    return true;
            }
            
            return false;
        }
        
        public void AddItem(FPSItem item, ItemType type, int count)
        {
            items.Add(item);
            
            // Resize arrays as needed
            if (itemTypes == null)
                itemTypes = new ItemType[0];
                
            if (itemCounts == null)
                itemCounts = new int[0];
                
            if (initialItemCounts == null)
                initialItemCounts = new int[0];
                
            System.Array.Resize(ref itemTypes, items.Count);
            System.Array.Resize(ref itemCounts, items.Count);
            System.Array.Resize(ref initialItemCounts, items.Count);
            
            // Set new values
            itemTypes[items.Count - 1] = type;
            itemCounts[items.Count - 1] = count;
            initialItemCounts[items.Count - 1] = count; // Set initial count same as current count
            
            // If weapon, initialize magazine list
            if (type == ItemType.Weapon)
            {
                if (weaponMagazineTemplates == null)
                    weaponMagazineTemplates = new List<List<MagazineData>>();
                    
                while (weaponMagazineTemplates.Count < items.Count)
                {
                    weaponMagazineTemplates.Add(new List<MagazineData>());
                    
                    if (weaponMagazineBullets == null)
                        weaponMagazineBullets = new List<List<List<BulletInfo>>>();
                        
                    weaponMagazineBullets.Add(new List<List<BulletInfo>>());
                }
            }
        }
    }

    [Header("Equipment Categories")]
    public LoadoutCategory primaryWeapons = new() { categoryName = "Primary" };
    public LoadoutCategory secondaryWeapons = new() { categoryName = "Secondary" };
    public LoadoutCategory throwables = new() { categoryName = "Throwables" };
    public LoadoutCategory specialEquipment = new() { categoryName = "Special" };
    public LoadoutCategory medicalEquipment = new() { categoryName = "Medical" };
    public LoadoutCategory tools = new() { categoryName = "Tools" };

    private FPSController fpsController;
    public int currentCategoryIndex = -1;

    // Track if the magazine system has been initialized
    private bool _magazineSystemInitialized = false;

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
        
        // Initialize magazines for weapons
        InitializeWeaponMagazines();
    }
    
    // Add method to check if magazine system is initialized
    public bool IsWeaponMagazineSystemInitialized()
    {
        return _magazineSystemInitialized;
    }
    
    private void InitializeWeaponMagazines()
    {
        for (int i = 0; i < 6; i++) // For each category
        {
            var category = GetCategory(i);
            if (category == null) continue;
            
            // Initialize initialItemCounts if not already set
            if (category.initialItemCounts == null || category.initialItemCounts.Length != category.itemCounts.Length)
            {
                category.initialItemCounts = new int[category.itemCounts.Length];
                
                // Copy current counts as initial counts
                for (int j = 0; j < category.itemCounts.Length; j++)
                {
                    category.initialItemCounts[j] = category.itemCounts[j];
                }
            }
            
            // Initialize magazine bullets lists if not already created
            if (category.weaponMagazineBullets == null)
                category.weaponMagazineBullets = new List<List<List<BulletInfo>>>();
            
            // Make sure we have bullet lists for each weapon
            while (category.weaponMagazineBullets.Count < category.items.Count)
                category.weaponMagazineBullets.Add(new List<List<BulletInfo>>());
            
            for (int j = 0; j < category.items.Count; j++)
            {
                if (category.itemTypes[j] == ItemType.Weapon)
                {
                    var weapon = category.items[j] as Demo.Scripts.Runtime.Item.Weapon;
                    if (weapon != null && weapon.defaultMagazine != null)
                    {
                        int magazineCount = category.itemCounts[j];
                        
                        // Make sure we have a magazine templates list for this weapon
                        if (category.weaponMagazineTemplates.Count <= j)
                        {
                            while (category.weaponMagazineTemplates.Count <= j)
                                category.weaponMagazineTemplates.Add(new List<MagazineData>());
                        }
                        
                        // Make sure we have a bullets list for this weapon
                        if (category.weaponMagazineBullets.Count <= j)
                        {
                            while (category.weaponMagazineBullets.Count <= j)
                                category.weaponMagazineBullets.Add(new List<List<BulletInfo>>());
                        }
                        
                        // Create magazines for this weapon
                        for (int k = 0; k < magazineCount; k++)
                        {
                            var magazineData = ScriptableObject.Instantiate(weapon.defaultMagazine);
                            
                            // Add template
                            category.weaponMagazineTemplates[j].Add(magazineData);
                            
                            // Add bullets list
                            var bullets = new List<BulletInfo>();
                            MagazineExtensions.FillMagazineWithDefaultBullets(magazineData, bullets);
                            category.weaponMagazineBullets[j].Add(bullets);
                        }
                    }
                }
            }
        }
        
        // Set flag to indicate the magazine system is ready
        _magazineSystemInitialized = true;
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

    // New method for equipping without cycling
    public void SelectCategoryWithoutCycle(int index)
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

        currentCategoryIndex = index;
        var currentItem = category.GetCurrentItem();
        EquipItem(currentItem);
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
            
            // Check if this weapon is already active in the FPSController to prevent double-equipping
            if (fpsController != null && fpsController._activeWeaponIndex == newIndex)
            {
                // Skip equipping if it's already the active weapon - avoids "confirmation" behavior
                return;
            }
            
            Debug.Log($"Switched to {item.gameObject.name} as {GetCategory(currentCategoryIndex).categoryName}");
            fpsController.StartWeaponChange(newIndex);
        }
    }

    public void ConsumeCurrentItem()
    {
        LoadoutCategory category = GetCategory(currentCategoryIndex);
        if (category == null) return;
        
        // Get current item index
        int currentItemIndex = category.currentIndex;
        
        // Only consumable items can be consumed
        if (category.itemTypes[currentItemIndex] != ItemType.Consumable) return;

        category.itemCounts[currentItemIndex]--;
        
        // Invoke event when consumable is used
        OnConsumableUsed?.Invoke();
        
        // If current item is depleted
        if (category.itemCounts[currentItemIndex] <= 0)
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
        if (category == null) return true;
        
        int currentItemIndex = category.currentIndex;
        
        // Only consumable items have "uses"
        if (category.itemTypes[currentItemIndex] != ItemType.Consumable) return true;
        
        return category.itemCounts[currentItemIndex] > 0;
    }

    // Modified GetAllItems to track indices
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

    // Check if current weapon has any available magazines with ammo
    public bool CurrentWeaponHasAmmo()
    {
        if (currentCategoryIndex < 0) return false;
        
        var category = GetCategory(currentCategoryIndex);
        if (category == null) return false;
        
        int currentItemIndex = category.currentIndex;
        if (currentItemIndex >= category.items.Count) return false;
        
        // Only weapons have ammo
        if (category.itemTypes[currentItemIndex] != ItemType.Weapon) return false;
        
        // Check if any magazines have bullets
        return category.weaponMagazineBullets[currentItemIndex].Any(mag => mag.Count > 0);
    }
    
    // Get the best magazine for the current weapon
    public (MagazineData, List<BulletInfo>) GetBestMagazineForCurrentWeapon()
    {
        if (currentCategoryIndex < 0) return (null, null);
        
        var category = GetCategory(currentCategoryIndex);
        if (category == null) return (null, null);
        
        int currentItemIndex = category.currentIndex;
        if (currentItemIndex >= category.items.Count) return (null, null);
        
        // Only weapons have magazines
        if (category.itemTypes[currentItemIndex] != ItemType.Weapon) return (null, null);
        
        // Check if we have magazine lists for this weapon
        if (category.weaponMagazineTemplates.Count <= currentItemIndex || 
            category.weaponMagazineBullets.Count <= currentItemIndex) 
            return (null, null);
        
        var templates = category.weaponMagazineTemplates[currentItemIndex];
        var bullets = category.weaponMagazineBullets[currentItemIndex];
        
        if (templates.Count == 0 || bullets.Count == 0) return (null, null);
        
        // Find the magazine with the most bullets
        MagazineData bestTemplate = null;
        List<BulletInfo> bestBullets = null;
        int mostBullets = 0;
        
        for (int i = 0; i < templates.Count; i++)
        {
            if (i >= bullets.Count) continue;
            
            int bulletCount = bullets[i].Count;
            
            // If this is a full magazine, return it immediately
            if (bulletCount == templates[i].ammoCount)
            {
                return (templates[i], bullets[i]);
            }
            
            // Otherwise, keep track of the magazine with most bullets
            if (bulletCount > mostBullets)
            {
                mostBullets = bulletCount;
                bestTemplate = templates[i];
                bestBullets = bullets[i];
            }
        }
        
        // Return the magazine with the most bullets, or null if all are empty
        return mostBullets > 0 ? (bestTemplate, bestBullets) : (null, null);
    }

    // Debug method to print magazine status for a weapon
    public void DebugWeaponMagazines(int categoryIndex, int itemIndex)
    {
        var category = GetCategory(categoryIndex);
        if (category == null) 
        {
            Debug.LogError($"Category {categoryIndex} not found");
            return;
        }
        
        if (itemIndex >= category.items.Count)
        {
            Debug.LogError($"Item index {itemIndex} out of range, max: {category.items.Count}");
            return;
        }
        
        if (category.weaponMagazineTemplates.Count <= itemIndex || 
            category.weaponMagazineBullets.Count <= itemIndex)
        {
            Debug.LogError($"Magazine data missing for weapon {category.items[itemIndex].name}");
            return;
        }
        
        var magazineBullets = category.weaponMagazineBullets[itemIndex];
        var magazineTemplates = category.weaponMagazineTemplates[itemIndex];
        
        Debug.Log($"===== Weapon Magazine Debug: {category.items[itemIndex].name} =====");
        Debug.Log($"Total magazine count: {magazineBullets.Count}");
        
        for (int i = 0; i < magazineBullets.Count; i++)
        {
            Debug.Log($"Magazine {i}: {magazineBullets[i].Count}/{magazineTemplates[i].ammoCount} bullets " + 
                     (i == 0 ? "(Current)" : ""));
        }
        
        Debug.Log("=====================================");
    }
} 