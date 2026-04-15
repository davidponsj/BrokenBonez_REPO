using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
    [Header("Input Movement Parameters")]
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float breakSpeed = 15f;
    [SerializeField] float deceleration = 10f;
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] bool canGoBackwards;

    [Header("References")]
    [SerializeField] PlayerMovement playerMovement;

    //privadas
    float p1Input;
    float p2Input;

    private void Update()
    {
        Move();
        Rotate();
    }

    private void Move()
    {
        if (p1Input > 0)
        {
            playerMovement.horizontalSpeed = Mathf.Clamp(playerMovement.horizontalSpeed += acceleration * Time.deltaTime, 0, maxSpeed);
        }
        else if (p1Input < 0)
        {
            if (canGoBackwards)
            playerMovement.horizontalSpeed = Mathf.Clamp(playerMovement.horizontalSpeed -= breakSpeed * Time.deltaTime, -maxSpeed, maxSpeed);
            if (!canGoBackwards)
                playerMovement.horizontalSpeed = Mathf.Clamp(playerMovement.horizontalSpeed -= breakSpeed * Time.deltaTime, 0, maxSpeed);
        }
        else
        {
            playerMovement.horizontalSpeed = Mathf.Clamp(playerMovement.horizontalSpeed -= deceleration * Time.deltaTime, 0, maxSpeed);
        }   
    }

    private void Rotate()
    {
        transform.Rotate(0, 0, p2Input * rotationSpeed * Time.deltaTime);
    }

    #region Input Actions

    public void OnMove(InputAction.CallbackContext context)
    {
        p1Input = context.ReadValue<float>();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        p2Input = context.ReadValue<float>();
        Debug.Log("OnRotate: " + p2Input);
    }

    #endregion
}
