using UnityEngine;
using Photon.Pun;
using System.Collections;

[System.Serializable]
public class NPCPatrolPoint
{
    public Transform point;
    public float waitTime;
}

public class NPCPatrolController : MonoBehaviourPun, IPunObservable
{
    [Header("Patrol Settings")]
    [SerializeField] private NPCPatrolPoint[] patrolPoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float reachDistance = 0.1f;
    [SerializeField] private bool loop = true;

    private int playersNearbyCount = 0;
    private int currentIndex = 0;
    private bool isWaiting = false;

    private Vector3 networkPosition;

    // ===== Animation =====
    private Animator animator;
    private bool networkIsWalking;
    private float networkScaleX = 1f;

    private void Start()
    {
        networkPosition = transform.position;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // ===== CLIENT =====
        if (!photonView.IsMine)
        {
            transform.localScale = new Vector3(networkScaleX, 1, 1);

            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition,
                Time.deltaTime * 15f
            );

            if (animator != null)
                animator.SetBool("isWalking", networkIsWalking);

            return;
        }

        // ===== MASTER =====
        if (playersNearbyCount > 0)
        {
            if (animator != null)
                animator.SetBool("isWalking", false);

            networkIsWalking = false;

            FaceNearestPlayer();   // look at player when they are nearby

            return;
        }

        if (isWaiting)
        {
            if (animator != null)
                animator.SetBool("isWalking", false);

            networkIsWalking = false;
            return;
        }

        MoveToPoint();
        UpdateAnimation();
    }

    private void MoveToPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentIndex].point;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < reachDistance)
        {
            StartCoroutine(WaitAndNext());
        }
    }

    private IEnumerator WaitAndNext()
    {
        isWaiting = true;

        float waitTime = patrolPoints[currentIndex].waitTime;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        currentIndex++;

        if (currentIndex >= patrolPoints.Length)
        {
            if (loop)
                currentIndex = 0;
            else
                currentIndex = patrolPoints.Length - 1;
        }

        isWaiting = false;
    }

    // ===== Animation Logic (SỬA PHẦN NÀY) =====
    private void UpdateAnimation()
    {
        if (animator == null) return;
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentIndex].point;
        Vector3 direction = target.position - transform.position;

        bool isMoving = direction.magnitude > reachDistance;

        animator.SetBool("isWalking", isMoving);
        networkIsWalking = isMoving;

        // Flip NGAY LẬP TỨC theo hướng target
        if (direction.x > 0.01f)
        {
            transform.localScale = new Vector3(1, 1, 1);
            networkScaleX = 1f;
        }
        else if (direction.x < -0.01f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            networkScaleX = -1f;
        }
    }

    // ===== Photon Sync =====
    public void OnPhotonSerializeView(
        PhotonStream stream,
        PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(networkIsWalking);
            stream.SendNext(networkScaleX);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkIsWalking = (bool)stream.ReceiveNext();
            networkScaleX = (float)stream.ReceiveNext();
        }
    }

    // ===== Player Detection =====
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        if (photonView.IsMine)
            playersNearbyCount++;
        else
            photonView.RPC("RPC_PlayerEntered", RpcTarget.MasterClient);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        if (photonView.IsMine)
        {
            playersNearbyCount--;
            if (playersNearbyCount < 0) playersNearbyCount = 0;
        }
        else
        {
            photonView.RPC("RPC_PlayerExited", RpcTarget.MasterClient);
        }
    }
    private void SetFlip(float scaleX)
    {
        transform.localScale = new Vector3(scaleX, 1, 1);
        networkScaleX = scaleX;
    }
    private void FaceNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");

        if (players.Length == 0) return;

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var p in players)
        {
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = p;
            }
        }

        if (nearest == null) return;

        float direction = nearest.transform.position.x - transform.position.x;

        if (direction > 0.01f)
        {
            SetFlip(1f);
        }
        else if (direction < -0.01f)
        {
            SetFlip(-1f);
        }
    }

    [PunRPC]
    private void RPC_PlayerEntered()
    {
        playersNearbyCount++;
    }

    [PunRPC]
    private void RPC_PlayerExited()
    {
        playersNearbyCount--;
        if (playersNearbyCount < 0)
            playersNearbyCount = 0;
    }
}