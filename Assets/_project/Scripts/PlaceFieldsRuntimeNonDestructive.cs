using System;
using System.Reflection;
using UnityEngine;

public class PlaceFieldsRuntimeNonDestructive : MonoBehaviour
{
    [Header("Path")]
    public Transform startPoint; // Earth
    public Transform endPoint;   // Mars

    [Header("Prefab")]
    [Tooltip("Must be a prefab (not a scene object).")]
    public GameObject fieldPrefab; // AsteroidField prefab

    [Header("Placement")]
    [Tooltip("Number of fields to create between start and end (not including endpoints).")]
    public int fieldCount = 3;
    [Tooltip("Perpendicular jitter to avoid perfect line")]
    public float lateralJitter = 30f;
    [Tooltip("Parent for created fields (recommended: your AsteroidFields container)")]
    public Transform parentContainer;

    [Header("Safety")]
    [Tooltip("Minimum distance (world units) from any existing AsteroidField to allow placement.")]
    public float minDistanceFromExistingFields = 120f;
    [Tooltip("If true, will attempt to call a spawn/init method on the spawned instance after instantiate.")]
    public bool callSpawnMethod = true;
    [Tooltip("Method name to call on the asteroid field (common names: SpawnInitial, SpawnNow, Initialize).")]
    public string spawnMethodName = "SpawnInitial";

    [Header("Naming")]
    public string nameSuffix = "_PathCopy_";

    // Call this at Start() automatically, or call PlaceFieldsFromCode() manually from other scripts.
    void Start()
    {
        // safety guard: only run if all required fields present
        if (startPoint == null || endPoint == null || fieldPrefab == null) return;
        PlaceFieldsFromCode();
    }

    /// <summary>
    /// Main placement function. Can be called from other scripts.
    /// </summary>
    public void PlaceFieldsFromCode()
    {
        if (fieldCount <= 0)
        {
            Debug.LogWarning("[PlaceFieldsRuntimeNonDestructive] fieldCount must be >= 1");
            return;
        }

        // collect existing AsteroidField transforms in scene to avoid overlap
        var existingFields = FindObjectsOfType<MonoBehaviour>(); // fallback - will filter below
        // better: try to find an AsteroidField type by name (if you have that script)
        Component[] existingAsteroidFields = new Component[0];
        Type afType = null;
        try
        {
            afType = Type.GetType("AsteroidField") ?? Type.GetType("AsteroidField, Assembly-CSharp");
            if (afType != null)
            {
                existingAsteroidFields = (Component[])FindObjectsOfType(afType);
            }
        }
        catch
        {
            // ignore — fallback below will scan transforms
        }

        // fallback: get all children of a known parent if parentContainer holds existing ones
        Transform[] existingTransforms;
        if (parentContainer != null)
        {
            existingTransforms = new Transform[parentContainer.childCount];
            for (int i = 0; i < parentContainer.childCount; i++) existingTransforms[i] = parentContainer.GetChild(i);
        }
        else
        {
            existingTransforms = new Transform[0];
        }

        int created = 0;
        for (int i = 1; i <= fieldCount; i++)
        {
            float t = (float)i / (fieldCount + 1); // even spacing, does not include endpoints
            Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, t);

            // compute perpendicular jitter
            Vector3 pathDir = (endPoint.position - startPoint.position).normalized;
            Vector3 perp = Vector3.Cross(pathDir, Vector3.up).normalized;
            if (perp == Vector3.zero) perp = Vector3.Cross(pathDir, Vector3.forward).normalized;
            pos += perp * UnityEngine.Random.Range(-lateralJitter, lateralJitter) + Vector3.up * UnityEngine.Random.Range(-lateralJitter * 0.15f, lateralJitter * 0.15f);

            // test distance to existing AsteroidField instances (using component type if available)
            bool tooClose = false;

            if (afType != null && existingAsteroidFields.Length > 0)
            {
                foreach (var comp in existingAsteroidFields)
                {
                    if (comp == null) continue;
                    var tf = ((Component)comp).transform;
                    if (Vector3.Distance(pos, tf.position) < minDistanceFromExistingFields) { tooClose = true; break; }
                }
            }
            else if (existingTransforms.Length > 0)
            {
                foreach (var tf in existingTransforms)
                {
                    if (tf == null) continue;
                    if (Vector3.Distance(pos, tf.position) < minDistanceFromExistingFields) { tooClose = true; break; }
                }
            }
            else
            {
                // Last resort: find all objects named "AsteroidField" in scene and check positions
                var allCandidates = GameObject.FindObjectsOfType<Transform>();
                foreach (var tf in allCandidates)
                {
                    if (tf == null) continue;
                    if (!tf.name.ToLower().Contains("asteroidfield")) continue;
                    if (Vector3.Distance(pos, tf.position) < minDistanceFromExistingFields) { tooClose = true; break; }
                }
            }

            if (tooClose)
            {
                Debug.Log($"[PlaceFieldsRuntime] Skipping placement at t={t:0.##} — too close to existing field.");
                continue;
            }

            // Instantiate prefab
            var createdGO = Instantiate(fieldPrefab, pos, fieldPrefab.transform.rotation, parentContainer ? parentContainer : this.transform);
            createdGO.name = fieldPrefab.name + nameSuffix + i;
            created++;

            // try call spawn/init method if requested
            if (callSpawnMethod && !string.IsNullOrEmpty(spawnMethodName))
            {
                // attempt a few strategies: try on component type, then on GameObject via reflection
                bool invoked = TryInvokeMethodByName(createdGO, spawnMethodName);
                if (!invoked)
                {
                    // try common alternate method names
                    TryInvokeMethodByName(createdGO, "SpawnNow");
                    TryInvokeMethodByName(createdGO, "Initialize");
                    TryInvokeMethodByName(createdGO, "SpawnInitial");
                }
            }
        }

        Debug.Log($"[PlaceFieldsRuntime] Requested {fieldCount} placements. Created: {created} new field(s).");
    }

    // Tries to find any component on the GameObject that has a method with 'methodName' and invoke it.
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
                    Debug.Log($"[PlaceFieldsRuntime] Invoked {methodName}() on {comp.GetType().Name} of {go.name}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlaceFieldsRuntime] Failed to invoke {methodName} on {comp.GetType().Name}: {ex.Message}");
                }
            }
        }
        return false;
    }
}
