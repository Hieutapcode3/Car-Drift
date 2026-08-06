using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CarColorData
{
    public CarColorType colorType;
    public Color color;
}

[CreateAssetMenu(fileName = "ColorParamSO", menuName = "CarDrift/ColorParamSO")]
public class ColorParamSO : ScriptableObject
{
    private static ColorParamSO instance;
    public static ColorParamSO Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ColorParamSO>("ColorParamSO");
            }
            return instance;
        }
    }

    public List<CarColorData> colorList = new List<CarColorData>()
    {
        new CarColorData { colorType = CarColorType.Red, color = Color.red },
        new CarColorData { colorType = CarColorType.Orange, color = new Color(1f, 0.5f, 0f) },
        new CarColorData { colorType = CarColorType.Yellow, color = Color.yellow },
        new CarColorData { colorType = CarColorType.Green, color = Color.green },
        new CarColorData { colorType = CarColorType.Cyan, color = Color.cyan },
        new CarColorData { colorType = CarColorType.Blue, color = Color.blue },
        new CarColorData { colorType = CarColorType.Purple, color = new Color(0.5f, 0f, 0.5f) },
        new CarColorData { colorType = CarColorType.Magenta, color = Color.magenta },
        new CarColorData { colorType = CarColorType.White, color = Color.white },
        new CarColorData { colorType = CarColorType.Black, color = Color.black },
        new CarColorData { colorType = CarColorType.Gray, color = Color.gray }
    };

    public Color GetColor(CarColorType colorType)
    {
        for (int i = 0; i < colorList.Count; i++)
        {
            if (colorList[i].colorType == colorType)
            {
                return colorList[i].color;
            }
        }
        return Color.white;
    }
}
