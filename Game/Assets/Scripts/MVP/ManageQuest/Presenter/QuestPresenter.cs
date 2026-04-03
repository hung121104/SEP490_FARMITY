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

    // ── Daily offer cache ──────────────────────────────────────────────────────
    /// <summary>
    /// Quest offered to the player today. Stays the same until a new day starts.
    /// Cleared by <see cref="ClearDailyOffer"/> which is called from OnDayChanged.
    /// </summary>
    private QuestCatalogData _cachedOffer;

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
    /// Returns the quest offered today (cached), or rolls a new one if none cached.
    /// The same quest is shown every time until a new day starts (see ClearDailyOffer).
    /// Returns false if no quest is available.
    /// </summary>
    public bool LoadAndDisplay(string lastQuestId)
    {
        if (!QuestCatalogService.Instance.IsReady) return false;

        QuestCatalogData selected;

        // ── Use cached offer if still valid for today ───────────────────────
        if (_cachedOffer != null
            && !service.IsQuestActive(_cachedOffer.questId)
            && !service.IsQuestTurnedIn(_cachedOffer.questId))
        {
            selected = _cachedOffer;
            Debug.Log($"[QuestPresenter] Using cached daily offer '{selected.questId}' for NPC '{npcName}'");
        }
        else
        {
            // ── Roll new quest ──────────────────────────────────────────────
            _cachedOffer = null;

            var available = QuestCatalogService.Instance.GetAllQuests()
                .Where(q => q.NPCName == npcName
                         && !service.IsQuestActive(q.questId)
                         && !service.IsQuestTurnedIn(q.questId)
                         && q.questId != lastQuestId)
                .ToList();

            if (available.Count == 0) return false;

            // Weighted random selection
            float total       = available.Sum(q => q.Weight);
            float roll        = total > 0 ? Random.Range(0f, total) : 0f;
            float accumulated = 0;
            selected = available[0];
            foreach (var q in available)
            {
                accumulated += q.Weight;
                if (roll <= accumulated) { selected = q; break; }
            }

            _cachedOffer = selected;
            Debug.Log($"[QuestPresenter] Rolled new daily offer '{selected.questId}' for NPC '{npcName}'");
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

    /// <summary>Call when the player cancels (Back). Does NOT clear the daily offer cache.</summary>
    public void CancelQuest()
    {
        quest = null;
    }

    /// <summary>
    /// Clears the cached daily offer so a new quest is rolled on next interaction.
    /// Subscribe this to <see cref="TimeManagerView.OnDayChanged"/>.
    /// </summary>
    public void ClearDailyOffer()
    {
        _cachedOffer = null;
        Debug.Log($"[QuestPresenter] Daily offer cleared for NPC '{npcName}'");
    }
}