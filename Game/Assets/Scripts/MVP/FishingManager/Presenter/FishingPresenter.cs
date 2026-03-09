using UnityEngine;
using System;

public class FishingPresenter : IDisposable
{
    private readonly IFishingView view;
    private readonly IFishingService service;
    private readonly FishingModel model;

    public FishingPresenter(IFishingView view, IFishingService service, FishingModel model)
    {
        this.view = view;
        this.service = service;
        this.model = model;

        // 1. Lắng nghe lúc người chơi quăng cần
        UseToolService.OnFishingRodRequested += HandleFishingRodUsed;

        // 2. Lắng nghe kết quả từ Minigame View
        this.view.OnMiniGameWon += HandleMiniGameWon;
        this.view.OnMiniGameLost += HandleMiniGameLost;
    }

    private void HandleFishingRodUsed(ToolData tool, Vector3 targetPosition)
    {
        if (service.IsFishingWater(targetPosition))
        {
            // Bắt đầu Fishing Mode: Mở UI minigame
            view.StartMiniGame();
        }
        else
        {
            view.ShowCannotFishWarning();
        }
    }

    private void HandleMiniGameWon()
    {
        // Hoàn thành minigame -> Drop cá
        bool success = service.CatchFish(); // Hàm này trong Service sẽ gọi lấy item và add inventory
        if (success)
        {
            view.ShowFishingSuccess(model.lastCaughtFish);
        }
    }

    private void HandleMiniGameLost()
    {
        // Thất bại -> In ra debug
        view.ShowFishingFailed();
    }

    public void Dispose()
    {
        UseToolService.OnFishingRodRequested -= HandleFishingRodUsed;
        this.view.OnMiniGameWon -= HandleMiniGameWon;
        this.view.OnMiniGameLost -= HandleMiniGameLost;
    }
}