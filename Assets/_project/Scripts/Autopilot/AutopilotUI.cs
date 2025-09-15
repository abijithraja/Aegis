using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI controller that listens for key (A) and updates a Button visual state.
/// It also calls the AutopilotController on toggle.
/// - Attach this to a Canvas or an empty UI GameObject.
/// - Assign the Button (for visual) and AutopilotController reference.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class AutopilotUI : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.A;
    public Button autopilotButton;
    public Image buttonImage;                // optional image to tint (if not set, will try autopilotButton.image)
    public Color offColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color onColor = new Color(0.1f, 0.8f, 0.1f, 1f);

    public AutopilotController autopilotController;

    void Start()
    {
        if (autopilotButton != null && buttonImage == null)
            buttonImage = autopilotButton.GetComponent<Image>();

        if (autopilotButton != null)
            autopilotButton.onClick.AddListener(OnUIButtonPressed);

        RefreshButtonVisual();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleAutopilot();
        }

        // keep visual state synced if autopilot is triggered externally
        RefreshButtonVisual();
    }

    void OnUIButtonPressed()
    {
        ToggleAutopilot();
    }

    void ToggleAutopilot()
    {
        if (autopilotController == null)
        {
            Debug.LogWarning("[AutopilotUI] No AutopilotController assigned.");
            return;
        }
        bool wasActive = autopilotController.IsAutopilotActive();
        if (wasActive) autopilotController.StopAutopilot();
        else
        {
            bool started = autopilotController.StartAutopilot();
            if (!started) Debug.LogWarning("[AutopilotUI] Autopilot failed to start. Check assignments.");
        }
        RefreshButtonVisual();
    }

    void RefreshButtonVisual()
    {
        if (buttonImage == null) return;
        if (autopilotController != null && autopilotController.IsAutopilotActive())
            buttonImage.color = onColor;
        else
            buttonImage.color = offColor;
    }
}
