using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using Ballistics;
using System.Linq; // Added for LINQ Sort

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

    [Header("Magazine/Item UI Settings")] // Renamed Header
    public RectTransform magazineContainer;
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

    // --- Magazine/Item UI Fields (from MagazineUI.cs) ---
    private int currentMagazineUICategory = -1; // Track category specifically for mag UI
    private int currentMagazineUIItemIndex = -1; // Track item index specifically for mag UI

    // Data structure to hold outline and fill image pairs
    private class MagazineIcon
    {
        public GameObject root;
        public Image outline;
        public Image fill;
    }
    private List<MagazineIcon> magazineIcons = new List<MagazineIcon>();
    // --- End Magazine/Item UI Fields ---

    private void Start()
    {
        // Find components if not assigned (Health, Loadout, FPSController, PlayerStateManager)
        FindComponents(); 

        // Subscribe to events
        SubscribeToEvents();

        // Validate UI setup
        ValidateUISetup();

        // Initialize UI
        InitializeHotbar();
        InitializeMagazineUI(); // Initialize the merged magazine UI

        // Set initial UI states
        if (hotbarCanvasGroup != null)
        {
            hotbarCanvasGroup.alpha = 0f;
            isHotbarActive = false;
        }
        
        // Track initial equipment
        if (playerLoadout != null)
        {
            currentlyEquippedCategory = playerLoadout.currentCategoryIndex;
            if (currentlyEquippedCategory != -1)
            {
                var category = playerLoadout.GetCategory(currentlyEquippedCategory);
                currentlyEquippedItem = category?.currentIndex ?? -1;
            }
            // Update currentMagazineUICategory/Index as well
            currentMagazineUICategory = currentlyEquippedCategory;
            currentMagazineUIItemIndex = currentlyEquippedItem;
        }
        
        // Force initial update and visibility after all setup
        UpdateHealthUI();
        ShowHealthUI();
        UpdateMagazineDisplay(); 
    }

    private void FindComponents()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) Debug.LogWarning("GameplayHUD: No PlayerHealth component found!");
        }
        if (playerLoadout == null)
        {
            playerLoadout = GetComponentInParent<PlayerLoadout>();
            if (playerLoadout == null) Debug.LogWarning("GameplayHUD: No PlayerLoadout component found!");
        }
        if (fpsController == null)
        {
            fpsController = GetComponentInParent<Demo.Scripts.Runtime.Character.FPSController>();
            if (fpsController == null) fpsController = FindObjectOfType<Demo.Scripts.Runtime.Character.FPSController>();
            if (fpsController == null) Debug.LogWarning("GameplayHUD: FPSController not found! Weapon equip animation sync will not work.");
        }
        if (playerStateManager == null)
        {
            playerStateManager = GetComponentInParent<PlayerStateManager>();
            if (playerStateManager == null) playerStateManager = FindObjectOfType<PlayerStateManager>();
            if (playerStateManager == null) Debug.LogWarning("GameplayHUD: PlayerStateManager not found! Falling back to legacy input system.");
        }
        // Validate magazine container
        if (magazineContainer == null)
        {
            Debug.LogError("GameplayHUD: Magazine container not assigned!");
            enabled = false; // Disable script if essential refs are missing
        }
    }

    private void SubscribeToEvents()
    {
        if (inventoryEvents != null)
        {
            inventoryEvents.OnCategorySelected += HandleCategorySelected;
            inventoryEvents.OnItemSelected += HandleItemSelected;
            inventoryEvents.OnItemConsumed += HandleItemConsumed;
            inventoryEvents.OnInputStateChanged += HandleInputStateChanged;
        }
        if (playerStateManager != null)
        {
            playerStateManager.OnStateChanged += HandlePlayerStateChanged;
        }
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        }
        // Subscribe to Magazine/Item events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded += OnWeaponReloaded; 
        if(playerLoadout != null)
        {
             playerLoadout.OnConsumableUsed += UpdateMagazineDisplay; 
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from all events to prevent errors
        if (inventoryEvents != null)
        {
            inventoryEvents.OnCategorySelected -= HandleCategorySelected;
            inventoryEvents.OnItemSelected -= HandleItemSelected;
            inventoryEvents.OnItemConsumed -= HandleItemConsumed;
            inventoryEvents.OnInputStateChanged -= HandleInputStateChanged;
        }
        if (playerStateManager != null)
        {
            playerStateManager.OnStateChanged -= HandlePlayerStateChanged;
        }
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
        }
        // Unsubscribe Magazine/Item events
        Demo.Scripts.Runtime.Item.Weapon.OnWeaponReloaded -= OnWeaponReloaded; 
        if(playerLoadout != null)
        {
             playerLoadout.OnConsumableUsed -= UpdateMagazineDisplay; 
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
            if (category == null) { Debug.LogError($"GameplayHUD: Category {i} is null!"); continue; }
            if (category.categoryIcon == null) Debug.LogError($"GameplayHUD: Category {i} is missing its category icon!");
            if (category.categoryNumber == null) Debug.LogError($"GameplayHUD: Category {i} is missing its category number text!");
            if (category.itemNameText == null) Debug.LogError($"GameplayHUD: Category {i} is missing its item name text!");
            if (category.itemContainer == null) Debug.LogError($"GameplayHUD: Category {i} is missing its item container!");
            if (category.itemIcons == null || category.itemIcons.Length != 3) Debug.LogError($"GameplayHUD: Category {i} must have exactly 3 item icons!");
            if (category.itemCounts == null || category.itemCounts.Length != 3) Debug.LogError($"GameplayHUD: Category {i} must have exactly 3 item count texts!");
            for (int j = 0; j < 3; j++)
            {
                if (category.itemIcons[j] == null) Debug.LogError($"GameplayHUD: Category {i} is missing item icon {j}!");
                if (category.itemCounts[j] == null) Debug.LogError($"GameplayHUD: Category {i} is missing item count text {j}!");
            }
        }
        // Validate Magazine UI specific references
        if (magazineContainer == null) Debug.LogError("GameplayHUD: Magazine container RectTransform is not assigned!");
        if (magazineOutlineSprite == null) Debug.LogWarning("GameplayHUD: Magazine Outline Sprite is not assigned.");
        if (magazineFillSprite == null) Debug.LogWarning("GameplayHUD: Magazine Fill Sprite is not assigned.");
    }

    private void InitializeHotbar()
    {
        // Initialize category UI elements
        for (int i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            if (category.categoryNumber != null) category.categoryNumber.text = (i + 1).ToString();
            if (category.itemContainer != null) category.itemContainer.gameObject.SetActive(false);
            if (category.categoryIcon != null) category.categoryIcon.color = unselectedColor;
            for (int j = 0; j < category.itemIcons.Length; j++)
            {
                if (category.itemIcons[j] != null)
                {
                    category.itemIcons[j].color = unselectedColor;
                    category.itemIcons[j].gameObject.SetActive(false);
                }
                if (category.itemCounts[j] != null) category.itemCounts[j].gameObject.SetActive(false);
            }
            var loadoutCategory = playerLoadout.GetCategory(i);
            if (loadoutCategory != null && loadoutCategory.items.Count > 0)
            {
                var firstItem = loadoutCategory.items[0];
                var itemDisplay = firstItem.GetComponent<ItemDisplay>();
                if (itemDisplay != null && category.categoryIcon != null) category.categoryIcon.sprite = itemDisplay.inventoryIcon;
            }
        }
    }

    // Merged Update method
    private void Update()
    {
        // --- Health UI Update --- 
        if (playerHealth != null)
        {
            UpdateHealthUI();
            ShowHealthUI();
        }

        // --- Hotbar Update & Visibility --- 
        UpdateHotbarUI();
        if (!isInEquipTransition)
        {
            UpdateHotbarVisibility();
            HandleHotbarInput();
        }
        else if (fpsController != null && fpsController._actionState == Demo.Scripts.Runtime.Character.FPSActionState.None)
        {
            HideHotbarImmediately();
            isInEquipTransition = false;
        }

        // --- Magazine/Item UI Update --- 
        UpdateMagazineUIState();
    }

    // --- Magazine/Item UI Update Logic (from MagazineUI.Update) --- 
    private void UpdateMagazineUIState()
    {
        if (playerLoadout == null) return;

        int newCategory = playerLoadout.currentCategoryIndex;
        int newItem = -1;

        if (newCategory >= 0)
        {
            var category = playerLoadout.GetCategory(newCategory);
            if (category != null && category.items.Count > 0)
            {
                newItem = Mathf.Clamp(category.currentIndex, 0, category.items.Count - 1);
            }
            else
            {
                newItem = -1; 
            }
        }
        else
        {
            newItem = -1;
        }

        // Use separate tracking vars (currentMagazineUICategory/ItemIndex) for mag UI updates
        if (newCategory != currentMagazineUICategory || newItem != currentMagazineUIItemIndex || 
            (magazineContainer.gameObject.activeSelf && !magazineIcons.Any(icon => icon.root.activeSelf))) // Update if category/item changed OR if container active but no icons shown
        {
            currentMagazineUICategory = newCategory;
            currentMagazineUIItemIndex = newItem;
            UpdateMagazineDisplay();
        }
    }
    // --- End Magazine/Item UI Update Logic ---

    private void HandleHotbarInput()
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta != 0)
            {
                ShowHotbar();
                HandleScrollWheel(scrollDelta);
            }
            if (Input.GetMouseButtonDown(0) && selectedCategoryIndex != -1 && IsHotbarActive())
            {
                EquipSelectedItem();
            }
            if (Input.GetMouseButtonDown(1) && IsHotbarActive())
        {
            StartFadeOut();
            if (playerStateManager != null) playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
            else if (inventoryEvents != null) inventoryEvents.RaiseInputStateChanged(InputState.Normal);
        }
    }

    // ... (Existing Hotbar Visibility methods: UpdateHotbarVisibility, ShowHotbarDuringEquip, HideHotbarImmediately, ShowHotbar, StartFadeOut, FadeOutHotbar) remain the same ...
    private void UpdateHotbarVisibility() { /* ... */ }
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
            hotbarCanvasGroup.interactable = true; // Ensure interactable
            hotbarCanvasGroup.blocksRaycasts = true; // Ensure blocks raycasts
        }
    }
    private void HideHotbarImmediately() { /* ... */ }
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
            hotbarCanvasGroup.interactable = true; // Ensure interactable
            hotbarCanvasGroup.blocksRaycasts = true; // Ensure blocks raycasts
        }
    }
    private void StartFadeOut() { /* ... */ }
    private IEnumerator FadeOutHotbar() { /* ... */ yield return null; }

    // ... (Existing Hotbar UI methods: ResetAllCategories, UpdateHotbarUI, SetCategorySize) remain the same ...
    private void ResetAllCategories() { /* ... */ }
    private void UpdateHotbarUI() { /* ... */ }
    private void SetCategorySize(int categoryIndex, float size) { /* ... */ }

    // ... (Existing Health UI methods: UpdateHealthUI, HandleHealthStateChanged) remain the same ...
    private void UpdateHealthUI() { /* ... */ }
    private void HandleHealthStateChanged(PlayerHealth.HealthState newState) { /* ... */ }

    // ... (Existing Hotbar Selection/Equip methods: SelectCategory, HandleScrollWheel, EquipSelectedItem) remain the same ...
    public void SelectCategory(int categoryIndex) { /* ... */ }
    private void HandleScrollWheel(float scrollDelta) { /* ... */ }
    private void EquipSelectedItem() { /* ... */ }

    // ... (Existing Event Handlers: HandleCategorySelected, HandleItemSelected, HandleItemConsumed, HandleInputStateChanged, HandlePlayerStateChanged) remain the same ...
    private void HandleCategorySelected(int categoryIndex) { /* ... */ }
    private void HandleItemSelected(int categoryIndex, int itemIndex) { /* ... */ }
    private void HandleItemConsumed(int categoryIndex) { /* ... */ }
    private void HandleInputStateChanged(InputState newState) { /* ... */ }
    private void HandlePlayerStateChanged(PlayerStateManager.PlayerState newState, PlayerStateManager.PlayerState oldState) { /* ... */ }

    // ... (Existing helper methods: IsHotbarActive, ShowHealthUI) remain the same ...
    public bool IsHotbarActive() { return isHotbarActive && !isInEquipTransition; }
    private void ShowHealthUI() { /* ... */ }

    // --- Magazine/Item UI Methods (from MagazineUI.cs) --- 

    // Update magazines after reloading with a small delay
    private void OnWeaponReloaded()
    {
        Invoke(nameof(UpdateMagazineDisplay), 0.05f); 
    }

    // Central method to update the display based on the current item type.
    public void UpdateMagazineDisplay()
    {
        // Ensure container is active before trying to display
        if (magazineContainer == null) return;
        magazineContainer.gameObject.SetActive(true);

        // Use the dedicated tracking variables for magazine UI
        if (currentMagazineUICategory < 0 || currentMagazineUIItemIndex < 0)
        {
            HideAllMagazines();
            return;
        }

        var category = playerLoadout.GetCategory(currentMagazineUICategory);
        if (category == null || currentMagazineUIItemIndex >= category.items.Count)
        {
            HideAllMagazines();
            return;
        }

        // Determine item type and call the appropriate display method
        ItemType currentItemType = category.itemTypes[currentMagazineUIItemIndex];

        switch (currentItemType)
        {
            case ItemType.Weapon:
                DisplayWeaponMagazines(category, currentMagazineUIItemIndex);
                break;
            case ItemType.Consumable:
                DisplayConsumableItems(category, currentMagazineUIItemIndex);
                break;
            case ItemType.Tool:
            default:
                HideAllMagazines();
                break;
        }
    }

    // Displays icons representing weapon magazines.
    private void DisplayWeaponMagazines(PlayerLoadout.LoadoutCategory category, int weaponIndex)
    {   
        if (category.weaponMagazineTemplates == null || category.weaponMagazineBullets == null ||
            category.weaponMagazineTemplates.Count <= weaponIndex || category.weaponMagazineBullets.Count <= weaponIndex)
        {
            HideAllMagazines(); return;
        }
        var templates = category.weaponMagazineTemplates[weaponIndex];
        var bullets = category.weaponMagazineBullets[weaponIndex];
        if (templates == null || bullets == null || templates.Count == 0 || bullets.Count == 0)
        {
            HideAllMagazines(); return;
        }

        List<int> sortedIndices = new List<int>();
        for (int i = 1; i < bullets.Count; i++)
        {
            if (i < templates.Count) sortedIndices.Add(i);
        }
        sortedIndices.Sort((a, b) => {
            int bulletsA = (a < bullets.Count) ? bullets[a].Count : 0;
            int bulletsB = (b < bullets.Count) ? bullets[b].Count : 0;
            return bulletsA.CompareTo(bulletsB);
        });
        if (bullets.Count > 0 && templates.Count > 0) sortedIndices.Add(0);
       
        int totalIcons = Mathf.Min(templates.Count, maxMagazineIcons);
        while (magazineIcons.Count < totalIcons) CreateMagazineIcon();
        
        for (int i = 0; i < totalIcons; i++)
        {
            int sortedListIndex = sortedIndices.Count - 1 - i;
            if (sortedListIndex < 0 || sortedListIndex >= sortedIndices.Count) continue;
            int magIndex = sortedIndices[sortedListIndex];
            
            if (magIndex >= 0 && magIndex < templates.Count && magIndex < bullets.Count)
            {
                var template = templates[magIndex];
                var currentBullets = bullets[magIndex];
                float fillPct = (template != null && template.ammoCount > 0) ? (float)currentBullets.Count / template.ammoCount : 0;
                magazineIcons[i].root.SetActive(true);
                magazineIcons[i].fill.color = GetColorForFillPercentage(fillPct);
            }
            else
            {
                magazineIcons[i].root.SetActive(false);
            }
        }
        for (int i = totalIcons; i < magazineIcons.Count; i++) magazineIcons[i].root.SetActive(false);
    }

    // Displays icons representing remaining consumable items.
    private void DisplayConsumableItems(PlayerLoadout.LoadoutCategory category, int itemIndex)
    {
        if (category.initialItemCounts == null || itemIndex >= category.initialItemCounts.Length || 
            category.itemCounts == null || itemIndex >= category.itemCounts.Length)
        {
            HideAllMagazines(); return;
        }
        int currentCount = category.itemCounts[itemIndex];
        int initialCount = category.initialItemCounts[itemIndex];
        int totalIcons = Mathf.Min(initialCount, maxMagazineIcons);

        while (magazineIcons.Count < totalIcons) CreateMagazineIcon();

        for (int i = 0; i < totalIcons; i++)
        {
            magazineIcons[i].root.SetActive(true);
            bool isAvailable = i < currentCount;
            magazineIcons[i].fill.color = isAvailable ? fullMagazineColor : emptyMagazineColor;
        }
        for (int i = totalIcons; i < magazineIcons.Count; i++) magazineIcons[i].root.SetActive(false);
    }

    // Hide all magazine icons
    private void HideAllMagazines()
    {
        // DO NOT hide the container itself here, only the icons.
        // The container's visibility is managed by whether there's anything to display.
        // if (magazineContainer != null) magazineContainer.gameObject.SetActive(false); // REMOVED THIS LINE

        foreach (var icon in magazineIcons)
        {
            if (icon != null && icon.root != null) icon.root.SetActive(false);
        }
    }

    // Create a new magazine icon with outline and fill
    private void CreateMagazineIcon()
    {
        if (magazineContainer == null) return; // Don't create if container missing

        int index = magazineIcons.Count;
        int row = index / maxIconsPerRow;
        int posInRow = index % maxIconsPerRow;
        
        GameObject iconObj = new GameObject($"ItemIcon_{index}");
        iconObj.transform.SetParent(magazineContainer, false);
        
        Image outlineImage = iconObj.AddComponent<Image>();
        outlineImage.sprite = magazineOutlineSprite;
        outlineImage.type = Image.Type.Simple;
        outlineImage.color = outlineColor;
        outlineImage.preserveAspect = true;
        
        RectTransform rect = iconObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(magazineIconWidth, magazineIconHeight);
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(
            -posInRow * (magazineIconWidth + magazineIconSpacing),
             row * (magazineIconHeight + magazineIconVerticalSpacing)
        );
        
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(iconObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = magazineFillSprite;
        fillImage.type = Image.Type.Simple;
        fillImage.preserveAspect = true;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        magazineIcons.Add(new MagazineIcon { root = iconObj, outline = outlineImage, fill = fillImage });
    }

    // Initialize Magazine UI specific parts
    private void InitializeMagazineUI()
    {
        // Clear any existing magazine icons from previous runs/editor state
        if (magazineContainer != null)
        {
            foreach (Transform child in magazineContainer)
            {
                Destroy(child.gameObject);
            }
        }
        magazineIcons.Clear();
    }

    // Get the appropriate color based on ammo fill percentage (Used for Weapons)
    private Color GetColorForFillPercentage(float fillPct)
    {
        if (fillPct >= 0.999f) return fullMagazineColor;
        else if (fillPct > 0.7f) return highAmmoColor;
        else if (fillPct <= 0f) return emptyMagazineColor;
        else if (fillPct > 0.4f) return Color.Lerp(mediumAmmoColor, highAmmoColor, (fillPct - 0.4f) / 0.3f);
        else if (fillPct > 0.2f) return Color.Lerp(lowAmmoColor, mediumAmmoColor, (fillPct - 0.2f) / 0.2f);
        else return Color.Lerp(criticalAmmoColor, lowAmmoColor, fillPct / 0.2f);
    }
    // --- End Magazine/Item UI Methods --- 
} 