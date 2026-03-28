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
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questItemPrefab;

    public void TogglePanel()
    {
        panelRoot.SetActive(!panelRoot.activeSelf);
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