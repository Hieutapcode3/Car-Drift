using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Controller Settings")]
    public ControllerType controllerType = ControllerType.Player;
    public bool isMenuModel = false;

    [Header("AI Settings")]
    public AIDifficulty aiDifficulty = AIDifficulty.Medium;

    [Header("Damage Settings")]
    public bool isDamageable = true;

    [Header("Menu Rotation Settings")]
    [Tooltip("Tốc độ xoay tự động trong Menu (độ/giây)")]
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;
    public bool freezePhysicsInMenu = true;

    [Header("References")]
    public RCCP_CarController carController;
    public RCCP_AI aiController;
    public RCCP_Damage damageController;
    public bool IsMenuModel => isMenuModel || controllerType == ControllerType.Menu;

    private void Awake()
    {
        FetchReferences();
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
        FetchReferences();
    }
    public void FetchReferences()
    {
        if (carController == null)
        {
            foreach (Transform child in transform)
            {
                carController = child.GetComponent<RCCP_CarController>();
                if (carController != null)
                    break;
            }

            if (carController == null)
            {
                carController = GetComponent<RCCP_CarController>();
            }
        }

        if (aiController == null)
        {
            aiController = GetComponentInChildren<RCCP_AI>(true);
        }

        if (aiController == null && controllerType == ControllerType.AI)
        {
            GetOrCreateAIController();
        }

        if (damageController == null)
        {
            damageController = GetComponentInChildren<RCCP_Damage>(true);
        }
    }
    public RCCP_AI GetOrCreateAIController()
    {
        if (aiController != null)
            return aiController;

        aiController = GetComponentInChildren<RCCP_AI>(true);
        if (aiController != null)
            return aiController;

        if (carController == null)
            FetchReferences();

        Transform parentTarget = transform;
        if (carController != null)
            parentTarget = carController.transform;
        RCCP_OtherAddons otherAddons = parentTarget.GetComponentInChildren<RCCP_OtherAddons>(true);
        Transform otherAddonsTransform = null;

        if (otherAddons != null)
        {
            otherAddonsTransform = otherAddons.transform;
        }
        else
        {
            Transform existingOtherAddonsObj = parentTarget.Find("OtherAddons");
            if (existingOtherAddonsObj != null)
            {
                otherAddons = existingOtherAddonsObj.GetComponent<RCCP_OtherAddons>();
                if (otherAddons == null)
                    otherAddons = existingOtherAddonsObj.gameObject.AddComponent<RCCP_OtherAddons>();
                otherAddonsTransform = existingOtherAddonsObj;
            }
            else
            {
                GameObject newOtherAddonsObj = new GameObject("OtherAddons");
                newOtherAddonsObj.transform.SetParent(parentTarget, false);
                otherAddons = newOtherAddonsObj.AddComponent<RCCP_OtherAddons>();
                otherAddonsTransform = newOtherAddonsObj.transform;
            }
        }
        Transform aiChildTransform = otherAddonsTransform.Find("AI");
        GameObject aiGo;

        if (aiChildTransform != null)
        {
            aiGo = aiChildTransform.gameObject;
        }
        else
        {
            aiGo = new GameObject("AI");
            aiGo.transform.SetParent(otherAddonsTransform, false);
        }

        aiController = aiGo.GetComponent<RCCP_AI>();
        if (aiController == null)
        {
            aiController = aiGo.AddComponent<RCCP_AI>();
        }

        return aiController;
    }
    public void ApplyControlState()
    {
        if (damageController != null)
        {
            damageController.enabled = isDamageable;
        }
        RCCP_DetachablePart[] detachableParts = GetComponentsInChildren<RCCP_DetachablePart>(true);
        foreach (RCCP_DetachablePart part in detachableParts)
        {
            part.enabled = isDamageable;
        }

        if (IsMenuModel)
        {
            if (carController != null)
            {
                carController.SetCanControl(false);
            }

            if (freezePhysicsInMenu)
            {
                SetKinematicAllParts(true);
            }

            if (aiController != null)
            {
                aiController.enabled = false;
            }
        }
        else if (controllerType == ControllerType.AI)
        {
            GetOrCreateAIController();
            if (aiController != null)
            {
                aiController.enabled = true;
                ApplyAIDifficulty(aiDifficulty);
            }
        }
        else // Player controller
        {
            if (aiController != null)
            {
                aiController.enabled = false;
            }
        }
    }
    public void ApplyAIDifficulty(AIDifficulty difficulty)
    {
        if (aiController == null)
            return;

        aiDifficulty = difficulty;

        switch (difficulty)
        {
            case AIDifficulty.Easy:
                aiController.limitSpeed = true;
                aiController.maximumSpeed = 70f;
                aiController.smoothedSteer = true;
                aiController.raycastLength = 10f;
                aiController.nextWaypointPassDistance = 20;
                break;

            case AIDifficulty.Medium:
                aiController.limitSpeed = true;
                aiController.maximumSpeed = 100f;
                aiController.smoothedSteer = true;
                aiController.raycastLength = 15f;
                aiController.nextWaypointPassDistance = 15;
                break;

            case AIDifficulty.Hard:
                aiController.limitSpeed = false;
                aiController.smoothedSteer = false;
                aiController.raycastLength = 25f;
                aiController.nextWaypointPassDistance = 10;
                break;
        }
    }
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
            transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime), Space.Self);
        }
    }
}

