using System;
using System.Collections;
using Cinemachine;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoSingleton<GameManager>
{
    [Title("Intro")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private GameObject introObj;
    [SerializeField] private RCCP_ShowroomCamera showroomCamera;
    [Title("MiniMap Settings")]
    [SerializeField] private CinemachineVirtualCamera miniMapCamera;

    [Title("Countdown Settings")]
    [SerializeField] private int countdownSeconds = 3;

    [SerializeField] private GameState gameState = GameState.Loading;

    public GameState GameState
    {
        get => gameState;
        set => SetGameState(value);
    }
    public static event Action<GameState> OnGameStateChanged;

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
            DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (playIntroOnStart)
        {
            PlayIntro();
        }
    }

    public void SetGameState(GameState newState)
    {
        if (gameState == newState) return;
        gameState = newState;
        Debug.Log($"[GameManager] GameState changed to: {gameState}");
        OnGameStateChanged?.Invoke(gameState);
    }

    public void PlayIntro()
    {
        if (RCCP_UIManager.Instance != null && RCCP_UIManager.Instance.dashboard != null)
        {
            RCCP_UIManager.Instance.dashboard.SetActive(false);
        }

        if (showroomCamera != null)
        {
            showroomCamera.gameObject.SetActive(false);
        }

        SetGameState(GameState.Intro);

        if (introObj != null)
        {
            introObj.SetActive(true);
        }

        RCCP_Camera mCamera = (RCCP_SceneManager.Instance != null) ? RCCP_SceneManager.Instance.activePlayerCamera : null;
        if (mCamera != null)
        {
            mCamera.gameObject.SetActive(false);
        }

        AnimatorCustom.PlayAnim(introObj, "Intro", 0.15f, () =>
        {
            introObj?.SetActive(false);

            if (RCCP_SceneManager.Instance != null && RCCP_SceneManager.Instance.activePlayerVehicle != null)
            {
                if (showroomCamera != null)
                {
                    showroomCamera.target = RCCP_SceneManager.Instance.activePlayerVehicle.transform;
                    showroomCamera.SnapToTarget();
                }
            }
            SetMiniMapFollowPlayer();

            if (showroomCamera != null && showroomCamera.target != null)
            {
                showroomCamera.gameObject.SetActive(true);
                DOVirtual.DelayedCall(7f, () =>
                {
                    showroomCamera.gameObject.SetActive(false);
                    if (mCamera != null)
                    {
                        mCamera.gameObject.SetActive(true);
                    }
                    StartRaceCountdown();
                });
                return;
            }

            if (mCamera != null)
            {
                mCamera.gameObject.SetActive(true);
            }
            StartRaceCountdown();
        });
    }

    public void StartRaceCountdown(int seconds = -1)
    {
        SetMiniMapFollowPlayer();
        if (RCCP_UIManager.Instance != null)
        {
            RCCP_UIManager.Instance.ResetLap();
        }
        int totalSec = seconds > 0 ? seconds : countdownSeconds;

        if (RCCP_UIManager.Instance != null)
        {
            RCCP_UIManager.Instance.StartCountdown(totalSec, () =>
            {
                SetGameState(GameState.Playing);
                RCCP_UIManager.Instance?.StartRaceTimer();
                DOVirtual.DelayedCall(2f, () =>
                {
                    if (RCCP_UIManager.Instance != null && RCCP_UIManager.Instance.dashboard != null)
                    {
                        RCCP_UIManager.Instance.dashboard.SetActive(true);
                    }
                });
            });
        }
        else
        {
            SetGameState(GameState.Playing);
        }
    }

    public void SetMiniMapFollowPlayer()
    {
        if (miniMapCamera == null) return;

        Transform playerTransform = null;
        if (RCCP_SceneManager.Instance != null && RCCP_SceneManager.Instance.activePlayerVehicle != null)
        {
            playerTransform = RCCP_SceneManager.Instance.activePlayerVehicle.transform;
        }

        if (playerTransform != null)
        {
            miniMapCamera.Follow = playerTransform;
        }
    }
    public void OnFinish()
    {
        if (GameState == GameState.Win || GameState == GameState.Lose) return;

        bool isWin = true;
        if (RCCP_SceneManager.Instance != null)
        {
            var activePlayer = RCCP_SceneManager.Instance.activePlayerVehicle;

            foreach (var vehicle in RCCP_SceneManager.Instance.finishedVehicles)
            {
                if (vehicle != activePlayer)
                {
                    isWin = false;
                    break;
                }
            }
            RCCP_UIManager.Instance?.dashboard?.SetActive(false);
            CarController playerCarController = activePlayer?.GetComponentInParent<CarController>();
            playerCarController?.GetOrCreateAIController();
            if (RCCP_SceneManager.Instance.activePlayerCamera != null)
            {
                RCCP_SceneManager.Instance.activePlayerCamera.cameraMode = RCCP_Camera.CameraMode.FIXED;
            }
        }

        GameState = isWin ? GameState.Win : GameState.Lose;

        Debug.Log(isWin ? "🏆 YOU WIN!" : "💀 YOU LOSE!");
    }


}
