using UnityEngine;
using UnityEngine.EventSystems;

public class GarageSwipeInput : MonoBehaviour
{
    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private bool enableSwipe = true;

    private Vector2 touchStartPos;
    private bool isSwiping = false;

    public bool EnableSwipe
    {
        get => enableSwipe;
        set => enableSwipe = value;
    }

    private void Update()
    {
        if (!Application.isPlaying || !enableSwipe || GarageManager.Instance == null) return;

        // Mobile touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.fingerId)) return;
                touchStartPos = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                isSwiping = false;
                Vector2 delta = touch.position - touchStartPos;
                ProcessSwipe(delta);
            }
        }
        // Mouse input for Unity Editor & PC
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI()) return;
                touchStartPos = Input.mousePosition;
                isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && isSwiping)
            {
                isSwiping = false;
                Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;
                ProcessSwipe(delta);
            }
        }
    }

    private void ProcessSwipe(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > swipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x < 0)
            {
                // Swiped Left -> Show Next Car
                GarageManager.Instance.NextCar();
            }
            else
            {
                // Swiped Right -> Show Previous Car
                GarageManager.Instance.PreviousCar();
            }
        }
    }

    private bool IsPointerOverUI(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;
        if (fingerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(fingerId);
        return EventSystem.current.IsPointerOverGameObject();
    }
}
