using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public static class AnimatorCustom
{
    public static float PlayAnim(GameObject target, string animKey, float crossFadeTime = 0.15f, Action callback = null, float delay = 0f)
    {
        if (target == null)
            return 0f;

        if (delay > 0f)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                PlayAnim(target, animKey, crossFadeTime, callback, 0f);
            }).SetUpdate(UpdateType.Fixed);
            return 0f;
        }

        if (target.TryGetComponent(out ModelAnimator modelAnimator))
        {
            return modelAnimator.PlayAnimStart(animKey, crossFadeTime, callback);
        }
        modelAnimator = target.GetComponentInChildren<ModelAnimator>();
        if (modelAnimator != null)
        {
            return modelAnimator.PlayAnimStart(animKey, crossFadeTime, callback);
        }
        if (target.TryGetComponent(out UnityEngine.Animator animator))
        {
            if (crossFadeTime > 0f)
                animator.CrossFadeInFixedTime(animKey, crossFadeTime);
            else
                animator.Play(animKey);

            float length = GetCurrentClipLength(animator);
            if (callback != null)
            {
                var helper = target.GetComponent<AnimatorCallbackHelper>();
                if (helper == null) helper = target.AddComponent<AnimatorCallbackHelper>();
                helper.StartCallback(length, callback);
            }
            return length;
        }
        animator = target.GetComponentInChildren<UnityEngine.Animator>();
        if (animator != null)
        {
            if (crossFadeTime > 0f)
                animator.CrossFadeInFixedTime(animKey, crossFadeTime);
            else
                animator.Play(animKey);

            float length = GetCurrentClipLength(animator);
            if (callback != null)
            {
                var helper = animator.gameObject.GetComponent<AnimatorCallbackHelper>();
                if (helper == null) helper = animator.gameObject.AddComponent<AnimatorCallbackHelper>();
                helper.StartCallback(length, callback);
            }
            return length;
        }

        Debug.LogWarning($"[Animator] No ModelAnimator or Animator component found on {target.name}");
        return 0f;
    }

    public static bool IsPlaying(GameObject target, string animKey)
    {
        if (target == null)
            return false;

        if (target.TryGetComponent(out ModelAnimator modelAnimator))
        {
            return modelAnimator.IsPlaying(animKey);
        }
        modelAnimator = target.GetComponentInChildren<ModelAnimator>();
        if (modelAnimator != null)
        {
            return modelAnimator.IsPlaying(animKey);
        }

        if (target.TryGetComponent(out UnityEngine.Animator animator))
        {
            if (animator.runtimeAnimatorController == null) return false;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(animKey) && (stateInfo.loop || stateInfo.normalizedTime < 1.0f))
            {
                return true;
            }
            return false;
        }
        animator = target.GetComponentInChildren<UnityEngine.Animator>();
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null) return false;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(animKey) && (stateInfo.loop || stateInfo.normalizedTime < 1.0f))
            {
                return true;
            }
            return false;
        }

        return false;
    }

    private static float GetCurrentClipLength(UnityEngine.Animator animator)
    {
        if (animator == null) return 0f;
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.length;
    }

    public static void PauseAnim(GameObject target)
    {
        if (target == null) return;

        if (target.TryGetComponent(out ModelAnimator modelAnimator))
        {
            modelAnimator.PauseAnim();
            return;
        }

        modelAnimator = target.GetComponentInChildren<ModelAnimator>();
        if (modelAnimator != null)
        {
            modelAnimator.PauseAnim();
            return;
        }

        if (target.TryGetComponent(out UnityEngine.Animator animator))
        {
            animator.speed = 0f;
            return;
        }

        animator = target.GetComponentInChildren<UnityEngine.Animator>();
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    public static void ResumeAnim(GameObject target, Action callback = null)
    {
        if (target == null) return;

        if (target.TryGetComponent(out ModelAnimator modelAnimator))
        {
            modelAnimator.ResumeAnim(callback);
            return;
        }

        modelAnimator = target.GetComponentInChildren<ModelAnimator>();
        if (modelAnimator != null)
        {
            modelAnimator.ResumeAnim(callback);
            return;
        }

        if (target.TryGetComponent(out UnityEngine.Animator animator))
        {
            animator.speed = 1f;
            if (callback != null)
            {
                var helper = target.GetComponent<AnimatorCallbackHelper>();
                if (helper == null) helper = target.AddComponent<AnimatorCallbackHelper>();
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                float remainingTime = stateInfo.length * (1f - (stateInfo.normalizedTime % 1f));
                helper.StartCallback(remainingTime, callback);
            }
            return;
        }

        animator = target.GetComponentInChildren<UnityEngine.Animator>();
        if (animator != null)
        {
            animator.speed = 1f;
            if (callback != null)
            {
                var helper = animator.gameObject.GetComponent<AnimatorCallbackHelper>();
                if (helper == null) helper = animator.gameObject.AddComponent<AnimatorCallbackHelper>();
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                float remainingTime = stateInfo.length * (1f - (stateInfo.normalizedTime % 1f));
                helper.StartCallback(remainingTime, callback);
            }
        }
    }
}

public class AnimatorCallbackHelper : MonoBehaviour
{
    private Coroutine activeCoroutine;

    public void StartCallback(float duration, Action callback)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(CoWait(duration, callback));
    }

    public void StopCallback()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    private IEnumerator CoWait(float duration, Action callback)
    {
        float timer = 0f;
        var animator = GetComponent<UnityEngine.Animator>();
        while (timer < duration)
        {
            yield return null;
            if (animator == null) yield break;
            if (animator.speed > 0f)
            {
                timer += Time.deltaTime * animator.speed;
            }
        }
        activeCoroutine = null;
        callback?.Invoke();
    }
}
