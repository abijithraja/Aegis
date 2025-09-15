using System.Collections;
using UnityEngine;

public class RandomRockSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject rockPrefab; // assign RockPrefab

    [Header("Spawn area")]
    public Transform areaCenter; // if null, uses this.transform
    public float spawnRadius = 200f; // spawn within sphere radius
    public float minDistanceFromShip = 40f; // avoid spawning right on top of ship

    [Header("Spawn timing")]
    public bool spawnOnStart = true;
    public bool spawnContinuously = false;
    public float spawnInterval = 10f; // seconds per rock when continuous
    public int initialSpawnCount = 5;

    [Header("Behavior")]
    public bool spawnFacingShip = true; // rotate to face ship initially
    public bool spawnBurst = false;
    public int burstCount = 3;
    public float burstInterval = 0.12f;

    public Transform ship; // assign ship transform (optional)

    void Start()
    {
        if (areaCenter == null) areaCenter = this.transform;
        if (ship == null)
        {
            var found = GameObject.FindWithTag("Player") ?? GameObject.FindWithTag("Ship");
            if (found) ship = found.transform;
        }

        if (spawnOnStart)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnOneRandom();
            }
        }

        if (spawnContinuously)
        {
            StartCoroutine(SpawnLoop());
        }
    }

    public IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOneRandom();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnBurst()
    {
        StartCoroutine(SpawnBurst(burstCount, burstInterval));
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
        if (rockPrefab == null)
        {
            Debug.LogWarning("[RandomRockSpawner] No rockPrefab assigned.");
            return null;
        }

        Vector3 pos;
        int tries = 0;
        do
        {
            Vector3 randomPoint = Random.insideUnitSphere * spawnRadius;
            pos = areaCenter.position + randomPoint;
            tries++;
            if (tries > 10) break;
        } while (ship != null && Vector3.Distance(pos, ship.position) < minDistanceFromShip);

        Quaternion rot = Random.rotation;
        GameObject go = Instantiate(rockPrefab, pos, rot, null);
        if (spawnFacingShip && ship != null)
        {
            go.transform.rotation = Quaternion.LookRotation((ship.position - pos).normalized, Vector3.up);
        }

        return go;
    }
}
