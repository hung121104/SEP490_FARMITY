using System.Collections.Generic;
using System.Linq;

public class QuestLogPresenter
{
    private readonly IQuestService service;

    /// <summary>View subscribes to this event and calls ShowQuestList itself — Presenter never calls View directly.</summary>
    public event System.Action<System.Collections.Generic.List<QuestLogItemData>> OnItemsRefreshed;

    /// <summary>Fires with the first active quest (or null when list is empty). PinnedQuestView subscribes.</summary>
    public event System.Action<QuestLogItemData> OnPinnedQuestChanged;

    public QuestLogPresenter(IQuestService service)
    {
        this.service = service;
    }

    // ── Called by View (button click via QuestLogController) ──
    public void OpenQuestLog() => Refresh();

    public void Refresh()
    {
        var items = service.GetActiveQuests()
            .Select(q => new QuestLogItemData
            {
                questName      = q.questName,
                objectiveTexts = q.objectives
                    .Select(o => $"{o.description} {o.currentAmount}/{o.requiredAmount}")
                    .ToList()
            })
            .ToList();

        // Fire event — View handles its own rendering.
        OnItemsRefreshed?.Invoke(items);

        // Pinned quest = first item in list (null if empty).
        OnPinnedQuestChanged?.Invoke(items.Count > 0 ? items[0] : null);
    }

    // De-dup safe: remove before add to prevent double-subscribe on OnEnable cycles.
    public void Subscribe()   { service.OnQuestUpdated -= Refresh; service.OnQuestUpdated += Refresh; }
    public void Unsubscribe() => service.OnQuestUpdated -= Refresh;
}