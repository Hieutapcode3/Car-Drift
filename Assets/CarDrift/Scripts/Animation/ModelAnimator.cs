using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Sirenix.OdinInspector;

[RequireComponent(typeof(UnityEngine.Animator))]
public class ModelAnimator : MonoBehaviour
{
    [System.Serializable]
    public struct AnimationData
    {
        [Required]
        public string key;
        [Required]
        public AnimationClip clip;

#if UNITY_EDITOR
        [Button("Play")]
        public void Play()
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[ModelAnimator] Can only play animations in Play Mode.");
                return;
            }

            foreach (var go in UnityEditor.Selection.gameObjects)
            {
                var animator = go.GetComponent<ModelAnimator>();
                if (animator == null)
                    animator = go.GetComponentInParent<ModelAnimator>();

                if (animator != null)
                {
                    animator.PlayAnimStart(key);
                    break;
                }
            }
        }
#endif
    }

    [TitleGroup("References")]
    [SerializeField] private UnityEngine.Animator animator;

    [TitleGroup("Settings")]
    [Tooltip("If true, plays clips directly using the Unity Playables API (no Animator Controller setup needed in Editor). If false, plays states in Animator Controller.")]
    [SerializeField] private bool usePlayables = true;
    [TitleGroup("Animations")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<AnimationData> animations = new();
    private readonly Dictionary<string, AnimationClip> animationMap = new();
    private PlayableGraph playableGraph;
    private AnimationMixerPlayable mixer;
    private Playable currentPlayable;
    private Playable previousPlayable;
    private float fadeDuration;
    private float fadeTimer;
    private bool isTransitioning;
    private bool isPaused;
    private float originalAnimatorSpeed = 1f;
    private AnimationClip currentClip;
    private Coroutine activeCallbackCoroutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<UnityEngine.Animator>();

        InitializeMap();

        if (usePlayables)
        {
            InitializePlayables();
        }
    }

    private void InitializeMap()
    {
        animationMap.Clear();
        foreach (var anim in animations)
        {
            if (anim.clip != null && !string.IsNullOrEmpty(anim.key))
            {
                animationMap[anim.key] = anim.clip;
            }
        }
    }

    private void InitializePlayables()
    {
        playableGraph = PlayableGraph.Create($"{gameObject.name}_AnimatorGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(playableGraph, 2);
        var output = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);
        output.SetSourcePlayable(mixer);

        playableGraph.Play();
    }

    private void Update()
    {
        if (isPaused) return;

        if (usePlayables && isTransitioning && playableGraph.IsValid())
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);
            mixer.SetInputWeight(0, 1f - t);
            mixer.SetInputWeight(1, t);
            if (t >= 1f)
            {
                isTransitioning = false;
                if (previousPlayable.IsValid())
                {
                    playableGraph.Disconnect(mixer, 0);
                    previousPlayable.Destroy();
                }
                playableGraph.Disconnect(mixer, 1);
                mixer.ConnectInput(0, currentPlayable, 0);
                mixer.SetInputWeight(0, 1f);
                mixer.SetInputWeight(1, 0f);
            }
        }
    }
    public float PlayAnimStart(string key, float crossFadeTime = 0.15f, Action callback = null)
    {
        if (animationMap.Count == 0 && animations.Count > 0)
            InitializeMap();

        if (!animationMap.TryGetValue(key, out AnimationClip clip))
        {
            Debug.LogWarning($"[ModelAnimator] Animation key '{key}' not found on {gameObject.name}", this);
            return 0f;
        }

        currentClip = clip;

        if (isPaused)
        {
            isPaused = false;
            if (usePlayables)
            {
                if (playableGraph.IsValid())
                    playableGraph.Play();
            }
            else
            {
                if (animator != null)
                    animator.speed = originalAnimatorSpeed;
            }
        }

        if (usePlayables)
        {
            PlayViaPlayables(clip, crossFadeTime);
        }
        else
        {
            PlayViaController(clip.name, crossFadeTime);
        }

        if (activeCallbackCoroutine != null)
        {
            StopCoroutine(activeCallbackCoroutine);
            activeCallbackCoroutine = null;
        }

        if (callback != null)
        {
            activeCallbackCoroutine = StartCoroutine(CoWaitAnimationComplete(clip.length, callback));
        }

        return clip.length;
    }

    public bool IsPlaying(string key)
    {
        if (animationMap.Count == 0 && animations.Count > 0)
            InitializeMap();

        if (!animationMap.TryGetValue(key, out AnimationClip clip))
            return false;

        if (usePlayables)
        {
            if (currentPlayable.IsValid() && currentClip == clip)
            {
                if (isPaused) return false;

                double time = currentPlayable.GetTime();
                if (clip.isLooping || time < clip.length)
                {
                    return true;
                }
            }
        }
        else
        {
            if (animator != null)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(clip.name) && (stateInfo.loop || stateInfo.normalizedTime < 1.0f))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void PlayViaPlayables(AnimationClip clip, float crossFadeTime)
    {
        if (!playableGraph.IsValid())
        {
            InitializePlayables();
        }
        if (isTransitioning)
        {
            mixer.SetInputWeight(0, 0f);
            mixer.SetInputWeight(1, 1f);
            if (previousPlayable.IsValid())
            {
                playableGraph.Disconnect(mixer, 0);
                previousPlayable.Destroy();
            }
            playableGraph.Disconnect(mixer, 1);
            mixer.ConnectInput(0, currentPlayable, 0);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            isTransitioning = false;
        }
        if (currentPlayable.IsValid())
        {
            previousPlayable = currentPlayable;
        }
        currentPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        currentPlayable.SetTime(0f);

        if (crossFadeTime > 0f && previousPlayable.IsValid())
        {
            playableGraph.Disconnect(mixer, 0);
            mixer.ConnectInput(0, previousPlayable, 0);

            playableGraph.Disconnect(mixer, 1);
            mixer.ConnectInput(1, currentPlayable, 0);

            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);

            fadeDuration = crossFadeTime;
            fadeTimer = 0f;
            isTransitioning = true;
        }
        else
        {
            if (previousPlayable.IsValid())
            {
                previousPlayable.Destroy();
            }
            playableGraph.Disconnect(mixer, 0);
            mixer.ConnectInput(0, currentPlayable, 0);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            isTransitioning = false;
        }
    }
    private void PlayViaController(string stateName, float crossFadeTime)
    {
        if (animator == null) return;

        if (crossFadeTime > 0f)
        {
            animator.CrossFadeInFixedTime(stateName, crossFadeTime);
        }
        else
        {
            animator.Play(stateName);
        }
    }

    public void PauseAnim()
    {
        if (isPaused) return;
        isPaused = true;

        if (usePlayables)
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Stop();
            }
        }
        else
        {
            if (animator != null)
            {
                originalAnimatorSpeed = animator.speed;
                animator.speed = 0f;
            }
        }
    }

    public void ResumeAnim(Action callback = null)
    {
        if (isPaused)
        {
            isPaused = false;

            if (usePlayables)
            {
                if (playableGraph.IsValid())
                {
                    playableGraph.Play();
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.speed = originalAnimatorSpeed;
                }
            }
        }

        if (callback != null)
        {
            if (activeCallbackCoroutine != null)
            {
                StopCoroutine(activeCallbackCoroutine);
            }

            float remainingTime = 0f;
            if (usePlayables)
            {
                if (currentPlayable.IsValid() && currentClip != null && currentClip.length > 0f)
                {
                    float elapsed = (float)currentPlayable.GetTime() % currentClip.length;
                    remainingTime = currentClip.length - elapsed;
                }
            }
            else
            {
                if (animator != null)
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    remainingTime = stateInfo.length * (1f - (stateInfo.normalizedTime % 1f));
                }
            }

            if (remainingTime > 0f)
            {
                activeCallbackCoroutine = StartCoroutine(CoWaitAnimationComplete(remainingTime, callback));
            }
            else
            {
                callback?.Invoke();
            }
        }
    }

    private IEnumerator CoWaitAnimationComplete(float duration, Action callback)
    {
        float timer = 0f;
        while (timer < duration)
        {
            yield return null;
            if (!isPaused)
            {
                timer += Time.deltaTime;
            }
        }
        activeCallbackCoroutine = null;
        callback?.Invoke();
    }

#if UNITY_EDITOR
    [HorizontalGroup("Controls")]
    [Button("Pause")]
    public void PauseEditor()
    {
        if (Application.isPlaying)
        {
            PauseAnim();
        }
    }

    [HorizontalGroup("Controls")]
    [Button("Resume")]
    public void ResumeEditor()
    {
        if (Application.isPlaying)
        {
            ResumeAnim();
        }
    }
#endif

    private void OnDisable()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Stop();
        }
    }
    private void OnEnable()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Play();
        }
    }
    private void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
    private void OnValidate()
    {
        if (animator == null)
            animator = GetComponent<UnityEngine.Animator>();
    }
}
