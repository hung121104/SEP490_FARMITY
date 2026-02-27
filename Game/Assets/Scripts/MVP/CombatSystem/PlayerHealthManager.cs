using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class PlayerHealthManager : MonoBehaviour
{
    public Slider healthBar;
    public Slider healthBarEase;
    public TextMeshProUGUI healthText;

    private Transform playerEntity;
    private float targetHealthValue;
    private StatsManager statsManager;
    private bool isInvulnerable = false;
    private bool isInitialized = false;

    private void Start()
    {
        StartCoroutine(DelayedInitialize());
    }

    private void Update()
    {
        if (!isInitialized || healthBarEase == null || statsManager == null)
            return;

        healthBarEase.value = Mathf.Lerp(
            healthBarEase.value,
            targetHealthValue,
            statsManager.easeSpeed * Time.deltaTime
        );

        UpdateHealthText();
    }

    private IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(0.5f);
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        GameObject playerObj = FindLocalPlayerEntity();
        if (playerObj == null)
        {
            Debug.LogError("[PlayerHealthManager] Local player not found!");
            enabled = false;
            return;
        }

        playerEntity = playerObj.transform;

        statsManager = StatsManager.Instance;
        if (statsManager == null)
        {
            statsManager = FindObjectOfType<StatsManager>();
            if (statsManager == null)
            {
                Debug.LogError("[PlayerHealthManager] StatsManager not found!");
                enabled = false;
                return;
            }
        }

        int maxHealth = statsManager.GetMaxHealth();
        statsManager.CurrentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = statsManager.CurrentHealth;
        }

        if (healthBarEase != null)
        {
            healthBarEase.maxValue = maxHealth;
            healthBarEase.value = statsManager.CurrentHealth;
        }

        targetHealthValue = statsManager.CurrentHealth;
        isInitialized = true;
    }

    private GameObject FindLocalPlayerEntity()
    {
        // Try "Player" tag first (multiplayer spawn)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        // Fallback to "PlayerEntity" tag (test scenes)
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
                return go;
        }

        return null;
    }

    public void ChangeHealth(int amount)
    {
        if (statsManager == null || (isInvulnerable && amount < 0))
            return;

        statsManager.CurrentHealth += amount;

        if (healthBar != null)
            healthBar.value = statsManager.CurrentHealth;

        targetHealthValue = statsManager.CurrentHealth;

        if (statsManager.CurrentHealth <= 0)
        {
            playerEntity.gameObject.SetActive(false);
        }
    }

    public void RefreshHealthBar()
    {
        if (statsManager == null)
            return;

        int maxHealth = statsManager.GetMaxHealth();

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = statsManager.CurrentHealth;
        }

        if (healthBarEase != null)
        {
            healthBarEase.maxValue = maxHealth;
            healthBarEase.value = statsManager.CurrentHealth;
        }

        targetHealthValue = statsManager.CurrentHealth;
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText != null && statsManager != null)
            healthText.text = statsManager.CurrentHealth.ToString();
    }

    #region Invulnerability

    public void SetInvulnerable(float duration)
    {
        StartCoroutine(InvulnerabilityCoroutine(duration));
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }

    private IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    public bool IsInvulnerable => isInvulnerable;

    #endregion
}