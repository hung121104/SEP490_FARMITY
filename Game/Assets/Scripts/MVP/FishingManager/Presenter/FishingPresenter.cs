using System;
using UnityEngine;

public class FishingPresenter
{
    private IFishingView view;
    private IFishingService service;
    private FishingModel model;

    
    public FishingPresenter(IFishingView view, IFishingService service, FishingModel model)
    {
        this.view = view;
        this.service = service;
        this.model = model;

        
        this.view.OnMiniGameWon += HandleMiniGameWon;
        this.view.OnMiniGameLost += HandleMiniGameLost;
    }

    public void HandleFishingRodUsed(Vector3 targetPosition, string rodID)
    {
        if (model.isFishing)
        {
            return;
        }
        model.currentRodID = rodID;

        if (service.IsFishingWater(targetPosition))
        {
            model.isFishing = true;
            // ép player dừng lại
            GameObject player = GameObject.FindGameObjectWithTag("PlayerEntity");
            if (player != null)
            {
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                Animator anim = player.GetComponent<Animator>();
                if (anim != null)
                { 
                    anim.SetFloat("Speed", 0f);
                    anim.SetBool("IsMoving", false);
                }
            }
            view.StartMiniGame(targetPosition);
        }
        else
        {
            view.ShowCannotFishWarning();
        }
    }

    private void HandleMiniGameWon()
    {
        model.isFishing = false;
        try
        {
           
            bool success = service.CatchFish();

            if (success)
            {
                view.ShowFishingSuccess(model.lastCaughtFishID);
            }
            else
            {
                view.ShowFishingFailed();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Lỗi hệ thống: " + e.Message);
            view.ShowFishingSuccess("");
        }
    }

    private void HandleMiniGameLost()
    {
        model.isFishing = false;
        view.ShowFishingFailed();
    }
}