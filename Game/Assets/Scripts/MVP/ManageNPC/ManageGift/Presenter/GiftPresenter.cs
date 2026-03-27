using UnityEngine;
using System;
using System.Collections.Generic;

public class GiftPresenter
{
    private readonly IGiftService giftService;
    private readonly IInventoryService inventoryService;
    private readonly IInventoryView inventoryView;
    private readonly InventoryGameView inventoryGameView;
    private readonly NPCDialogueView dialogueView;
    private readonly NPCRelationshipModel relationshipModel;
    private readonly NPCDialogueModel dialogueModel;
    public Action OnRequestCloseInventory;  
    private ItemModel selectedItem;
    private bool isWaitingForConfirm;
    private bool waitingForClose;
    private bool isInMessageWait;
    private Action<int> slotClickedHandler; // Store delegate for unsubscribe

    public event Action OnGiftFinished;

    public GiftPresenter(
        IGiftService giftService,
        IInventoryService inventoryService,
        IInventoryView inventoryView,
        InventoryGameView inventoryGameView,
        NPCDialogueView dialogueView,
        NPCRelationshipModel relationshipModel,
        NPCDialogueModel dialogueModel)
    {
        this.giftService = giftService;
        this.inventoryService = inventoryService;
        this.inventoryView = inventoryView;
        this.inventoryGameView = inventoryGameView;
        this.dialogueView = dialogueView;
        this.relationshipModel = relationshipModel;
        this.dialogueModel = dialogueModel;
    }

    // ===============================
    // START / STOP
    // ===============================

    public void StartGiftMode()
    {
        Debug.Log($"[GiftPresenter] StartGiftMode called - inventoryView is {(inventoryView == null ? "NULL" : "valid")}");
        if (inventoryView == null)
        {
            Debug.LogError("[GiftPresenter] inventoryView is NULL! Cannot subscribe to events!");
            return;
        }
        
        // Subscribe to slot clicks using reflection since OnSlotClicked is not public in interface
        try
        {
            var eventInfo = typeof(IInventoryView).GetEvent("OnSlotClicked", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (eventInfo == null)
            {
                // Try the actual InventoryView class if it's not in interface
                var inventoryClass = inventoryView.GetType();
                eventInfo = inventoryClass.GetEvent("OnSlotClicked", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }
            
            if (eventInfo != null)
            {
                slotClickedHandler = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), this, typeof(GiftPresenter).GetMethod("HandleSlotClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
                eventInfo.AddEventHandler(inventoryView, slotClickedHandler);
                Debug.Log("[GiftPresenter] Successfully subscribed to OnSlotClicked event using reflection");
            }
            else
            {
                Debug.LogError("[GiftPresenter] OnSlotClicked event not found!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GiftPresenter] Failed to subscribe to OnSlotClicked: {ex.Message}");
        }
        
        Debug.Log("[GiftPresenter] Gift Mode Started - Ready to select gifts");
    }

    public void StopGiftMode()
    {
        // Unsubscribe using reflection
        try
        {
            var eventInfo = typeof(IInventoryView).GetEvent("OnSlotClicked", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (eventInfo == null)
            {
                var inventoryClass = inventoryView.GetType();
                eventInfo = inventoryClass.GetEvent("OnSlotClicked", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }
            
            if (eventInfo != null && slotClickedHandler != null)
            {
                eventInfo.RemoveEventHandler(inventoryView, slotClickedHandler);
                Debug.Log("[GiftPresenter] Successfully unsubscribed from OnSlotClicked event");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GiftPresenter] Failed to unsubscribe from OnSlotClicked: {ex.Message}");
        }
        
        slotClickedHandler = null;
        isWaitingForConfirm = false;
        waitingForClose = false;
        isInMessageWait = false;
        Debug.Log("[GiftPresenter] Gift Mode Stopped");
    }

    // ===============================
    // HANDLE SLOT CLICK
    // ===============================

    private void HandleSlotClicked(int slotIndex)
    {
        Debug.Log($"[GiftPresenter] HandleSlotClicked called with slotIndex={slotIndex}");
        
        if (isWaitingForConfirm || waitingForClose || isInMessageWait)
        {
            Debug.LogWarning($"[GiftPresenter] Blocked - isWaitingForConfirm={isWaitingForConfirm}, waitingForClose={waitingForClose}, isInMessageWait={isInMessageWait}");
            return;
        }

        var item = inventoryService.GetItemAtSlot(slotIndex);
        if (item == null)
        {
            Debug.LogWarning($"[GiftPresenter] Item at slot {slotIndex} is null");
            return;
        }

        Debug.Log($"[GiftPresenter] Selected item: {item.ItemName}");
        
        // Kiểm tra nếu item không hợp lệ để tặng
        if (!giftService.CanGift(item))
        {
            Debug.Log($"[GiftPresenter] Item '{item.ItemName}' không hợp lệ để tặng");
            // Close inventory and show invalid message
            inventoryGameView.CloseInventory();
            OnRequestCloseInventory?.Invoke();
            ShowInvalidGiftMessage(item);
            return;
        }

        // Item hợp lệ - tắt inventory rồi hiển thị confirm dialog
        inventoryGameView.CloseInventory();
        OnRequestCloseInventory?.Invoke();
        
        selectedItem = item;
        ShowConfirmDialog(item);
    }

    // ===============================
    // CONFIRM DIALOG
    // ===============================

    private void ShowConfirmDialog(ItemModel item)
    {
        DialogueNode confirmNode = new DialogueNode();
        confirmNode.dialogueText = $"Would you like to give {item.ItemName} as a gift?";

        confirmNode.options = new List<DialogueOption>
        {
            new DialogueOption { optionText = "Yes", nextNodeIndex = 0 },
            new DialogueOption { optionText = "No", nextNodeIndex = 1 }
        };

        dialogueView.ShowNode(dialogueModel.npcName, confirmNode, dialogueModel.avatar);

        isWaitingForConfirm = true;
    }

    private void ShowInvalidGiftMessage(ItemModel item)
    {
        Debug.Log($"[GiftPresenter] ShowInvalidGiftMessage called for {item.ItemName}");
        Debug.Log($"[GiftPresenter] dialogueView: {(dialogueView == null ? "NULL" : "VALID")}, dialogueModel: {(dialogueModel == null ? "NULL" : "VALID")}");
        
        DialogueNode invalidNode = new DialogueNode();
        invalidNode.dialogueText = $"Sorry, {item.ItemName} are not valid gifts..";
        invalidNode.options = null;

        if (dialogueView != null)
        {
            dialogueView.ShowNode(dialogueModel.npcName, invalidNode, dialogueModel.avatar);
            Debug.Log("[GiftPresenter] Invalid message dialogue shown");
        }
        else
        {
            Debug.LogError("[GiftPresenter] Cannot show invalid message - dialogueView is NULL!");
        }

        isInMessageWait = true;
        Debug.Log("[GiftPresenter] Set isInMessageWait = true");
    }

    public void Update()
    {
        // Chờ xác nhận confirm dialog (Yes/No)
        if (isWaitingForConfirm)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("[GiftPresenter] Confirm option selected (Yes)");
                OnConfirmSelected(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("[GiftPresenter] Confirm option selected (No)");
                OnConfirmSelected(1);
            }

            return;
        }

        // Chờ E để đóng message (invalid item)
        if (isInMessageWait)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[GiftPresenter] Closing invalid message by pressing E");
                isInMessageWait = false;
                Finish();
            }

            return;
        }

        // Chờ E để đóng result message (gift successful/failed)
        if (waitingForClose)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[GiftPresenter] Closing result message by pressing E");
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
            ShowResultMessage(result.reactionText);
            return;
        }

        inventoryService.RemoveItem(selectedItem.ItemId, 1);
        relationshipModel.AddAffection(result.affectionGain);

        Debug.Log($"[GiftPresenter] Affection hiện tại: {relationshipModel.Affection}");

        ShowResultMessage(result.reactionText);
    }

    private void CancelGift()
    {
        ShowResultMessage("Cancel!!.");
    }

    // ===============================
    // MESSAGE FLOW
    // ===============================

    private void ShowResultMessage(string message)
    {
        DialogueNode node = new DialogueNode();
        node.dialogueText = message;
        node.options = null;

        dialogueView.ShowNode(dialogueModel.npcName, node, dialogueModel.avatar);

        waitingForClose = true;
    }

    private void Finish()
    {
        OnGiftFinished?.Invoke();
    }
}