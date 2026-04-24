using UnityEngine;

public class RampTrigger : MonoBehaviour
{
    [SerializeField] PlayerMovement playerMovement;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger tocado por: " + other.name + " tag: " + other.tag);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Activando salto");
            playerMovement.ActivateJump();
        }
    }
}