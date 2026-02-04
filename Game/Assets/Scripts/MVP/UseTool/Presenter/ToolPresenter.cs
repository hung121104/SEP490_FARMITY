using UnityEngine;

public class ToolPresenter
{
    private ToolStateModel toolModel;
    private ToolServiceRouter serviceRouter;
    private PlayerModel playerModel;

    public ToolPresenter(ToolStateModel toolModel, ToolServiceRouter serviceRouter, PlayerModel playerModel)
    {
        this.toolModel = toolModel;
        this.serviceRouter = serviceRouter;
        this.playerModel = playerModel;
    }

    public void EquipTool(ToolDataSO toolData)
    {
        toolModel.SetTool(toolData);
    }

    public void UnequipTool()
    {
        toolModel.SetTool(null);
    }

    public bool CanUseTool()
    {
        if (!toolModel.HasTool()) return false;

        ToolDataSO tool = toolModel.ToolData;
        float currentTime = Time.time;

        if (playerModel.CurrentStamina < tool.staminaCost) return false;

        if (tool.category == ToolCategory.Weapon && tool.useCooldown > 0)
        {
            float timeSinceLastUse = currentTime - toolModel.LastUseTime;
            if (timeSinceLastUse < tool.useCooldown) return false;
        }

        return true;
    }

    /// <summary>
    /// Standard use tool
    /// </summary>
    public bool TryUseTool(Vector3 playerPosition)
    {
        if (!CanUseTool()) return false;

        ToolDataSO tool = toolModel.ToolData;
        BaseToolService service = serviceRouter.GetService(tool);

        if (service == null) return false;

        // Check CanExecute
        if (!service.CanExecute(playerPosition, tool))
        {
            Debug.Log("Cannot execute tool here");
            return false;
        }

        // Drain stamina
        if (tool.staminaCost > 0)
        {
            playerModel.DrainStamina(tool.staminaCost);
            playerModel.NotifyStaminaChanged();
        }

        // Execute
        bool success = service.Execute(playerPosition, tool);

        if (success)
        {
            toolModel.SetLastUseTime(Time.time);
            service.PlayEffect(playerPosition, tool);
            service.OnExecuteComplete(playerPosition, tool);
        }

        return success;
    }

    // ✅ Call specific tool service methods below

    /// <summary>
    /// Example: Start fishing minigame
    /// </summary>
    //public bool TryStartFishingMinigame(Vector3 position)
    //{
    //    if (!toolModel.HasTool()) return false;

    //    ToolDataSO tool = toolModel.ToolData;
    //    if (tool.category != ToolCategory.Fishing) return false;

    //    // Get service as FishingToolService
    //    if (serviceRouter.GetService(tool) is FishingToolService fishingService)
    //    {
    //        fishingService.StartMinigame(position, tool);
    //        return true;
    //    }

    //    return false;
    //}

    /// <summary>
    /// Example: Weapon combo attack
    /// </summary>
    //public bool TryComboAttack(Vector3[] positions)
    //{
    //    if (!toolModel.HasTool()) return false;

    //    ToolDataSO tool = toolModel.ToolData;
    //    if (tool.category != ToolCategory.Weapon) return false;

    //    // Get service as WeaponToolService
    //    if (serviceRouter.GetService(tool) is WeaponToolService weaponService)
    //    {
    //        return weaponService.ComboAttack(positions, tool);
    //    }

    //    return false;
    //}

    public float GetCooldownProgress()
    {
        if (!toolModel.HasTool()) return 0f;

        ToolDataSO tool = toolModel.ToolData;
        if (tool.useCooldown <= 0) return 0f;

        float timeSinceLastUse = Time.time - toolModel.LastUseTime;
        return Mathf.Clamp01(1f - (timeSinceLastUse / tool.useCooldown));
    }
}
