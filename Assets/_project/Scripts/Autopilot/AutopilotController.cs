using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class AutopilotController : MonoBehaviour
{
    [Header("References")]
    public Transform ship;               // assign your ship
    public Transform mars;               // assign Mars (target)

    [Header("Fuel")]
    public FuelSystem fuelSystem;        // ship fuel
    [Tooltip("Fuel units consumed per second while autopilot active.")]
    public float autopilotFuelConsumptionPerSecond = 2f;
    [Tooltip("If true, autopilot will stop when fuel reaches 0")]
    public bool stopAutopilotOnFuelEmpty = true;

    [Header("Stop / Arrival")]
    public float stopDistanceFromSurface = 500f;
    public float arrivalTolerance = 5f;

    [Header("Motion")]
    public float cruiseSpeed = 120f;
    public float rotateSpeed = 3f;

    [Header("Safety / Input disabling")]
    public bool disableInputs = true;
    public string[] inputComponentNameContains = new string[] { "Movement", "Input", "Player", "Controller" };
    public string[] inputComponentNameExcludes = new string[] { "Engine", "Thruster", "ShipEngine", "MainEngine", "Particle", "FX", "Exhaust" };

    [Header("Debug")]
    public bool verbose = true;

    // internals
    Rigidbody _rigidbody;
    bool _isAuto = false;
    List<Behaviour> _disabledComponents = new List<Behaviour>();
    object _shipControllerInstance = null;
    MethodInfo _setControlEnabledMethod = null;

    void Awake()
    {
        if (ship == null)
        {
            var go = GameObject.Find("Ship");
            if (go != null) ship = go.transform;
        }
    }

    void Start()
    {
        if (ship != null) _rigidbody = ship.GetComponent<Rigidbody>();

        if (ship != null)
        {
            var comps = ship.GetComponents<MonoBehaviour>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                var m = c.GetType().GetMethod("SetControlEnabled", BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    _shipControllerInstance = c;
                    _setControlEnabledMethod = m;
                    if (verbose) Debug.Log($"[Autopilot] Found ShipController: {c.GetType().Name}");
                    break;
                }
            }
        }
    }

    [ContextMenu("StartAutopilot_Inspector")]
    public void StartAutopilot_Inspector() { StartAutopilot(); }

    [ContextMenu("StopAutopilot_Inspector")]
    public void StopAutopilot_Inspector() { StopAutopilot(); }

    public bool StartAutopilot()
    {
        if (_isAuto) return true;
        if (ship == null || mars == null) return false;

        if (disableInputs)
        {
            DisableLikelyInputComponents();
            if (_shipControllerInstance != null && _setControlEnabledMethod != null)
                _setControlEnabledMethod.Invoke(_shipControllerInstance, new object[] { false });
        }

        _isAuto = true;
        StopAllCoroutines();
        StartCoroutine(DoAutopilotToStopPoint());
        if (verbose) Debug.Log("[Autopilot] Started.");
        return true;
    }

    public void StopAutopilot()
    {
        if (!_isAuto) return;
        _isAuto = false;
        StopAllCoroutines();
        RestoreDisabledComponents();

        if (_shipControllerInstance != null && _setControlEnabledMethod != null)
            _setControlEnabledMethod.Invoke(_shipControllerInstance, new object[] { true });

        if (_rigidbody != null) _rigidbody.velocity = Vector3.zero;

        if (verbose) Debug.Log("[Autopilot] Stopped and restored input.");
    }

    IEnumerator DoAutopilotToStopPoint()
    {
        Vector3 planetCenter = mars.position;
        float planetRadius = EstimatePlanetRadius(mars.gameObject);
        Vector3 dirFromPlanetToShip = (ship.position - planetCenter).normalized;
        if (dirFromPlanetToShip.sqrMagnitude < 0.0001f) dirFromPlanetToShip = Vector3.back;

        Vector3 stopPoint = (stopDistanceFromSurface > 0f)
            ? planetCenter + dirFromPlanetToShip * (planetRadius + stopDistanceFromSurface)
            : planetCenter;

        while (_isAuto)
        {
            if (ship == null || mars == null) break;

            // --- Fuel consumption ---
            if (fuelSystem != null)
            {
                fuelSystem.Consume(autopilotFuelConsumptionPerSecond * Time.deltaTime);
                if (stopAutopilotOnFuelEmpty && fuelSystem.CurrentFuel <= 0f)
                {
                    Debug.Log("[Autopilot] Fuel empty — stopping autopilot.");
                    _isAuto = false;
                    break;
                }
            }
            // ------------------------

            Vector3 toTarget = stopPoint - ship.position;
            float dist = toTarget.magnitude;

            if (dist <= Mathf.Max(arrivalTolerance, 0.01f))
            {
                if (verbose) Debug.Log("[Autopilot] Arrived at stop point.");
                _isAuto = false;
                break;
            }

            Vector3 dir = toTarget.normalized;
            Quaternion desired = Quaternion.LookRotation(dir, Vector3.up);
            ship.rotation = Quaternion.Slerp(ship.rotation, desired, rotateSpeed * Time.deltaTime);

            if (_rigidbody != null)
            {
                Vector3 desiredVel = ship.forward * cruiseSpeed;
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, desiredVel, 0.5f);
            }
            else
            {
                ship.position = Vector3.MoveTowards(
                    ship.position,
                    ship.position + ship.forward * cruiseSpeed * Time.deltaTime,
                    cruiseSpeed * Time.deltaTime
                );
            }

            yield return null;
        }

        if (_rigidbody != null) _rigidbody.velocity = Vector3.zero;
        RestoreDisabledComponents();

        if (_shipControllerInstance != null && _setControlEnabledMethod != null)
            _setControlEnabledMethod.Invoke(_shipControllerInstance, new object[] { true });

        if (verbose) Debug.Log("[Autopilot] Completed.");
    }

    float EstimatePlanetRadius(GameObject planet)
    {
        var renderers = planet.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; ++i) b.Encapsulate(renderers[i].bounds);
            return b.extents.magnitude;
        }
        var sc = planet.GetComponentInChildren<SphereCollider>();
        if (sc != null)
            return sc.radius * Mathf.Max(sc.transform.lossyScale.x, sc.transform.lossyScale.y, sc.transform.lossyScale.z);
        return 0f;
    }

    void DisableLikelyInputComponents()
    {
        _disabledComponents.Clear();

        var comps = ship.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c == null || c == this) continue;
            string typeName = c.GetType().Name;

            bool include = false;
            foreach (var key in inputComponentNameContains)
                if (!string.IsNullOrEmpty(key) && typeName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) { include = true; break; }
            if (!include) continue;

            bool exclude = false;
            foreach (var ex in inputComponentNameExcludes)
                if (!string.IsNullOrEmpty(ex) && typeName.IndexOf(ex, StringComparison.OrdinalIgnoreCase) >= 0) { exclude = true; break; }
            if (exclude) continue;

            if (c.enabled)
            {
                c.enabled = false;
                _disabledComponents.Add(c);
            }
        }
    }

    void RestoreDisabledComponents()
    {
        foreach (var b in _disabledComponents)
            if (b != null) b.enabled = true;
        _disabledComponents.Clear();
    }

    public void ToggleAutopilot()
    {
        if (_isAuto) StopAutopilot(); else StartAutopilot();
    }

    public bool IsAutopilotActive() => _isAuto;
}
