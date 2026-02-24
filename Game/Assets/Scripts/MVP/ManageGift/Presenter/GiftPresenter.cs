using UnityEngine;
using System;
using System.Collections.Generic;

public class GiftPresenter
{
    private readonly IGiftService giftService;
    private readonly IInventoryService inventoryService;
    private readonly InventoryView inventoryView;
    private readonly NPCDialogueView dialogueView;
    private readonly NPCRelationshipModel relationshipModel;
    public Action OnRequestCloseInventory;  
    private ItemModel selectedItem;
    private bool isWaitingForConfirm;
    private bool waitingForClose;

    public event Action OnGiftFinished;

    public GiftPresenter(
        IGiftService giftService,
        IInventoryService inventoryService,
        InventoryView inventoryView,
        NPCDialogueView dialogueView,
        NPCRelationshipModel relationshipModel)
    {
        this.giftService = giftService;
        this.inventoryService = inventoryService;
        this.inventoryView = inventoryView;
        this.dialogueView = dialogueView;
        this.relationshipModel = relationshipModel;
    }

    // ===============================
    // START / STOP
    // ===============================

    public void StartGiftMode()
    {
        inventoryView.OnSlotClicked += HandleSlotClicked;
        Debug.Log("Gift Mode Started");
    }

    public void StopGiftMode()
    {
        inventoryView.OnSlotClicked -= HandleSlotClicked;
        isWaitingForConfirm = false;
        waitingForClose = false;
    }

    // ===============================
    // HANDLE SLOT CLICK
    // ===============================

    private void HandleSlotClicked(int slotIndex)
    {
        if (isWaitingForConfirm || waitingForClose) return;

        var item = inventoryService.GetItemAtSlot(slotIndex);
        if (item == null) return;

        if (!giftService.CanGift(item))
        {
            ShowMessageAndWait("This item cannot be given as a gift.");
            return;
        }

        selectedItem = item;

        // close inventory before showing confirm dialog to avoid UI overlap
        OnRequestCloseInventory?.Invoke();

        ShowConfirmDialog(item);
    }

    // ===============================
    // CONFIRM DIALOG
    // ===============================

    private void ShowConfirmDialog(ItemModel item)
    {
        DialogueNode confirmNode = new DialogueNode();
        confirmNode.dialogueText = $"Do you want to give {item.ItemName}?";

        confirmNode.options = new List<DialogueOption>
        {
            new DialogueOption { optionText = "Yes", nextNodeIndex = 0 },
            new DialogueOption { optionText = "Cancel", nextNodeIndex = 1 }
        };

        dialogueView.ShowNode("Confirm", confirmNode, null);

        isWaitingForConfirm = true;
    }

    // ===============================
    // UPDATE (Call from NPCInteractor)
    // ===============================

    public void Update()
    {
        if (isWaitingForConfirm)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                OnConfirmSelected(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                OnConfirmSelected(1);

            return;
        }

        if (waitingForClose)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                waitingForClose = false;
                Finish();
            }
        }
    }

    private void OnConfirmSelected(int optionIndex)
    {
        isWaitingForConfirm = false;

        if (optionIndex == 0)
            ProcessGift();
        else
            CancelGift();
    }

    // ===============================
    // PROCESS GIFT
    // ===============================

    private void ProcessGift()
    {
        var result = giftService.ProcessGift(selectedItem);

        if (!result.success)
        {
            ShowMessageAndWait(result.reactionText);
            return;
        }

        inventoryService.RemoveItem(selectedItem.ItemId, 1);
        relationshipModel.AddAffection(result.affectionGain);

        Debug.Log($"Affection hiện tại: {relationshipModel.Affection}");

        ShowMessageAndWait(result.reactionText);
    }

    private void CancelGift()
    {
        ShowMessageAndWait("Gift cancelled.");
    }

    // ===============================
    // MESSAGE FLOW
    // ===============================

    private void ShowMessageAndWait(string message)
    {
        DialogueNode node = new DialogueNode();
        node.dialogueText = message;
        node.options = null;

        dialogueView.ShowNode("NPC", node, null);

        waitingForClose = true;
    }

    private void Finish()
    {
        OnGiftFinished?.Invoke();
    }
}