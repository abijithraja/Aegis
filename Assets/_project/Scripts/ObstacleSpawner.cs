using UnityEngine;
using System.Collections;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject obstaclePrefab;      // prefab (with collider + optional Rigidbody)
    public Transform shipTransform;        // player's ship
    public Transform marsTransform;        // destination

    [Header("Trigger (distance to Mars)")]
    [Tooltip("Spawn trigger: when ship-to-Mars distance falls below this value.")]
    public float triggerDistance = 500f;
    [Tooltip("If true, spawns every time the ship crosses (useful for multiple runs).")]
    public bool resetOnDeparture = false;

    [Header("Spawn placement")]
    [Tooltip("Distance in front of the ship where base spawn attempts occur.")]
    public float spawnDistanceInFront = 100f;
    [Tooltip("A random spherical offset applied to the base spawn point.")]
    public float randomOffset = 20f;
    [Tooltip("Minimum allowed distance from ship to final spawn pos (prevents spawning inside ship).")]
    public float minDistanceFromShip = 10f;
    [Tooltip("Maximum attempts to find a valid spawn position to avoid overlaps.")]
    public int maxPlacementAttempts = 12;
    [Tooltip("Layer mask to consider when checking overlap (default - everything).")]
    public LayerMask overlapCheckMask = ~0;

    [Header("Quantity / Timing")]
    [Tooltip("Number of obstacles to spawn when triggered.")]
    public int spawnCount = 1;
    [Tooltip("If > 0, adds a small random delay (0..delayVariance) before each spawn.")]
    public float delayVariance = 0f;

    [Header("Physics")]
    [Tooltip("Add an impulse to spawned obstacle (optional).")]
    public float spawnImpulse = 0f;

    // internal state
    private bool hasTriggered = false;

    void Update()
    {
        if (shipTransform == null || marsTransform == null || obstaclePrefab == null)
            return;

        float distToMars = Vector3.Distance(shipTransform.position, marsTransform.position);

        if (!hasTriggered && distToMars < triggerDistance)
        {
            StartCoroutine(SpawnSequence());
            hasTriggered = true;
        }
        else if (hasTriggered && resetOnDeparture && distToMars > triggerDistance + 50f)
        {
            // allow re-trigger after ship moves away (small hysteresis)
            hasTriggered = false;
        }
    }

    IEnumerator SpawnSequence()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            if (delayVariance > 0f)
                yield return new WaitForSeconds(Random.Range(0f, delayVariance));

            Vector3 spawnPos;
            if (FindValidSpawnPosition(out spawnPos))
            {
                var go = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
                // try to orient obstacle to face the ship (optional)
                go.transform.LookAt(shipTransform.position);

                // add physics impulse if Rigidbody exists
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null && spawnImpulse != 0f)
                {
                    // small push roughly perpendicular/random so it moves into path
                    Vector3 dir = (shipTransform.position - spawnPos).normalized;
                    Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
                    Vector3 impulse = (dir * -0.2f + perp * Random.Range(-0.6f, 0.6f)).normalized * spawnImpulse;
                    rb.AddForce(impulse, ForceMode.Impulse);
                }

                Debug.Log($"ObstacleSpawner: spawned obstacle #{i + 1} at {spawnPos}");
            }
            else
            {
                Debug.LogWarning("ObstacleSpawner: couldn't find a valid spawn position after attempts");
            }
        }
    }

    // Attempts to find a safe spawn position in front of the ship and avoids overlaps
    bool FindValidSpawnPosition(out Vector3 result)
    {
        Vector3 basePos = shipTransform.position + shipTransform.forward * spawnDistanceInFront;

        // Try multiple variants with increasing random offset if required
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            // start with base, then add randomness
            Vector3 candidate = basePos + Random.insideUnitSphere * randomOffset * (1f + attempt * 0.25f);

            // ensure candidate is not too close to ship or Mars
            if (Vector3.Distance(candidate, shipTransform.position) < minDistanceFromShip)
                continue;
            if (Vector3.Distance(candidate, marsTransform.position) < 20f) // don't spawn right on Mars
                continue;

            // check for overlaps (so we don't spawn inside other colliders)
            float checkRadius = 1.0f;
            var col = obstaclePrefab.GetComponent<Collider>();
            if (col != null)
            {
                // approximate radius from prefab bounds
                checkRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z);
            }

            // use OverlapSphere to verify space is free
            Collider[] hits = Physics.OverlapSphere(candidate, checkRadius * 0.9f, overlapCheckMask);
            bool collidesWithShip = false;
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(shipTransform) || h.transform == shipTransform)
                {
                    collidesWithShip = true;
                    break;
                }
            }
            if (collidesWithShip)
                continue;

            // Accept candidate
            result = candidate;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // visualize spawn area in editor
    void OnDrawGizmosSelected()
    {
        if (shipTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 basePos = shipTransform.position + (Application.isPlaying ? shipTransform.forward : Vector3.forward) * spawnDistanceInFront;
            Gizmos.DrawWireSphere(basePos, randomOffset + 1f);
        }

        if (marsTransform != null && shipTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(shipTransform.position, marsTransform.position);
        }
    }
}
