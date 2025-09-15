using UnityEngine;

public class FuelBinder : MonoBehaviour
{
    public FuelSystem fuelSystem;
    public FuelUI fuelUI;

    void Start()
    {
        if (fuelUI != null && fuelSystem != null)
            fuelUI.SetFuelSystem(fuelSystem);
    }
}
