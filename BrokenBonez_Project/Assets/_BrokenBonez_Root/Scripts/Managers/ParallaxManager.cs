using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    [Header("Parallax Parameters")]
    [SerializeField] float globalSpeed = 1f;
    [Header("Parallax References")]
    [SerializeField] ParallaxLayer[] parallaxLayers;

    private void Update()
    {
        foreach (ParallaxLayer parallaxLayer in parallaxLayers)
        {
            parallaxLayer.speedLayer = globalSpeed;
        }
    }

    // WorldScroller llama esto cada frame
    public void SetSpeed(float speed)
    {
        globalSpeed = speed;
    }
}