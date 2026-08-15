using UnityEngine;
using UnityEngine.EventSystems;

public class CarRotateDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Rotation Settings")]
    [Tooltip("Tốc độ xoay khi kéo chuột/ngón tay")]
    [SerializeField] private float rotateSensitivity = 0.25f;

    [Tooltip("Độ hãm phanh quán tính khi buông tay (giá trị càng nhỏ dừng càng nhanh, ví dụ 0.80)")]
    [Range(0.1f, 0.95f)]
    [SerializeField] private float damping = 0.80f;

    [Tooltip("Giới hạn góc lướt tối đa sau khi thả tay ra (tránh bị quay tít mù khi vuốt mạnh)")]
    [SerializeField] private float maxInertiaSpeed = 1.5f;

    [Header("Auto Idle Rotation")]
    [Tooltip("Tự động xoay chậm để ngắm xe khi không chạm vào màn hình")]
    [SerializeField] private bool autoRotateWhenIdle = false;
    [SerializeField] private float autoRotateSpeed = 8f;
    [SerializeField] private float idleDelay = 2f;

    private float currentVelocity = 0f;
    private bool isDragging = false;
    private float lastInteractionTime = 0f;

    private void Start()
    {
        lastInteractionTime = Time.time;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentVelocity = 0f;
        lastInteractionTime = Time.time;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        currentVelocity = 0f;
        lastInteractionTime = Time.time;
    }

    public void OnDrag(PointerEventData eventData)
    {
        lastInteractionTime = Time.time;
        float delta = -eventData.delta.x * rotateSensitivity;
        // Giới hạn vận tốc quán tính khi đang kéo
        currentVelocity = Mathf.Clamp(delta * 0.4f, -maxInertiaSpeed, maxInertiaSpeed);
        ApplyRotation(delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        lastInteractionTime = Time.time;
        currentVelocity = Mathf.Clamp(currentVelocity, -maxInertiaSpeed, maxInertiaSpeed);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Apply inertia when released
        if (!isDragging && Mathf.Abs(currentVelocity) > 0.02f)
        {
            ApplyRotation(currentVelocity);
            currentVelocity *= damping;
        }
        else if (!isDragging && Mathf.Abs(currentVelocity) <= 0.02f)
        {
            currentVelocity = 0f;
        }

        // Auto rotate when idle
        if (!isDragging && autoRotateWhenIdle && (Time.time - lastInteractionTime) > idleDelay)
        {
            ApplyRotation(autoRotateSpeed * Time.deltaTime);
        }
    }

    private void ApplyRotation(float angle)
    {
        if (GarageManager.Instance != null)
        {
            GarageManager.Instance.RotateCameraOrbit(angle);
        }
    }
}
