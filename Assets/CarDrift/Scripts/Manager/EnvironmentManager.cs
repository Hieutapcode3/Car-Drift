using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;

public class EnvironmentManager : MonoSingleton<EnvironmentManager>
{
    public const string KEY_LAST_ENVIRONMENT = "KEY_LAST_PLAYED_ENVIRONMENT";

    [Header("Data Reference")]
    [SerializeField] private EnvironmentSO environmentSO;

    [Header("Scene References")]
    [SerializeField] private Light directionalLight;

    [Header("Current Environment Settings")]
    [SerializeField] private EnvironmentType currentEnvironment = EnvironmentType.Sun;
    [Tooltip("Tự động chọn ngẫu nhiên môi trường khác với lần chơi trước đó")]
    [SerializeField] private bool randomizeOnStart = true;
    [Tooltip("Áp dụng môi trường currentEnvironment khi không bật randomizeOnStart")]
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
        if (randomizeOnStart)
        {
            SetRandomEnvironmentDifferentFromPrevious();
        }
        else if (applyOnStart)
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

    [Button("Randomize (Different From Previous)")]
    public void SetRandomEnvironmentDifferentFromPrevious()
    {
        Array allValues = Enum.GetValues(typeof(EnvironmentType));
        if (allValues.Length == 0) return;

        EnvironmentType selectedEnv;

        if (PlayerPrefs.HasKey(KEY_LAST_ENVIRONMENT))
        {
            EnvironmentType lastEnv = (EnvironmentType)PlayerPrefs.GetInt(KEY_LAST_ENVIRONMENT);
            List<EnvironmentType> candidates = new List<EnvironmentType>();

            foreach (EnvironmentType env in allValues)
            {
                if (env != lastEnv)
                {
                    candidates.Add(env);
                }
            }

            if (candidates.Count > 0)
            {
                selectedEnv = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }
            else
            {
                selectedEnv = (EnvironmentType)allValues.GetValue(UnityEngine.Random.Range(0, allValues.Length));
            }
        }
        else
        {
            // Lần đầu chơi chưa có lịch sử, chọn ngẫu nhiên 1 môi trường
            selectedEnv = (EnvironmentType)allValues.GetValue(UnityEngine.Random.Range(0, allValues.Length));
        }

        SetEnvironment(selectedEnv);

        // Lưu lại môi trường vừa chọn để các lần chơi tiếp theo không bị trùng
        SaveLastEnvironment(selectedEnv);
    }

    public static void SaveLastEnvironment(EnvironmentType type)
    {
        PlayerPrefs.SetInt(KEY_LAST_ENVIRONMENT, (int)type);
        PlayerPrefs.Save();
    }

    public static EnvironmentType GetSavedLastEnvironment()
    {
        return (EnvironmentType)PlayerPrefs.GetInt(KEY_LAST_ENVIRONMENT, (int)EnvironmentType.Sun);
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
