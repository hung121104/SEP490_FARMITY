using UnityEngine;

public class FishingView : MonoBehaviour, IFishingView
{
    public void ShowCannotFishWarning()
    {
        // In ra đúng dòng chữ bạn yêu cầu
        Debug.Log("cant fishing here");

        // Sau này bạn có thể thêm logic hiện thông báo nổi (Floating Text) ở đây
    }

    public void ShowFishingSuccess(FishInfo fish)
    {
        
        Debug.Log($"[FishingView] Player successfully fished: {fish.fishName}");
    }
}