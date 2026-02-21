using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RainZoneStreamer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject rainPrefab;

    [SerializeField] private float zoneSize = 30f;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float heightOffset = 20f;
    [SerializeField] private float fadeDuration = 3f;

    private List<GameObject> activeZones = new List<GameObject>();
    private Vector2 currentCenter;

    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerMovement>().transform;

        currentCenter = player.position;

        SpawnZone(player.position);
    }

    private void Update()
    {
        if (player == null) return;

        Vector2 playerPos = player.position;

        float distance = Vector2.Distance(playerPos, currentCenter);

        if (distance > spawnRadius)
        {
            Vector2 direction = (playerPos - currentCenter).normalized;

            Vector2 newCenter = currentCenter + direction * zoneSize;

            SpawnZone(new Vector3(newCenter.x, newCenter.y, 0));

            currentCenter = newCenter;   // cập nhật SAU khi spawn
        }
    }

    private void SpawnZone(Vector3 pos)
    {
        GameObject newZone = Instantiate(
            rainPrefab,
            new Vector3(pos.x, pos.y + heightOffset, pos.z),
            rainPrefab.transform.rotation   // giữ đúng rotation
        );

        activeZones.Add(newZone);

        if (activeZones.Count > 2)
        {
            StartCoroutine(FadeAndDestroy(activeZones[0]));
            activeZones.RemoveAt(0);
        }
        Debug.Log("Spawn Rain Zone");
    }

    private IEnumerator FadeAndDestroy(GameObject zone)
    {
        ParticleSystem ps = zone.GetComponent<ParticleSystem>();
        var emission = ps.emission;

        float startRate = emission.rateOverTime.constant;
        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            emission.rateOverTime = Mathf.Lerp(startRate, 0, t);
            time += Time.deltaTime;
            yield return null;
        }

        emission.rateOverTime = 0;

        Destroy(zone, 2f);
    }
}