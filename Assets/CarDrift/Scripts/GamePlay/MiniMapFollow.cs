using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Xe cần follow. Nếu để trống, script sẽ tự động tìm xe Player khi xe được load xong.")]
    public Transform target;

    [Header("Position Settings")]
    [Tooltip("Giữ nguyên độ cao Y của MiniMapCam lúc đặt trong Scene")]
    [SerializeField] private bool keepInitialHeight = true;
    [Tooltip("Độ cao Y cố định nếu không dùng keepInitialHeight")]
    [SerializeField] private float fixedHeightY = 101.22f;
    [Tooltip("Offset cộng thêm (nếu muốn chỉnh lệch tâm X, Z)")]
    [SerializeField] private Vector2 offsetXZ = Vector2.zero;

    [Header("Rotation Settings")]
    [Tooltip("Góc xoay cố định của MiniMapCam (Mặc định chuẩn là 90, -90, 0)")]
    [SerializeField] private Vector3 fixedRotationEuler = new Vector3(90f, -90f, 0f);
    [Tooltip("Bật xoay MiniMap theo hướng xe Player (chỉ xoay trên trục Z của Camera)")]
    public bool rotateWithPlayer = false;
    [Tooltip("Offset góc xoay trục Z nếu cần bù góc")]
    [SerializeField] private float rotationOffsetZ = 0f;
    [Tooltip("Độ mượt khi xoay theo Player")]
    [SerializeField] private bool smoothRotation = false;
    [SerializeField] private float smoothRotationSpeed = 15f;

    [Header("Smooth Options")]
    [SerializeField] private bool smoothFollow = false;
    [SerializeField] private float smoothSpeed = 15f;

    private float initialY;
    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.position;
        initialY = transform.position.y;
        transform.rotation = Quaternion.Euler(fixedRotationEuler);
    }

    private void LateUpdate()
    {
        // 1. Tìm xe Player thật sự đang chạy
        ResolveTarget();

        // 2. Cập nhật góc xoay: Chỉ xoay trục Z theo Player nếu bật rotateWithPlayer
        if (rotateWithPlayer && target != null)
        {
            float targetZ = fixedRotationEuler.z - target.eulerAngles.y + rotationOffsetZ;
            Vector3 targetEuler = new Vector3(fixedRotationEuler.x, fixedRotationEuler.y, targetZ);
            if (smoothRotation)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetEuler), smoothRotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Euler(targetEuler);
            }
        }
        else
        {
            transform.rotation = Quaternion.Euler(fixedRotationEuler);
        }

        // Nếu xe chưa load xong hoặc target đang ở toạ độ rác (0,0,0), giữ nguyên vị trí camera ban đầu
        if (target == null)
            return;

        Vector3 targetWorldPos = target.position;

        // Nếu target chưa được đặt đúng vị trí (ở gốc toạ độ 0,0,0 trong khi map ở 1100+), bỏ qua frame này
        if (targetWorldPos.sqrMagnitude < 1f)
            return;

        // 3. Tính toán toạ độ X, Z theo xe (Y giữ nguyên độ cao ban đầu)
        float targetY = keepInitialHeight ? initialY : fixedHeightY;
        Vector3 desiredPosition = new Vector3(
            targetWorldPos.x + offsetXZ.x,
            targetY,
            targetWorldPos.z + offsetXZ.y
        );

        // 4. Di chuyển camera
        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = desiredPosition;
        }
    }

    private void ResolveTarget()
    {
        // Nếu target chưa được gán hoặc bị null
        if (target == null)
        {
            if (RCCP_SceneManager.Instance != null && RCCP_SceneManager.Instance.activePlayerVehicle != null)
            {
                target = RCCP_SceneManager.Instance.activePlayerVehicle.transform;
            }
            else
            {
                // Thử tìm xe Player qua CarController trong Scene
                CarController[] cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
                foreach (var car in cars)
                {
                    if (car.controllerType == ControllerType.Player)
                    {
                        if (car.carController != null)
                            target = car.carController.transform;
                        else if (car.transform.position.sqrMagnitude > 10f)
                            target = car.transform;
                        break;
                    }
                }
            }
        }
        else
        {
            // Nếu target đang trỏ vào Object cha rỗng, tự động lấy RCCP_CarController con có Rigidbody
            RCCP_CarController rccp = target.GetComponentInChildren<RCCP_CarController>();
            if (rccp != null && target != rccp.transform)
            {
                target = rccp.transform;
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            RCCP_CarController rccp = newTarget.GetComponentInChildren<RCCP_CarController>();
            target = rccp != null ? rccp.transform : newTarget;
        }
        else
        {
            target = null;
        }
    }
}
