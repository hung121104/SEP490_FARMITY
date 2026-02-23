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

    [Header("Player Detection")]

    private int playersNearbyCount = 0;
    private int currentIndex = 0;
    private bool isWaiting = false;
    

    private Vector3 networkPosition;

    private void Update()
    {
        if (!photonView.IsMine)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition,
                Time.deltaTime * 10f
            );
            return;
        }

        if (playersNearbyCount > 0 || isWaiting)
            return;

        MoveToPoint();
    }
    private void Start()
    {
        networkPosition = transform.position;
    }

    private void MoveToPoint()
    {
        if (patrolPoints.Length == 0) return;

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

   

    // Photon Sync
    public void OnPhotonSerializeView(
    PhotonStream stream,
    PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
        }
    }

 
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;

        if (photonView.IsMine)
        {
            playersNearbyCount++;
        }
        else
        {
            photonView.RPC("RPC_PlayerEntered", RpcTarget.MasterClient);
        }
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