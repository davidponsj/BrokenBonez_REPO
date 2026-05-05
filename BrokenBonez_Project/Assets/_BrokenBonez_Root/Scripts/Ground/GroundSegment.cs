using UnityEngine;

public class GroundSegment : MonoBehaviour
{
    [Header("Segment Configuration")]
    public float width = 30.72f;
    [Range(0f, 1f)] public float spawnProbability = 1f;

    [HideInInspector] public Rigidbody2D[] rigidbodies;

    public float CurrentX => rigidbodies.Length > 0 && rigidbodies[0] != null
        ? rigidbodies[0].position.x
        : transform.position.x;

    void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody2D>();
    }
}