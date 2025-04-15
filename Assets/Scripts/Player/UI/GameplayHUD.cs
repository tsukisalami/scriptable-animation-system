using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using Ballistics;

public class GameplayHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public PlayerLoadout playerLoadout;

    [Header("Health UI")]
    public TMP_Text healthText;
    public Image healthBar;
    public CanvasGroup healthUICanvasGroup;

    [Header("Hotbar UI")]
    public RectTransform[] categoryContainers;
    public float categorySpacing = 100f;
    public float normalSize = 60f;
    public float expandedSize = 100f;
    public float itemSpacing = 10f;
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(1f, 1f, 1f, 0.6f);
    public Color depleteItemColor = Color.yellow;

    [Header("Magazine UI")]
    public RectTransform magazineContainer;
    public Sprite magazineOutlineSprite;  // The border sprite
    public Sprite magazineFillSprite;     // The interior fill sprite
    public float magazineIconWidth = 30f;   // Width of the magazine icon
    public float magazineIconHeight = 50f;  // Height of the magazine icon
    public float magazineIconSpacing = 5f;
    public float magazineIconVerticalSpacing = 10f;
    public int maxIconsPerRow = 15;
    public int maxMagazineIcons = 30;
    public Color outlineColor = Color.black;  // Color for the outline/border

    // Magazine colors based on ammo percentage
    public Color fullMagazineColor = Color.white;
    public Color highAmmoColor = Color.yellow;      // 99-70%
    public Color mediumAmmoColor = new Color(1f, 0.5f, 0f, 1f); // Orange 70-40%
    public Color lowAmmoColor = new Color(1f, 0.3f, 0.3f, 1f);  // Light red 40-20%
    public Color criticalAmmoColor = new Color(0.8f, 0f, 0f, 1f); // Dark red 20-1%
    public Color emptyMagazineColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark grey 0%

    // Runtime magazine icon list
    private List<Image> magazineIcons = new List<Image>();
    private Demo.Scripts.Runtime.Item.Weapon currentWeapon;

    [Header("Hotbar Visibility")]
    public float visibilityDuration = 3f;
    public float fadeOutDuration = 0.3f;
    public CanvasGroup hotbarCanvasGroup;
    
    // Add reference to FPSController for tracking equip state
    [Header("Weapon Animation")]
    [SerializeField] private Demo.Scripts.Runtime.Character.FPSController fpsController;
    private bool isInEquipTransition = false;

    [Header("Events")]
    [SerializeField] private InventoryEvents inventoryEvents;
    [SerializeField] private PlayerStateManager playerStateManager;

    [System.Serializable]
    public class CategoryUI
    {
        public Image categoryIcon;
        public TMP_Text categoryNumber;
        public TMP_Text itemNameText;
        public RectTransform itemContainer;
        public Image[] itemIcons = new Image[3];
        public TMP_Text[] itemCounts = new TMP_Text[3];
    }

    public CategoryUI[] categories;

    private int selectedCategoryIndex = -1;
    private int selectedItemIndex = -1;
    private float visibilityTimer;
    private bool isHotbarActive;
    private Coroutine fadeCoroutine;
    private int currentlyEquippedCategory = -1;
    private int currentlyEquippedItem = -1;

    [System.Serializable]
    private class MagazineIconData
    {
        public Image outlineImage;
        public Image fillImage;
        public RectTransform iconTransform;
    }

    private List<MagazineIconData> magazineIconData;

    private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("GameplayHUD: No PlayerHealth component found!");
            }
        }
        
        if (playerLoadout == null)
        {
            playerLoadout = GetComponentInParent<PlayerLoadout>();
            if (playerLoadout == null)
            {
                Debug.LogWarning("GameplayHUD: No PlayerLoadout component found!");
            }
        }

        // Try to find FPSController if not assigned
        if (fpsController == null)
        {
            fpsController = GetComponentInParent<Demo.Scripts.Runtime.Character.FPSController>();
            
            if (fpsController == null)
            {
                fpsController = FindObjectOfType<Demo.Scripts.Runtime.Character.FPSController>();
                
                if (fpsController == null)
                {
                    Debug.LogWarning("GameplayHUD: FPSController not found! Weapon equip animation sync will not work.");
                }
            }
        }

        // Subscribe to FPSController events
        if (fpsController != null)
        {
            fpsController.OnAmmoStatusChanged += HandleAmmoStatusChanged;
        }

        // Try to find PlayerStateManager if not assigned
        if (playerStateManager == null)
        {
            playerStateManager = GetComponentInParent<PlayerStateManager>();
            
            if (playerStateManager == null)
            {
                playerStateManager = FindObjectOfType<PlayerStateManager>();
                
                if (playerStateManager == null)
                {
                    Debug.LogWarning("GameplayHUD: PlayerStateManager not found! Falling back to legacy input system.");
                }
            }
        }

        // Subscribe to events
        if (inventoryEvents != null)
        {
            inventoryEvents.OnCategorySelected += HandleCategorySelected;
            inventoryEvents.OnItemSelected += HandleItemSelected;
            inventoryEvents.OnItemConsumed += HandleItemConsumed;
            inventoryEvents.OnInputStateChanged += HandleInputStateChanged;
        }
        
        // Subscribe to player state changes
        if (playerStateManager != null)
        {
            playerStateManager.OnStateChanged += HandlePlayerStateChanged;
        }

        // Validate UI setup
        ValidateUISetup();

        // Subscribe to health events if component exists
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        }

        // Initialize UI
        UpdateHealthUI();
        InitializeCategories();
        InitializeMagazineUI();

        // Set initial hotbar visibility
        if (hotbarCanvasGroup != null)
        {
            hotbarCanvasGroup.alpha = 0f;
            isHotbarActive = false;
        }
        
        // IMPORTANT: Ensure health UI is visible at all times
        ShowHealthUI();

        // Add listener for loadout changes
        if (playerLoadout != null)
        {
            // Initialize current equipment
            currentlyEquippedCategory = playerLoadout.currentCategoryIndex;
            if (currentlyEquippedCategory != -1)
            {
                var category = playerLoadout.GetCategory(currentlyEquippedCategory);
                currentlyEquippedItem = category?.currentIndex ?? -1;
            }
            
            // Initial update of magazine UI
            UpdateMagazineUI();
        }
    }

    private void ValidateUISetup()
    {
        if (hotbarCanvasGroup == null)
        {
            Debug.LogError("GameplayHUD: Hotbar CanvasGroup is not assigned!");
        }

        if (categories == null || categories.Length != 6)
        {
            Debug.LogError("GameplayHUD: Categories array must contain exactly 6 categories!");
            return;
        }

        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            if (category == null)
            {
                Debug.LogError($"GameplayHUD: Category {i} is null!");
                continue;
            }

            if (category.categoryIcon == null)
                Debug.LogError($"GameplayHUD: Category {i} is missing its category icon!");
            
            if (category.categoryNumber == null)
                Debug.LogError($"GameplayHUD: Category {i} is missing its category number text!");
            
            if (category.itemNameText == null)
                Debug.LogError($"GameplayHUD: Category {i} is missing its item name text!");
            
            if (category.itemContainer == null)
                Debug.LogError($"GameplayHUD: Category {i} is missing its item container!");

            if (category.itemIcons == null || category.itemIcons.Length != 3)
                Debug.LogError($"GameplayHUD: Category {i} must have exactly 3 item icons!");
            
            if (category.itemCounts == null || category.itemCounts.Length != 3)
                Debug.LogError($"GameplayHUD: Category {i} must have exactly 3 item count texts!");

            // Validate individual item slots
            for (int j = 0; j < 3; j++)
            {
                if (category.itemIcons[j] == null)
                    Debug.LogError($"GameplayHUD: Category {i} is missing item icon {j}!");
                
                if (category.itemCounts[j] == null)
                    Debug.LogError($"GameplayHUD: Category {i} is missing item count text {j}!");
            }
        }
    }

    private void InitializeCategories()
    {
        // Initialize category UI elements
        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            
            // Set category number
            if (category.categoryNumber != null)
                category.categoryNumber.text = (i + 1).ToString();

            // Hide item containers initially
            if (category.itemContainer != null)
                category.itemContainer.gameObject.SetActive(false);

            // Set initial opacity
            if (category.categoryIcon != null)
                category.categoryIcon.color = unselectedColor;

            // Initialize item icons and counts
            for (int j = 0; j < category.itemIcons.Length; j++)
            {
                if (category.itemIcons[j] != null)
                {
                    category.itemIcons[j].color = unselectedColor;
                    category.itemIcons[j].gameObject.SetActive(false); // Hide additional slots by default
                }
                
                if (category.itemCounts[j] != null)
                    category.itemCounts[j].gameObject.SetActive(false);
            }

            // Set initial category icon to first item's icon if available
            var loadoutCategory = playerLoadout.GetCategory(i);
            if (loadoutCategory != null && loadoutCategory.items.Count > 0)
            {
                var firstItem = loadoutCategory.items[0];
                var itemDisplay = firstItem.GetComponent<ItemDisplay>();
                if (itemDisplay != null && category.categoryIcon != null)
                {
                    category.categoryIcon.sprite = itemDisplay.inventoryIcon;
                }
            }
        }
    }

    private void Update()
    {
        // Ensure health UI is ALWAYS updated and visible
        if (playerHealth != null)
        {
            UpdateHealthUI();
            ShowHealthUI();
        }

        // Check for loadout changes
        if (playerLoadout != null)
        {
            // Track if weapon changed to update magazine UI
            bool weaponChanged = false;
            
            // Only show hotbar on number key press
            if (playerLoadout.currentCategoryIndex != currentlyEquippedCategory)
            {
                currentlyEquippedCategory = playerLoadout.currentCategoryIndex;
                if (currentlyEquippedCategory != -1)
                {
                    var category = playerLoadout.GetCategory(currentlyEquippedCategory);
                    currentlyEquippedItem = category?.currentIndex ?? -1;
                    
                    // Show hotbar but track that we're in equip transition
                    ShowHotbarDuringEquip();
                    
                    selectedCategoryIndex = currentlyEquippedCategory;
                    selectedItemIndex = currentlyEquippedItem;
                    
                    weaponChanged = true;
                }
            }
            // Check for item changes within same category
            else if (currentlyEquippedCategory != -1)
            {
                var category = playerLoadout.GetCategory(currentlyEquippedCategory);
                if (category != null && category.currentIndex != currentlyEquippedItem)
                {
                    currentlyEquippedItem = category.currentIndex;
                    
                    // Show hotbar but track that we're in equip transition
                    ShowHotbarDuringEquip();
                    
                    selectedCategoryIndex = currentlyEquippedCategory;
                    selectedItemIndex = currentlyEquippedItem;
                    
                    weaponChanged = true;
                }
            }
            
            // Update magazine UI if weapon changed or if UI was empty
            if (weaponChanged || (magazineIcons.Count == 0 && currentlyEquippedCategory != -1))
            {
                UpdateMagazineUI();
            }
            else
            {
                // Update active magazine in real-time
                UpdateActiveMagazineColor();
            }
        }

        UpdateHotbarUI();
        
        // Only update visibility timer when not in equip transition
        if (!isInEquipTransition)
        {
            UpdateHotbarVisibility();
        }
        // Check if equip transition has ended
        else if (fpsController != null && fpsController._actionState == Demo.Scripts.Runtime.Character.FPSActionState.None)
        {
            // If FPSController is no longer in WeaponChange state, the equip animation is done
            HideHotbarImmediately();
            isInEquipTransition = false;
            
            // Make sure magazine UI is updated once equip animation is complete
            UpdateMagazineUI();
        }

        // Handle scroll wheel input only when not in equip transition
        if (!isInEquipTransition)
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta != 0)
            {
                ShowHotbar();
                HandleScrollWheel(scrollDelta);
            }

            // Handle left click to equip selected item only when hotbar is fully visible
            if (Input.GetMouseButtonDown(0) && selectedCategoryIndex != -1 && IsHotbarActive())
            {
                EquipSelectedItem();
            }
            
            // Handle right click to exit hotbar mode
            if (Input.GetMouseButtonDown(1) && IsHotbarActive())
            {
                // Right-click should start the fade out process
                StartFadeOut();
                
                // If using player state manager, also return to normal state
                if (playerStateManager != null)
                {
                    playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
                }
                else if (inventoryEvents != null)
                {
                    // Legacy support
                    inventoryEvents.RaiseInputStateChanged(InputState.Normal);
                }
            }
        }
    }

    private void UpdateHotbarVisibility()
    {
        if (!isHotbarActive) return;

        visibilityTimer -= Time.deltaTime;
        if (visibilityTimer <= 0)
        {
            StartFadeOut();
        }
    }

    // New method to show hotbar during weapon equip
    private void ShowHotbarDuringEquip()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        isHotbarActive = true;
        isInEquipTransition = true;
        
        if (hotbarCanvasGroup != null)
        {
            hotbarCanvasGroup.alpha = 1f;
        }
    }
    
    // New method to immediately hide hotbar without fade
    private void HideHotbarImmediately()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        if (hotbarCanvasGroup != null)
        {
            hotbarCanvasGroup.alpha = 0f;
        }
        
        isHotbarActive = false;
        selectedCategoryIndex = -1;
        selectedItemIndex = -1;
        ResetAllCategories();
    }

    private void ShowHotbar()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;  // Clear the coroutine reference
        }

        isHotbarActive = true;
        isInEquipTransition = false; // Reset equip transition flag for manual hotbar activation
        visibilityTimer = visibilityDuration;
        
        if (hotbarCanvasGroup != null)
        {
            hotbarCanvasGroup.alpha = 1f;
        }
    }

    private void StartFadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;  // Clear the coroutine reference
        }

        fadeCoroutine = StartCoroutine(FadeOutHotbar());
    }

    private IEnumerator FadeOutHotbar()
    {
        float elapsedTime = 0f;
        float startAlpha = hotbarCanvasGroup.alpha;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            hotbarCanvasGroup.alpha = newAlpha;
            yield return null;
        }

        hotbarCanvasGroup.alpha = 0f;
        isHotbarActive = false;
        selectedCategoryIndex = -1;
        selectedItemIndex = -1;
        ResetAllCategories();
        fadeCoroutine = null;  // Clear the coroutine reference
    }

    private void ResetAllCategories()
    {
        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            if (category == null) continue;

            // Reset category size
            SetCategorySize(i, normalSize);

            // Hide item container
            if (category.itemContainer != null)
                category.itemContainer.gameObject.SetActive(false);

            // Hide item name
            if (category.itemNameText != null)
                category.itemNameText.text = "";

            // Reset icon opacity
            if (category.categoryIcon != null)
                category.categoryIcon.color = unselectedColor;

            // Hide additional item slots
            for (int j = 1; j < category.itemIcons.Length; j++)
            {
                if (category.itemIcons[j] != null)
                    category.itemIcons[j].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateHotbarUI()
    {
        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            var loadoutCategory = playerLoadout.GetCategory(i);
            
            if (loadoutCategory == null || loadoutCategory.items.Count == 0) continue;

            // Always show first item's icon as category icon
            if (category.categoryIcon != null)
            {
                var firstItem = loadoutCategory.items[0];
                var firstItemDisplay = firstItem.GetComponent<ItemDisplay>();
                if (firstItemDisplay != null)
                    category.categoryIcon.sprite = firstItemDisplay.inventoryIcon;

                // Update opacity - category icon should be highlighted if it's selected and either the only item or the first item is selected
                bool isSelected = i == selectedCategoryIndex && (loadoutCategory.items.Count == 1 || selectedItemIndex == 0);
                category.categoryIcon.color = isSelected ? selectedColor : unselectedColor;

                // Update size based on selection
                SetCategorySize(i, (i == selectedCategoryIndex) ? expandedSize : normalSize);

                // Show count for first item if it's a consumable - ALWAYS show this regardless of selection
                if (loadoutCategory.itemTypes != null && loadoutCategory.itemTypes.Length > 0 && 
                    loadoutCategory.itemTypes[0] == ItemType.Consumable && category.itemCounts[0] != null)
                {
                    // Make sure the parent containers are active
                    category.itemContainer.gameObject.SetActive(true);
                    category.itemIcons[0].gameObject.SetActive(true);
                    category.itemCounts[0].gameObject.SetActive(true);
                    
                    int count = loadoutCategory.itemCounts[0];
                    category.itemCounts[0].text = count.ToString();
                    category.itemCounts[0].color = count > 0 ? Color.green : depleteItemColor;
                }
            }

            // Hide item name by default
            if (category.itemNameText != null)
                category.itemNameText.text = "";

            // Only show expanded category info when hotbar is active and category is selected
            if (!isHotbarActive || i != selectedCategoryIndex)
            {
                // Don't disable the container if it's a consumable with a single item
                bool isConsumable = loadoutCategory.itemTypes != null && loadoutCategory.itemTypes.Length > 0 && 
                                    loadoutCategory.itemTypes[0] == ItemType.Consumable;
                
                if (category.itemContainer != null && (!isConsumable || loadoutCategory.items.Count > 1))
                    category.itemContainer.gameObject.SetActive(false);
                continue;
            }

            // Show name only for selected item in selected category
            if (category.itemNameText != null && selectedItemIndex >= 0 && selectedItemIndex < loadoutCategory.items.Count)
            {
                var selectedItem = loadoutCategory.items[selectedItemIndex];
                var itemDisplay = selectedItem.GetComponent<ItemDisplay>();
                category.itemNameText.text = itemDisplay != null ? itemDisplay.displayName : selectedItem.name;

                // Position name to the left of all items
                RectTransform nameRect = category.itemNameText.GetComponent<RectTransform>();
                float totalWidth = (loadoutCategory.items.Count - 1) * (normalSize + itemSpacing);
                nameRect.anchoredPosition = new Vector2(-totalWidth - normalSize - itemSpacing - 10, 0);
            }

            // Handle additional items display for selected category
            if (category.itemContainer != null && loadoutCategory.items.Count > 1)
            {
                category.itemContainer.gameObject.SetActive(true);
                
                // Show additional items (skip first item as it's shown as category icon)
                for (int j = 1; j < loadoutCategory.items.Count && j < category.itemIcons.Length; j++)
                {
                    if (category.itemIcons[j] != null)
                    {
                        category.itemIcons[j].gameObject.SetActive(true);
                        var itemDisplay = loadoutCategory.items[j].GetComponent<ItemDisplay>();
                        if (itemDisplay != null)
                        {
                            category.itemIcons[j].sprite = itemDisplay.inventoryIcon;
                        }
                        
                        // Update item opacity - only selected item should be full opacity
                        bool isSelected = (j == selectedItemIndex);
                        category.itemIcons[j].color = isSelected ? selectedColor : unselectedColor;
                    }

                    // Update count for consumables
                    if (loadoutCategory.itemTypes != null && loadoutCategory.itemTypes.Length > j && 
                        loadoutCategory.itemTypes[j] == ItemType.Consumable && category.itemCounts[j] != null)
                    {
                        category.itemCounts[j].gameObject.SetActive(true);
                        int count = loadoutCategory.itemCounts[j];
                        category.itemCounts[j].text = count.ToString();
                        category.itemCounts[j].color = count > 0 ? Color.green : depleteItemColor;
                    }
                }

                // Hide unused item slots
                for (int j = loadoutCategory.items.Count; j < category.itemIcons.Length; j++)
                {
                    if (category.itemIcons[j] != null)
                        category.itemIcons[j].gameObject.SetActive(false);
                    if (category.itemCounts[j] != null)
                        category.itemCounts[j].gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"Health: {playerHealth.GetHealthAsInt()}";
        
        if (healthBar != null)
            healthBar.fillAmount = playerHealth.GetHealthAsInt() / 100f;
    }

    private void HandleHealthStateChanged(PlayerHealth.HealthState newState)
    {
        if (healthText == null) return;

        switch (newState)
        {
            case PlayerHealth.HealthState.Critical:
                healthText.color = Color.red;
                break;
            case PlayerHealth.HealthState.Weak:
                healthText.color = Color.yellow;
                break;
            case PlayerHealth.HealthState.Healthy:
                healthText.color = Color.green;
                break;
        }
    }

    public void SelectCategory(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Length)
            return;

        // Don't allow category selection during equip transition
        if (isInEquipTransition)
            return;

        // Keybind selection mode - track it as an equip transition
        isInEquipTransition = true;
        
        // Show hotbar during equip
        ShowHotbarDuringEquip();

        // Use PlayerStateManager if available to set hotbar state
        if (playerStateManager != null)
        {
            playerStateManager.SetState(PlayerStateManager.PlayerState.Hotbar);
        }

        // If selecting the same category
        if (categoryIndex == selectedCategoryIndex)
        {
            // For primary and secondary, don't cycle
            if (categoryIndex <= 1) return;

            // For other categories, cycle through items
            var category = playerLoadout.GetCategory(categoryIndex);
            if (category != null && category.HasAvailableItems())
            {
                var nextItem = category.CycleNext();
                if (nextItem != null)
                {
                    selectedItemIndex = category.currentIndex;
                    currentlyEquippedItem = selectedItemIndex;
                    EquipSelectedItem();
                }
            }
        }
        else
        {
            // Selecting a new category
            var category = playerLoadout.GetCategory(categoryIndex);
            if (category == null || !category.HasAvailableItems())
                return;

            // Reset previous category
            if (selectedCategoryIndex != -1 && selectedCategoryIndex < categories.Length)
            {
                SetCategorySize(selectedCategoryIndex, normalSize);
                if (categories[selectedCategoryIndex].itemContainer != null)
                    categories[selectedCategoryIndex].itemContainer.gameObject.SetActive(false);
            }

            selectedCategoryIndex = categoryIndex;
            selectedItemIndex = category.currentIndex;
            currentlyEquippedCategory = categoryIndex;
            currentlyEquippedItem = selectedItemIndex;
            
            // Expand new category
            SetCategorySize(categoryIndex, expandedSize);
            if (categories[categoryIndex].itemContainer != null)
                categories[categoryIndex].itemContainer.gameObject.SetActive(true);
            
            // Equip first item in category
            EquipSelectedItem();
        }
    }

    private void HandleScrollWheel(float scrollDelta)
    {
        if (categories.Length == 0) return;

        // If nothing is selected, select the first category
        if (selectedCategoryIndex == -1)
        {
            SelectCategory(0);
            return;
        }

        int direction = scrollDelta > 0 ? -1 : 1;
        
        // Get current category
        var currentCategory = playerLoadout.GetCategory(selectedCategoryIndex);
        if (currentCategory == null) return;

        // Try to move within current category first
        int newItemIndex = selectedItemIndex + direction;
        
        // If we can't move within current category, move to next/previous category
        if (newItemIndex < 0 || newItemIndex >= currentCategory.items.Count)
        {
            int newCategoryIndex = selectedCategoryIndex + direction;
            
            // Handle category wrapping
            if (newCategoryIndex < 0)
            {
                newCategoryIndex = categories.Length - 1;
                var lastCategory = playerLoadout.GetCategory(newCategoryIndex);
                if (lastCategory != null)
                    newItemIndex = lastCategory.items.Count - 1;
            }
            else if (newCategoryIndex >= categories.Length)
            {
                newCategoryIndex = 0;
                newItemIndex = 0;
            }

            // Only select the new category if it has items
            var newCategory = playerLoadout.GetCategory(newCategoryIndex);
            if (newCategory != null && newCategory.HasAvailableItems())
            {
                selectedCategoryIndex = newCategoryIndex;
                selectedItemIndex = direction > 0 ? 0 : newCategory.items.Count - 1;
            }
        }
        else
        {
            // Stay in current category and update selected item
            selectedItemIndex = newItemIndex;
        }
    }

    private void SetCategorySize(int categoryIndex, float size)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Length)
            return;

        var category = categories[categoryIndex];
        if (category == null || category.categoryIcon == null) return;

        category.categoryIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
    }

    private void EquipSelectedItem()
    {
        if (selectedCategoryIndex == -1 || playerLoadout == null) return;

        var category = playerLoadout.GetCategory(selectedCategoryIndex);
        if (category == null || selectedItemIndex >= category.items.Count) return;

        // Only equip if the item has uses (for consumables)
        if (category.itemTypes != null && category.itemTypes.Length > 0 && category.itemTypes[0] == ItemType.Consumable && !playerLoadout.HasCurrentItemUses())
            return;

        // Set the selected item as current in the category WITHOUT cycling
        category.currentIndex = selectedItemIndex;
        currentlyEquippedItem = selectedItemIndex;
        
        // Tell playerLoadout to equip this category without cycling
        playerLoadout.SelectCategoryWithoutCycle(selectedCategoryIndex);
        
        // Return to normal state after equipping
        if (playerStateManager != null)
        {
            playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
        }
        
        // For manual selection (not keybind), start fade out
        if (!isInEquipTransition)
        {
            // Immediately start fade out after equipping
            StartFadeOut();
        }
        // For keybind selection, the hotbar will be hidden when equip animation finishes
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        }

        if (inventoryEvents != null)
        {
            inventoryEvents.OnCategorySelected -= HandleCategorySelected;
            inventoryEvents.OnItemSelected -= HandleItemSelected;
            inventoryEvents.OnItemConsumed -= HandleItemConsumed;
            inventoryEvents.OnInputStateChanged -= HandleInputStateChanged;
        }
        
        // Unsubscribe from player state changes
        if (playerStateManager != null)
        {
            playerStateManager.OnStateChanged -= HandlePlayerStateChanged;
        }
        
        // Unsubscribe from weapon events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded -= OnWeaponReloaded;
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponFired -= OnWeaponFired;
        
        // Unsubscribe from FPSController events
        if (fpsController != null)
        {
            fpsController.OnAmmoStatusChanged -= HandleAmmoStatusChanged;
        }
    }

    private void HandleCategorySelected(int categoryIndex)
    {
        SelectCategory(categoryIndex);
    }

    private void HandleItemSelected(int categoryIndex, int itemIndex)
    {
        if (categoryIndex == selectedCategoryIndex)
        {
            selectedItemIndex = itemIndex;
            EquipSelectedItem();
        }
    }

    private void HandleItemConsumed(int categoryIndex)
    {
        // Update the UI for the consumed item
        if (categoryIndex >= 0 && categoryIndex < categories.Length)
        {
            var category = categories[categoryIndex];
            var loadoutCategory = playerLoadout.GetCategory(categoryIndex);
            
            if (loadoutCategory != null && loadoutCategory.itemTypes != null && loadoutCategory.itemTypes.Length > 0 && 
                loadoutCategory.itemTypes[0] == ItemType.Consumable)
            {
                int count = loadoutCategory.itemCounts[0];
                if (category.itemCounts[0] != null)
                {
                    category.itemCounts[0].text = count.ToString();
                    category.itemCounts[0].color = count > 0 ? Color.green : depleteItemColor;
                }
            }
        }
    }

    private void HandleInputStateChanged(InputState newState)
    {
        // If using new system, defer to that handler instead
        if (playerStateManager != null) return;
        
        switch (newState)
        {
            case InputState.HotbarActive:
                ShowHotbar();
                break;
            case InputState.Normal:
                if (isHotbarActive)
                    StartFadeOut();
                break;
        }
    }

    public bool IsHotbarActive()
    {
        // Consider hotbar active when either the internal flag is set OR it's at full opacity
        // When in equip transition, don't allow clicking even if hotbar is visible
        return isHotbarActive && !isInEquipTransition;
    }

    // Handler for the new player state system
    private void HandlePlayerStateChanged(PlayerStateManager.PlayerState newState, PlayerStateManager.PlayerState oldState)
    {
        // Always ensure health UI is visible regardless of state
        ShowHealthUI();
        
        switch (newState)
        {
            case PlayerStateManager.PlayerState.Hotbar:
                ShowHotbar();
                break;
            case PlayerStateManager.PlayerState.Normal:
                // Only fade out hotbar if it's active
                if (isHotbarActive)
                    StartFadeOut();
                break;
            // Don't hide UI elements in build mode or other states
            // Just leave them as they are
        }
    }

    // Event handler for ammo status changes
    private void HandleAmmoStatusChanged(bool isOutOfAmmo)
    {
        // Update the magazine UI when ammo status changes
        UpdateMagazineUI();
    }
    
    // Method to update UI when a reload occurs - call this from Weapon.cs after reload
    public void UpdateMagazineUIAfterReload()
    {
        // Update Magazine UI after short delay to ensure data is updated
        StartCoroutine(DelayedMagazineUIUpdate());
    }
    
    private IEnumerator DelayedMagazineUIUpdate()
    {
        // Small delay to ensure magazine data is updated
        yield return new WaitForSeconds(0.1f);
        UpdateMagazineUI();
    }
    
    // Method to hook into weapon events
    private void OnEnable()
    {
        // Subscribe to static weapon events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded += OnWeaponReloaded;
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponFired += OnWeaponFired;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from static weapon events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded -= OnWeaponReloaded;
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponFired -= OnWeaponFired;
        
        // Unsubscribe from FPSController events if needed
        if (fpsController != null)
        {
            fpsController.OnAmmoStatusChanged -= HandleAmmoStatusChanged;
        }
    }
    
    private void OnWeaponReloaded()
    {
        // When a reload happens, wait a short moment for PlayerLoadout to update
        StartCoroutine(DelayedReloadUpdate());
    }
    
    private IEnumerator DelayedReloadUpdate()
    {
        // Small delay to ensure PlayerLoadout has updated magazine data
        yield return new WaitForSeconds(0.1f);
        
        // Get the magazine data directly from PlayerLoadout
        if (currentlyEquippedCategory >= 0 && playerLoadout != null)
        {
            var category = playerLoadout.GetCategory(currentlyEquippedCategory);
            if (category != null && category.currentIndex < category.items.Count)
            {
                int weaponIndex = category.currentIndex;
                
                if (category.weaponMagazineTemplates.Count > weaponIndex &&
                    category.weaponMagazineBullets.Count > weaponIndex &&
                    category.weaponMagazineTemplates[weaponIndex].Count > 0 &&
                    category.weaponMagazineBullets[weaponIndex].Count > 0)
                {
                    var activeMagazine = category.weaponMagazineTemplates[weaponIndex][0];
                    var activeBullets = category.weaponMagazineBullets[weaponIndex][0];
                    
                    float fillPercentage = 0;
                    if (activeMagazine.ammoCount > 0)
                    {
                        fillPercentage = (float)activeBullets.Count / activeMagazine.ammoCount;
                    }
                    
                    // Update the active magazine color (rightmost icon)
                    if (magazineIcons.Count > 0)
                    {
                        int activeIconIndex = magazineIcons.Count - 1;
                        Color newColor = GetMagazineColor(fillPercentage);
                        
                        Debug.Log($"After Reload from PlayerLoadout: Bullets: {activeBullets.Count}/{activeMagazine.ammoCount}, " +
                                 $"Fill %: {fillPercentage}, Color: {newColor}");
                        
                        // Force color update
                        magazineIcons[activeIconIndex].color = newColor;
                    }
                }
            }
        }
    }
    
    private void OnWeaponFired()
    {
        // Update the active magazine color when weapon is fired
        UpdateActiveMagazineColor();
    }

    // ADD A NEW METHOD FOR HEALTH UI VISIBILITY
    private void ShowHealthUI()
    {
        // Make sure health UI elements are visible regardless of player state
        if (healthText != null)
        {
            // Ensure the health text's parent objects are active
            Transform parent = healthText.transform.parent;
            while (parent != null)
            {
                parent.gameObject.SetActive(true);
                
                // Check if this parent has a CanvasGroup and ensure it's visible
                CanvasGroup cg = parent.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                parent = parent.parent;
            }
            
            // Ensure the health text itself is active and visible
            healthText.gameObject.SetActive(true);
        }
        
        if (healthBar != null)
        {
            // Ensure the health bar's parent objects are active
            Transform parent = healthBar.transform.parent;
            while (parent != null)
            {
                parent.gameObject.SetActive(true);
                
                // Check if this parent has a CanvasGroup and ensure it's visible
                CanvasGroup cg = parent.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                parent = parent.parent;
            }
            
            // Ensure the health bar itself is active and visible
            healthBar.gameObject.SetActive(true);
        }
        
        // If we have a direct reference to the health UI canvas group, ensure it's visible
        if (healthUICanvasGroup != null)
        {
            healthUICanvasGroup.alpha = 1f;
            healthUICanvasGroup.interactable = true;
            healthUICanvasGroup.blocksRaycasts = true;
        }
    }

    #region Magazine UI Methods
    
    // Initialize magazine UI
    private void InitializeMagazineUI()
    {
        // Clear any existing magazine icons
        ClearMagazineIcons();
        
        // Make sure we have the magazine container
        if (magazineContainer == null)
        {
            Debug.LogError("Magazine container is not assigned in the inspector!");
            return;
        }
    }
    
    // Update the magazine icons when a weapon is equipped
    public void UpdateMagazineUI()
    {
        // If no container, initialize first
        if (magazineContainer == null) return;
        
        // Make sure the container is active
        magazineContainer.gameObject.SetActive(true);
        
        // Get the current weapon and its magazine info
        if (currentlyEquippedCategory < 0 || playerLoadout == null) return;
        
        var category = playerLoadout.GetCategory(currentlyEquippedCategory);
        if (category == null || category.currentIndex >= category.items.Count) return;
        
        // Only weapons have magazines
        if (category.itemTypes[category.currentIndex] != ItemType.Weapon) 
        {
            // Hide magazine UI for non-weapons
            magazineContainer.gameObject.SetActive(false);
            return;
        }
        
        // Try to get the weapon component
        var weaponItem = category.items[category.currentIndex] as Demo.Scripts.Runtime.Item.Weapon;
        if (weaponItem == null) return;
        
        // Store reference to current weapon
        currentWeapon = weaponItem;
        
        // Get magazine data for this weapon
        int weaponIndex = category.currentIndex;
        
        // Make sure we have magazine data
        if (category.weaponMagazineTemplates.Count <= weaponIndex || 
            category.weaponMagazineBullets.Count <= weaponIndex)
        {
            ClearMagazineIcons();
            return;
        }
        
        var magazineTemplates = category.weaponMagazineTemplates[weaponIndex];
        var magazineBullets = category.weaponMagazineBullets[weaponIndex];
        
        // Create a sorted list of magazine indices (excluding the active one at index 0)
        List<int> sortedMagazineIndices = new List<int>();
        
        // Skip the active magazine (index 0), we'll add it later
        // Add the rest of the magazines in any order for now
        for (int i = 1; i < magazineBullets.Count; i++)
        {
            sortedMagazineIndices.Add(i);
        }
        
        // Sort magazines by bullet count in ASCENDING order (least to most)
        sortedMagazineIndices.Sort((a, b) => {
            int bulletsA = magazineBullets[a].Count;
            int bulletsB = magazineBullets[b].Count;
            return bulletsA.CompareTo(bulletsB); // ASCENDING order - least bullets first
        });
        
        // Add active magazine at the END (it will be rightmost position)
        sortedMagazineIndices.Add(0);
        
        // Determine total magazines to show
        int totalMagazines = Mathf.Min(magazineTemplates.Count, maxMagazineIcons);
        
        // Create or update magazine icons
        // First, ensure we have enough icons
        while (magazineIcons.Count < totalMagazines)
        {
            CreateMagazineIcon();
        }
        
        // Hide excess icons if needed
        for (int i = totalMagazines; i < magazineIcons.Count; i++)
        {
            if (magazineIconData[i].outlineImage != null)
            {
                magazineIconData[i].outlineImage.gameObject.SetActive(false);
            }
        }
        
        // Update magazine data for each icon
        for (int i = 0; i < totalMagazines; i++)
        {
            // Get the magazine data using the correct sorted index
            int sortedIndex = sortedMagazineIndices.Count - 1 - i; // Reverse order to put active mag rightmost
            int magIndex = sortedIndex >= 0 && sortedIndex < sortedMagazineIndices.Count 
                ? sortedMagazineIndices[sortedIndex] 
                : 0;
                
            MagazineData template = null;
            List<BulletInfo> bullets = null;
            
            if (magIndex < magazineTemplates.Count && magIndex < magazineBullets.Count)
            {
                template = magazineTemplates[magIndex];
                bullets = magazineBullets[magIndex];
                
                // Ensure icon is active
                if (i < magazineIconData.Count && magazineIconData[i].outlineImage != null)
                {
                    magazineIconData[i].outlineImage.gameObject.SetActive(true);
                }
                
                // Calculate fill percentage
                float fillPercentage = 0;
                if (template.ammoCount > 0)
                {
                    // For active magazine (index 0), get direct bullet count
                    if (magIndex == 0)
                    {
                        // Use PlayerLoadout data rather than weapon
                        fillPercentage = (float)bullets.Count / template.ammoCount;
                    }
                    else
                    {
                        fillPercentage = (float)bullets.Count / template.ammoCount;
                    }
                }
                
                // Set color based on fill percentage
                if (i < magazineIcons.Count)
                {
                    magazineIcons[i].color = GetMagazineColor(fillPercentage);
                }
            }
            else if (i < magazineIconData.Count && magazineIconData[i].outlineImage != null)
            {
                // Hide this icon if no magazine data
                magazineIconData[i].outlineImage.gameObject.SetActive(false);
            }
        }
        
        // Make sure icons are positioned correctly
        UpdateMagazineIconPositions();
    }
    
    // Get color based on ammo percentage
    private Color GetMagazineColor(float fillPercentage)
    {
        if (fillPercentage >= 1.0f)
        {
            // Full magazine
            return fullMagazineColor;
        }
        else if (fillPercentage <= 0.0f)
        {
            // Empty magazine
            return emptyMagazineColor;
        }
        else if (fillPercentage > 0.7f)
        {
            // High ammo (99-70%)
            float t = (fillPercentage - 0.7f) / 0.3f;
            return Color.Lerp(highAmmoColor, fullMagazineColor, t);
        }
        else if (fillPercentage > 0.4f)
        {
            // Medium ammo (70-40%)
            float t = (fillPercentage - 0.4f) / 0.3f;
            return Color.Lerp(mediumAmmoColor, highAmmoColor, t);
        }
        else if (fillPercentage > 0.2f)
        {
            // Low ammo (40-20%)
            float t = (fillPercentage - 0.2f) / 0.2f;
            return Color.Lerp(lowAmmoColor, mediumAmmoColor, t);
        }
        else
        {
            // Critical ammo (20-1%)
            float t = fillPercentage / 0.2f;
            return Color.Lerp(criticalAmmoColor, lowAmmoColor, t);
        }
    }
    
    // Create a new magazine icon
    private void CreateMagazineIcon()
    {
        // Figure out the row and position within row
        int totalIcons = magazineIcons.Count;
        int row = totalIcons / maxIconsPerRow;
        int posInRow = totalIcons % maxIconsPerRow;
        
        // Create parent GameObject for outline
        GameObject iconObj = new GameObject("MagazineIcon");
        iconObj.transform.SetParent(magazineContainer, false);
        
        // Add outline Image component
        Image outlineImage = iconObj.AddComponent<Image>();
        if (magazineOutlineSprite != null)
        {
            outlineImage.sprite = magazineOutlineSprite;
            outlineImage.type = Image.Type.Simple;
            outlineImage.color = outlineColor;
            outlineImage.preserveAspect = true;
        }
        
        // Set size and position
        RectTransform rect = iconObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(magazineIconWidth, magazineIconHeight); // Adjust size as needed
        
        // Position the icon - to the LEFT of the origin point (negative X)
        // And UP for additional rows (positive Y)
        rect.anchoredPosition = new Vector2(
            -posInRow * (rect.sizeDelta.x + magazineIconSpacing), 
            row * (rect.sizeDelta.y + magazineIconVerticalSpacing)
        );
        
        // Create child GameObject for interior fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(iconObj.transform, false);
        
        // Add fill Image component (colored part)
        Image fillImage = fillObj.AddComponent<Image>();
        if (magazineFillSprite != null)
        {
            fillImage.sprite = magazineFillSprite;
            fillImage.type = Image.Type.Simple;
            fillImage.preserveAspect = true;
        }
        
        // Ensure perfect alignment of fill with outline by positioning explicitly
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        // Store both outline and fill for reference
        if (magazineIconData == null)
            magazineIconData = new List<MagazineIconData>();
            
        magazineIconData.Add(new MagazineIconData {
            outlineImage = outlineImage,
            fillImage = fillImage,
            iconTransform = rect
        });
        
        // Add to list - we track the FILL image for color changes
        magazineIcons.Add(fillImage);
    }
    
    // Update positions of magazine icons
    private void UpdateMagazineIconPositions()
    {
        for (int i = 0; i < magazineIcons.Count; i++)
        {
            if (magazineIcons[i] != null)
            {
                int row = i / maxIconsPerRow;
                int posInRow = i % maxIconsPerRow;
                
                RectTransform rect = magazineIcons[i].GetComponent<RectTransform>();
                
                // Position to the LEFT (negative X) and UP for additional rows
                rect.anchoredPosition = new Vector2(
                    -posInRow * (rect.sizeDelta.x + magazineIconSpacing),
                    row * (rect.sizeDelta.y + magazineIconVerticalSpacing)
                );
            }
        }
    }
    
    // Clear all magazine icons
    private void ClearMagazineIcons()
    {
        // Also clear the icon data list
        if (magazineIconData != null)
        {
            foreach (var iconData in magazineIconData)
            {
                if (iconData.outlineImage != null)
                {
                    Destroy(iconData.outlineImage.gameObject);
                }
            }
            magazineIconData.Clear();
        }
        
        foreach (var icon in magazineIcons)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }
        magazineIcons.Clear();
    }
    
    #endregion

    // Update active magazine color in real-time - we'll make this more direct
    private void UpdateActiveMagazineColor()
    {
        // Only continue if we have magazine icons and a valid weapon
        if (magazineIcons.Count == 0 || magazineIconData.Count == 0 || 
            currentlyEquippedCategory < 0 || playerLoadout == null) 
            return;
            
        var category = playerLoadout.GetCategory(currentlyEquippedCategory);
        if (category == null || category.currentIndex >= category.items.Count) 
            return;
            
        // Only weapons have magazines
        if (category.itemTypes[category.currentIndex] != ItemType.Weapon)
            return;
            
        // Get the active weapon
        var weaponItem = category.items[category.currentIndex] as Demo.Scripts.Runtime.Item.Weapon;
        if (weaponItem == null) 
            return;
            
        int weaponIndex = category.currentIndex;
        
        // Make sure we have magazine data in the PlayerLoadout
        if (category.weaponMagazineTemplates.Count <= weaponIndex || 
            category.weaponMagazineBullets.Count <= weaponIndex ||
            category.weaponMagazineTemplates[weaponIndex].Count == 0 ||
            category.weaponMagazineBullets[weaponIndex].Count == 0)
            return;
            
        // Get data directly from PlayerLoadout for the active magazine (index 0)
        var activeMagazine = category.weaponMagazineTemplates[weaponIndex][0];
        var activeBullets = category.weaponMagazineBullets[weaponIndex][0];
        
        // Calculate fill percentage using actual magazine bullet data
        float fillPercentage = 0;
        if (activeMagazine != null && activeMagazine.ammoCount > 0)
        {
            fillPercentage = (float)activeBullets.Count / activeMagazine.ammoCount;
        }
        
        // The active magazine is always the rightmost (given our sorting logic)
        if (magazineIcons.Count > 0)
        {
            Color newColor = GetMagazineColor(fillPercentage);
            int activeIconIndex = magazineIcons.Count - 1; // Rightmost
            
            // Debug logs to help diagnose
            Debug.Log($"Active magazine from PlayerLoadout: Bullets: {activeBullets.Count}/{activeMagazine.ammoCount}, " +
                      $"Fill %: {fillPercentage}, Color: {newColor}");
            
            magazineIcons[activeIconIndex].color = newColor;
        }
    }
} 