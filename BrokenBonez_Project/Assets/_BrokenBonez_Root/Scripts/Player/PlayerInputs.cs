using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
    [Header("Input Movement Parameters")]
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float breakSpeed = 15f;
    [SerializeField] float deceleration = 10f;
    [SerializeField] bool canGoBackwards;

    [Header("Rotation")]
    [SerializeField] float rotationSpeed = 250f;
    [SerializeField] float rotationSpeedAtMaxVelocity = 80f;  // velocidad de rotación cuando vas al máximo
    [SerializeField] float maxSpeedForRotation = 25f;          // velocidad del player a la que la rotación es mínima
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

    bool wasBraking = false;

    void Move()
    {
        if (playerMovement.isJumping || playerMovement.isFloating || playerMovement.isFalling) return;

        isAccelerating = p1Input > 0;
        playerMovement.isBraking = p1Input < 0;

        if (p1Input > 0)
        {
            wasBraking = false;
            playerMovement.horizontalSpeed = Mathf.Clamp(
                playerMovement.horizontalSpeed + acceleration * Time.deltaTime, 0, maxSpeed);
        }
        else if (p1Input < 0)
        {
            wasBraking = true;
            playerMovement.horizontalSpeed = Mathf.Clamp(
                playerMovement.horizontalSpeed - breakSpeed * Time.deltaTime, 0, maxSpeed);
        }
        else
        {
            if (wasBraking)
            {
                playerMovement.OnBrakeReleased();
                wasBraking = false;
            }
            playerMovement.isBraking = false;

            playerMovement.horizontalSpeed = Mathf.Clamp(
                playerMovement.horizontalSpeed - deceleration * Time.deltaTime, 0, maxSpeed);
        }
    }
    void Rotate()
    {
        // Detectar agachado: input de rotar hacia atrás mientras está en suelo
        if (playerMovement.isGrounded && p2Input < 0)
        {
            playerMovement.isCrouching = true;
            return;
        }
        else
        {
            playerMovement.isCrouching = false;
        }

        // No rotar en salto, flotación o suelo
        if (playerMovement.isJumping || playerMovement.isFloating || playerMovement.isGrounded) return;

        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        if (currentAngle >= airRotationMax && p2Input > 0) return;
        if (currentAngle <= airRotationMin && p2Input < 0) return;

        float speedFactor = Mathf.InverseLerp(0f, maxSpeedForRotation, playerMovement.horizontalSpeed);
        float currentRotSpeed = Mathf.Lerp(rotationSpeed, rotationSpeedAtMaxVelocity, speedFactor);

        transform.Rotate(0, 0, p2Input * currentRotSpeed * Time.deltaTime);
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