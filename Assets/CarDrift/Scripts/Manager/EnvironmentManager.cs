using System;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;

public class EnvironmentManager : MonoSingleton<EnvironmentManager>
{
    [Header("Data Reference")]
    [SerializeField] private EnvironmentSO environmentSO;

    [Header("Scene References")]
    [SerializeField] private Light directionalLight;

    [Header("Current Environment Settings")]
    [SerializeField] private EnvironmentType currentEnvironment = EnvironmentType.Sun;
    [SerializeField] private bool applyOnStart = true;
    [Title("Fx")]
    [SerializeField] private GameObject sunFx;
    [SerializeField] private GameObject nightFx;
    [SerializeField] private GameObject rainFx;
    public EnvironmentType CurrentEnvironment => currentEnvironment;
    public static event Action<EnvironmentType> OnEnvironmentChanged;

    protected override void Awake()
    {
        base.Awake();
        FindReferences();
    }

    private void Start()
    {
        if (applyOnStart)
        {
            SetEnvironment(currentEnvironment);
        }
    }

    public void FindReferences()
    {
        if (environmentSO == null)
        {
            environmentSO = EnvironmentSO.Instance;
        }

        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
            if (directionalLight == null)
            {
#if UNITY_2023_1_OR_NEWER
                directionalLight = FindFirstObjectByType<Light>();
#else
                directionalLight = FindObjectOfType<Light>();
#endif
            }
        }
    }

    [Button("Set Environment")]
    public void SetEnvironment(EnvironmentType type)
    {
        currentEnvironment = type;
        FindReferences();

        if (environmentSO != null)
        {
            environmentSO.ApplyEnvironment(currentEnvironment, directionalLight);
            ApplyEnvironmentFx(currentEnvironment);
            OnEnvironmentChanged?.Invoke(currentEnvironment);
            // Debug.Log($"[EnvironmentManager] ✅ Đã chuyển sang môi trường: {currentEnvironment}");
        }
    }

    private void ApplyEnvironmentFx(EnvironmentType type)
    {
        if (sunFx != null)
            sunFx.SetActive(type == EnvironmentType.Sun);
        if (nightFx != null)
            nightFx.SetActive(type == EnvironmentType.Night);
        if (rainFx != null)
            rainFx.SetActive(type == EnvironmentType.Rain);
    }

    [Button("Next Environment")]
    public void NextEnvironment()
    {
        Array values = Enum.GetValues(typeof(EnvironmentType));
        int nextIndex = ((int)currentEnvironment + 1) % values.Length;
        SetEnvironment((EnvironmentType)values.GetValue(nextIndex));
    }

    [Button("Previous Environment")]
    public void PreviousEnvironment()
    {
        Array values = Enum.GetValues(typeof(EnvironmentType));
        int prevIndex = ((int)currentEnvironment - 1 + values.Length) % values.Length;
        SetEnvironment((EnvironmentType)values.GetValue(prevIndex));
    }
}
