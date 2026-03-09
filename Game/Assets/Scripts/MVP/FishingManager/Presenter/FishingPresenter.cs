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

        // Đăng ký lắng nghe sự kiện dùng cần câu từ UseToolService
        UseToolService.OnFishingRodRequested += HandleFishingRodUsed;
    }

    private void HandleFishingRodUsed(ToolData tool, Vector3 targetPosition)
    {
        // 1. Hỏi Service xem chỗ chuột click có phải Fishingtiltemap không
        if (service.IsFishingWater(targetPosition))
        {
            // 2. Chỗ này câu được -> Bắt cá và add vào inventory
            bool success = service.CatchFish();

            if (success)
            {
                // Cập nhật View
                view.ShowFishingSuccess(model.lastCaughtFish);
            }
        }
        else
        {
            // 3. Không phải ô nước -> Báo lỗi ra View
            view.ShowCannotFishWarning();
        }
    }

    // Luôn nhớ hủy đăng ký sự kiện khi Presenter bị hủy để tránh memory leak
    public void Dispose()
    {
        UseToolService.OnFishingRodRequested -= HandleFishingRodUsed;
    }
}