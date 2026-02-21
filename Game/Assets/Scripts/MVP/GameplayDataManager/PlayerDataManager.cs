using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Stores character/player data for all players in the world.
/// Data is provided by WorldDataBootstrapper — no API calls here.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
	public static PlayerDataManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this) { Destroy(gameObject); return; }
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	[Header("Player Data")]
	[SerializeField] public List<PlayerData> players = new List<PlayerData>();

	/// <summary>
	/// Called by WorldDataBootstrapper after the API response is received.
	/// Populates the players list from the characters array in the response.
	/// </summary>
	public void Populate(List<WorldApiResponse.CharacterEntry> characters)
	{
		players.Clear();
		foreach (var c in characters)
		{
			players.Add(new PlayerData
			{
				_id          = c._id,
				worldId      = c.worldId,
				accountId    = c.accountId,
				positionX    = c.positionX,
				positionY    = c.positionY,
				sectionIndex = c.sectionIndex
			});
		}
		Debug.Log($"[PlayerDataManager] Populated {players.Count} characters.");
	}
}


