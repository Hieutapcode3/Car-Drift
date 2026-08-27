using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDSystem : UIPanels<HUDSystem>
{
    private static bool isLock = false;
    public static bool TapToHideLocked;
    private readonly List<Type> _excludeIndexes = new List<Type>();

    private static HUDSystem instance;
    public new static HUDSystem Instance
    {
        get
        {
            if (instance == null)
            {
#if UNITY_6000 || UNITY_2023_1_OR_NEWER
                instance = FindFirstObjectByType<HUDSystem>();
#else
                instance = FindObjectOfType<HUDSystem>();
#endif
            }
            return instance;
        }
    }

    public static bool IsLock
    {
        get => isLock;
        set => isLock = value;
    }

    public bool ScrollingLocked { get; set; }
    public bool ZoomingLocked { get; set; }
    public bool SwipeLocked { get; set; }
    public bool TouchLocked { get; set; }

    protected override void Awake()
    {
        // Kiểm tra Singleton: Nếu đã có instance cũ (từ scene trước chuyển sang), HỦY instance mới sinh ra ở Scene này!
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitializeSingleton();
        EnsureCanvasesSettings();
    }

    private void Start()
    {
        ResetAllFlags();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetAllFlags();
        EnsureCanvasesSettings();
        CleanMissingReferences();

        // Tự động điều chỉnh trạng thái UI theo từng scene
        if (scene.name.Contains("Menu") || scene.name.Contains("Garage"))
        {
            // Khi quay lại MenuScene: Ẩn các panel trong game nếu có
            Hide<PausePanel>();
        }
        else if (scene.name.Contains("InGame") || scene.name.Contains("Game"))
        {
            // Khi sang InGameScene: Ẩn các panel của Menu
            Hide<MenuPanel>();
            Hide<CarShopPanel>();
            Hide<CustomPanel>();
            Hide<UpgradePanel>();
        }
    }

    public void ResetAllFlags()
    {
        isLock = false;
        TapToHideLocked = false;
        ScrollingLocked = false;
        SwipeLocked = false;
        ZoomingLocked = false;
        TouchLocked = false;
    }

    public void EnsureCanvasesSettings()
    {
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (c.worldCamera == null)
                {
                    c.worldCamera = Camera.main;
                }
            }
        }
    }

    private void CleanMissingReferences()
    {
        panels.RemoveAll(p => p == null);
        _cachedPanels.RemoveAll(p => p == null);
    }

    public override void UpdateActiveScroll()
    {
        if (isLock)
            return;

        ScrollingLocked = false == CheckScrollCamera();
        SwipeLocked = ScrollingLocked;
    }

    public void GetActivePanel<T>(List<T> lstPanel) where T : Panel
    {
        lstPanel.Clear();
        foreach (var p in panels)
        {
            if (p is T t)
                lstPanel.Add(t);
        }
    }

    public void HideAllPanelExclude<T>() where T : Panel
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] is T)
                continue;

            Hide(panels[i].GetType());
        }
    }

    public void ShowOrHide<T>(bool showed, out T panel) where T : Panel
    {
        if (showed)
        {
            panel = Show<T>();
            return;
        }

        panel = GetActivePanel<T>();
        Hide<T>();
    }

    public IEnumerator<T> FindPanel<T>() where T : Panel
    {
        var panel = GetActivePanel<T>();

        while (!panel)
        {
            yield return null;
            panel = GetActivePanel<T>();
        }

        yield return panel;
    }

    public bool BlockByPanel()
    {
        foreach (var panel in panels)
        {
            if (panel && panel.gameObject.activeSelf && panel.blockTouched)
            {
                return true;
            }
        }

        return false;
    }
}