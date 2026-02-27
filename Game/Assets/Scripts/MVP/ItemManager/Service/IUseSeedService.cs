using UnityEngine;

public interface IUseSeedService
{
    (bool, int) UseSeed(ItemDataSO item, Vector3 pos);
}
