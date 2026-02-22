using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RainManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject rainPrefab;

    [Header("Zone Settings")]
    [SerializeField] private float zoneSize = 40f;
    [SerializeField] private float spawnEdgeDistance = 15f;
    [SerializeField] private float heightOffset = 20f;

    [Header("Density Settings")]
    [SerializeField] private float baseEmissionRate = 800f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 3f;

    private Transform player;
    private List<GameObject> activeZones = new List<GameObject>();
    private Vector2 currentCenter;

    private bool initialized = false;
    private float baseCameraArea;

    private void Start()
    {
        
        float h = Camera.main.orthographicSize * 2f;
        float w = h * Camera.main.aspect;
        baseCameraArea = w * h;
    }

    private void Update()
    {
        
        if (!initialized)
        {
            GameObject p = GameObject.FindWithTag("PlayerEntity");
            if (p != null)
            {
                player = p.transform;
                currentCenter = player.position;
                SpawnZone(currentCenter);
                initialized = true;
            }
            return;
        }

        
        Vector2 playerPos = player.position;
        float distance = Vector2.Distance(playerPos, currentCenter);

        if (distance > spawnEdgeDistance)
        {
            Vector2 direction = (playerPos - currentCenter).normalized;
            Vector2 newCenter = currentCenter + direction * zoneSize;

            SpawnZone(newCenter);
            currentCenter = newCenter;
        }
    }

    private void SpawnZone(Vector2 center)
    {
        GameObject zone = Instantiate(rainPrefab);

        zone.transform.position = new Vector3(
            center.x,
            center.y + heightOffset,
            rainPrefab.transform.position.z
        );

        ParticleSystem ps = zone.GetComponent<ParticleSystem>();

        AdjustShapeToCamera(ps);
        AdjustEmissionToCamera(ps);

        activeZones.Add(zone);

        if (activeZones.Count > 2)
        {
            StartCoroutine(FadeAndDestroy(activeZones[0]));
            activeZones.RemoveAt(0);
        }
    }

    private void AdjustShapeToCamera(ParticleSystem ps)
    {
        var shape = ps.shape;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;

        shape.scale = new Vector3(
            camWidth * 1.5f,
            shape.scale.y,
            camHeight * 1.5f
        );
    }

    private void AdjustEmissionToCamera(ParticleSystem ps)
    {
        var emission = ps.emission;

        float h = Camera.main.orthographicSize * 2f;
        float w = h * Camera.main.aspect;
        float currentArea = w * h;

        float densityFactor = currentArea / baseCameraArea;

        emission.rateOverTime = baseEmissionRate * densityFactor;
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