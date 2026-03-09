using System;
using UnityEngine;

public class FishingView : MonoBehaviour, IFishingView
{
    public event Action OnMiniGameWon;
    public event Action OnMiniGameLost;

    [Header("References")]
    public FishingMiniGameView miniGameView;

    private void Awake()
    {
        miniGameView.OnMiniGameWon += () => OnMiniGameWon?.Invoke();
        miniGameView.OnMiniGameLost += () => OnMiniGameLost?.Invoke();
    }

    private void Start()
    {
        if (miniGameView != null)
        {
            miniGameView.gameObject.SetActive(false);
        }
    }

    // --- HÀM TỰ ĐỘNG TÌM VÀ KHÓA/MỞ KHÓA PLAYER ---
    private void SetPlayerMovementState(bool isActive)
    {
        // Tự động quét trong Scene để tìm nhân vật đang được spawn ra
        PlayerMovement player = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();

        if (player != null)
        {
            player.enabled = isActive;
        }
        else
        {
            Debug.LogWarning("[FishingView] Không tìm thấy PlayerMovement nào trong scene!");
        }
    }

    public void StartMiniGame()
    {
        Debug.Log("[FishingView] Bắt đầu câu cá, mở UI MiniGame!");

        // KHÓA DI CHUYỂN (Truyền false)
        SetPlayerMovementState(false);

        miniGameView.gameObject.SetActive(true);
        miniGameView.StartMiniGame();
    }

    public void ShowCannotFishWarning()
    {
        Debug.Log("cant fishing here");
    }

    public void ShowFishingSuccess(FishInfo fish)
    {
        // MỞ KHÓA DI CHUYỂN (Truyền true)
        SetPlayerMovementState(true);

        miniGameView.gameObject.SetActive(false);
        Debug.Log($"[FishingView] Hoàn thành minigame! Rớt cá: {fish.fishName}");
    }

    public void ShowFishingFailed()
    {
        // MỞ KHÓA DI CHUYỂN (Truyền true)
        SetPlayerMovementState(true);

        miniGameView.gameObject.SetActive(false);
        Debug.Log("fishing fail");
    }
}