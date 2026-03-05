using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestLogView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questItemPrefab;

    [Header("Detail")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private GameObject detailPanel;

    private QuestModel currentOpenedQuest;

    public void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    public void ShowQuestList(List<QuestModel> quests)
    {
        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);

        foreach (var quest in quests)
        {
            GameObject item = Instantiate(questItemPrefab, questListContainer);

            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            text.text = quest.questName;

            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                ToggleQuestDetail(quest);
            });
        }
    }

    private void ToggleQuestDetail(QuestModel quest)
    {
        if (currentOpenedQuest == quest)
        {
            // collapse
            detailPanel.SetActive(false);
            currentOpenedQuest = null;
            return;
        }

        // open new detail
        currentOpenedQuest = quest;

        questNameText.text = quest.questName;
        questDescriptionText.text = quest.description;

        detailPanel.SetActive(true);
    }
}