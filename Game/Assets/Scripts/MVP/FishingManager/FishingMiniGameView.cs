using System;
using UnityEngine;
using UnityEngine.UI; // Thêm thư viện UI

public class FishingMiniGameView : MonoBehaviour
{
    // Cung cấp sự kiện cho Presenter lắng nghe
    public event Action OnMiniGameWon;

    public event Action OnMiniGameLost;
    [Header("UI References")]
    // THÊM DÒNG NÀY: Trỏ đến object FishingMiniGame nằm trong Canvas
    [SerializeField] GameObject miniGamePanel;

    [Header("UI References")]
    [SerializeField] RectTransform topPivot;
    [SerializeField] RectTransform bottomPivot;
    [SerializeField] RectTransform fish;
    [SerializeField] RectTransform hook;
    [SerializeField] Image hookImage; // Thay SpriteRenderer bằng Image
    [SerializeField] RectTransform progressBarContainer;

    [Header("Game Logic")]
    float fishPosition;
    float fishDestination;
    float fishTimer;
    [SerializeField] float timerMutilicator = 3f;
    float fishSpeed;
    [SerializeField] float smoothMotion = 1f;

    float hookPosition;
    [SerializeField] float hookSize = 0.1f;
    [SerializeField] float hookPower = 5f;

    float hookProgress;
    float hookPullVelocity;
    [SerializeField] float hookPullPower = 0.01f;
    [SerializeField] float hoookGravityPower = 0.005f;
    [SerializeField] float hookProgessDegradationPower = 0.1f;

    [SerializeField] float failTimer = 10f;
    private float currentFailTimer;

    // Quản lý trạng thái chơi
    private bool isPlaying = false;

    private void Awake()
    {
        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false); // Ép tắt UI lúc vừa vào game
        }
    }   
    public void StartMiniGame()
    {
        // BẬT giao diện lên
        miniGamePanel.SetActive(true);

        // Reset lại toàn bộ thông số mỗi khi quăng cần
        hookProgress = 0f;
        hookPosition = 0.5f;
        fishPosition = 0.5f;
        hookPullVelocity = 0f;
        currentFailTimer = failTimer;

        isPlaying = true;
        Resize();
    }

    private void Update()
    {
        if (!isPlaying) return;

        FishLogic();
        HookLogic();
        ProgressCheck();
    }

    private void Resize()
    {
        // UI chuẩn: Dùng localPosition để tính khoảng cách thay vì World Distance
        float distance = Mathf.Abs(topPivot.localPosition.y - bottomPivot.localPosition.y);

        // UI chuẩn: Dùng sizeDelta để đổi chiều cao thay vì dùng localScale gây méo hình
        Vector2 newSize = hook.sizeDelta;
        newSize.y = distance * hookSize;
        hook.sizeDelta = newSize;

        // Reset lại Scale về 1 để xóa bỏ lỗi kéo giãn hình ở ảnh 2
        hook.localScale = Vector3.one;
        fish.localScale = Vector3.one;
    }

    private void ProgressCheck()
    {
        Vector3 ls = progressBarContainer.localScale;
        ls.y = hookProgress;
        progressBarContainer.localScale = ls;

        float min = hookPosition - hookSize / 2;
        float max = hookPosition + hookSize / 2;

        if (min < fishPosition && fishPosition < max)
        {
            hookProgress += hookPower * Time.deltaTime;
        }
        else
        {
            hookProgress -= hookProgessDegradationPower * Time.deltaTime;
            currentFailTimer -= Time.deltaTime;
            if (currentFailTimer < 0f)
            {
                Lose();
            }
        }

        if (hookProgress >= 1f)
        {
            Win();
        }

        hookProgress = Mathf.Clamp(hookProgress, 0f, 1f);
    }

    private void Lose()
    {
        isPlaying = false;

        // TẮT giao diện đi
        miniGamePanel.SetActive(false);

        // Bắn sự kiện THUA cho FishingView và Presenter biết
        OnMiniGameLost?.Invoke();
    }

    private void Win()
    {
        isPlaying = false;

        // TẮT giao diện đi
        miniGamePanel.SetActive(false);

        // Bắn sự kiện THẮNG cho FishingView và Presenter biết
        OnMiniGameWon?.Invoke();
    }

    void HookLogic()
    {
        // 1. Nhấn giữ phím Space hoặc Chuột trái thì tạo lực kéo lên
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
        {
            hookPullVelocity += hookPullPower * Time.deltaTime;
        }

        // 2. Trọng lực luôn kéo xuống
        hookPullVelocity -= hoookGravityPower * Time.deltaTime;

        // 3. THÊM MỚI: Lực cản của nước. Ép vận tốc từ từ trở về 0 để tạo độ "nặng" và mượt mà
        hookPullVelocity = Mathf.Lerp(hookPullVelocity, 0f, 5f * Time.deltaTime);

        // 4. ĐÃ FIX: Nhân thêm Time.deltaTime để vị trí di chuyển đều theo thời gian thực (không bị vọt)
        hookPosition += hookPullVelocity * Time.deltaTime;

        // 5. Chạm đáy hoặc đỉnh thì triệt tiêu vận tốc ngay lập tức (tránh bị dính tường)
        if (hookPosition - hookSize / 2 <= 0f && hookPullVelocity < 0f)
        {
            hookPullVelocity = 0f;
        }
        if (hookPosition + hookSize / 2 >= 1f && hookPullVelocity > 0f)
        {
            hookPullVelocity = 0f;
        }

        // 6. Giới hạn vị trí không cho lọt ra ngoài thanh
        hookPosition = Mathf.Clamp(hookPosition, hookSize / 2, 1 - hookSize / 2);

        // 7. Cập nhật vị trí UI
        hook.localPosition = Vector3.Lerp(bottomPivot.localPosition, topPivot.localPosition, hookPosition);
    }

    void FishLogic()
    {
        fishTimer -= Time.deltaTime;
        if (fishTimer < 0f)
        {
            fishTimer = UnityEngine.Random.value * timerMutilicator;
            fishDestination = UnityEngine.Random.value;
        }
        fishPosition = Mathf.SmoothDamp(fishPosition, fishDestination, ref fishSpeed, smoothMotion);

        // UI chuẩn: Dùng localPosition thay vì position
        fish.localPosition = Vector3.Lerp(bottomPivot.localPosition, topPivot.localPosition, fishPosition);
    }
}