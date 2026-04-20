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

    [Header("Rotation Limits")]
    [SerializeField] float groundRotationMin = -45f;
    [SerializeField] float groundRotationMax = 45f;
    [SerializeField] float airRotationMin = -80f;
    [SerializeField] float airRotationMax = 80f;

    [Header("References")]
    [SerializeField] PlayerMovement playerMovement;

    public bool isAccelerating = false;

    float p1Input;
    float p2Input;

    void Update()
    {
        Move();
        Rotate();
    }

    void Move()
    {
        if (playerMovement.isJumping || playerMovement.isFloating || playerMovement.isFalling) return;

        isAccelerating = p1Input > 0;

        if (p1Input > 0)
        {
            playerMovement.horizontalSpeed = Mathf.Clamp(
                playerMovement.horizontalSpeed + acceleration * Time.deltaTime, 0, maxSpeed);
        }
        else if (p1Input < 0)
        {
            if (canGoBackwards)
                playerMovement.horizontalSpeed = Mathf.Clamp(
                    playerMovement.horizontalSpeed - breakSpeed * Time.deltaTime, -maxSpeed, maxSpeed);
            else
                playerMovement.horizontalSpeed = Mathf.Clamp(
                    playerMovement.horizontalSpeed - breakSpeed * Time.deltaTime, 0, maxSpeed);
        }
        else
        {
            playerMovement.horizontalSpeed = Mathf.Clamp(
                playerMovement.horizontalSpeed - deceleration * Time.deltaTime, 0, maxSpeed);
        }
    }

    void Rotate()
    {
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        float min, max;

        if (playerMovement.isGrounded)
        {
            min = groundRotationMin;
            max = groundRotationMax;
        }
        else
        {
            min = airRotationMin;
            max = airRotationMax;
        }

        Debug.Log($"Rotate - angle: {currentAngle:F1} min: {min} max: {max} input: {p2Input}");

        if (currentAngle >= max && p2Input > 0) return;
        if (currentAngle <= min && p2Input < 0) return;

        transform.Rotate(0, 0, p2Input * rotationSpeed * Time.deltaTime);
    }

    public bool IsRotating()
    {
        return Mathf.Abs(p2Input) > 0.1f;
    }

    #region Input Actions
    public void OnMove(InputAction.CallbackContext context)
    {
        p1Input = context.ReadValue<float>();
    }

    public void OnRotate(InputAction.CallbackContext context)
    {
        p2Input = context.ReadValue<float>();
    }
    #endregion
}