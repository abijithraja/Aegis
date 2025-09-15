using UnityEngine;
using UnityEngine.UI;
using TMPro; // optional - remove if not using TextMeshPro

public class FuelUI : MonoBehaviour
{
    [Header("Assign either a Slider OR an Image (Fill)")]
    public Slider fuelSlider;           // optional (UnityEngine.UI)
    public Image fuelFillImage;         // optional (Image.type = Filled)
    public Text fuelText;               // optional UI.Text
    public TextMeshProUGUI fuelTextTMP; // optional TMP

    [Header("Colors")]
    public Color lowColor = new Color(0.8f, 0.1f, 0.1f);
    public Color highColor = new Color(0.1f, 0.8f, 0.1f);

    FuelSystem _fuel;

    void Start()
    {
        // nothing automatically binds here — use SetFuelSystem or FuelBinder to connect
    }

    public void SetFuelSystem(FuelSystem fuel)
    {
        if (_fuel != null) _fuel.OnFuelChanged -= OnFuelChanged;
        _fuel = fuel;
        if (_fuel != null) _fuel.OnFuelChanged += OnFuelChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (_fuel != null) _fuel.OnFuelChanged -= OnFuelChanged;
    }

    void OnFuelChanged(float current, float max)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_fuel == null) return;
        float pct = (_fuel.MaxFuel <= 0f) ? 0f : _fuel.CurrentFuel / _fuel.MaxFuel;

        if (fuelSlider != null)
        {
            fuelSlider.maxValue = _fuel.MaxFuel;
            fuelSlider.value = _fuel.CurrentFuel;
        }

        if (fuelFillImage != null)
        {
            fuelFillImage.fillAmount = pct;
            fuelFillImage.color = Color.Lerp(lowColor, highColor, pct);
        }

        if (fuelText != null)
            fuelText.text = $"{Mathf.CeilToInt(_fuel.CurrentFuel)} / {Mathf.CeilToInt(_fuel.MaxFuel)}";

        if (fuelTextTMP != null)
            fuelTextTMP.text = $"{Mathf.CeilToInt(_fuel.CurrentFuel)} / {Mathf.CeilToInt(_fuel.MaxFuel)}";
    }
}
