using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>Render-only DTO for one quest row in the quest log. View never touches QuestModel.</summary>
public class QuestLogItemData
{
    public string questName;
    public List<string> objectiveTexts; // pre-formatted by Presenter: "desc amount/required"
}

public class QuestLogView : MonoBehaviour
{
    [Header("Quest Log Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questItemPrefab;

    [Header("Pinned Quest Panel")]
    [SerializeField] private GameObject pinnedPanelRoot;
    [SerializeField] private TMP_Text pinnedObjectiveText;

    private void Awake()
    {
        if (pinnedPanelRoot != null)
            pinnedPanelRoot.SetActive(false);
    }

    public void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    /// <summary>Called by QuestLogController when Presenter fires OnPinnedQuestChanged.</summary>
    public void ShowPinnedQuest(QuestLogItemData data)
    {
        if (pinnedPanelRoot == null) return;

        if (data == null)
        {
            pinnedPanelRoot.SetActive(false);
            return;
        }

        pinnedPanelRoot.SetActive(true);

        if (pinnedObjectiveText != null)
            pinnedObjectiveText.text = data.objectiveTexts != null && data.objectiveTexts.Count > 0
                ? string.Join("\n", data.objectiveTexts)
                : string.Empty;
    }

    public void ShowQuestList(List<QuestLogItemData> items)
    {
        // Clear old UI
        foreach (Transform child in questListContainer)
            Destroy(child.gameObject);

        foreach (var item in items)
        {
            GameObject entry = Instantiate(questItemPrefab, questListContainer);

            TMP_Text questNameText =
                entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();

            Transform objectiveList =
                entry.transform.Find("ObjectiveList");

            TMP_Text objectiveTemplate =
                objectiveList.GetChild(0).GetComponent<TMP_Text>();

            questNameText.text = item.questName;

            objectiveTemplate.gameObject.SetActive(false);

            foreach (var text in item.objectiveTexts)
            {
                TMP_Text objective = Instantiate(objectiveTemplate, objectiveList);
                objective.gameObject.SetActive(true);
                objective.text = text;
            }
        }
    }
}