using System.Collections;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    public const string LoadingSceneName = "LoadingScene";
    public const string MenuSceneName = "MenuScene";
    public const string InGameSceneName = "InGameScene";

    public bool IsLoading { get; private set; }
    public float LoadingProgress { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
            DontDestroyOnLoad(gameObject);
    }

    public void LoadLoadingScene()
    {
        LoadScene(LoadingSceneName);
    }

    public void LoadMenu()
    {
        LoadScene(MenuSceneName);
    }

    public void LoadInGame()
    {
        LoadScene(InGameSceneName);
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadScene(string sceneName)
    {
        if (!IsLoading)
            StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;
        LoadingProgress = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone)
        {
            LoadingProgress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        LoadingProgress = 1f;
        IsLoading = false;
    }
}
