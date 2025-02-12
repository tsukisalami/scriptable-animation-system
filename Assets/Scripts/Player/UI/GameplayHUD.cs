using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameplayHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public PlayerLoadout playerLoadout;

    [Header("Health UI")]
    public TMP_Text healthText;
    public Image healthBar;

    [Header("Hotbar UI")]
    public RectTransform[] categoryContainers;
    public float categorySpacing = 100f;
    public float normalSize = 60f;
    public float expandedSize = 100f;
    public float itemSpacing = 10f;
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(1f, 1f, 1f, 0.6f);
    public Color depleteItemColor = Color.yellow;

    [Header("Hotbar Visibility")]
    public float visibilityDuration = 3f;
    public float fadeOutDuration = 0.3f;
    public CanvasGroup hotbarCanvasGroup;

    [Header("Events")]
    [SerializeField] private InventoryEvents inventoryEvents;

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

        // Subscribe to events
        if (inventoryEvents != null)
        {
            inventoryEvents.OnCategorySelected += HandleCategorySelected;
            inventoryEvents.OnItemSelected += HandleItemSelected;
            inventoryEvents.OnItemConsumed += HandleItemConsumed;
            inventoryEvents.OnInputStateChanged += HandleInputStateChanged;
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

        // Set initial hotbar visibility
        if (hotbarCanvasGroup != null)
        {
            hotbarCanvasGroup.alpha = 0f;
            isHotbarActive = false;
        }

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
        if (playerHealth != null)
            UpdateHealthUI();

        // Check for loadout changes
        if (playerLoadout != null)
        {
            // Only show hotbar on number key press
            if (playerLoadout.currentCategoryIndex != currentlyEquippedCategory)
            {
                currentlyEquippedCategory = playerLoadout.currentCategoryIndex;
                if (currentlyEquippedCategory != -1)
                {
                    var category = playerLoadout.GetCategory(currentlyEquippedCategory);
                    currentlyEquippedItem = category?.currentIndex ?? -1;
                    ShowHotbar();
                    selectedCategoryIndex = currentlyEquippedCategory;
                    selectedItemIndex = currentlyEquippedItem;
                }
            }
            // Check for item changes within same category
            else if (currentlyEquippedCategory != -1)
            {
                var category = playerLoadout.GetCategory(currentlyEquippedCategory);
                if (category != null && category.currentIndex != currentlyEquippedItem)
                {
                    currentlyEquippedItem = category.currentIndex;
                    ShowHotbar();
                    selectedCategoryIndex = currentlyEquippedCategory;
                    selectedItemIndex = currentlyEquippedItem;
                }
            }
        }

        UpdateHotbarUI();
        UpdateHotbarVisibility();

        // Handle scroll wheel input only
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

    private void ShowHotbar()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;  // Clear the coroutine reference
        }

        isHotbarActive = true;
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
                if (loadoutCategory.isConsumable && category.itemCounts[0] != null)
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
                if (category.itemContainer != null && (!loadoutCategory.isConsumable || loadoutCategory.items.Count > 1))
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
                    if (loadoutCategory.isConsumable && category.itemCounts[j] != null)
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

        ShowHotbar();

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
        if (selectedCategoryIndex == -1 || playerLoadout == null || !IsHotbarActive()) return;

        var category = playerLoadout.GetCategory(selectedCategoryIndex);
        if (category == null || selectedItemIndex >= category.items.Count) return;

        // Only equip if the item has uses (for consumables)
        if (category.isConsumable && !playerLoadout.HasCurrentItemUses())
            return;

        // Set the selected item as current in the category WITHOUT cycling
        category.currentIndex = selectedItemIndex;
        currentlyEquippedItem = selectedItemIndex;
        
        // Tell playerLoadout to equip this category without cycling
        playerLoadout.SelectCategoryWithoutCycle(selectedCategoryIndex);
        
        // Immediately start fade out after equipping
        StartFadeOut();
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
            
            if (loadoutCategory != null && loadoutCategory.isConsumable)
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
        // Only consider hotbar active when it's at full opacity
        return hotbarCanvasGroup != null && hotbarCanvasGroup.alpha >= 1f;
    }
} 