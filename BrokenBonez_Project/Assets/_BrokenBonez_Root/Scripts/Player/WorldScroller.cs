using UnityEngine;

public class WorldScroller : MonoBehaviour
{
    [Header("Scroll")]
    [HideInInspector] public float scrollSpeed = 0f;

    [Header("References")]
    [SerializeField] ParallaxManager parallaxManager;
    [SerializeField] Transform[] groundObjects;

    void Update()
    {
        foreach (Transform ground in groundObjects)
        {
            ground.position += Vector3.left * scrollSpeed * Time.deltaTime;
        }

        parallaxManager.SetSpeed(scrollSpeed);
    }
}