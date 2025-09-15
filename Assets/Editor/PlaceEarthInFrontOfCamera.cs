#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PlaceEarthInFrontOfCamera : EditorWindow
{
    public Camera mainCamera; // fallback main camera
    public Transform cinemaVCamTransform; // optional: if you want to use the VCam transform itself
    public GameObject earthObject; // assign Earth instance (or FBX root)
    public float distanceFromCamera = 1200f;
    public Vector3 localOffset = Vector3.zero; // offset relative to camera forward/right/up
    public float scale = 120f;

    [MenuItem("Tools/Planets/Place Earth In Front Of Camera")]
    static void Open() => GetWindow<PlaceEarthInFrontOfCamera>("Place Earth");

    void OnGUI()
    {
        GUILayout.Label("Place Earth In Front of Camera (Editor only)", EditorStyles.boldLabel);
        mainCamera = (Camera)EditorGUILayout.ObjectField("Main Camera (fallback)", mainCamera, typeof(Camera), true);
        cinemaVCamTransform = (Transform)EditorGUILayout.ObjectField("Use VCam Transform (optional)", cinemaVCamTransform, typeof(Transform), true);
        earthObject = (GameObject)EditorGUILayout.ObjectField("Earth Object", earthObject, typeof(GameObject), true);
        distanceFromCamera = EditorGUILayout.FloatField("Distance from Camera", distanceFromCamera);
        localOffset = EditorGUILayout.Vector3Field("Local Offset (right, up, forward)", localOffset);
        scale = EditorGUILayout.FloatField("Scale (uniform)", scale);

        if (GUILayout.Button("Place Earth Now"))
            PlaceNow();
    }

    void PlaceNow()
    {
        if (earthObject == null)
        {
            EditorUtility.DisplayDialog("Missing", "Assign the Earth GameObject to place.", "OK");
            return;
        }

        Transform camT = cinemaVCamTransform;
        if (camT == null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog("No Camera", "No camera found. Assign Main Camera or VCam transform.", "OK");
                return;
            }
            camT = mainCamera.transform;
        }

        // compute world position: camera position + forward*distance + local offset (right, up, forward)
        Vector3 worldPos = camT.position
            + camT.right * localOffset.x
            + camT.up * localOffset.y
            + camT.forward * (distanceFromCamera + localOffset.z);

        Undo.RecordObject(earthObject.transform, "Place Earth In Front Of Camera");
        earthObject.transform.position = worldPos;
        earthObject.transform.localScale = Vector3.one * scale;
        earthObject.transform.rotation = Quaternion.identity; // optional: reset rotation or keep as desired

        // disable colliders (optional safe)
        var cols = earthObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols) c.enabled = false;

        EditorUtility.SetDirty(earthObject);
        Selection.activeGameObject = earthObject;
        Debug.Log($"Placed {earthObject.name} at {worldPos} (in front of {camT.name})");
    }
}
#endif
