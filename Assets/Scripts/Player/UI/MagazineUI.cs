using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Ballistics;
using System.Linq; // Added for LINQ Sort

/// <summary>
/// Dedicated script for handling magazine UI visualization
/// This is purposely kept simple and directly tied to PlayerLoadout data
/// </summary>
public class MagazineUI : MonoBehaviour
{
    [Header("References")]
    public PlayerLoadout playerLoadout;
    public RectTransform magazineContainer;

    [Header("Magazine Icon Settings")]
    public Sprite magazineOutlineSprite;
    public Sprite magazineFillSprite; // Used for weapons AND consumables
    public float magazineIconWidth = 30f;
    public float magazineIconHeight = 50f;
    public float magazineIconSpacing = 5f;
    public float magazineIconVerticalSpacing = 10f;
    public int maxIconsPerRow = 15;
    public int maxMagazineIcons = 30; // Max icons for weapons or consumables
    public Color outlineColor = Color.black;

    [Header("Magazine Colors")]
    public Color fullMagazineColor = Color.white; // Used for full mags AND available consumables
    public Color highAmmoColor = Color.yellow;      // 99-70%
    public Color mediumAmmoColor = new Color(1f, 0.5f, 0f, 1f); // Orange 70-40%
    public Color lowAmmoColor = new Color(1f, 0.3f, 0.3f, 1f);  // Light red 40-20%
    public Color criticalAmmoColor = new Color(0.8f, 0f, 0f, 1f); // Dark red 20-1%
    public Color emptyMagazineColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark grey 0%

    // The currently displayed item category and index
    private int currentCategory = -1;
    private int currentItemIndex = -1;

    // Data structure to hold outline and fill image pairs
    private class MagazineIcon
    {
        public GameObject root;
        public Image outline;
        public Image fill;
    }

    // List of magazine icons - kept simple
    private List<MagazineIcon> magazineIcons = new List<MagazineIcon>();

    private void Start()
    {
        // Find PlayerLoadout if not set
        if (playerLoadout == null)
        {
            playerLoadout = FindObjectOfType<PlayerLoadout>();
            if (playerLoadout == null)
            {
                Debug.LogError("MagazineUI: PlayerLoadout reference not found!");
                enabled = false;
                return;
            }
        }

        // Create container if needed
        if (magazineContainer == null)
        {
            Debug.LogError("MagazineUI: Magazine container not assigned!");
            enabled = false;
            return;
        }
        
        // Subscribe to events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded += OnWeaponReloaded;
        playerLoadout.OnConsumableUsed += UpdateMagazineDisplay; // Subscribe to consumable event
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded -= OnWeaponReloaded;
        if (playerLoadout != null) // Check if playerLoadout exists before unsubscribing
        {
            playerLoadout.OnConsumableUsed -= UpdateMagazineDisplay;
        }
    }

    // Update magazines after reloading with a small delay to let data sync
    private void OnWeaponReloaded()
    {
        // Add a small delay before updating to ensure PlayerLoadout has updated
        Invoke(nameof(UpdateMagazineDisplay), 0.05f); 
    }

    private void Update()
    {
        // Only update if PlayerLoadout is available
        if (playerLoadout == null) return;

        // Check if item has changed
        int newCategory = playerLoadout.currentCategoryIndex;
        int newItem = -1;

        if (newCategory >= 0)
        {
            var category = playerLoadout.GetCategory(newCategory);
            if (category != null && category.items.Count > 0) // Check if category has items
            {
                // Ensure index is valid before accessing
                newItem = Mathf.Clamp(category.currentIndex, 0, category.items.Count - 1);
            }
            else
            {
                // If category is empty or invalid, reset newItem
                newItem = -1; 
            }
        }
        else
        {
             // If no category selected, reset newItem
            newItem = -1;
        }

        // Update display if category/item changed or if no icons are currently shown (e.g., initial setup)
        if (newCategory != currentCategory || newItem != currentItemIndex || !magazineIcons.Any(icon => icon.root.activeSelf))
        {
            // Store new values
            currentCategory = newCategory;
            currentItemIndex = newItem;
            
            // Update the UI based on the new item
            UpdateMagazineDisplay();
        }
    }

    /// <summary>
    /// Central method to update the display based on the current item type.
    /// </summary>
    public void UpdateMagazineDisplay()
    {
        // If no valid item selected, hide all icons
        if (currentCategory < 0 || currentItemIndex < 0)
        {
            HideAllMagazines();
            return;
        }

        var category = playerLoadout.GetCategory(currentCategory);
        if (category == null || currentItemIndex >= category.items.Count)
        {
            HideAllMagazines();
            return;
        }

        // Determine item type and call the appropriate display method
        ItemType currentItemType = category.itemTypes[currentItemIndex];

        switch (currentItemType)
        {
            case ItemType.Weapon:
                DisplayWeaponMagazines(category, currentItemIndex);
                break;
            case ItemType.Consumable:
                DisplayConsumableItems(category, currentItemIndex);
                break;
            case ItemType.Tool:
            default:
                HideAllMagazines(); // Tools and others don't show icons
                break;
        }
    }

    /// <summary>
    /// Displays icons representing weapon magazines.
    /// </summary>
    private void DisplayWeaponMagazines(PlayerLoadout.LoadoutCategory category, int weaponIndex)
    {   
        // Make sure weapon has magazine data
        if (category.weaponMagazineTemplates == null || category.weaponMagazineBullets == null ||
            category.weaponMagazineTemplates.Count <= weaponIndex || category.weaponMagazineBullets.Count <= weaponIndex)
        {
            HideAllMagazines();
            return;
        }

        var templates = category.weaponMagazineTemplates[weaponIndex];
        var bullets = category.weaponMagazineBullets[weaponIndex];

        // Check if lists are valid and contain data
        if (templates == null || bullets == null || templates.Count == 0 || bullets.Count == 0)
        {
            HideAllMagazines();
            return;
        }

        // Sort magazines by ammo count (excluding active magazine at index 0)
        List<int> sortedIndices = new List<int>();
        for (int i = 1; i < bullets.Count; i++)
        {
            // Ensure index is valid for both lists before accessing
            if (i < templates.Count)
            {
                sortedIndices.Add(i);
            }
        }
        
        // Sort by bullet count (ascending: least bullets first)
        sortedIndices.Sort((a, b) => {
            // Add safety checks for index bounds here as well
            int bulletsA = (a < bullets.Count) ? bullets[a].Count : 0;
            int bulletsB = (b < bullets.Count) ? bullets[b].Count : 0;
            return bulletsA.CompareTo(bulletsB);
        });
        
        // Add active magazine index (0) at the end (will be rightmost)
        // Only add if it actually exists
        if (bullets.Count > 0 && templates.Count > 0) 
        {
             sortedIndices.Add(0);
        }
       
        // Determine total icons to show (limited by data and max setting)
        int totalIcons = Mathf.Min(templates.Count, maxMagazineIcons);
        
        // Ensure enough icon objects exist
        while (magazineIcons.Count < totalIcons)
        {
            CreateMagazineIcon();
        }
        
        // Update icons with magazine data
        for (int i = 0; i < totalIcons; i++)
        {
            // Get sorted index (display right-to-left, active mag rightmost)
            int sortedListIndex = sortedIndices.Count - 1 - i;
            if (sortedListIndex < 0 || sortedListIndex >= sortedIndices.Count) continue; // Safety check
            
            int magIndex = sortedIndices[sortedListIndex];
            
            // Get magazine data safely
            if (magIndex >= 0 && magIndex < templates.Count && magIndex < bullets.Count)
            {
                var template = templates[magIndex];
                var currentBullets = bullets[magIndex]; // Renamed to avoid conflict
                
                // Calculate fill percentage
                float fillPct = (template != null && template.ammoCount > 0) 
                    ? (float)currentBullets.Count / template.ammoCount 
                    : 0;
                
                // Set color and make visible
                magazineIcons[i].root.SetActive(true);
                magazineIcons[i].fill.color = GetColorForFillPercentage(fillPct);
            }
            else
            {
                // Hide if no valid data (shouldn't happen with checks, but belt-and-suspenders)
                magazineIcons[i].root.SetActive(false);
            }
        }
        
        // Hide excess icons
        for (int i = totalIcons; i < magazineIcons.Count; i++)
        {
            magazineIcons[i].root.SetActive(false);
        }
    }

    /// <summary>
    /// Displays icons representing remaining consumable items.
    /// </summary>
    private void DisplayConsumableItems(PlayerLoadout.LoadoutCategory category, int itemIndex)
    {
        // Check if initial counts are available
        if (category.initialItemCounts == null || itemIndex >= category.initialItemCounts.Length || 
            category.itemCounts == null || itemIndex >= category.itemCounts.Length)
        {
            HideAllMagazines();
            return;
        }

        // Get current count and initial/maximum count
        int currentCount = category.itemCounts[itemIndex];
        int initialCount = category.initialItemCounts[itemIndex];
        
        // Limit total icons to either initial count or max allowed
        int totalIcons = Mathf.Min(initialCount, maxMagazineIcons);

        // Ensure enough icon objects exist
        while (magazineIcons.Count < totalIcons)
        {
            CreateMagazineIcon();
        }

        // Activate all icons up to totalIcons
        for (int i = 0; i < totalIcons; i++)
        {
            magazineIcons[i].root.SetActive(true);
            
            // Determine if this icon represents an available or used item
            bool isAvailable = i < currentCount;
            
            // Set color - white for available, gray for used
            magazineIcons[i].fill.color = isAvailable ? fullMagazineColor : emptyMagazineColor;
        }

        // Hide excess icons
        for (int i = totalIcons; i < magazineIcons.Count; i++)
        {
            magazineIcons[i].root.SetActive(false);
        }
    }

    /// <summary>
    /// Hide all magazine icons
    /// </summary>
    private void HideAllMagazines()
    {
        foreach (var icon in magazineIcons)
        {
            if (icon != null && icon.root != null)
            {
                 icon.root.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Create a new magazine icon with outline and fill
    /// </summary>
    private void CreateMagazineIcon()
    {
        // Calculate position
        int index = magazineIcons.Count;
        int row = index / maxIconsPerRow;
        int posInRow = index % maxIconsPerRow;
        
        // Create icon root
        GameObject iconObj = new GameObject($"ItemIcon_{index}"); // Generic name
        iconObj.transform.SetParent(magazineContainer, false);
        
        // Set up outline image
        Image outlineImage = iconObj.AddComponent<Image>();
        outlineImage.sprite = magazineOutlineSprite;
        outlineImage.type = Image.Type.Simple;
        outlineImage.color = outlineColor;
        outlineImage.preserveAspect = true;
        
        // Set icon size and position (Right-to-left layout)
        RectTransform rect = iconObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(magazineIconWidth, magazineIconHeight);
        rect.anchorMin = new Vector2(1, 0); // Anchor bottom-right
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);     // Pivot bottom-right
        
        // Position icons from right-to-left, bottom-to-top
        rect.anchoredPosition = new Vector2(
            -posInRow * (magazineIconWidth + magazineIconSpacing),
             row * (magazineIconHeight + magazineIconVerticalSpacing)
        );
        
        // Create fill image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(iconObj.transform, false);
        
        // Set up fill image
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = magazineFillSprite; // Use the same fill sprite for now
        fillImage.type = Image.Type.Simple;
        fillImage.preserveAspect = true;
        
        // Make fill match outline size
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        // Add to list
        magazineIcons.Add(new MagazineIcon {
            root = iconObj,
            outline = outlineImage,
            fill = fillImage
        });
    }

    /// <summary>
    /// Get the appropriate color based on ammo fill percentage (Used for Weapons)
    /// </summary>
    private Color GetColorForFillPercentage(float fillPct)
    {
        // Epsilon check for near-100% to account for float precision
        if (fillPct >= 0.999f)
        {
            return fullMagazineColor;
        }
        // Directly use highAmmoColor for anything less than full but above 70%
        else if (fillPct > 0.7f)
        {
            // High ammo (99-70%) - No longer lerping from full
            return highAmmoColor;
        }
        else if (fillPct <= 0f)
        {
            return emptyMagazineColor;
        }
        else if (fillPct > 0.4f)
        {
            // Medium ammo (70-40%) - Lerp from medium to high
            float t = (fillPct - 0.4f) / 0.3f;
            return Color.Lerp(mediumAmmoColor, highAmmoColor, t);
        }
        else if (fillPct > 0.2f)
        {
            // Low ammo (40-20%) - Lerp from low to medium
            float t = (fillPct - 0.2f) / 0.2f;
            return Color.Lerp(lowAmmoColor, mediumAmmoColor, t);
        }
        else
        {
            // Critical ammo (20-1%) - Lerp from critical to low
            float t = fillPct / 0.2f;
            return Color.Lerp(criticalAmmoColor, lowAmmoColor, t);
        }
    }
} 