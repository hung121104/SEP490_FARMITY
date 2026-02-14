using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// Displays network statistics like ping, latency, FPS
/// </summary>
public class NetworkStatsDisplay : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [Tooltip("Text component to display network stats")]
    public TextMeshProUGUI statsText;
    
    [Header("Display Settings")]
    [Tooltip("Update interval in seconds")]
    public float updateInterval = 0.5f;
    
    [Tooltip("Show FPS")]
    public bool showFPS = true;
    
    [Tooltip("Show ping")]
    public bool showPing = true;
    
    [Tooltip("Show room info")]
    public bool showRoomInfo = true;
    
    [Tooltip("Show player count")]
    public bool showPlayerCount = true;
    
    [Header("Color Coding")]
    public Color goodPingColor = Color.green;
    public Color mediumPingColor = Color.yellow;
    public Color badPingColor = Color.red;
    
    [Tooltip("Ping threshold for medium color (ms)")]
    public int mediumPingThreshold = 100;
    
    [Tooltip("Ping threshold for bad color (ms)")]
    public int badPingThreshold = 200;
    
    private float nextUpdateTime;
    private float deltaTime;
    
    private void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        // Update display
        if (Time.time >= nextUpdateTime)
        {
            UpdateStatsDisplay();
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    private void UpdateStatsDisplay()
    {
        if (statsText == null) return;
        
        string stats = "";
        
        // FPS
        if (showFPS)
        {
            float fps = 1.0f / deltaTime;
            Color fpsColor = fps >= 30 ? Color.green : (fps >= 20 ? Color.yellow : Color.red);
            stats += $"<color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>FPS: {fps:0.}</color>\n";
        }
        
        // Network stats (only if connected)
        if (PhotonNetwork.IsConnected)
        {
            // Ping
            if (showPing)
            {
                int ping = PhotonNetwork.GetPing();
                Color pingColor = GetPingColor(ping);
                stats += $"<color=#{ColorUtility.ToHtmlStringRGB(pingColor)}>Ping: {ping} ms</color>\n";
            }
            
            // Connection state
            stats += $"Status: {PhotonNetwork.NetworkClientState}\n";
            
            // Room info
            if (showRoomInfo && PhotonNetwork.InRoom)
            {
                stats += $"Room: {PhotonNetwork.CurrentRoom.Name}\n";
                
                if (showPlayerCount)
                {
                    stats += $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}\n";
                }
            }
            
            // Server region
            stats += $"Region: {PhotonNetwork.CloudRegion}\n";
            
            // Master client indicator
            if (PhotonNetwork.IsMasterClient)
            {
                stats += $"<color=#{ColorUtility.ToHtmlStringRGB(Color.cyan)}>[Master Client]</color>\n";
            }
        }
        else
        {
            stats += "<color=#FF0000>Disconnected</color>\n";
        }
        
        statsText.text = stats.TrimEnd();
    }
    
    private Color GetPingColor(int ping)
    {
        if (ping < mediumPingThreshold)
            return goodPingColor;
        else if (ping < badPingThreshold)
            return mediumPingColor;
        else
            return badPingColor;
    }
    
    public override void OnConnectedToMaster()
    {
        UpdateStatsDisplay();
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatsDisplay();
    }
    
    public override void OnJoinedRoom()
    {
        UpdateStatsDisplay();
    }
    
    public override void OnLeftRoom()
    {
        UpdateStatsDisplay();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatsDisplay();
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateStatsDisplay();
    }
}
