using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Controller Settings")]
    public ControllerType controllerType = ControllerType.Player;
    [Tooltip("Đánh dấu đây là xe dùng trong Menu/Showcase")]
    public bool isMenuModel = false;

    [Header("Menu Rotation Settings")]
    [Tooltip("Tốc độ xoay tự động trong Menu (độ/giây)")]
    public float rotationSpeed = 30f;
    [Tooltip("Trục xoay (mặc định là xoay quanh trục Y - Vector3.up)")]
    public Vector3 rotationAxis = Vector3.up;
    [Tooltip("Đóng băng Physics (Rigidbody) khi ở chế độ Menu để tránh xe bị di chuyển hay nẩy")]
    public bool freezePhysicsInMenu = true;

    [Header("References")]
    public RCCP_CarController carController;

    /// <summary>
    /// Kiểm tra xem xe có đang ở chế độ Menu hay không.
    /// </summary>
    public bool IsMenuModel => isMenuModel || controllerType == ControllerType.Menu;

    private void Awake()
    {
        if (carController == null)
            carController = GetComponent<RCCP_CarController>();
    }

    private void Start()
    {
        ApplyControlState();
    }

    private void OnEnable()
    {
        ApplyControlState();
    }

    private void OnValidate()
    {
        if (carController == null)
            carController = GetComponent<RCCP_CarController>();
    }

    /// <summary>
    /// Áp dụng trạng thái điều khiển cho xe dựa trên IsMenuModel.
    /// </summary>
    public void ApplyControlState()
    {
        if (IsMenuModel)
        {
            // Vô hiệu hóa khả năng điều khiển xe
            if (carController != null)
            {
                carController.SetCanControl(false);
            }

            if (freezePhysicsInMenu)
            {
                SetKinematicAllParts(true);
            }
        }
    }

    /// <summary>
    /// Đặt tất cả Rigidbody của thân xe và toàn bộ linh kiện con (Cửa, Nắp capo, Cản...) thành Kinematic.
    /// </summary>
    public void SetKinematicAllParts(bool kinematic)
    {
        Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in allRigidbodies)
        {
            rb.isKinematic = kinematic;
            if (kinematic)
            {
                rb.useGravity = false;
            }
        }
    }

    private void Update()
    {
        if (IsMenuModel)
        {
            // Xoay tròn đều trong Menu Scene
            transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime), Space.Self);
        }
    }
}
