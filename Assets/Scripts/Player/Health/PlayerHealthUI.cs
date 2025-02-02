using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public PlayerLoadout playerLoadout;

    [Header("General Health UI")]
    public Text healthText;
    public Image healthBar;
    public Image bleedingIndicator;
    public Text bandageCountText;

    [Header("Downed State UI")]
    public GameObject downedPanel;
    public Text downedTimerText;
    public Text nearestMedicText;
    public Text nearestPlayerText;
    public Button callMedicButton;
    public Button giveUpButton;
    public Image screenBloodOverlay;

    private void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("PlayerHealthUI: No PlayerHealth component found!");
                enabled = false;
                return;
            }
        }
        
        if (playerLoadout == null)
        {
            playerLoadout = GetComponentInParent<PlayerLoadout>();
            if (playerLoadout == null)
            {
                Debug.LogWarning("PlayerHealthUI: No PlayerLoadout component found!");
            }
        }

        // Subscribe to health events
        playerHealth.OnHealthStateChanged.AddListener(HandleHealthStateChanged);
        playerHealth.OnPlayerDowned.AddListener(HandlePlayerDowned);
        playerHealth.OnPlayerRevived.AddListener(HandlePlayerRevived);
        playerHealth.OnPlayerDied.AddListener(HandlePlayerDied);

        // Initialize UI
        UpdateHealthUI();
        if (downedPanel != null) downedPanel.SetActive(false);
        if (bleedingIndicator != null) bleedingIndicator.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerHealth == null) return;

        UpdateHealthUI();
        UpdateBleedingIndicator();
        UpdateBandageCount();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"Health: {playerHealth.GetHealthAsInt()}";
        
        if (healthBar != null)
            healthBar.fillAmount = playerHealth.GetHealthAsInt() / 100f;
        
        if (screenBloodOverlay != null)
        {
            float painAlpha = playerHealth.PainFactor * 0.5f;
            screenBloodOverlay.color = new Color(1, 0, 0, painAlpha);
        }
    }

    private void UpdateBleedingIndicator()
    {
        if (bleedingIndicator == null) return;

        if (playerHealth.IsBleeding)
        {
            bleedingIndicator.gameObject.SetActive(true);
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) / 2f;
            bleedingIndicator.color = new Color(1, 0, 0, pulse * 0.7f);
        }
        else
        {
            bleedingIndicator.gameObject.SetActive(false);
        }
    }

    private void UpdateBandageCount()
    {
        if (bandageCountText == null || playerLoadout == null) return;

        var medicalCategory = playerLoadout.GetCategory(4); // 4 is medical category
        if (medicalCategory != null)
        {
            int bandageIndex = medicalCategory.items.FindIndex(item => item != null && item.name.Contains("Bandage"));
            if (bandageIndex != -1)
            {
                bandageCountText.text = $"Bandages: {medicalCategory.itemCounts[bandageIndex]}";
            }
        }
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

    private void HandlePlayerDowned()
    {
        if (downedPanel == null) return;

        downedPanel.SetActive(true);
        StartCoroutine(UpdateDownedTimer());
    }

    private void HandlePlayerRevived()
    {
        if (downedPanel == null) return;

        downedPanel.SetActive(false);
        StopAllCoroutines();
    }

    private void HandlePlayerDied()
    {
        if (downedPanel == null) return;

        downedPanel.SetActive(false);
        // Show death screen or handle permanent death
    }

    private IEnumerator UpdateDownedTimer()
    {
        if (downedTimerText == null || playerHealth == null) yield break;

        while (playerHealth.DownedTimeRemaining > 0)
        {
            float remainingTime = playerHealth.DownedTimeRemaining;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            downedTimerText.text = $"Time until death: {minutes:00}:{seconds:00}";
            
            yield return new WaitForSeconds(1f);
        }
    }

    public void OnCallMedicPressed()
    {
        // TODO: Implement medic call functionality
        Debug.Log("Medic called!");
    }

    public void OnGiveUpPressed()
    {
        if (playerHealth != null)
            playerHealth.GiveUp();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthStateChanged.RemoveListener(HandleHealthStateChanged);
            playerHealth.OnPlayerDowned.RemoveListener(HandlePlayerDowned);
            playerHealth.OnPlayerRevived.RemoveListener(HandlePlayerRevived);
            playerHealth.OnPlayerDied.RemoveListener(HandlePlayerDied);
        }
    }
} 