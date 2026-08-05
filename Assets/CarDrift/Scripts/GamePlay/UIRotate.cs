using UnityEngine;
public class UIRotate : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera targetCamera;

    [Header("Rotation Options")]
    public bool matchCameraRotation = true;
    public bool lockYAxis = false;
    public Vector3 rotationOffset = Vector3.zero;
    private Transform cameraTransform;

    private void Awake()
    {
        CacheCamera();
    }

    private void OnEnable()
    {
        CacheCamera();
    }

    private void CacheCamera()
    {
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
        else if (Camera.main != null)
        {
            targetCamera = Camera.main;
            cameraTransform = targetCamera.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            CacheCamera();
            if (cameraTransform == null)
                return;
        }

        if (matchCameraRotation)
        {
            transform.rotation = cameraTransform.rotation;
        }
        else
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }

        if (rotationOffset != Vector3.zero)
        {
            transform.Rotate(rotationOffset, Space.Self);
        }
        if (lockYAxis)
        {
            Vector3 currentEuler = transform.eulerAngles;
            transform.eulerAngles = new Vector3(0f, currentEuler.y, 0f);
        }
    }
}
