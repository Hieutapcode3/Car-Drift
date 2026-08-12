using System;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace VTLTools
{
    public enum LevelMode
    {
        Default,
        Creative
    }

    public class StaticVariables
    {
        public static string PREF_USER_DATA = "PREF_USER_DATA";
        #region Public Variables
        [ShowInInspector, BoxGroup("Setting")]
        public static bool IsSoundOn
        {
            get => UserData.isSoundOn;
            set
            {
                UserData.isSoundOn = value;
                SaveData();
            }
        }

        [ShowInInspector, BoxGroup("Setting")]
        public static bool IsMusicOn
        {
            get => UserData.isMusicOn;
            set
            {
                UserData.isMusicOn = value;
                SaveData();
            }
        }

        [ShowInInspector, BoxGroup("Setting")]
        public static bool IsVibrationOn
        {
            get => UserData.isVibrationOn;
            set
            {
                UserData.isVibrationOn = value;
                SaveData();
            }
        }
        #endregion

        #region User Data
        public static UserData UserData { get; private set; }

        public static void SetUserData(UserData _data)
        {
            UserData = _data;
            SaveData();
        }

        static StaticVariables()
        {
            UserData = GetData();
            if (UserData == null)
            {
                UserData = new UserData();
                SaveData();
            }
        }

        #region Coins

        public static void BindEventCoinReduce(Action<int, int> onCoinReduce)
        {
            UserData.OnCoinReduce += onCoinReduce;
        }

        public static void UnbindEventCoinReduce(Action<int, int> onCoinReduce)
        {
            UserData.OnCoinReduce -= onCoinReduce;
        }

        public static void BindEventCoinIncrease(Action<int, int> onCoinIncrease)
        {
            UserData.OnCoinIncrease += onCoinIncrease;
        }

        public static void UnbindEventCoinIncrease(Action<int, int> onCoinIncrease)
        {
            UserData.OnCoinIncrease -= onCoinIncrease;
        }

        #endregion

        #endregion

        static void SaveData()
        {
            VTLPlayerPrefs.SetObjectValue(PREF_USER_DATA, UserData);
        }
        static UserData GetData()
        {
            return VTLPlayerPrefs.GetObjectValue<UserData>(PREF_USER_DATA);
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Clear Data")]
        public static void ClearData()
        {
            VTLPlayerPrefs.DeleteKey(PREF_USER_DATA);
        }
#endif
    }

    [Serializable]
    public class UserData
    {
        public bool isSoundOn;
        public bool isMusicOn;
        public bool isVibrationOn;
        public UserData()
        {
            isSoundOn = true;
            isMusicOn = true;
            isVibrationOn = true;
        }

        public override string ToString()
        {
            return $"userData: \n" +
                   $"isSoundOn: {isSoundOn}\n" +
                   $"isMusicOn: {isMusicOn}\n" +
                   $"isVibrationOn: {isVibrationOn}\n";
        }

        #region Coins

        [NonSerialized] public Action<int, int> OnCoinReduce;
        [NonSerialized] public Action<int, int> OnCoinIncrease;


        #endregion

    }


}
