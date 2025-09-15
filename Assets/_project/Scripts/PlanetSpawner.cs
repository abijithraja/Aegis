using UnityEngine;

public class PlanetSpawner : MonoBehaviour
{
    [Header("Assign FBX or prefab assets")]
    public GameObject earth;
    public GameObject mars;
    public GameObject saturn;

    [Header("Positions & scales (tweak in inspector)")]
    public Vector3 earthPos = new Vector3(800f, 50f, -1200f);
    public float earthScale = 120f;

    public Vector3 marsPos = new Vector3(300f, 20f, 500f);
    public float marsScale = 28f;

    public Vector3 saturnPos = new Vector3(-1400f, 100f, 700f);
    public float saturnScale = 220f;

    [Tooltip("Disable colliders on spawned planets (recommended).")]
    public bool disableColliders = true;

    [ContextMenu("Spawn Planets Now")]
    public void SpawnPlanets()
    {
        ClearChildren();

        SpawnOne(earth, earthPos, earthScale, "Planet_Earth");
        SpawnOne(mars, marsPos, marsScale, "Planet_Mars");
        SpawnOne(saturn, saturnPos, saturnScale, "Planet_saturn");
    }

    void SpawnOne(GameObject prefab, Vector3 pos, float scale, string name)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, transform);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;

        if (disableColliders)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; --i)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
