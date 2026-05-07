using UnityEngine;

public class PinaTrigger : MonoBehaviour
{
    [SerializeField] GameObject pinaPrefab;
    [SerializeField] float spawnOffsetX = 5f; // cuánto a la derecha del trigger spawna la piña
    [SerializeField] float spawnOffsetY = 1f;

    PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3 spawnPos = new Vector3(
            transform.position.x + spawnOffsetX,
            transform.position.y + spawnOffsetY,  // 
            0f);

        GameObject pina = Instantiate(pinaPrefab, spawnPos, Quaternion.identity);

        // Dar referencia al WorldScroller para que se mueva con el mundo
        Rigidbody2D pinaRb = pina.GetComponent<Rigidbody2D>();
        if (pinaRb != null)
        {
            var scroller = Object.FindFirstObjectByType<WorldScroller>();
            if (scroller != null)
            {
                var list = new System.Collections.Generic.List<Rigidbody2D>(scroller.groundObjects);
                list.Add(pinaRb);
                scroller.groundObjects = list.ToArray();
            }
        }
    }
}