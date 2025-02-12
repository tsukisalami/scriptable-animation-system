using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class UISetup
{
    [MenuItem("GameObject/UI/Custom/Gameplay HUD")]
    public static void CreateGameplayHUD()
    {
        // Create Canvas if it doesn't exist
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create main UI object and add GameplayHUD component
        GameObject hudObj = new GameObject("Gameplay HUD");
        hudObj.transform.SetParent(canvas.transform, false);
        hudObj.AddComponent<RectTransform>();
        GameplayHUD hudScript = hudObj.AddComponent<GameplayHUD>();
        
        // Create General HUD Panel (left side)
        GameObject generalHUD = new GameObject("General HUD");
        generalHUD.transform.SetParent(hudObj.transform, false);
        RectTransform generalHUDRect = generalHUD.AddComponent<RectTransform>();
        generalHUDRect.anchorMin = new Vector2(0, 0);
        generalHUDRect.anchorMax = new Vector2(0, 1);
        generalHUDRect.pivot = new Vector2(0, 0.5f);
        generalHUDRect.anchoredPosition = new Vector2(20, 0);
        generalHUDRect.sizeDelta = new Vector2(200, 0);

        // Create Health Panel
        GameObject healthPanel = new GameObject("Health Panel");
        healthPanel.transform.SetParent(generalHUD.transform, false);
        RectTransform healthPanelRect = healthPanel.AddComponent<RectTransform>();
        healthPanelRect.anchorMin = new Vector2(0, 1);
        healthPanelRect.anchorMax = new Vector2(0, 1);
        healthPanelRect.pivot = new Vector2(0, 1);
        healthPanelRect.anchoredPosition = new Vector2(0, -20);
        healthPanelRect.sizeDelta = new Vector2(200, 100);

        // Create Health Text
        TextMeshProUGUI healthTextComponent = CreateTMPText("Health Text", healthPanel.transform);
        healthTextComponent.text = "Health: 100";
        healthTextComponent.color = Color.white;
        healthTextComponent.alignment = TextAlignmentOptions.Left;
        healthTextComponent.rectTransform.anchoredPosition = new Vector2(0, 0);
        healthTextComponent.rectTransform.sizeDelta = new Vector2(200, 30);
        hudScript.healthText = healthTextComponent;

        // Create Health Bar
        GameObject healthBar = new GameObject("Health Bar");
        healthBar.transform.SetParent(healthPanel.transform, false);
        RectTransform healthBarRect = healthBar.AddComponent<RectTransform>();
        healthBarRect.anchoredPosition = new Vector2(0, -35);
        healthBarRect.sizeDelta = new Vector2(180, 20);
        Image healthBarImage = healthBar.AddComponent<Image>();
        healthBarImage.type = Image.Type.Filled;
        healthBarImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarImage.color = Color.green;
        hudScript.healthBar = healthBarImage;

        // Create Hotbar Panel (right side)
        GameObject hotbarPanel = new GameObject("Hotbar Panel");
        hotbarPanel.transform.SetParent(hudObj.transform, false);
        RectTransform hotbarPanelRect = hotbarPanel.AddComponent<RectTransform>();
        hotbarPanelRect.anchorMin = new Vector2(1, 0.5f);
        hotbarPanelRect.anchorMax = new Vector2(1, 0.5f);
        hotbarPanelRect.pivot = new Vector2(1, 0.5f);
        hotbarPanelRect.anchoredPosition = new Vector2(-20, 0);
        hotbarPanelRect.sizeDelta = new Vector2(300, 600);

        // Create a container for all hotbar elements that will fade
        GameObject hotbarContainer = new GameObject("Hotbar Container");
        hotbarContainer.transform.SetParent(hotbarPanel.transform, false);
        RectTransform hotbarContainerRect = hotbarContainer.AddComponent<RectTransform>();
        hotbarContainerRect.anchorMin = Vector2.zero;
        hotbarContainerRect.anchorMax = Vector2.one;
        hotbarContainerRect.sizeDelta = Vector2.zero;
        hotbarContainerRect.anchoredPosition = Vector2.zero;

        // Add CanvasGroup to the container
        CanvasGroup hotbarCanvasGroup = hotbarContainer.AddComponent<CanvasGroup>();
        hudScript.hotbarCanvasGroup = hotbarCanvasGroup;

        // Create background panel
        GameObject bgPanel = new GameObject("Background Panel");
        bgPanel.transform.SetParent(hotbarContainer.transform, false);
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Create categories
        string[] categories = { "Primary", "Secondary", "Throwables", "Special", "Medical", "Tools" };
        float spacing = 100f;
        float normalSize = 60f;
        float itemSpacing = 10f;

        hudScript.categories = new GameplayHUD.CategoryUI[categories.Length];

        for (int i = 0; i < categories.Length; i++)
        {
            GameObject categoryObj = new GameObject(categories[i]);
            categoryObj.transform.SetParent(hotbarContainer.transform, false);
            RectTransform categoryRect = categoryObj.AddComponent<RectTransform>();
            
            // Position from top to bottom
            float yPos = 250 - (i * spacing);
            categoryRect.anchorMin = new Vector2(1, 0.5f);
            categoryRect.anchorMax = new Vector2(1, 0.5f);
            categoryRect.pivot = new Vector2(1, 0.5f);
            categoryRect.anchoredPosition = new Vector2(0, yPos);
            categoryRect.sizeDelta = new Vector2(normalSize, normalSize);

            // Create category UI container
            var categoryUI = new GameplayHUD.CategoryUI();
            hudScript.categories[i] = categoryUI;

            // Create category icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(categoryObj.transform, false);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = new Color(1f, 1f, 1f, 0.6f);
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;
            categoryUI.categoryIcon = iconImage;

            // Create category number
            TextMeshProUGUI numberText = CreateTMPText("Number", iconObj.transform);
            numberText.text = (i + 1).ToString();
            numberText.fontSize = 20;
            numberText.color = Color.white;
            numberText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform numberRect = numberText.GetComponent<RectTransform>();
            numberRect.anchorMin = Vector2.zero;
            numberRect.anchorMax = Vector2.one;
            numberRect.offsetMin = new Vector2(5, 0);
            numberRect.offsetMax = new Vector2(0, -5);
            categoryUI.categoryNumber = numberText;

            // Create item name text (left of category)
            TextMeshProUGUI itemNameText = CreateTMPText("ItemName", categoryObj.transform);
            itemNameText.color = Color.white;
            itemNameText.alignment = TextAlignmentOptions.Right;
            RectTransform itemNameRect = itemNameText.GetComponent<RectTransform>();
            itemNameRect.anchorMin = new Vector2(0, 0.5f);
            itemNameRect.anchorMax = new Vector2(0, 0.5f);
            itemNameRect.pivot = new Vector2(1, 0.5f);
            itemNameRect.anchoredPosition = new Vector2(-10, 0);
            itemNameRect.sizeDelta = new Vector2(200, 30);
            categoryUI.itemNameText = itemNameText;

            // Create item container (for expanded view)
            GameObject itemContainer = new GameObject("Items");
            itemContainer.transform.SetParent(categoryObj.transform, false);
            RectTransform itemContainerRect = itemContainer.AddComponent<RectTransform>();
            itemContainerRect.anchorMin = new Vector2(0, 0.5f);
            itemContainerRect.anchorMax = new Vector2(1, 0.5f);
            itemContainerRect.pivot = new Vector2(1, 0.5f);
            itemContainerRect.anchoredPosition = new Vector2(-normalSize - itemSpacing, 0);
            itemContainerRect.sizeDelta = new Vector2(normalSize * 3 + itemSpacing * 2, normalSize);
            categoryUI.itemContainer = itemContainerRect;

            // Create item slots
            categoryUI.itemIcons = new Image[3];
            categoryUI.itemCounts = new TextMeshProUGUI[3];

            for (int j = 0; j < 3; j++)
            {
                GameObject itemSlot = new GameObject($"Item_{j}");
                itemSlot.transform.SetParent(itemContainer.transform, false);
                Image itemImage = itemSlot.AddComponent<Image>();
                itemImage.color = new Color(1f, 1f, 1f, 0.6f);
                RectTransform itemRect = itemImage.GetComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0, 0);
                itemRect.anchorMax = new Vector2(0, 1);
                itemRect.pivot = new Vector2(0, 0.5f);
                itemRect.anchoredPosition = new Vector2(j * (normalSize + itemSpacing), 0);
                itemRect.sizeDelta = new Vector2(normalSize, normalSize);
                categoryUI.itemIcons[j] = itemImage;

                // Create count text
                TextMeshProUGUI countText = CreateTMPText("Count", itemSlot.transform);
                countText.color = Color.green;
                countText.fontSize = 16;
                countText.alignment = TextAlignmentOptions.BottomLeft;
                RectTransform countRect = countText.GetComponent<RectTransform>();
                countRect.anchorMin = Vector2.zero;
                countRect.anchorMax = Vector2.one;
                countRect.offsetMin = new Vector2(5, 5);
                countRect.offsetMax = new Vector2(-5, -5);
                categoryUI.itemCounts[j] = countText;
            }

            // Initially hide item container
            itemContainer.SetActive(false);
        }

        // Set layer for all UI elements
        SetLayerRecursively(hudObj, LayerMask.NameToLayer("UI"));

        Selection.activeGameObject = hudObj;
        Debug.Log("Gameplay HUD created successfully!");
    }

    private static TextMeshProUGUI CreateTMPText(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        return tmp;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
} 