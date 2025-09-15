using System;
using System.Reflection;
using UnityEngine;

public class PlaceFieldsRuntimeSporadic : MonoBehaviour
{
    [Header("Path")]
    public Transform startPoint; // Earth
    public Transform endPoint;   // Mars

    [Header("Prefab")]
    [Tooltip("Must be a prefab (not a scene object).")]
    public GameObject fieldPrefab; // AsteroidField prefab

    [Header("Slotting & randomness")]
    [Tooltip("Divide the path into this many slots to decide placement positions.")]
    public int slots = 12;
    [Tooltip("Approximate total fields to attempt to place (will stop early when reached).")]
    public int maxFieldsToCreate = 4;
    [Range(0f, 1f), Tooltip("Base probability a slot will get a field.")]
    public float placementChance = 0.35f;
    [Tooltip("Maximum number of fields allowed consecutively (creates clusters).")]
    public int maxConsecutive = 2;
    [Range(0f, 1f), Tooltip("If a slot was chosen, this is the chance to continue the cluster to the next slot (subject to maxConsecutive).")]
    public float clusterContinuationChance = 0.45f;
    [Tooltip("Perpendicular jitter (world units) to offset fields from exact line.")]
    public float lateralJitter = 30f;

    [Header("Safety & parenting")]
    public Transform parentContainer;
    [Tooltip("Skip placement if closer than this to any existing AsteroidField.")]
    public float minDistanceFromExistingFields = 120f;
    [Tooltip("If true, will attempt to call a spawn/init method on the spawned instance.")]
    public bool callSpawnMethod = true;
    public string spawnMethodName = "SpawnInitial";
    public string nameSuffix = "_Sporadic_";

    [Header("Spawn Timing")]
    [Tooltip("Delay (seconds) after Play before placing fields. Set to 0 to place immediately.")]
    public float spawnDelay = 5f;
    [Tooltip("If true, the delay uses realtime and ignores Time.timeScale.")]
    public bool useRealtimeDelay = false;

    private bool _hasSpawned = false;

    // Coroutine Start replaces normal Start
    private System.Collections.IEnumerator Start()
    {
        if (startPoint == null || endPoint == null || fieldPrefab == null) yield break;

        if (spawnDelay <= 0f)
        {
            PlaceSporadicFields();
            _hasSpawned = true;
            yield break;
        }

        if (useRealtimeDelay)
            yield return new WaitForSecondsRealtime(spawnDelay);
        else
            yield return new WaitForSeconds(spawnDelay);

        if (!_hasSpawned)
        {
            PlaceSporadicFields();
            _hasSpawned = true;
        }
    }

    public void PlaceSporadicFields()
    {
        if (slots <= 0 || maxFieldsToCreate <= 0)
        {
            Debug.LogWarning("[PlaceFieldsRuntimeSporadic] slots and maxFieldsToCreate must be > 0");
            return;
        }

        // find existing fields to avoid overlap
        Component[] existingAsteroidFields = new Component[0];
        Type afType = null;
        try
        {
            afType = Type.GetType("AsteroidField") ?? Type.GetType("AsteroidField, Assembly-CSharp");
            if (afType != null) existingAsteroidFields = (Component[])FindObjectsOfType(afType);
        }
        catch { }

        int placed = 0;
        int consecutive = 0;

        for (int i = 1; i <= slots && placed < maxFieldsToCreate; i++)
        {
            bool tryPlaceThisSlot = false;

            if (consecutive > 0)
            {
                if (consecutive < maxConsecutive && UnityEngine.Random.value < clusterContinuationChance)
                    tryPlaceThisSlot = true;
                else
                    consecutive = 0; // end cluster
            }

            if (!tryPlaceThisSlot && UnityEngine.Random.value < placementChance)
                tryPlaceThisSlot = true;

            if (!tryPlaceThisSlot) continue;

            float t = (float)i / (slots + 1f);
            Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, t);

            // add jitter
            Vector3 pathDir = (endPoint.position - startPoint.position).normalized;
            Vector3 perp = Vector3.Cross(pathDir, Vector3.up).normalized;
            if (perp == Vector3.zero) perp = Vector3.Cross(pathDir, Vector3.forward).normalized;
            pos += perp * UnityEngine.Random.Range(-lateralJitter, lateralJitter)
                 + Vector3.up * UnityEngine.Random.Range(-lateralJitter * 0.12f, lateralJitter * 0.12f);

            // check distance from existing fields
            bool tooClose = false;
            if (afType != null && existingAsteroidFields.Length > 0)
            {
                foreach (var comp in existingAsteroidFields)
                {
                    if (comp == null) continue;
                    if (Vector3.Distance(pos, comp.transform.position) < minDistanceFromExistingFields)
                    {
                        tooClose = true; break;
                    }
                }
            }

            if (tooClose) { consecutive = 0; continue; }

            // instantiate field
            var createdGO = Instantiate(fieldPrefab, pos, fieldPrefab.transform.rotation,
                parentContainer ? parentContainer : this.transform);
            createdGO.name = fieldPrefab.name + nameSuffix + (placed + 1);

            if (callSpawnMethod && !string.IsNullOrEmpty(spawnMethodName))
            {
                TryInvokeMethodByName(createdGO, spawnMethodName);
                TryInvokeMethodByName(createdGO, "SpawnNow");
                TryInvokeMethodByName(createdGO, "Initialize");
                TryInvokeMethodByName(createdGO, "SpawnInitial");
            }

            placed++;
            consecutive++;

            if (placed >= maxFieldsToCreate) break;
        }

        Debug.Log($"[PlaceFieldsRuntimeSporadic] Slots={slots}, Wanted={maxFieldsToCreate}, Placed={placed}");
    }

    private bool TryInvokeMethodByName(GameObject go, string methodName)
    {
        var comps = go.GetComponents<MonoBehaviour>();
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            var mi = comp.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                try
                {
                    mi.Invoke(comp, null);
                    Debug.Log($"[PlaceFieldsRuntimeSporadic] Invoked {methodName}() on {comp.GetType().Name} of {go.name}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlaceFieldsRuntimeSporadic] Failed {methodName} on {comp.GetType().Name}: {ex.Message}");
                }
            }
        }
        return false;
    }
}
