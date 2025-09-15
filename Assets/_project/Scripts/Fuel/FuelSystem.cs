using UnityEngine;
using System;

[DisallowMultipleComponent]
public class FuelSystem : MonoBehaviour
{
    [Header("Fuel")]
    public float maxFuel = 1000f;
    public float currentFuel = 1000f;

    [Header("Rates")]
    [Tooltip("Optional idle drain (units per second) when autopilot/offline.")]
    public float idleConsumptionPerSecond = 0f;

    // Events
    public event Action<float, float> OnFuelChanged; // (current, max)
    public event Action OnFuelEmpty;

    void Reset()
    {
        maxFuel = 1000f;
        currentFuel = maxFuel;
    }

    void Awake()
    {
        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
    }

    void Update()
    {
        if (idleConsumptionPerSecond > 0f && currentFuel > 0f)
            Consume(idleConsumptionPerSecond * Time.deltaTime);
    }

    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;
    public float FuelPercent => (maxFuel <= 0f) ? 0f : currentFuel / maxFuel;

    /// <summary>Consume positive amount. Returns actually consumed.</summary>
    public float Consume(float amount)
    {
        if (amount <= 0f) return 0f;
        float prev = currentFuel;
        currentFuel = Mathf.Max(0f, currentFuel - amount);
        float consumed = prev - currentFuel;
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
        if (Mathf.Approximately(currentFuel, 0f)) OnFuelEmpty?.Invoke();
        return consumed;
    }

    public void Refill(float amount)
    {
        if (amount <= 0f) return;
        currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
    }

    public void SetFull()
    {
        currentFuel = maxFuel;
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
    }

    // ---------------- Compatibility wrappers ----------------
    // These are thin adapters so older/newer scripts can use common names
    // without changing the rest of your project.

    /// <summary>Compatibility: consume fuel (float)</summary>
    public void ConsumeFuel(float amount)
    {
        Consume(amount);
    }

    /// <summary>Compatibility: consume fuel (int)</summary>
    public void ConsumeFuel(int amount)
    {
        Consume((float)amount);
    }

    /// <summary>Compatibility: get current fuel as float</summary>
    public float GetCurrentFuel()
    {
        return currentFuel;
    }

    /// <summary>Compatibility: get max fuel as float</summary>
    public float GetMaxFuel()
    {
        return maxFuel;
    }

    /// <summary>Compatibility: get current fuel as int (ceil)</summary>
    public int GetCurrentFuelInt()
    {
        return Mathf.CeilToInt(currentFuel);
    }

    /// <summary>Compatibility: is fuel empty?</summary>
    public bool IsEmpty()
    {
        return Mathf.Approximately(currentFuel, 0f) || currentFuel <= 0f;
    }
    // -------------------------------------------------------
}
