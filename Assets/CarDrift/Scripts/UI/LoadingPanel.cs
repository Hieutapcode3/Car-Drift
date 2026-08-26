using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : Panel<LoadingPanel>
{
    [SerializeField] private TextMeshProUGUI loadingTxt;
    [SerializeField] private Image loadingFill;
    [SerializeField] private float fillSpeed = 0.85f; // Tốc độ tăng thanh loading mỗi giây

    private float targetProgress = 0f;
    private float currentDisplayedProgress = 0f;
    private string currentMessage = "Loading...";

    public override void Show(object data = null, bool duplicated = false)
    {
        base.Show(data, duplicated);
        targetProgress = 0f;
        currentDisplayedProgress = 0f;
        currentMessage = "Loading...";
        UpdateDisplay(0f);
    }

    private void Update()
    {
        if (currentDisplayedProgress < targetProgress)
        {
            currentDisplayedProgress = Mathf.MoveTowards(currentDisplayedProgress, targetProgress, fillSpeed * Time.unscaledDeltaTime);
            UpdateDisplay(currentDisplayedProgress);
        }
    }

    public void SetProgress(float progress, string message = "")
    {
        targetProgress = Mathf.Clamp01(progress);
        if (!string.IsNullOrEmpty(message))
        {
            currentMessage = message;
        }
        UpdateDisplay(currentDisplayedProgress);
    }

    private void UpdateDisplay(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        if (loadingFill != null)
        {
            loadingFill.fillAmount = clampedProgress;
        }

        if (loadingTxt != null)
        {
            int percent = Mathf.RoundToInt(clampedProgress * 100f);
            loadingTxt.text = string.IsNullOrEmpty(currentMessage) ? $"{percent}%" : $"{currentMessage} ({percent}%)";
        }
    }

    public IEnumerator WaitForVisualProgress()
    {
        while (currentDisplayedProgress < targetProgress - 0.005f)
        {
            yield return null;
        }
        currentDisplayedProgress = targetProgress;
        UpdateDisplay(currentDisplayedProgress);
    }

    public async System.Threading.Tasks.Task WaitForVisualProgressAsync()
    {
        while (currentDisplayedProgress < targetProgress - 0.005f)
        {
            await System.Threading.Tasks.Task.Yield();
        }
        currentDisplayedProgress = targetProgress;
        UpdateDisplay(currentDisplayedProgress);
    }
}
