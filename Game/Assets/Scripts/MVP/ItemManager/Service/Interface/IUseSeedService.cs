using UnityEngine;

public interface IUseSeedService
{
    (bool, int) UseSeed(ItemData item, Vector3 pos);
}
