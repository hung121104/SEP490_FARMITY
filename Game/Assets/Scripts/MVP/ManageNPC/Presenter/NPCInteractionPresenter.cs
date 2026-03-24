using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure-C# Presenter that owns ALL NPC interaction logic.
/// No MonoBehaviour, no Input.GetKeyDown — uses InputManager for input.
/// The MonoBehaviour View (NPCInteractor) delegates Update() and trigger events here.
/// </summary>
public class NPCInteractionPresenter
{
    // ─── State ───
    private NPCInteractionState currentState = NPCInteractionState.Idle;
    private bool playerInRange;
    private bool blockInteractOnce;
    private PlayerMovement playerMovement;
    private string lastCompletedQuestId = "";

    // ─── View interface (callbacks to MonoBehaviour) ───
    private readonly INPCInteractorView view;

    // ─── Unity component references (passed at construction) ───
    private readonly NPCDialogueView dialogueView;
    private readonly NPCDialogueModel dialogueModel;
    private readonly QuestView questView;
    private readonly NPCRelationshipModel relationshipModel;
    private readonly GiftDatabaseSO giftDatabase;
    private readonly InventoryGameView inventoryGameView;
    private readonly MonoBehaviour hotbarScript;

    // ─── Sub-presenters ───
    private NPCDialoguePresenter dialoguePresenter;
    private QuestPresenter questPresenter;
    private GiftPresenter giftPresenter;

    // ─── Services ───
    private IQuestService questService;

    // ─── Interaction menu node ───
    private DialogueNode interactionNode;

    // ─────────────────────────────────────────────────────────────────
    // Construction
    // ─────────────────────────────────────────────────────────────────

    public NPCInteractionPresenter(
        INPCInteractorView view,
        NPCDialogueView dialogueView,
        NPCDialogueModel dialogueModel,
        QuestView questView,
        NPCRelationshipModel relationshipModel,
        GiftDatabaseSO giftDatabase,
        InventoryGameView inventoryGameView,
        MonoBehaviour hotbarScript)
    {
        this.view = view;
        this.dialogueView = dialogueView;
        this.dialogueModel = dialogueModel;
        this.questView = questView;
        this.relationshipModel = relationshipModel;
        this.giftDatabase = giftDatabase;
        this.inventoryGameView = inventoryGameView;
        this.hotbarScript = hotbarScript;
    }

    // ─────────────────────────────────────────────────────────────────
    // Initialization (called once from View.Start)
    // ─────────────────────────────────────────────────────────────────

    public void Initialize()
    {
        var inventoryService = view.GetInventoryService();

        if (inventoryService != null)
            inventoryService.OnInventoryChanged += UpdateQuestObjectives;
        else
            Debug.LogError($"[NPCInteractionPresenter] InventoryService on {dialogueModel.npcName} is null!");

        questService = QuestManager.QuestService;

        questPresenter = new QuestPresenter(
            questView,
            questService,
            inventoryService,
            dialogueModel.npcName,
            dialogueModel.avatar
        );

        INPCDialogueService dialogueService = new NPCDialogueService(dialogueModel);
        dialoguePresenter = new NPCDialoguePresenter(dialogueService, dialogueView, questPresenter);

        CreateInteractionNode();
    }

    // ─────────────────────────────────────────────────────────────────
    // Trigger callbacks (from View.OnTriggerEnter/Exit2D)
    // ─────────────────────────────────────────────────────────────────

    public void OnPlayerEnter(PlayerMovement pm)
    {
        playerInRange = true;
        playerMovement = pm;
    }

    public void OnPlayerExit()
    {
        playerInRange = false;
        dialogueView?.Hide();
        view.EnableHotbar(true);
        view.UnlockPlayer();
        playerMovement = null;
        currentState = NPCInteractionState.Idle;
    }

    // ─────────────────────────────────────────────────────────────────
    // Update loop (called every frame by View.Update)
    // ─────────────────────────────────────────────────────────────────

    public void Update()
    {
        if (!playerInRange) return;

        switch (currentState)
        {
            case NPCInteractionState.Idle:
                HandleIdleState();
                break;

            case NPCInteractionState.InteractionMenu:
                HandleInteractionMenuInput();
                break;

            case NPCInteractionState.Gift:
                HandleGiftState();
                break;

            case NPCInteractionState.SimpleDialogue:
                HandleSimpleDialogueState();
                break;

            case NPCInteractionState.Dialogue:
                HandleDialogueUpdate();
                break;

            case NPCInteractionState.Quest:
                HandleOptionInput();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // State handlers
    // ─────────────────────────────────────────────────────────────────

    private void HandleIdleState()
    {
        if (blockInteractOnce)
        {
            blockInteractOnce = false;
            return;
        }

        if (InputManager.Instance.Interact.WasPressedThisFrame())
            ShowInteractionMenu();
    }

    private void HandleGiftState()
    {
        if (giftPresenter == null)
        {
            Debug.LogError("[NPCInteractionPresenter] Gift state active but giftPresenter is NULL!");
            return;
        }
        giftPresenter.Update();
    }

    private void HandleSimpleDialogueState()
    {
        if (InputManager.Instance.Interact.WasPressedThisFrame())
        {
            dialogueView.Hide();
            view.UnlockPlayer();
            currentState = NPCInteractionState.Idle;
            blockInteractOnce = true;
        }
    }

    private void HandleDialogueUpdate()
    {
        if (!dialoguePresenter.IsDialogueActive())
        {
            dialogueView.Hide();
            view.EnableHotbar(true);
            view.UnlockPlayer();
            currentState = NPCInteractionState.Idle;
            blockInteractOnce = true;
            return;
        }

        if (dialogueView.IsTyping())
        {
            if (InputManager.Instance.Interact.WasPressedThisFrame())
                dialogueView.ShowFullText(dialoguePresenter.GetCurrentNode());
            return;
        }

        if (dialogueView.IsShowingOptions())
        {
            HandleOptionInput();
            return;
        }

        if (InputManager.Instance.Interact.WasPressedThisFrame())
            dialoguePresenter.Continue();
    }

    private void HandleInteractionMenuInput()
    {
        for (int i = 0; i < interactionNode.options.Count; i++)
        {
            var slotAction = InputManager.Instance.GetHotbarSlotAction(i);
            if (slotAction == null || !slotAction.WasPressedThisFrame()) continue;

            if (i == 0) // Talk
            {
                dialogueView.Hide();
                currentState = NPCInteractionState.Dialogue;
                dialoguePresenter.StartDialogue();
            }
            else if (i == 1) // Gift
            {
                dialogueView.Hide();
                StartGiftMode();
            }
            else if (i == 2) // Quest
            {
                dialogueView.Hide();
                HandleQuestInteraction();
            }
            break;
        }
    }

    private void HandleOptionInput()
    {
        // Quest accept/back options
        if (currentState == NPCInteractionState.Quest)
        {
            if (InputManager.Instance.GetHotbarSlotAction(0).WasPressedThisFrame()) // Accept
            {
                questPresenter.AcceptQuest();
                dialogueView.Hide();
                view.UnlockPlayer();
                currentState = NPCInteractionState.Idle;
            }
            else if (InputManager.Instance.GetHotbarSlotAction(1).WasPressedThisFrame()) // Back
            {
                ShowInteractionMenu();
            }
            return;
        }

        // Normal dialogue branch options
        var node = dialoguePresenter.GetCurrentNode();
        if (node == null || node.options == null) return;

        for (int i = 0; i < node.options.Count; i++)
        {
            var slotAction = InputManager.Instance.GetHotbarSlotAction(i);
            if (slotAction != null && slotAction.WasPressedThisFrame())
            {
                dialoguePresenter.SelectOption(i);
                if (!dialoguePresenter.IsDialogueActive())
                    view.UnlockPlayer();
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Quest logic
    // ─────────────────────────────────────────────────────────────────

    private void HandleQuestInteraction()
    {
        UpdateQuestObjectives();
        var inventory = view.GetInventoryService();

        QuestModel activeQuest = questService.GetActiveQuests()
            .Find(q => q.npcName == dialogueModel.npcName);

        if (activeQuest != null)
        {
            if (questService.IsQuestCompleted(activeQuest.questId))
            {
                if (questService.SubmitQuestItems(activeQuest.questId, inventory))
                {
                    lastCompletedQuestId = activeQuest.questId;
                    questService.GiveReward(activeQuest.questId, inventory);
                    questService.CompleteQuest(activeQuest.questId);

                    var reward = activeQuest.reward;
                    Sprite rewardSprite = ItemCatalogService.Instance.GetCachedSprite(reward.itemId);
                    ItemData itemData = ItemCatalogService.Instance.GetItemData(reward.itemId);
                    string displayName = itemData != null ? itemData.itemName : reward.itemId;

                    dialogueView.ShowReward(rewardSprite, reward.quantity);
                    ShowSimpleDialogue($"Thank you! You received: {displayName} x{reward.quantity}.");
                }
            }
            else
            {
                ShowSimpleDialogue("You haven't finished the task I assigned you. Please come back when you've collected enough items.!");
            }
        }
        else
        {
            if (questPresenter.TryPickRandomQuest(lastCompletedQuestId))
            {
                questPresenter.ShowQuest();
                currentState = NPCInteractionState.Quest;
            }
            else
            {
                ShowSimpleDialogue("I don't need your help with anything right now..");
            }
        }
    }

    private void UpdateQuestObjectives()
    {
        var inventory = view.GetInventoryService();
        if (inventory == null) return;

        foreach (var quest in questService.GetActiveQuests())
        {
            foreach (var obj in quest.objectives)
            {
                int count = inventory.GetItemCount(obj.itemId);
                questService.UpdateObjective(obj.objectiveId, count);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Gift mode
    // ─────────────────────────────────────────────────────────────────

    private void StartGiftMode()
    {
        if (inventoryGameView == null || giftDatabase == null)
        {
            Debug.LogError("[NPCInteractionPresenter] Missing InventoryGameView or GiftDatabase!");
            currentState = NPCInteractionState.Idle;
            return;
        }

        view.LockPlayer();
        view.EnableHotbar(false);
        view.SetInventoryMenuRoot(true);
        view.CloseInventory();

        // Coroutine lives in Presenter; View hosts it via StartPresenterCoroutine
        view.StartPresenterCoroutine(OpenGiftModeCoroutine());
    }

    private IEnumerator OpenGiftModeCoroutine()
    {
        yield return null;

        view.NotifyExternalAction();
        RefreshInventoryItems(inventoryGameView);
        view.OpenInventory();

        yield return null;
        yield return null; // extra frame for UI init

        InventoryView inventoryViewForGift = GetInventoryViewFromGameView(inventoryGameView);

        if (inventoryViewForGift == null)
        {
            Debug.LogError("[NPCInteractionPresenter] inventoryViewForGift is NULL in OpenGiftModeCoroutine!");
            yield break;
        }

        IGiftService giftService = new GiftService(giftDatabase);
        var inventoryService = view.GetInventoryService();

        giftPresenter = new GiftPresenter(
            giftService,
            inventoryService,
            inventoryViewForGift as IInventoryView,
            inventoryGameView,
            dialogueView,
            relationshipModel,
            dialogueModel
        );

        giftPresenter.OnGiftFinished += ExitGiftMode;
        giftPresenter.OnRequestCloseInventory += () =>
        {
            view.CloseInventory();
            view.SetInventoryMenuRoot(false);
        };

        giftPresenter.StartGiftMode();
        currentState = NPCInteractionState.Gift;
    }

    private void ExitGiftMode()
    {
        giftPresenter.StopGiftMode();
        giftPresenter.OnGiftFinished -= ExitGiftMode;

        view.CloseInventory();
        view.EnableHotbar(true);
        view.UnlockPlayer();

        currentState = NPCInteractionState.Idle;
        blockInteractOnce = true;
        dialogueView.Hide();
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private void ShowInteractionMenu()
    {
        view.LockPlayer();
        view.EnableHotbar(false);
        currentState = NPCInteractionState.InteractionMenu;
        dialogueView.ShowNode(dialogueModel.npcName, interactionNode, dialogueModel.avatar);
    }

    private void ShowSimpleDialogue(string message)
    {
        var node = new DialogueNode
        {
            dialogueText = message,
            options = null
        };
        dialogueView.ShowNode(dialogueModel.npcName, node, dialogueModel.avatar);
        currentState = NPCInteractionState.SimpleDialogue;
    }

    private void CreateInteractionNode()
    {
        interactionNode = new DialogueNode
        {
            dialogueText = "What do you want to do?",
            options = new List<DialogueOption>
            {
                new DialogueOption { optionText = "Talk",      nextNodeIndex = -1 },
                new DialogueOption { optionText = "Send Gift", nextNodeIndex = -1 },
                new DialogueOption { optionText = "Quest",     nextNodeIndex = -1 }
            }
        };
    }

    // ─── Reflection helpers (access private fields of InventoryGameView) ───

    private static InventoryView GetInventoryViewFromGameView(InventoryGameView invGameView)
    {
        try
        {
            var field = typeof(InventoryGameView).GetField(
                "inventoryView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(invGameView) as InventoryView;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NPCInteractionPresenter] Failed to get InventoryView via reflection: {ex.Message}");
            return null;
        }
    }

    private static void RefreshInventoryItems(InventoryGameView invGameView)
    {
        try
        {
            var presenterField = typeof(InventoryGameView).GetField(
                "presenter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (presenterField == null) return;

            var presenter = presenterField.GetValue(invGameView);
            if (presenter == null) return;

            var refreshMethod = presenter.GetType().GetMethod(
                "RefreshView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            refreshMethod?.Invoke(presenter, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NPCInteractionPresenter] Could not refresh inventory: {ex.Message}");
        }
    }
}
