using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;

public class PlayerHealthManager : MonoBehaviour
{
    [SerializeField] private Transform playerEntity;
    
    public Slider healthBar;
    public Slider healthBarEase;
    public TextMeshProUGUI healthText;

    private float targetHealthValue;
    private StatsManager statsManager;
    private bool isInvulnerable = false;

    private void Start()
    {
        // Find local player using Photon
        if (playerEntity == null)
        {
            GameObject playerObj = FindLocalPlayerEntity();
            if (playerObj != null)
            {
                playerEntity = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("PlayerHealthManager: Local PlayerEntity not found!");
                enabled = false;
                return;
            }
        }

        statsManager = StatsManager.Instance;
        if (statsManager == null)
        {
            statsManager = FindObjectOfType<StatsManager>();
            if (statsManager == null)
            {
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
    }

    private void Update()
    {
        if (healthBarEase != null)
        {
            healthBarEase.value = Mathf.Lerp(
                healthBarEase.value,
                targetHealthValue,
                statsManager.easeSpeed * Time.deltaTime
            );
        }

        UpdateHealthText();
    }

    private GameObject FindLocalPlayerEntity()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return go;
            }
        }
        return null;
    }

    public void ChangeHealth(int amount)
    {
        if (statsManager == null || (isInvulnerable && amount < 0))
            return;

        statsManager.CurrentHealth += amount;

        if (healthBar != null)
        {
            healthBar.value = statsManager.CurrentHealth;
        }

        targetHealthValue = statsManager.CurrentHealth;

        if (statsManager.CurrentHealth <= 0)
        {
            playerEntity.gameObject.SetActive(false);
        }
    }

    public void RefreshHealthBar()
    {
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
        if (healthText != null)
        {
            healthText.text = statsManager.CurrentHealth.ToString();
        }
    }

    #region Invulnerability (iFrames)

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