using UnityEngine;

public class RampTrigger : MonoBehaviour
{
    PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerMovement.ActivateJump();
        }
    }
}