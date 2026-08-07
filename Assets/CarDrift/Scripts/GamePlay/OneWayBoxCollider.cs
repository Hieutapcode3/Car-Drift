using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tạo tường / vật cản 1 chiều (One-Way Wall) dành cho Xe (RCCP) và Vật thể Physics.
/// Xe chạy theo chiều mũi tên XANH sẽ đi xuyên qua, chạy theo chiều mũi tên ĐỎ sẽ bị chặn lại.
/// Tự động xử lý biến Tường thành Trigger để bánh xe (WheelCollider) đi qua mượt mà 100%.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class OneWayBoxCollider : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Bật Log thông báo lên Console để kiểm tra tại sao xe không đi qua được.")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Direction Settings")]
    [Tooltip("Hướng xe được phép chạy xuyên qua (Mặc định Vector3.forward = hướng Z tới).")]
    [SerializeField] private Vector3 entryDirection = Vector3.forward;

    [Tooltip("Sử dụng hướng theo góc xoay local của Object này?")]
    [SerializeField] private bool localDirection = true;

    [Header("Trigger Settings")]
    [Tooltip("Kích thước Trigger phát hiện xe so với BoxCollider gốc. Tăng trục Z (ví dụ 4.0) cho xe chạy nhanh.")]
    [SerializeField] private Vector3 triggerScale = new Vector3(1.5f, 1.5f, 4f);

    [Tooltip("Tự động áp dụng cho tất cả Collider thuộc về xe (WheelColliders + Body Colliders)?")]
    [SerializeField] private bool ignoreAllCarColliders = true;

    private BoxCollider mainCollider;
    private BoxCollider triggerCollider;

    // Danh sách các Collider đang nằm trong vùng cho phép đi qua
    private readonly HashSet<Collider> passingColliders = new HashSet<Collider>();

    /// <summary>
    /// Hướng cho phép đi qua được tính theo không gian Local hoặc World.
    /// </summary>
    public Vector3 PassthroughDirection => localDirection
        ? transform.TransformDirection(entryDirection.normalized)
        : entryDirection.normalized;

    private void Awake()
    {
        mainCollider = GetComponent<BoxCollider>();
        mainCollider.isTrigger = false;

        // Tạo Trigger kiểm tra khoảng cách và hướng di chuyển từ xa
        triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.center = mainCollider.center;
        triggerCollider.size = new Vector3(
            mainCollider.size.x * triggerScale.x,
            mainCollider.size.y * triggerScale.y,
            mainCollider.size.z * triggerScale.z
        );
        triggerCollider.isTrigger = true;

        if (showDebugLogs)
        {
            Debug.Log($"[OneWayBoxCollider] Init thành công trên '{gameObject.name}'. " +
                      $"MainSize={mainCollider.size}, TriggerSize={triggerCollider.size}, PassthroughDir={PassthroughDirection}");
        }
    }

    private void OnValidate()
    {
        mainCollider = GetComponent<BoxCollider>();
        if (mainCollider != null)
        {
            mainCollider.isTrigger = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == mainCollider || other == triggerCollider || other.transform.IsChildOf(transform))
            return;

        if (showDebugLogs)
        {
            Debug.Log($"[OneWayBoxCollider] 🚘 OnTriggerEnter với: '{other.name}' (Root: '{other.transform.root.name}')");
        }

        TryIgnoreCollision(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == mainCollider || other == triggerCollider || other.transform.IsChildOf(transform))
            return;

        TryIgnoreCollision(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == mainCollider || other == triggerCollider || other.transform.IsChildOf(transform))
            return;

        if (passingColliders.Contains(other))
        {
            passingColliders.Remove(other);
        }

        // Tự động khôi phục va chạm cho các Collider phụ
        SetCollisionIgnored(other, false);

        // Nếu không còn xe nào nằm trong vùng cho phép, trả lại tường cứng
        if (passingColliders.Count == 0 && mainCollider != null)
        {
            mainCollider.isTrigger = false;

            if (showDebugLogs)
            {
                Debug.Log($"[OneWayBoxCollider] 🚪 OnTriggerExit với: '{other.name}'. Đã đóng tường lại (mainCollider.isTrigger = false).");
            }
        }
    }

    public void TryIgnoreCollision(Collider other)
    {
        if (mainCollider == null || other == null)
            return;

        if (other == mainCollider || other == triggerCollider || other.transform.IsChildOf(transform))
            return;

        Vector3 passDir = PassthroughDirection;
        bool shouldAllowPass = false;
        string debugReason = "";

        // 1. Kiểm tra nếu có Rigidbody (Xe đua RCCP)
        if (other.attachedRigidbody != null)
        {
#if UNITY_2023_1_OR_NEWER
            Vector3 carVelocity = other.attachedRigidbody.linearVelocity;
#else
            Vector3 carVelocity = other.attachedRigidbody.velocity;
#endif
            Vector3 carPos = other.attachedRigidbody.worldCenterOfMass;

            // Kiểm tra theo vận tốc di chuyển của xe
            if (carVelocity.sqrMagnitude > 0.05f)
            {
                float dotVel = Vector3.Dot(passDir, carVelocity.normalized);
                shouldAllowPass = dotVel > -0.2f; // Trùng chiều hoặc vuông góc => Cho qua
                debugReason = $"Speed={carVelocity.magnitude:F1}m/s, DotVel={dotVel:F2} => AllowPass={shouldAllowPass}";
            }
            else
            {
                // Xe đứng yên: kiểm tra vị trí tương quan so với tường
                Vector3 localCarPos = transform.InverseTransformPoint(carPos);
                float dotPos = Vector3.Dot(entryDirection.normalized, localCarPos);
                shouldAllowPass = dotPos <= 0.2f;
                debugReason = $"DotPos={dotPos:F2} => AllowPass={shouldAllowPass}";
            }
        }
        else
        {
            Vector3 relativePos = (other.bounds.center - transform.TransformPoint(mainCollider.center)).normalized;
            shouldAllowPass = Vector3.Dot(passDir, relativePos) < 0f;
            debugReason = $"No Rigidbody Dot={Vector3.Dot(passDir, relativePos):F2}";
        }

        if (shouldAllowPass)
        {
            passingColliders.Add(other);
            SetCollisionIgnored(other, true);

            // MỞ TƯỜNG (isTrigger = true) để cả Thân xe và Bánh xe (WheelCollider) chạy qua mượt mà!
            if (!mainCollider.isTrigger)
            {
                mainCollider.isTrigger = true;

                if (showDebugLogs)
                {
                    Debug.Log($"[OneWayBoxCollider] 🟢 MỞ TƯỜNG cho '{other.name}'! Set mainCollider.isTrigger = TRUE. ({debugReason})");
                }
            }
        }
        else
        {
            if (passingColliders.Contains(other))
            {
                passingColliders.Remove(other);
            }

            SetCollisionIgnored(other, false);

            if (passingColliders.Count == 0 && mainCollider.isTrigger)
            {
                mainCollider.isTrigger = false;

                if (showDebugLogs)
                {
                    Debug.Log($"[OneWayBoxCollider] 🔴 CHẶN TƯỜNG cho '{other.name}'! Set mainCollider.isTrigger = FALSE. ({debugReason})");
                }
            }
        }
    }

    private void SetCollisionIgnored(Collider other, bool ignore)
    {
        if (ignoreAllCarColliders && other.attachedRigidbody != null)
        {
            Collider[] allColliders = other.attachedRigidbody.GetComponentsInChildren<Collider>();
            foreach (Collider col in allColliders)
            {
                if (col != null && col != triggerCollider && col != mainCollider)
                {
                    Physics.IgnoreCollision(mainCollider, col, ignore);
                }
            }
        }
        else
        {
            Physics.IgnoreCollision(mainCollider, other, ignore);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (mainCollider == null)
            mainCollider = GetComponent<BoxCollider>();
        if (mainCollider == null)
            return;

        // 1. Vẽ khung Trigger Scale (Màu VÀNG)
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 triggerSize = new Vector3(
            mainCollider.size.x * triggerScale.x,
            mainCollider.size.y * triggerScale.y,
            mainCollider.size.z * triggerScale.z
        );

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(mainCollider.center, triggerSize);

        Gizmos.matrix = oldMatrix;

        // 2. Vẽ mũi tên hướng đi
        Vector3 centerWorld = transform.TransformPoint(mainCollider.center);
        Vector3 dir = PassthroughDirection;

        // Mũi tên XANH LÁ (Cho qua)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(centerWorld, dir * 3f);
        DrawGizmoArrowHead(centerWorld + dir * 3f, dir, Color.green);

        // Mũi tên ĐỎ (Bị chặn)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(centerWorld, -dir * 3f);
        DrawGizmoArrowHead(centerWorld - dir * 3f, -dir, Color.red);
    }

    private void DrawGizmoArrowHead(Vector3 pos, Vector3 dir, Color color)
    {
        Gizmos.color = color;
        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 200, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 160, 0) * Vector3.forward;
        Gizmos.DrawRay(pos, right * 0.5f);
        Gizmos.DrawRay(pos, left * 0.5f);
    }
}
