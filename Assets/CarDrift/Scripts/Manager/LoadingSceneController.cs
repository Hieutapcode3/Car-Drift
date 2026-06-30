using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private float minimumLoadingTime = 0.5f;
    [SerializeField] private string nextSceneName = GameManager.MenuSceneName;

    private IEnumerator Start()
    {
        DataManager.Instance.Load();

        float timer = 0f;
        while (timer < minimumLoadingTime)
        {
            timer += Time.deltaTime;
            if (progressBar)
                progressBar.value = Mathf.Clamp01(timer / minimumLoadingTime);

            yield return null;
        }

        GameManager.Instance.LoadScene(nextSceneName);
    }
}
