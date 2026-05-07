using UnityEngine;

public class Pina : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 180f;   // grados por segundo
    [SerializeField] float destroyOffsetX = 5f;

    PlayerMovement playerMovement;
    WorldScroller worldScroller;
    Camera cam;

    void Start()
    {
        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        worldScroller = Object.FindFirstObjectByType<WorldScroller>();
        cam = Camera.main;
    }

    void Update()
    {
        // Rotar hacia la izquierda
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        // Destruirse cuando sale de pantalla por la izquierda
        float leftEdge = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x - destroyOffsetX;
        if (transform.position.x < leftEdge)
        {
            RemoveFromScroller();
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerMovement.isCrouching)
        {
            // Agachado: pasa sin daño, desaparece
            RemoveFromScroller();
            Destroy(gameObject);
        }
        else
        {
            // No agachado: daño y desaparece
            ScoreManager.Instance?.RegisterFailHard();
            RemoveFromScroller();
            Destroy(gameObject);
        }
    }

    void RemoveFromScroller()
    {
        if (worldScroller == null) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;
        var list = new System.Collections.Generic.List<Rigidbody2D>(worldScroller.groundObjects);
        list.Remove(rb);
        worldScroller.groundObjects = list.ToArray();
    }
}