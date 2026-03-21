    using UnityEngine;
    using System.Collections.Generic;

public class NPCInteractor : MonoBehaviour
{
    private PlayerMovement playerMovement;
    [SerializeField] private NPCDialogueView dialogueView;
    [SerializeField] private NPCDialogueModel dialogueModel;
    [Header("Input Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Header("Option Keys")]
    [SerializeField]
    private List<KeyCode> optionKeys = new List<KeyCode>
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3
        };
    [Header("Gameplay Systems")]
    [SerializeField] private MonoBehaviour hotbarScript;
    [Header("Relationship")]
    [SerializeField] private NPCRelationshipModel relationshipModel;
    [Header("Gift System")]
    [SerializeField] private GiftDatabaseSO giftDatabase;
    [Header("Inventory")]
    [SerializeField] private InventoryGameView inventoryGameView;
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private GameObject inventoryMenuRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Quest System")]
    [SerializeField] private QuestView questView;
    //[SerializeField] private QuestDatabase questDatabase;
    //[SerializeField] private int questIndex;
    [SerializeField] private QuestLogController questLogController;

    private IQuestService questService;
    private QuestPresenter questPresenter;
    private GiftPresenter giftPresenter;
    private DialogueNode interactionNode;
    private NPCState currentState = NPCState.Idle;
    private NPCDialoguePresenter presenter;
    private bool playerInRange;
    private bool blockInteractOnce;
    private enum NPCState
    {
        Idle,
        InteractionMenu,
        Dialogue,
        Gift,
        Quest,
        SimpleDialogue
    }
    private void Start()
    {
        var inventoryService = inventoryGameView.GetInventoryService();

        if (inventoryService != null)
        {
            inventoryService.OnInventoryChanged += UpdateQuestObjectives;
        }
        else
        {
            Debug.LogError($"[NPCInteractor] InventoryService trên {inventoryGameView.name} bị null!");
        }

        INPCDialogueService service = new NPCDialogueService(dialogueModel);
        questService = QuestManager.QuestService;
        questPresenter = new QuestPresenter(
            questView,
            questService,
            inventoryService,
            dialogueModel.npcName,
            dialogueModel.avatar
        );
        presenter = new NPCDialoguePresenter(
            service,
            dialogueView,
            questPresenter
        );

        CreateInteractionNode();
    }
    private void Update()
    {
        if (!playerInRange) return;

        // =====================
        // IDLE → Press E to open menu
        // =====================
        if (currentState == NPCState.Idle)
        {
            if (blockInteractOnce)
            {
                blockInteractOnce = false;
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                ShowInteractionMenu();
            }
            return;
        }

        // =====================
        // INTERACTION MENU
        // =====================
        if (currentState == NPCState.InteractionMenu)
        {
            HandleInteractionMenuInput();
            return;
        }

        // =====================
        // GIFT STATE
        // =====================
        if (currentState == NPCState.Gift)
        {
            if (giftPresenter == null)
            {
                Debug.LogError("[NPCInteractor] Gift state active but giftPresenter is NULL!");
                return;
            }
            giftPresenter.Update();
            return;
        }

        // =====================
        // SIMPLE DIALOGUE
        // =====================
        if (currentState == NPCState.SimpleDialogue)
        {
            if (Input.GetKeyDown(interactKey))
            {
                dialogueView.Hide();
                UnlockPlayer();
                currentState = NPCState.Idle;
                blockInteractOnce = true;
            }

            return;
        }

        // =====================
        // DIALOGUE STATE
        // =====================
        if (currentState == NPCState.Dialogue)
        {
            HandleDialogueUpdate();
            return;
        }

        // =====================
        // QUEST STATE
        // =====================
        if (currentState == NPCState.Quest)
        {
            HandleOptionInput();
            return;
        }
    }
    private void HandleInteractionMenuInput()
    {
        for (int i = 0; i < interactionNode.options.Count; i++)
        {
            if (i < optionKeys.Count && Input.GetKeyDown(optionKeys[i]))
            {
                if (i == 0) // talk
                {
                    dialogueView.Hide();
                    currentState = NPCState.Dialogue;
                    presenter.StartDialogue();
                }
                else if (i == 1) // GIFT
                {
                    dialogueView.Hide();
                    // Don't set currentState yet - wait for coroutine to complete
                    StartGiftMode();
                }
                

                else if (i == 2) 
                {
                    dialogueView.Hide();
                    var inventory = inventoryGameView.GetInventoryService();

                    QuestModel activeQuest = questService.GetActiveQuests()
                        .Find(q => q.npcName == dialogueModel.npcName);

                    if (activeQuest != null)
                    {
                        foreach (var obj in activeQuest.objectives)
                        {
                            int count = inventory.GetItemCount(obj.itemId);
                            questService.UpdateObjective(obj.objectiveId, count);
                        }

                        if (questService.IsQuestCompleted(activeQuest.questId))
                        {
                            if (questService.SubmitQuestItems(activeQuest.questId, inventory))
                            {
                                questService.GiveReward(activeQuest.questId, inventory);
                                questService.CompleteQuest(activeQuest.questId);
                                ShowSimpleDialogue("Cảm ơn bạn rất nhiều! Đây là phần thưởng.");
                            }
                        }
                        else
                        {
                            ShowSimpleDialogue("Bạn vẫn chưa thu thập đủ vật phẩm tôi cần.");
                        }
                    }
                    else
                    {
                        questPresenter = new QuestPresenter(
                            questView, questService, inventory,
                            dialogueModel.npcName, dialogueModel.avatar
                        );

                        if (questPresenter.TryPickRandomQuest())
                        {
                            currentState = NPCState.Quest;
                            questPresenter.ShowQuest();
                        }
                        else
                        {
                            ShowSimpleDialogue("Hiện tại tôi không có yêu cầu nào mới cho bạn.");
                        }
                    }
                    break;
                }
            }
        }
    }

    private void StartGiftMode()
    {
        if (inventoryGameView == null || giftDatabase == null)
        {
            Debug.LogError("[NPCInteractor] Missing InventoryGameView or GiftDatabase!");
            currentState = NPCState.Idle;
            return;
        }

        // Lock player
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Get InventoryView from InventoryGameView using reflection (like ShopPresenter does)
        InventoryView inventoryView = null;
        try
        {
            System.Reflection.FieldInfo field = typeof(InventoryGameView).GetField("inventoryView", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                inventoryView = (InventoryView)field.GetValue(inventoryGameView);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NPCInteractor] Failed to get InventoryView via reflection: {ex.Message}");
        }

        if (inventoryView == null)
        {
            Debug.LogError("[NPCInteractor] InventoryView is null!");
            currentState = NPCState.Idle;
            return;
        }

        if (hotbarScript != null)
            hotbarScript.enabled = false;

        if (inventoryMenuRoot != null)
            inventoryMenuRoot.SetActive(true);

        // Close inventory first to reset state from any previous use (e.g., Shop's crafting mode)
        inventoryGameView.CloseInventory();

        // Use coroutine to ensure proper state transitions
        StartCoroutine(OpenGiftModeInventory());
    }

    private System.Collections.IEnumerator OpenGiftModeInventory()
    {
        // Wait one frame to ensure state is reset
        yield return null;

        Debug.Log("[NPCInteractor] OpenGiftModeInventory started");

        // Notify inventory presenter about external action to reset sync cooldown
        inventoryGameView.NotifyExternalAction();
        Debug.Log("[NPCInteractor] NotifyExternalAction called");

        // Refresh inventory items before opening
        // This must be done BEFORE opening to ensure items display correctly
        // Use reflection since RefreshView is private but we need to force a refresh
        RefreshInventoryItems(inventoryGameView);
        Debug.Log("[NPCInteractor] RefreshInventoryItems called");

        // Open inventory in normal mode (not crafting)
        inventoryGameView.OpenInventory();
        Debug.Log("[NPCInteractor] OpenInventory called");
        
        // Wait for inventory UI to fully initialize
        yield return null;
        yield return null; // Extra frame for safety
        Debug.Log("[NPCInteractor] Waited for UI to initialize");

        // Get InventoryView from InventoryGameView using reflection
        InventoryView inventoryViewForGift = null;
        try
        {
            System.Reflection.FieldInfo field = typeof(InventoryGameView).GetField("inventoryView", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                inventoryViewForGift = (InventoryView)field.GetValue(inventoryGameView);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NPCInteractor] Failed to get InventoryView in coroutine: {ex.Message}");
        }

        if (inventoryViewForGift == null)
        {
            Debug.LogError("[NPCInteractor] inventoryViewForGift is NULL in coroutine!");
            yield break;
        }

        IGiftService giftService = new GiftService(giftDatabase);
        var inventoryService = inventoryGameView.GetInventoryService();

        Debug.Log($"[NPCInteractor] About to create GiftPresenter with inventoryViewForGift");
        
        giftPresenter = new GiftPresenter(
            giftService,
            inventoryService,
            inventoryViewForGift as IInventoryView,
            inventoryGameView,
            dialogueView,
            relationshipModel,
            dialogueModel   
        );

        Debug.Log("[NPCInteractor] GiftPresenter created");

        giftPresenter.OnGiftFinished += ExitGiftMode;

        giftPresenter.OnRequestCloseInventory += () =>
        {
            Debug.Log("[NPCInteractor] OnRequestCloseInventory invoked");
            inventoryGameView.CloseInventory();

            if (inventoryMenuRoot != null)
                inventoryMenuRoot.SetActive(false);
        };

        Debug.Log("[NPCInteractor] About to call StartGiftMode()");
        giftPresenter.StartGiftMode();
        Debug.Log("[NPCInteractor] StartGiftMode() called");
        
        // NOW set the state after everything is ready
        currentState = NPCState.Gift;
        Debug.Log("[NPCInteractor] Gift mode fully initialized and ready for input");
    }

    /// <summary>
    /// Refresh inventory items display using reflection.
    /// Ensures items are visible even after inventory was hidden by other systems (e.g., Shop).
    /// </summary>
    private void RefreshInventoryItems(InventoryGameView invGameView)
    {
        try
        {
            // Get the presenter field
            var presenterField = typeof(InventoryGameView).GetField("presenter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (presenterField == null) return;
            
            var presenter = presenterField.GetValue(invGameView);
            if (presenter == null) return;

            // Call the private RefreshView method
            var refreshMethod = presenter.GetType().GetMethod("RefreshView", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(presenter, null);
                Debug.Log("[NPCInteractor] Inventory items refreshed");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NPCInteractor] Could not refresh inventory: {ex.Message}");
        }
    }

    private void ExitGiftMode()
    {
        giftPresenter.StopGiftMode();
        giftPresenter.OnGiftFinished -= ExitGiftMode;

        inventoryGameView.CloseInventory();

        if (hotbarScript != null)
            hotbarScript.enabled = true;

        UnlockPlayer();
        currentState = NPCState.Idle;
        blockInteractOnce = true;
        dialogueView.Hide();
    }
    private void HandleDialogueUpdate()
    {
        if (!presenter.IsDialogueActive())
        {
            dialogueView.Hide();

            if (hotbarScript != null)
                hotbarScript.enabled = true;

            UnlockPlayer();

            currentState = NPCState.Idle;
            blockInteractOnce = true;   

            return;
        }

        if (dialogueView.IsTyping())
        {
            if (Input.GetKeyDown(interactKey))
            {
                dialogueView.ShowFullText(presenter.GetCurrentNode());
            }
            return;
        }

        if (dialogueView.IsShowingOptions())
        {
            HandleOptionInput();
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            presenter.Continue();
        }
    }
    private void CreateInteractionNode()
        {
            interactionNode = new DialogueNode();
            interactionNode.dialogueText = "What do you want to do?";

        interactionNode.options = new List<DialogueOption>
{
    new DialogueOption { optionText = "Talk", nextNodeIndex = -1 },
    new DialogueOption { optionText = "Send Gift", nextNodeIndex = -1 },
    new DialogueOption { optionText = "Quest", nextNodeIndex = -1 }
    };
    }
    private void ShowInteractionMenu()
    {
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (hotbarScript != null)
            hotbarScript.enabled = false;

        currentState = NPCState.InteractionMenu;

        dialogueView.ShowNode(
            dialogueModel.npcName,
            interactionNode,
            dialogueModel.avatar
        );
    }
    private void HandleOptionInput()
    {
        var node = presenter.GetCurrentNode();

        // =====================
        // QUEST OPTIONS
        // =====================
        if (currentState == NPCState.Quest)
        {
            if (Input.GetKeyDown(optionKeys[0])) // Accept
            {
                questPresenter.AcceptQuest();

                dialogueView.Hide();
                UnlockPlayer();
                currentState = NPCState.Idle;
            }
            else if (Input.GetKeyDown(optionKeys[1])) // Back
            {
                ShowInteractionMenu();
            }

            return;
        }

        // =====================
        // NORMAL DIALOGUE
        // =====================
        if (node == null || node.options == null) return;

        for (int i = 0; i < node.options.Count; i++)
        {
            if (i < optionKeys.Count && Input.GetKeyDown(optionKeys[i]))
            {
                presenter.SelectOption(i);

                if (!presenter.IsDialogueActive())
                {
                    UnlockPlayer();
                }

                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("PlayerEntity")) return;

            playerInRange = true;

            // Get PlayerMovement from the player that entered
            playerMovement = other.GetComponent<PlayerMovement>();
        
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        playerInRange = false;

        if (dialogueView != null)
            dialogueView.Hide();

        if (hotbarScript != null)
            hotbarScript.enabled = true;

        UnlockPlayer();
        playerMovement = null;
        currentState = NPCState.Idle;
    }
    private void ShowSimpleDialogue(string message)
    {
        DialogueNode node = new DialogueNode();
        node.dialogueText = message;
        node.options = null;

        dialogueView.ShowNode(
            dialogueModel.npcName,
            node,
            dialogueModel.avatar
        );

        currentState = NPCState.SimpleDialogue;
    }
    private void UpdateQuestObjectives()
    {
        var inventory = inventoryGameView.GetInventoryService();

        foreach (var quest in questService.GetActiveQuests())
        {

            foreach (var obj in quest.objectives)
            {

                int count = inventory.GetItemCount(obj.itemId);
                obj.currentAmount = Mathf.Min(count, obj.requiredAmount);
            }
        }
        QuestService.OnQuestUpdated?.Invoke();
    }
    private void UnlockPlayer()
        {
            if (playerMovement != null)
                playerMovement.enabled = true;
        }
    }
