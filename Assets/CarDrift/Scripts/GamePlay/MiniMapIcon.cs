using UnityEngine;

public class MiniMapIcon : MonoBehaviour
{
    [SerializeField] private Sprite PlayerIcon;
    [SerializeField] private Sprite EnemyIcon;
    [SerializeField] private SpriteRenderer iconRenderer;

    public void Init(bool player)
    {
        if (player)
        {
            iconRenderer.sprite = PlayerIcon;
            iconRenderer.color = Color.green;
            iconRenderer.sortingOrder = 10;
        }
        else
        {
            iconRenderer.sprite = EnemyIcon;
            iconRenderer.color = Color.red;
        }
    }
}
