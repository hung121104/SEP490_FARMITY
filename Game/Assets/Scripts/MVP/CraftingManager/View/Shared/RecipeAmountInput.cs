using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeAmountInput : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private InputField amountInputField;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button maxButton;

    [Header("Settings")]
    [SerializeField] private int minAmount = 1;
    [SerializeField] private int defaultAmount = 1;

    // Events
    public event Action<int> OnAmountChanged;

    // State
    private int currentAmount;
    private int maxPossibleAmount = 999;

    public int CurrentAmount => currentAmount;

    private void Awake()
    {
        SetupButtons();
        SetAmount(defaultAmount);
    }

    private void SetupButtons()
    {
        increaseButton?.onClick.AddListener(IncreaseAmount);
        decreaseButton?.onClick.AddListener(DecreaseAmount);
        maxButton?.onClick.AddListener(SetToMaxAmount);

        amountInputField?.onEndEdit.AddListener(OnInputFieldChanged);
        amountInputField?.onValueChanged.AddListener(OnInputFieldValueChanged);
    }

    /// <summary>
    /// Set current amount
    /// </summary>
    public void SetAmount(int amount)
    {
        currentAmount = Mathf.Clamp(amount, minAmount, maxPossibleAmount);
        UpdateDisplay();
        OnAmountChanged?.Invoke(currentAmount);
    }

    /// <summary>
    /// Set maximum possible amount (based on ingredients available)
    /// </summary>
    public void SetMaxPossibleAmount(int maxAmount)
    {
        maxPossibleAmount = Mathf.Max(minAmount, maxAmount);

        // Clamp current amount if it exceeds new max
        if (currentAmount > maxPossibleAmount)
        {
            SetAmount(maxPossibleAmount);
        }
        else
        {
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Reset to default amount
    /// </summary>
    public void Reset()
    {
        SetAmount(defaultAmount);
    }

    private void IncreaseAmount()
    {
        SetAmount(currentAmount + 1);
    }

    private void DecreaseAmount()
    {
        SetAmount(currentAmount - 1);
    }

    private void SetToMaxAmount()
    {
        SetAmount(maxPossibleAmount);
    }

    private void OnInputFieldChanged(string value)
    {
        if (int.TryParse(value, out int amount))
        {
            SetAmount(amount);
        }
        else
        {
            SetAmount(defaultAmount);
        }
    }

    private void OnInputFieldValueChanged(string value)
    {
        // Prevent invalid characters while typing
        if (string.IsNullOrEmpty(value)) return;

        if (!int.TryParse(value, out _))
        {
            amountInputField.text = currentAmount.ToString();
        }
    }

    private void UpdateDisplay()
    {
        // Update input field
        if (amountInputField != null)
        {
            amountInputField.text = currentAmount.ToString();
        }

        // Update button states
        if (decreaseButton != null)
        {
            decreaseButton.interactable = (currentAmount > minAmount);
        }

        if (increaseButton != null)
        {
            increaseButton.interactable = (currentAmount < maxPossibleAmount);
        }

        if (maxButton != null)
        {
            maxButton.interactable = (currentAmount < maxPossibleAmount);
        }
    }

    private void OnDestroy()
    {
        increaseButton?.onClick.RemoveAllListeners();
        decreaseButton?.onClick.RemoveAllListeners();
        maxButton?.onClick.RemoveAllListeners();
        amountInputField?.onEndEdit.RemoveAllListeners();
        amountInputField?.onValueChanged.RemoveAllListeners();
    }
}
