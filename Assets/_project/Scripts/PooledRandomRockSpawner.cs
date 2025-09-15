using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledRandomRockSpawner : MonoBehaviour
{
    [Header("Prefab (assign your 'asteroid' prefab)")]
    public GameObject rockPrefab;

    [Header("Spawn area")]
    public Transform areaCenter; // if null uses this.transform
    public float spawnRadius = 200f;
    public float minDistanceFromShip = 40f;

    [Header("Pool")]
    public int poolSize = 40;

    [Header("Timing")]
    public bool spawnOnStart = true;
    public bool spawnContinuously = false;
    public float spawnInterval = 6f; // seconds between spawns if continuous
    public int initialSpawnCount = 8;
    public bool spawnBurstOnStart = false;
    public int burstCount = 5;
    public float burstInterval = 0.12f;

    [Header("Ship (optional)")]
    public Transform ship;

    Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        if (areaCenter == null) areaCenter = this.transform;
        if (ship == null)
        {
            var f = GameObject.FindWithTag("Player") ?? GameObject.FindWithTag("Ship");
            if (f) ship = f.transform;
        }
        // create pool
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(rockPrefab, Vector3.one * 99999f, Quaternion.identity);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    void Start()
    {
        if (spawnOnStart)
        {
            for (int i = 0; i < initialSpawnCount; i++) SpawnOneRandom();
            if (spawnBurstOnStart) StartCoroutine(SpawnBurst(burstCount, burstInterval));
        }
        if (spawnContinuously) StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOneRandom();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public IEnumerator SpawnBurst(int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOneRandom();
            yield return new WaitForSeconds(interval);
        }
    }

    public GameObject SpawnOneRandom()
    {
        if (rockPrefab == null) { Debug.LogWarning("[Spawner] rockPrefab not assigned."); return null; }
        if (pool.Count == 0) { Debug.LogWarning("[Spawner] Pool empty — consider increasing poolSize."); return null; }

        Vector3 pos;
        int tries = 0;
        do
        {
            pos = areaCenter.position + Random.insideUnitSphere * spawnRadius;
            tries++;
            if (tries > 12) break;
        } while (ship != null && Vector3.Distance(pos, ship.position) < minDistanceFromShip);

        var go = pool.Dequeue();
        go.transform.position = pos;
        go.transform.rotation = Random.rotation;
        go.SetActive(true);

        // ensure components reactivate properly (if your rock uses pooled-reset logic, call it here)
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        // optional: give slight initial nudge
        var ai = go.GetComponent<MonoBehaviour>();
        foreach (var m in go.GetComponents<MonoBehaviour>())
        {
            if (m.GetType().Name == "RockAI")
            {
                // optionally reset internal state via reflection if needed
                var mi = m.GetType().GetMethod("EndChase", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (mi != null) mi.Invoke(m, null);
                break;
            }
        }

        // schedule to return to pool
        StartCoroutine(ReturnToPoolAfter(go, 60f)); // 60s default max lifetime
        return go;
    }

    IEnumerator ReturnToPoolAfter(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go == null) yield break;
        go.SetActive(false);
        pool.Enqueue(go);
    }

    // optional: manual recycle (call when rock destroyed)
    public void Recycle(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        pool.Enqueue(go);
    }
}
