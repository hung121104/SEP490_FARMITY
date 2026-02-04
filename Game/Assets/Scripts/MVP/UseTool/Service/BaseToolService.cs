using UnityEngine;

public abstract class BaseToolService
{
    /// <summary>
    /// Execute tool action - MUST override in child classes
    /// </summary>
    /// <param name="worldPos">World position to use tool</param>
    /// <param name="tool">Tool data</param>
    /// <returns>True if action was successful</returns>
    public abstract bool Execute(Vector3 worldPos, ToolDataSO tool);

    /// <summary>
    /// Play effects - Common implementation, can override if needed
    /// </summary>
    /// <param name="position">Position to spawn effects</param>
    /// <param name="tool">Tool data containing effect references</param>
    public virtual void PlayEffect(Vector3 position, ToolDataSO tool)
    {
        // Default implementation - works for all tools
        if (tool.useEffectPrefab != null)
        {
            GameObject.Instantiate(tool.useEffectPrefab, position, Quaternion.identity);
        }

        if (tool.useSound != null)
        {
            AudioSource.PlayClipAtPoint(tool.useSound, position);
        }
    }

    /// <summary>
    /// Optional: Called before Execute() - for validation
    /// Override if need custom validation
    /// </summary>
    public virtual bool CanExecute(Vector3 worldPos, ToolDataSO tool)
    {
        // Default: always can execute
        return true;
    }

    /// <summary>
    /// Optional: Called after successful Execute() - for cleanup/callbacks
    /// Override if need post-execution logic
    /// </summary>
    public virtual void OnExecuteComplete(Vector3 worldPos, ToolDataSO tool)
    {
        // Default: do nothing
    }

    /// <summary>
    /// Helper: Log debug info
    /// </summary>
    protected void LogAction(string toolName, Vector3 position)
    {
        Debug.Log($"{GetType().Name}: '{toolName}' used at {position}");
    }
}
