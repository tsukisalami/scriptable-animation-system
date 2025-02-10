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
    public GameObject selectionIndicator;
    public float categorySpacing = 100f;
    public float normalSize = 60f;
    public float expandedSize = 100f;
    public float itemSpacing = 10f;
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(1f, 1f, 1f, 0.6f);
    public Color depleteItemColor = Color.yellow;

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

        // Subscribe to health events if component exists
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        }

        // Initialize UI
        UpdateHealthUI();
        InitializeCategories();
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
                    category.itemIcons[j].color = unselectedColor;
                
                if (category.itemCounts[j] != null)
                    category.itemCounts[j].gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (playerHealth != null)
        {
            UpdateHealthUI();
        }

        UpdateHotbarUI();

        // Handle scroll wheel input
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (scrollDelta != 0)
        {
            HandleScrollWheel(scrollDelta);
        }

        // Handle left click to equip selected item
        if (Input.GetMouseButtonDown(0) && selectedCategoryIndex != -1)
        {
            EquipSelectedItem();
        }
    }

    private void UpdateHotbarUI()
    {
        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            var loadoutCategory = playerLoadout.GetCategory(i);
            
            if (loadoutCategory == null) continue;

            // Update category icon opacity
            if (category.categoryIcon != null)
            {
                category.categoryIcon.color = (i == selectedCategoryIndex) ? selectedColor : unselectedColor;
            }

            // Update item name if category is selected
            if (i == selectedCategoryIndex && category.itemNameText != null)
            {
                var currentItem = loadoutCategory.GetCurrentItem();
                if (currentItem != null)
                {
                    var itemDisplay = currentItem.GetComponent<ItemDisplay>();
                    category.itemNameText.text = itemDisplay != null ? itemDisplay.displayName : currentItem.name;
                }
                else
                {
                    category.itemNameText.text = "";
                }
            }

            // Update items if category is expanded
            if (i == selectedCategoryIndex && category.itemContainer != null)
            {
                category.itemContainer.gameObject.SetActive(true);
                
                // Update each item in the category
                for (int j = 0; j < Mathf.Min(loadoutCategory.items.Count, category.itemIcons.Length); j++)
                {
                    if (category.itemIcons[j] != null)
                    {
                        // Update item icon using ItemDisplay
                        var itemDisplay = loadoutCategory.items[j].GetComponent<ItemDisplay>();
                        if (itemDisplay != null)
                        {
                            category.itemIcons[j].sprite = itemDisplay.inventoryIcon;
                        }
                        
                        // Update item opacity
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
            }
            else if (category.itemContainer != null)
            {
                category.itemContainer.gameObject.SetActive(false);
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
            }
            else if (newCategoryIndex >= categories.Length)
            {
                newCategoryIndex = 0;
            }

            // Only select the new category if it has items
            var newCategory = playerLoadout.GetCategory(newCategoryIndex);
            if (newCategory != null && newCategory.HasAvailableItems())
            {
                selectedItemIndex = direction > 0 ? 0 : newCategory.items.Count - 1;
                SelectCategory(newCategoryIndex);
            }
        }
        else
        {
            // Stay in current category but update selected item
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
        if (category == null) return;

        // Only equip if the item has uses (for consumables)
        if (category.isConsumable && !playerLoadout.HasCurrentItemUses())
            return;

        playerLoadout.SelectCategory(selectedCategoryIndex);
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        }
    }
} 