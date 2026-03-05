using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class QuestView : MonoBehaviour
{
    [Header("Root Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("UI")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button acceptButton;

    public event Action OnAcceptQuest;

    private void Awake()
    {
        panelRoot.SetActive(false);

        acceptButton.onClick.AddListener(() =>
        {
            OnAcceptQuest?.Invoke();
        });
    }

    public void ShowQuest(QuestModel quest)
    {
        questNameText.text = quest.questName;
        descriptionText.text = quest.description;

        panelRoot.SetActive(true);
    }
    public void Hide()
    {
        panelRoot.SetActive(false);
    }

    public void Toggle()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    public bool IsOpen()
    {
        return panelRoot.activeSelf;
    }

}
