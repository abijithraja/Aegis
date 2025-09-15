using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class ModeController : MonoBehaviour
{
    [Header("UI (optional)")]
    // --- CHANGE 1: Added a toggle to control the auto-generated UI ---
    public bool createDebugUI = false; // Set this to true ONLY to auto-generate buttons for testing
    public Button balanceButton;     // optional: assign in inspector
    public Button explorerButton;    // optional: assign in inspector
    public Text statusText;          // optional: small on-screen status

    [Header("Fuel")]
    public FuelSystem fuelSystem;    // assign your fuel system (must expose CurrentFuel)
    public float fuelThreshold = 650f;

    [Header("Autopilot")]
    public MonoBehaviour autopilotController; // optional; should expose public bool IsAutopilotActive()
    public bool autopilotActiveForTesting = false; // fallback for testing if no autopilot assigned

    [Header("Input")]
    public KeyCode keyboardToggleKey = KeyCode.A;
    public string gamepadToggleButton = "joystick button 0"; // gamepad A

    [Header("Events (optional)")]
    public UnityEvent onBalanceEnabled;
    public UnityEvent onBalanceDisabled;
    public UnityEvent onExplorerEnabled;
    public UnityEvent onExplorerDisabled;

    // internal state
    bool balanceActive = false;
    bool explorerActive = false;
    bool thresholdActive = false; // true when fuel >= threshold (Explorer priority)

    // visuals
    readonly Color transparentColor = new Color(1f, 1f, 1f, 0.25f);
    readonly Color greenColor = new Color(0.15f, 0.8f, 0.15f, 1f);

    void Start()
    {
        // wire buttons if assigned
        if (balanceButton != null)
        {
            balanceButton.onClick.RemoveAllListeners();
            balanceButton.onClick.AddListener(OnBalanceButtonClicked);
        }
        if (explorerButton != null)
        {
            explorerButton.onClick.RemoveAllListeners();
            explorerButton.onClick.AddListener(OnExplorerButtonClicked);
        }

        // --- CHANGE 2: Only create the UI if the new checkbox is ticked ---
        if (createDebugUI && (balanceButton == null || explorerButton == null || statusText == null))
            CreateSimpleUIIfNeeded();

        // initialize based on current fuel
        float startFuel = fuelSystem != null ? fuelSystem.CurrentFuel : 0f;
        if (startFuel >= fuelThreshold)
        {
            thresholdActive = true;
            ForceExplorerOn();
        }
        else
        {
            thresholdActive = false;
            ForceBalanceOn();
        }

        UpdateStatusText();
    }

    void Update()
    {
        // monitor fuel crossing threshold
        if (fuelSystem != null)
        {
            float f = fuelSystem.CurrentFuel;
            if (!thresholdActive && f >= fuelThreshold)
            {
                // crossed up: explorer priority
                thresholdActive = true;
                ForceExplorerOn();
            }
            else if (thresholdActive && f < fuelThreshold)
            {
                // dropped below: revert to balance
                thresholdActive = false;
                ForceBalanceOn();
            }
        }

        // input: A key or gamepad A
        if (Input.GetKeyDown(keyboardToggleKey) || Input.GetKeyDown(gamepadToggleButton))
        {
            // try to enable balance via input (requires autopilot & fuel below threshold)
            if (IsAutopilotActive())
            {
                if (fuelSystem == null || fuelSystem.CurrentFuel < fuelThreshold)
                {
                    // enable balance explicitly
                    SetBalance(true);
                    SetExplorer(false);
                }
                else
                {
                    Debug.Log("[ModeController] Balance blocked: fuel >= threshold (Explorer priority).");
                }
            }
            else
            {
                Debug.Log("[ModeController] Balance blocked: autopilot not active.");
            }
        }

        UpdateStatusText();
    }

    // Button callbacks
    public void OnBalanceButtonClicked()
    {
        // same logic as pressing A
        if (IsAutopilotActive())
        {
            if (fuelSystem == null || fuelSystem.CurrentFuel < fuelThreshold)
            {
                SetBalance(true);
                SetExplorer(false);
            }
            else Debug.Log("[ModeController] Balance blocked by threshold.");
        }
        else Debug.Log("[ModeController] Balance blocked: autopilot not active.");
    }

    public void OnExplorerButtonClicked()
    {
        // manual explorer enabling allowed but if thresholdActive is true it's already on
        SetExplorer(true);
        SetBalance(false);
    }

    // Core setters (no forced logic here; forcing is handled by ForceExplorerOn/ForceBalanceOn)
    void SetBalance(bool on)
    {
        balanceActive = on;
        UpdateBalanceVisual();
        if (on) onBalanceEnabled?.Invoke(); else onBalanceDisabled?.Invoke();

        // balance interactable only when not blocked by threshold
        if (balanceButton != null)
            balanceButton.interactable = (balanceActive && !thresholdActive);
    }

    void SetExplorer(bool on)
    {
        explorerActive = on;
        UpdateExplorerVisual();
        if (on) onExplorerEnabled?.Invoke(); else onExplorerDisabled?.Invoke();

        if (explorerButton != null)
            explorerButton.interactable = explorerActive; // explorer interactable when on
    }

    // Force functions used when threshold changes
    void ForceExplorerOn()
    {
        // Explorer must be ON (now: transparent when active per flipped visuals), Balance must be OFF (now: green when inactive)
        explorerActive = true;
        balanceActive = false;

        UpdateExplorerVisual();
        UpdateBalanceVisual();

        if (explorerButton != null) explorerButton.interactable = true;
        if (balanceButton != null) balanceButton.interactable = false;

        onExplorerEnabled?.Invoke();
        onBalanceDisabled?.Invoke();
    }

    void ForceBalanceOn()
    {
        // Balance must be ON (now: transparent when active), Explorer must be OFF (now: green when inactive)
        explorerActive = false;
        balanceActive = true;

        UpdateExplorerVisual();
        UpdateBalanceVisual();

        if (explorerButton != null) explorerButton.interactable = false;
        if (balanceButton != null) balanceButton.interactable = true;

        onBalanceEnabled?.Invoke();
        onExplorerDisabled?.Invoke();
    }

    // Visual helpers (FLIPPED: active => transparent, inactive => green)
    void UpdateBalanceVisual()
    {
        if (balanceButton == null) return;
        var img = balanceButton.GetComponent<Image>();
        if (img == null) return;
        // flipped: when active => transparent, when inactive => green
        img.color = balanceActive ? transparentColor : greenColor;
    }

    void UpdateExplorerVisual()
    {
        if (explorerButton == null) return;
        var img = explorerButton.GetComponent<Image>();
        if (img == null) return;
        // flipped: when active => transparent, when inactive => green
        img.color = explorerActive ? transparentColor : greenColor;
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        string fuelStr = fuelSystem != null ? fuelSystem.CurrentFuel.ToString("F0") : "N/A";
        statusText.text = $"Balance: {(balanceActive ? "ON" : "OFF")}\nExplorer: {(explorerActive ? "ON" : "OFF")}\nFuel: {fuelStr}\nThresholdActive: {thresholdActive}";
    }

    bool IsAutopilotActive()
    {
        if (autopilotController != null)
        {
            var method = autopilotController.GetType().GetMethod("IsAutopilotActive", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                try
                {
                    object r = method.Invoke(autopilotController, null);
                    if (r is bool b) return b;
                }
                catch { }
            }
        }
        return autopilotActiveForTesting;
    }

    // --- simple UI builder (if no UI assigned) ---
    void CreateSimpleUIIfNeeded()
    {
        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Canvas
        GameObject canvasGO = new GameObject("ModeUI_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Balance Button
        balanceButton = CreateButton(canvasGO.transform, "BalanceButton", "Balance Mode", new Vector2(20, -20));
        // Explorer Button
        explorerButton = CreateButton(canvasGO.transform, "ExplorerButton", "Explorer", new Vector2(20, -90));
        // Status Text
        GameObject st = new GameObject("StatusText");
        st.transform.SetParent(canvasGO.transform, false);
        var txt = st.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.alignment = TextAnchor.UpperLeft;
        txt.rectTransform.anchorMin = new Vector2(0, 1);
        txt.rectTransform.anchorMax = new Vector2(0, 1);
        txt.rectTransform.pivot = new Vector2(0, 1);
        txt.rectTransform.anchoredPosition = new Vector2(220, -10);
        txt.rectTransform.sizeDelta = new Vector2(300, 120);
        txt.fontSize = 16;
        txt.color = Color.white;
        statusText = txt;

        // wire callbacks
        if (balanceButton != null) balanceButton.onClick.AddListener(OnBalanceButtonClicked);
        if (explorerButton != null) explorerButton.onClick.AddListener(OnExplorerButtonClicked);
    }

    Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject b = new GameObject(name);
        b.transform.SetParent(parent, false);
        var img = b.AddComponent<Image>();
        img.color = transparentColor; // default transparent

        var btn = b.AddComponent<Button>();
        var rt = b.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 50);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;

        // Text child
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(b.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;

        return btn;
    }
}