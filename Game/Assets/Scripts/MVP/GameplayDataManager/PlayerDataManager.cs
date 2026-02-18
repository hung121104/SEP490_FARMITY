using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using Photon.Pun;

public class PlayerDataManager : MonoBehaviourPunCallbacks
{
	public static PlayerDataManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		PopulateIds();
	}

	[Header("API Settings")]
	public string apiBaseUrl = "https://LocalHost:3000";
	public string worldId;
	public string accountId;

	[Header("Auth")]
	[SerializeField] private string authToken;
	public string authHeaderName = "Authorization";
	public string authHeaderPrefix = "Bearer ";

	[Header("Fetched Players")]
	[SerializeField] private List<PlayerData> players = new List<PlayerData>();

	private void PopulateIds()
	{
		if (string.IsNullOrEmpty(worldId) && WorldSelectionManager.Instance != null)
			worldId = WorldSelectionManager.Instance.SelectedWorldId;

		if (SessionManager.Instance != null)
		{
			if (string.IsNullOrEmpty(accountId))
				accountId = SessionManager.Instance.UserId;
			if (string.IsNullOrEmpty(authToken))
				authToken = SessionManager.Instance.JwtToken;
		}
	}

	private void Start()
	{
		// Populate IDs here (Start is guaranteed to run after all Awakes)
		PopulateIds();

		// If already in a room when this script starts (OnJoinedRoom was missed due to timing),
		// trigger the fetch directly here.
		if (PhotonNetwork.InRoom)
		{
			Debug.Log("[PlayerDataManager] Already in room on Start, triggering fetch. Is master: " + PhotonNetwork.IsMasterClient);
			TriggerFetch();
		}
	}

	public override void OnJoinedRoom()
	{
		Debug.Log("[PlayerDataManger] Joined: " + accountId + ". Is master: " + PhotonNetwork.IsMasterClient);
		TriggerFetch();
	}

	private void TriggerFetch()
	{
		PopulateIds();

		if (PhotonNetwork.IsMasterClient)
		{
			Debug.Log("[PlayerDataManager] Master fetching own data: " + accountId);
			if (!string.IsNullOrEmpty(worldId) && !string.IsNullOrEmpty(accountId))
				StartCoroutine(FetchPlayerData(accountId));
		}
		else
		{
			Debug.Log("[PlayerDataManger] Sending RPC with accountId: " + accountId);
			photonView.RPC(nameof(RPC_RequestFetch), RpcTarget.MasterClient, accountId);
		}
	}

	[PunRPC]
	private void RPC_RequestFetch(string requesterAccountId)
	{
		Debug.Log("[PlayerDataManager] RPC received for: " + requesterAccountId);
		StartCoroutine(FetchPlayerData(requesterAccountId));
	}

	public void FetchFromApi() => StartCoroutine(FetchPlayerData(accountId));

	private IEnumerator FetchPlayerData(string targetAccountId)
	{
		Debug.Log("[PlayerDataManger] Fetching Data: "+targetAccountId);

		string url = $"{apiBaseUrl.TrimEnd('/')}/player-data/worlds/{worldId}/characters/{targetAccountId}";

		using (UnityWebRequest req = UnityWebRequest.Get(url))
		{
			if (!string.IsNullOrEmpty(authToken))
				req.SetRequestHeader(authHeaderName, authHeaderPrefix + authToken);

			req.certificateHandler = new AcceptAllCertificates();

			yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
			if (req.result != UnityWebRequest.Result.Success)
#else
			if (req.isNetworkError || req.isHttpError)
#endif
			{
				Debug.LogWarning($"[PlayerDataManager] Fetch failed: {req.responseCode} {req.error}");
				yield break;
			}

			try
			{
				ApiResponse resp = JsonUtility.FromJson<ApiResponse>(req.downloadHandler.text);
				PlayerData data = new PlayerData
				{
					_id          = resp._id,
					worldId      = resp.worldId,
					accountId    = resp.accountId,
					positionX    = resp.positionX,
					positionY    = resp.positionY,
					sectionIndex = resp.sectionIndex
				};

				int idx = players.FindIndex(p => p._id == data._id);
				if (idx >= 0) players[idx] = data;
				else players.Add(data);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[PlayerDataManager] Parse error: {ex.Message}");
			}
		}
	}

	private class AcceptAllCertificates : CertificateHandler
	{
		protected override bool ValidateCertificate(byte[] certificateData) => true;
	}

	[Serializable]
	private class ApiResponse
	{
		public string _id;
		public string worldId;
		public string accountId;
		public float positionX;
		public float positionY;
		public int sectionIndex;
	}
}


