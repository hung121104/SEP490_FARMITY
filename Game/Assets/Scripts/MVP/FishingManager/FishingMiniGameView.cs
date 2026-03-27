using System;
using UnityEngine;
using UnityEngine.UI; // Thêm thư viện UI

public class FishingMiniGameView : MonoBehaviour
{
    // Cung cấp sự kiện cho Presenter lắng nghe
    public event Action OnMiniGameWon;

    public event Action OnMiniGameLost;
    [Header("UI References")]
    
    [SerializeField] GameObject miniGamePanel;

    [Header("UI References")]
    [SerializeField] RectTransform topPivot;
    [SerializeField] RectTransform bottomPivot;
    [SerializeField] RectTransform fish;
    [SerializeField] RectTransform hook;
    [SerializeField] Image hookImage; 
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

    
    private bool isPlaying = false;

    private void Awake()
    {
        if (miniGamePanel != null)
        {
            miniGamePanel.SetActive(false); 
        }
    }   
    public void StartMiniGame()
    {
        
        miniGamePanel.SetActive(true);

        
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
        
        float distance = Mathf.Abs(topPivot.localPosition.y - bottomPivot.localPosition.y);

      
        //Vector2 newSize = hook.sizeDelta;
        //newSize.y = distance * hookSize;
        //hook.sizeDelta = newSize;

        
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

        
        miniGamePanel.SetActive(false);

        
        OnMiniGameLost?.Invoke();
    }

    private void Win()
    {
        isPlaying = false;

        
        miniGamePanel.SetActive(false);

       
        OnMiniGameWon?.Invoke();
    }

    void HookLogic()
    {
        
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
        {
            hookPullVelocity += hookPullPower * Time.deltaTime;
        }

       
        hookPullVelocity -= hoookGravityPower * Time.deltaTime;

        
        hookPullVelocity = Mathf.Lerp(hookPullVelocity, 0f, 5f * Time.deltaTime);

        
        hookPosition += hookPullVelocity * Time.deltaTime;

        
        if (hookPosition - hookSize / 2 <= 0f && hookPullVelocity < 0f)
        {
            hookPullVelocity = 0f;
        }
        if (hookPosition + hookSize / 2 >= 1f && hookPullVelocity > 0f)
        {
            hookPullVelocity = 0f;
        }

        hookPosition = Mathf.Clamp(hookPosition, hookSize / 2, 1 - hookSize / 2);

        
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





        fish.localPosition = Vector3.Lerp(bottomPivot.localPosition, topPivot.localPosition, fishPosition);

    }
}