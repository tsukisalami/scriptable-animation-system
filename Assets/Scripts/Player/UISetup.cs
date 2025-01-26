using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public class UISetup : MonoBehaviour
{
    [MenuItem("GameObject/UI/Custom/Player Health UI")]
    public static void CreatePlayerHealthUI()
    {
        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create main UI object and add PlayerHealthUI component
        GameObject healthUI = new GameObject("Player Health UI");
        healthUI.transform.SetParent(canvas.transform, false);
        healthUI.AddComponent<RectTransform>();
        PlayerHealthUI healthUIScript = healthUI.AddComponent<PlayerHealthUI>();

        // Create General HUD
        GameObject generalHUD = new GameObject("General HUD");
        generalHUD.transform.SetParent(healthUI.transform, false);
        generalHUD.AddComponent<RectTransform>();

        // Create Health Panel
        GameObject healthPanel = new GameObject("Health Panel");
        healthPanel.transform.SetParent(generalHUD.transform, false);
        RectTransform healthPanelRect = healthPanel.AddComponent<RectTransform>();
        healthPanelRect.anchorMin = new Vector2(0, 1);
        healthPanelRect.anchorMax = new Vector2(0, 1);
        healthPanelRect.pivot = new Vector2(0, 1);
        healthPanelRect.anchoredPosition = new Vector2(20, -20);
        healthPanelRect.sizeDelta = new Vector2(200, 100);

        // Create Health Text
        GameObject healthText = new GameObject("Health Text");
        healthText.transform.SetParent(healthPanel.transform, false);
        Text healthTextComponent = healthText.AddComponent<Text>();
        healthTextComponent.text = "Health: 100";
        healthTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        healthTextComponent.color = Color.white;
        healthTextComponent.alignment = TextAnchor.MiddleLeft;
        RectTransform healthTextRect = healthText.GetComponent<RectTransform>();
        healthTextRect.anchoredPosition = new Vector2(0, 0);
        healthTextRect.sizeDelta = new Vector2(200, 30);
        healthUIScript.healthText = healthTextComponent;

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
        healthUIScript.healthBar = healthBarImage;

        // Create Bleeding Indicator
        GameObject bleedingIndicator = new GameObject("Bleeding Indicator");
        bleedingIndicator.transform.SetParent(generalHUD.transform, false);
        RectTransform bleedingRect = bleedingIndicator.AddComponent<RectTransform>();
        bleedingRect.anchorMin = new Vector2(0, 1);
        bleedingRect.anchorMax = new Vector2(0, 1);
        bleedingRect.pivot = new Vector2(0, 1);
        bleedingRect.anchoredPosition = new Vector2(230, -20);
        bleedingRect.sizeDelta = new Vector2(32, 32);
        Image bleedingImage = bleedingIndicator.AddComponent<Image>();
        bleedingImage.color = new Color(1, 0, 0, 0.7f);
        healthUIScript.bleedingIndicator = bleedingImage;

        // Create Bandage Count Text
        GameObject bandageCount = new GameObject("Bandage Count Text");
        bandageCount.transform.SetParent(healthPanel.transform, false);
        Text bandageTextComponent = bandageCount.AddComponent<Text>();
        bandageTextComponent.text = "Bandages: 0";
        bandageTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        bandageTextComponent.color = Color.white;
        bandageTextComponent.alignment = TextAnchor.MiddleLeft;
        RectTransform bandageCountRect = bandageCount.GetComponent<RectTransform>();
        bandageCountRect.anchoredPosition = new Vector2(0, -70);
        bandageCountRect.sizeDelta = new Vector2(200, 30);
        healthUIScript.bandageCountText = bandageTextComponent;

        // Create Pain Overlay
        GameObject painOverlay = new GameObject("Pain Overlay");
        painOverlay.transform.SetParent(healthUI.transform, false);
        RectTransform painRect = painOverlay.AddComponent<RectTransform>();
        painRect.anchorMin = Vector2.zero;
        painRect.anchorMax = Vector2.one;
        painRect.sizeDelta = Vector2.zero;
        Image painImage = painOverlay.AddComponent<Image>();
        painImage.color = new Color(1, 0, 0, 0);
        healthUIScript.screenBloodOverlay = painImage;

        // Create Downed Panel
        GameObject downedPanel = new GameObject("Downed Panel");
        downedPanel.transform.SetParent(healthUI.transform, false);
        RectTransform downedRect = downedPanel.AddComponent<RectTransform>();
        downedRect.anchorMin = new Vector2(0.5f, 0.5f);
        downedRect.anchorMax = new Vector2(0.5f, 0.5f);
        downedRect.sizeDelta = new Vector2(400, 300);
        downedRect.anchoredPosition = Vector2.zero;
        Image downedBg = downedPanel.AddComponent<Image>();
        downedBg.color = new Color(0, 0, 0, 0.8f);
        healthUIScript.downedPanel = downedPanel;

        // Create Downed Timer Text
        GameObject timerText = new GameObject("Downed Timer Text");
        timerText.transform.SetParent(downedPanel.transform, false);
        Text timerTextComponent = timerText.AddComponent<Text>();
        timerTextComponent.text = "Time until death: 03:00";
        timerTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        timerTextComponent.color = Color.white;
        timerTextComponent.alignment = TextAnchor.MiddleCenter;
        RectTransform timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchoredPosition = new Vector2(0, 100);
        timerRect.sizeDelta = new Vector2(300, 30);
        healthUIScript.downedTimerText = timerTextComponent;

        // Set layer for all UI elements
        SetLayerRecursively(healthUI, LayerMask.NameToLayer("UI"));

        // Initially hide downed panel
        downedPanel.SetActive(false);

        Selection.activeGameObject = healthUI;
        Debug.Log("Player Health UI created successfully!");
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
#endif 