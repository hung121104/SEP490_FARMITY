using System;
using System.Collections;
using UnityEngine;
using Photon.Pun; // Bắt buộc phải có để dùng mạng

// Đổi MonoBehaviour thành MonoBehaviourPun
public class FishingView : MonoBehaviourPun, IFishingView
{
    public event Action OnMiniGameWon;
    public event Action OnMiniGameLost;

    [Header("References")]
    public FishingMiniGameView miniGameView;

    // Biến lưu hướng quay mặt để lúc thu cần nhân vật không bị quay ngược lại
    private float lastFacingX = 1f;

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

    // --- 1. TỰ ĐỘNG TÌM PLAYER LÀ LOCAL CLIENT ---
    private PlayerMovement GetLocalPlayer()
    {
        PlayerMovement[] allPlayers = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return player;
            }
        }
        return null;
    }

    private void SetPlayerMovementState(bool isActive)
    {
        PlayerMovement localPlayer = GetLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.enabled = isActive;
        }
        else
        {
            Debug.LogWarning("[FishingView] Không tìm thấy PlayerMovement của máy này trong scene!");
        }
    }

    
    public void StartMiniGame(Vector3 targetPosition)
    {
        // Khóa di chuyển
        SetPlayerMovementState(false);

        // Chạy Coroutine quăng cần
        StartCoroutine(CastRodRoutine(targetPosition));
    }

    private IEnumerator CastRodRoutine(Vector3 targetPosition)
    {
        PlayerMovement localPlayer = GetLocalPlayer();

        if (localPlayer != null)
        {
            Animator anim = localPlayer.GetComponent<Animator>();
            if (anim != null)
            {
                // Tính toán hướng (-1 là trái, 1 là phải) dựa vào vị trí click chuột
                lastFacingX = targetPosition.x >= localPlayer.transform.position.x ? 1f : -1f;

                // Truyền vào Blend Tree (Dùng ActionX và ActionY theo đúng ảnh của bạn)
                anim.SetFloat("ActionX", lastFacingX);
                anim.SetFloat("ActionY", 0f);
                anim.SetTrigger("CastRod");

                
                photonView.RPC(nameof(RPC_SyncFishingAnimation), RpcTarget.Others, "CastRod", lastFacingX, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }

        
        yield return new WaitForSeconds(1.5f);

        Debug.Log("[FishingView] Bắt đầu câu cá, mở UI MiniGame!");
        miniGameView.gameObject.SetActive(true);
        miniGameView.StartMiniGame();
    }

    public void ShowCannotFishWarning()
    {
        Debug.Log("cant fishing here");
    }


    public void ShowFishingSuccess(string fishID) 
    {
        miniGameView.gameObject.SetActive(false);
        StartCoroutine(ReelFishRoutine(true, fishID));
    }
    public void ShowFishingFailed()
    {
        miniGameView.gameObject.SetActive(false);
        
        StartCoroutine(ReelFishRoutine(false, ""));
    }

    private IEnumerator ReelFishRoutine(bool isSuccess, string fishID)
    {
        PlayerMovement localPlayer = GetLocalPlayer();

        if (localPlayer != null)
        {
            Animator anim = localPlayer.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("ActionX", lastFacingX);
                anim.SetFloat("ActionY", 0f);
                anim.SetTrigger("ReelRod");
                photonView.RPC(nameof(RPC_SyncFishingAnimation), RpcTarget.Others, "ReelRod", lastFacingX, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }

        
        yield return new WaitForSeconds(1.0f);

        
        SetPlayerMovementState(true);

        if (isSuccess && !string.IsNullOrEmpty(fishID))
        {
            Debug.Log($"[FishingView] Complete Fishing!  Drop Fish ID: {fishID}");
        }
        else
        {
            Debug.Log("[FishingView]  Fishing fail");
        }
    }   

    // --- 4. NHẬN TÍN HIỆU ĐỒNG BỘ TỪ MÁY KHÁC ---
    [PunRPC]
    private void RPC_SyncFishingAnimation(string triggerName, float facingX, int ownerActorNumber)
    {
        PlayerMovement[] allPlayers = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.Owner.ActorNumber == ownerActorNumber)
            {
                Animator anim = player.GetComponent<Animator>();
                if (anim != null)
                {
                    // Cập nhật hướng xoay mặt và chạy trigger
                    anim.SetFloat("ActionX", facingX);
                    anim.SetFloat("ActionY", 0f);
                    anim.SetTrigger(triggerName);
                }
                break;
            }
        }
    }
}