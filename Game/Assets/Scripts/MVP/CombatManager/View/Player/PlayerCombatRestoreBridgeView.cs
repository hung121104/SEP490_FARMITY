using Photon.Pun;
using UnityEngine;

public class PlayerCombatRestoreBridgeView : MonoBehaviourPun
{
    private const string HPTRACE = "[HPTRACE]";
    private const string PROGTRACE = "[PROGTRACE]";

    [PunRPC]
    private void RPC_CombatRestoreHealthFromMaster(int restoredHealth)
    {
        if (!photonView.IsMine)
            return;

        int normalizedHealth = Mathf.Max(0, restoredHealth);
        Debug.Log($"{HPTRACE} [PlayerCombatRestoreBridgeView] RPC_CombatRestoreHealthFromMaster received health={normalizedHealth}");
        StartCoroutine(ApplyRestoredHealthWhenReady(normalizedHealth));
    }

    [PunRPC]
    private void RPC_CombatRestoreProgressionFromMaster(
        int level,
        int currentExp,
        int expToNextLevel,
        int baseStrength,
        int baseVitality)
    {
        if (!photonView.IsMine)
            return;

        int safeLevel = Mathf.Max(1, level);
        int safeCurrentExp = Mathf.Max(0, currentExp);
        int safeExpToNext = Mathf.Max(1, expToNextLevel);
        int safeStrength = Mathf.Max(1, baseStrength);
        int safeVitality = Mathf.Max(1, baseVitality);

        Debug.Log($"{PROGTRACE} [PlayerCombatRestoreBridgeView] RPC_CombatRestoreProgressionFromMaster received lv={safeLevel} exp={safeCurrentExp}/{safeExpToNext} str={safeStrength} vit={safeVitality}");
        StartCoroutine(ApplyRestoredProgressionWhenReady(
            safeLevel,
            safeCurrentExp,
            safeExpToNext,
            safeStrength,
            safeVitality));
    }

    private System.Collections.IEnumerator ApplyRestoredHealthWhenReady(int restoredHealth)
    {
        float deadline = Time.realtimeSinceStartup + 10f;

        while (Time.realtimeSinceStartup < deadline)
        {
            var presenter = CombatManager.Presenter.PlayerHealthPresenter.FindLocal();
            if (presenter != null)
            {
                presenter.SetHealthFromSave(restoredHealth);
                Debug.Log($"{HPTRACE} [PlayerCombatRestoreBridgeView] Applied restored health to local presenter health={restoredHealth}");
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning($"{HPTRACE} [PlayerCombatRestoreBridgeView] Timed out waiting to apply restored health={restoredHealth}");
    }

    private System.Collections.IEnumerator ApplyRestoredProgressionWhenReady(
        int level,
        int currentExp,
        int expToNextLevel,
        int baseStrength,
        int baseVitality)
    {
        float deadline = Time.realtimeSinceStartup + 10f;

        while (Time.realtimeSinceStartup < deadline)
        {
            var statsPresenter = FindObjectOfType<CombatManager.Presenter.StatsPresenter>();
            if (statsPresenter != null)
            {
                statsPresenter.SetProgressionFromSave(level, currentExp, expToNextLevel, baseStrength, baseVitality);
                Debug.Log($"{PROGTRACE} [PlayerCombatRestoreBridgeView] Applied restored progression to StatsPresenter lv={level} exp={currentExp}/{expToNextLevel} str={baseStrength} vit={baseVitality}");
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning($"{PROGTRACE} [PlayerCombatRestoreBridgeView] Timed out waiting to apply restored progression lv={level} exp={currentExp}/{expToNextLevel}");
    }
}
