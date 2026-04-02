using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestPresenter
{
    private readonly IQuestService service;
    private readonly IInventoryService inventory;
    private readonly string npcName;
    private readonly Sprite avatar;

    private QuestModel quest;

    /// <summary>View subscribes to this event and renders itself — Presenter never calls View directly.</summary>
    public event System.Action<QuestDisplayData> OnQuestDataReady;
    public event System.Action OnQuestAccepted;
    public event System.Action OnQuestAcceptFailed;

    public QuestPresenter(
        IQuestService service,
        IInventoryService inventory,
        string npcName,
        Sprite avatar)
    {
        this.service   = service;
        this.inventory = inventory;
        this.npcName   = npcName;
        this.avatar    = avatar;
    }

    // ── Called by NPCInteractionPresenter ──────────────────────────────
    /// <summary>
    /// Picks a random quest, renders it in the shared QuestView, and subscribes
    /// this presenter's AcceptQuest to the view's OnAccept event for this session.
    /// Returns false if no quest is available.
    /// </summary>
    public bool LoadAndDisplay(string lastQuestId)
    {
        if (!QuestCatalogService.Instance.IsReady) return false;

        var available = QuestCatalogService.Instance.GetAllQuests()
            .Where(q => q.NPCName == npcName
                     && !service.IsQuestActive(q.questId)
                     && q.questId != lastQuestId)
            .ToList();

        if (available.Count == 0) return false;

        // Weighted random selection
        float total       = available.Sum(q => q.Weight);
        float roll        = Random.Range(0, total);
        float accumulated = 0;
        QuestCatalogData selected = available[0];
        foreach (var q in available)
        {
            accumulated += q.Weight;
            if (roll <= accumulated) { selected = q; break; }
        }

        quest = new QuestModel
        {
            questId     = selected.questId,
            questName   = selected.questName,
            description = selected.description,
            npcName     = selected.NPCName,
            reward      = selected.reward,
            objectives  = selected.objectives,
            status      = QuestStatus.NotAccepted
        };

        // Build render-only DTO — View never sees QuestModel.
        var data = new QuestDisplayData
        {
            questName      = quest.questName,
            description    = quest.description,
            npcName        = this.npcName,
            avatar         = this.avatar,
            hasReward      = quest.reward != null,
            rewardQuantity = quest.reward != null ? quest.reward.quantity : 0,
            rewardIcon     = quest.reward != null
                             ? ItemCatalogService.Instance.GetCachedSprite(quest.reward.itemId)
                             : null
        };

        // Fire event — View subscribes to render itself. Presenter never calls View directly.
        OnQuestDataReady?.Invoke(data);
        return true;
    }

    // ── Called directly by NPCInteractionPresenter when player confirms accept ──
    public void HandleAccept()
    {
        if (quest == null) { OnQuestAcceptFailed?.Invoke(); return; }

        bool success = service.AcceptQuest(quest, inventory);

        if (success)
            OnQuestAccepted?.Invoke();
        else
            OnQuestAcceptFailed?.Invoke();
    }

    /// <summary>Call when the player cancels (Back).</summary>
    public void CancelQuest()
    {
        quest = null;
    }
}