using UnityEngine;

/// <summary>
/// Interface for tool action execution
/// Handles tile manipulation and effect playback
/// </summary>
public interface IToolActionService
{
    /// <summary>
    /// Executes tool action at specified world position
    /// </summary>
    /// <param name="worldPos">World position to use tool</param>
    /// <param name="tool">Tool data to execute</param>
    /// <returns>Result containing success status and affected position</returns>
    (bool success, Vector3Int tilePos, Vector3 worldPos) UseToolAtPosition(Vector3 worldPos, ToolDataSO tool);

    /// <summary>
    /// Plays visual and audio effects for tool usage
    /// </summary>
    /// <param name="position">World position to spawn effects</param>
    /// <param name="tool">Tool data containing effect references</param>
    void PlayToolEffect(Vector3 position, ToolDataSO tool);
}
