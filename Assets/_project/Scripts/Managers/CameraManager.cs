using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class CameraManager : MonoBehaviour
{
    enum VirtualCameras
    {
        NoSelection = -1,
        CockpitCamera = 0,
        FollowCamera = 1,
        EnemyFollowCamera = 2,
    }

    [SerializeField]
    List<CinemachineVirtualCamera> _virtualCameras;

    [Header("Auto-fill")]
    [Tooltip("If true and _virtualCameras is empty, this will try to auto-fill from child CinemachineVirtualCamera components.")]
    public bool autoFillFromChildren = true;

    public Transform ActiveCamera { get; private set; }
    public UnityEvent ActiveCameraChanged;

    VirtualCameras CameraKeyPressed
    {
        get
        {
            if (_virtualCameras == null || _virtualCameras.Count == 0) return VirtualCameras.NoSelection;

            for (int i = 0; i < _virtualCameras.Count; ++i)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return (VirtualCameras)i;
            }

            return VirtualCameras.NoSelection;
        }
    }

    void Awake()
    {
        ActiveCameraChanged = ActiveCameraChanged ?? new UnityEvent();

        // Defensive: ensure list exists
        if (_virtualCameras == null)
            _virtualCameras = new List<CinemachineVirtualCamera>();

        // Optionally auto-fill from child virtual cameras
        if (autoFillFromChildren && _virtualCameras.Count == 0)
        {
            var found = GetComponentsInChildren<CinemachineVirtualCamera>(true);
            if (found != null && found.Length > 0)
            {
                foreach (var v in found) _virtualCameras.Add(v);
                Debug.Log($"[CameraManager] Auto-filled {_virtualCameras.Count} virtual cameras from children.");
            }
        }
    }

    void Start()
    {
        if (_virtualCameras == null || _virtualCameras.Count == 0)
        {
            Debug.LogWarning("[CameraManager] No virtual cameras assigned. Please assign CinemachineVirtualCamera(s) to the _virtualCameras list in the Inspector.");
            return;
        }

        // Optional safety: clamp indexes if list size differs from enum assumed count
        SetActiveCamera(VirtualCameras.CockpitCamera);
    }

    void Update()
    {
        SetActiveCamera(CameraKeyPressed);
    }

    void SetActiveCamera(VirtualCameras selectedCamera)
    {
        if (selectedCamera == VirtualCameras.NoSelection) return;

        if (_virtualCameras == null || _virtualCameras.Count == 0)
        {
            Debug.LogWarning("[CameraManager] Attempted SetActiveCamera but _virtualCameras is empty.");
            return;
        }

        VirtualCameras camIndex = VirtualCameras.CockpitCamera;
        foreach (var cam in _virtualCameras)
        {
            if (cam == null)
            {
                Debug.LogWarning("[CameraManager] Found null entry inside _virtualCameras list. Check Inspector.");
                camIndex++;
                continue;
            }

            if (camIndex++ == selectedCamera)
            {
                cam.gameObject.SetActive(true);
                ActiveCamera = cam.transform;
                ActiveCameraChanged?.Invoke();
            }
            else
            {
                cam.gameObject.SetActive(false);
            }
        }
    }
}
