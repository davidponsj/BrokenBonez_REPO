using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [Header("Scroll")]
    [HideInInspector] public float scrollSpeed = 0f;

    [Header("References")]
    [SerializeField] ParallaxManager parallaxManager;
    [SerializeField] Rigidbody2D[] groundObjects;

    void FixedUpdate()
    {
        foreach (Rigidbody2D ground in groundObjects)
        {
            if (ground == null) continue;
            Vector2 newPos = ground.position + Vector2.left * scrollSpeed * Time.fixedDeltaTime;
            ground.MovePosition(newPos);
        }
    }

    void Update()
    {
        // El parallax sigue en Update porque es solo visual, no físico
        parallaxManager.SetSpeed(scrollSpeed);
    }
}