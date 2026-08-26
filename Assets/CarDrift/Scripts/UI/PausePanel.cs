using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanel : Panel<PausePanel>
{
    [Header("Button References (Tự động gán nếu để trống)")]
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button homeBtn;
    [SerializeField] private Button quitBtn;
    protected override void Awake()
    {
        base.Awake();
    }
    private void OnEnable()
    {
        Time.timeScale = 0f;
        RCCP_UIManager.Instance.dashboard.SetActive(false);
        AutoFindAndBindButtons();
        if (popup != null && popup.localScale == Vector3.zero)
        {
            popup.localScale = Vector3.one;
        }
    }
    private void AutoFindAndBindButtons()
    {
        if (resumeBtn == null || homeBtn == null || quitBtn == null)
        {
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string btnName = btn.gameObject.name.ToLower();
                if (resumeBtn == null && btnName.Contains("resume"))
                {
                    resumeBtn = btn;
                }
                else if (homeBtn == null && btnName.Contains("home"))
                {
                    homeBtn = btn;
                }
                else if (quitBtn == null && btnName.Contains("quit"))
                {
                    quitBtn = btn;
                }
            }
        }
        if (resumeBtn != null)
        {
            resumeBtn.onClick.RemoveListener(OnClickResume);
            resumeBtn.onClick.AddListener(OnClickResume);
        }
        if (homeBtn != null)
        {
            homeBtn.onClick.RemoveListener(OnClickHome);
            homeBtn.onClick.AddListener(OnClickHome);
        }
        if (quitBtn != null)
        {
            quitBtn.onClick.RemoveListener(OnClickQuit);
            quitBtn.onClick.AddListener(OnClickQuit);
        }
    }
    public void OnClickResume()
    {
        Time.timeScale = 1f;
        if (HUDSystem.Instance != null)
            HUDSystem.Instance.Hide<PausePanel>();
        else
            Hide();
    }
    public void OnClickHome()
    {
        Time.timeScale = 1f;
        if (HUDSystem.Instance != null)
            HUDSystem.Instance.StartCoroutine(ReturnHomeRoutine());
        else
            StartCoroutine(ReturnHomeRoutine());
    }

    public void OnCllickHome()
    {
        OnClickHome();
    }

    public void OnClickQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator ReturnHomeRoutine()
    {
        Time.timeScale = 1f;
        GarageManager.hasCompletedInitialMenuLoading = false;

        LoadingPanel loadingPanel = null;
        if (HUDSystem.Instance != null)
        {
            loadingPanel = HUDSystem.Instance.Show<LoadingPanel>();
            if (loadingPanel != null)
                loadingPanel.SetProgress(0f, "Loading...");
            HUDSystem.Instance.Hide<PausePanel>();
        }
        else
            Hide();
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MenuScene");
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f)
        {
            float sceneProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f) * 0.35f;
            if (loadingPanel != null)
            {
                loadingPanel.SetProgress(sceneProgress, "Loading ...");
            }
            yield return null;
        }
        if (loadingPanel != null)
        {
            loadingPanel.SetProgress(0.35f, "Loading...");
            yield return loadingPanel.WaitForVisualProgress();
        }
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
