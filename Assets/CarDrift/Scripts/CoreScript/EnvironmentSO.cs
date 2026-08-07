using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnvironmentData
{
    public EnvironmentType envType;

    [Header("Skybox Settings")]
    public Material skyboxMaterial;

    [Header("Directional Light Settings")]
    public Color lightColor = Color.white;
    public Flare lightFlare;
}

[CreateAssetMenu(fileName = "EnvironmentSO", menuName = "CarDrift/EnvironmentSO")]
public class EnvironmentSO : ScriptableObject
{
    private static EnvironmentSO instance;
    public static EnvironmentSO Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<EnvironmentSO>("EnvironmentSO");
            }
            return instance;
        }
    }

    [Header("Environment Configurations")]
    public List<EnvironmentData> envList = new List<EnvironmentData>()
    {
        new EnvironmentData { envType = EnvironmentType.Sun, lightColor = Color.white },
        new EnvironmentData { envType = EnvironmentType.Night, lightColor = new Color(0.15f, 0.2f, 0.4f) },
        new EnvironmentData { envType = EnvironmentType.Rain, lightColor = new Color(0.6f, 0.65f, 0.7f) }
    };
    public EnvironmentData GetEnvironmentData(EnvironmentType type)
    {
        for (int i = 0; i < envList.Count; i++)
        {
            if (envList[i].envType == type)
            {
                return envList[i];
            }
        }
        return null;
    }
    public void ApplyEnvironment(EnvironmentType type, Light directionalLight = null)
    {
        EnvironmentData data = GetEnvironmentData(type);
        if (data == null)
        {
            Debug.LogWarning($"[EnvironmentSO] Không tìm thấy dữ liệu cho {type}");
            return;
        }
        if (data.skyboxMaterial != null)
        {
            RenderSettings.skybox = data.skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
        if (directionalLight != null)
        {
            directionalLight.color = data.lightColor;
            directionalLight.flare = data.lightFlare;
        }
    }
}
