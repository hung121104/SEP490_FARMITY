using System;
using System.Collections;
using UnityEngine;
using Photon.Pun; 


public class FishingView : MonoBehaviourPun, IFishingView
{
    public event Action OnMiniGameWon;
    public event Action OnMiniGameLost;

    [Header("References")]
    private Coroutine castRoutine; 
    private bool isWaitingForFish = false; 
    public FishingMiniGameView miniGameView;
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
    private void Update()
    {
        if (isWaitingForFish && Input.GetMouseButtonDown(0))
        {
            CancelFishingEarly();
        }
    }

    private void CancelFishingEarly()
    {
        isWaitingForFish = false; 

        
        if (castRoutine != null)
        {
            StopCoroutine(castRoutine);
            castRoutine = null;
        }
        Debug.Log("[FishingView] Player cancel fishing!");

        OnMiniGameLost?.Invoke();
    }

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
        SetPlayerMovementState(false);
        if (castRoutine != null) StopCoroutine(castRoutine);
        castRoutine = StartCoroutine(CastRodRoutine(targetPosition));
    }

    private IEnumerator CastRodRoutine(Vector3 targetPosition)
    {
        isWaitingForFish = false; 
        PlayerMovement localPlayer = GetLocalPlayer();

        // --- 1. CastRod ---
        if (localPlayer != null)
        {
            Animator anim = localPlayer.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                lastFacingX = targetPosition.x >= localPlayer.transform.position.x ? 1f : -1f;
                anim.SetFloat("ActionX", lastFacingX);
                anim.SetFloat("ActionY", 0f);

                anim.SetTrigger("CastRod");
                photonView.RPC(nameof(RPC_SyncFishingAnimation), RpcTarget.Others, "CastRod", lastFacingX, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }

        yield return new WaitForSeconds(1.5f);

        // --- 2. WaitFishing ---
        if (localPlayer != null)
        {
            Animator anim = localPlayer.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("WaitFishing");
                photonView.RPC(nameof(RPC_SyncFishingAnimation), RpcTarget.Others, "WaitFishing", lastFacingX, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }

        // allow player to cancel fishing while waiting
        isWaitingForFish = true;

        float waitTime = UnityEngine.Random.Range(1f, 4f);
        Debug.Log($"[FishingView] Waiting for {waitTime:F1} second...");
        yield return new WaitForSeconds(waitTime);

        // catch fish successfully, close the chance to cancel
        isWaitingForFish = false;

        // --- 3. OPEN MINIGAME ---
        Debug.Log("[FishingView] catching! Open UI MiniGame!");
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
            Animator anim = localPlayer.GetComponentInChildren<Animator>();
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

    // --- sync ---
    [PunRPC]
    private void RPC_SyncFishingAnimation(string triggerName, float facingX, int ownerActorNumber)
    {
        PlayerMovement[] allPlayers = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.Owner.ActorNumber == ownerActorNumber)
            {
                Animator anim = player.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetFloat("ActionX", facingX);
                    anim.SetFloat("ActionY", 0f);
                    anim.SetTrigger(triggerName);
                }
                break;
            }
        }
    }
}