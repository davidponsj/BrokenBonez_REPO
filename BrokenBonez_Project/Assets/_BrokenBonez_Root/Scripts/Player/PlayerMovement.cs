using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    #region Inspector Parameters

    [Header("Movement")]
    public float horizontalSpeed = 5f;
    [SerializeField] float gravity = 20f;
    [SerializeField] float passiveRetreatSpeed = 2f;
    [SerializeField] float brakeRetreatSpeed = 5f;
    [SerializeField] float brakeReleaseBoost = 3f;

    [Header("Raycasts")]
    [SerializeField] float longitudDerecha = 0.5f;
    [SerializeField] float longitudAbajo = 0.5f;
    [SerializeField] float rayAbajoOffset = 0.95f;

    [Header("Ground Alignment & Friction")]
    [SerializeField] float groundAlignSpeed = 15f;
    [SerializeField] float comfortAngle = 5f;
    [SerializeField] float maxPenaltyAngle = 30f;
    [SerializeField] float maxFrictionForce = 10f;

    [Header("Ramp")]
    [SerializeField] float rampAngle = 40f;
    [SerializeField] float rampSnapSpeed = 40f;
    [SerializeField] float rampExitSnapSpeed = 20f;
    [SerializeField] float rampMinSpeed = 5f;
    [SerializeField] float rampSurfaceOffset = 0.2f;

    [Header("Screen Limits")]
    [SerializeField, Range(0f, 1f)] float frontLimitPercent = 0.55f;
    [SerializeField, Range(0f, 1f)] float backLimitPercent = 0.25f;
    [SerializeField] float limitMargin = 0.05f;

    [Header("Speed Wobble")]
    [SerializeField] float wobbleStartSpeed = 15f;        // velocidad a la que empieza el temblor
    [SerializeField] float wobbleMaxSpeed = 25f;           // velocidad a la que el temblor es máximo
    [SerializeField] float wobbleMaxIntensity = 8f;        // grados máximos de oscilación
    [SerializeField] float wobbleFrequency = 15f;          // frecuencia del temblor
    [SerializeField] float wobbleWarningTime = 3f;         // segundos antes del aviso
    [SerializeField] float wobbleWarningDuration = 0.5f;    // duración del wobble intenso
    [SerializeField] float wobbleWarningMultiplier = 3f;    // multiplicador del temblor en el aviso

    [Header("World Scroll")]
    [SerializeField] float minScrollSpeed = 2f;
    [SerializeField] float baseScrollSpeed = 5f;       // velocidad normal del scroll sin input
    [SerializeField] float scrollMultiplier = 0.5f;
    [SerializeField] float scrollAccelBoost = 3f;
    [SerializeField] float scrollBrakeMultiplier = 0.3f;

    [Header("Jump - Ascend")]
    [SerializeField] float jumpUpSpeed = 8f;
    [SerializeField] float ascendGravity = 15f;
    [SerializeField] float jumpRotationResetSpeed = 5f;

    [Header("Jump - Float")]
    [SerializeField] float baseFloatDuration = 1.5f;
    [SerializeField] float maxFloatDuration = 3.5f;
    [SerializeField] float speedForMaxFloat = 20f;

    [Header("Jump - Fall")]
    [SerializeField] float landingGravity = 30f;

    [Header("Landing")]
    [SerializeField] float safeLandingAngleMin = -45f;
    [SerializeField] float safeLandingAngleMax = 45f;
    [SerializeField] float landingSnapSpeed = 10f;

    [Header("References")]
    [SerializeField] Camera cam;
    [SerializeField] GameObject der;
    [SerializeField] GameObject down;
    [SerializeField] WorldScroller worldScroller;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] PlayerInputs playerInputs;
    [SerializeField] BoxCollider2D playerCollider;

    #endregion

    #region Public State

     public bool isGrounded;
     public bool isJumping;
     public bool isFloating;
     public bool isFalling;
     public bool isOnRamp;
     public bool isBraking;
     public bool isCrouching;
     public System.Action onAccelerating;

    #endregion

    #region Private State

    Rigidbody2D rb;

    float verticalSpeed;
    float floatTimer;
    float wobbleTimer;
    float wobbleAccumulator;      // tiempo acumulado en zona de wobble
    bool wobbleWarningActive;
    float wobbleWarningTimer;
    float currentFloatDuration;
    bool justJumped;
    int rampContactCount;

    Coroutine landingCoroutine;

    RaycastHit2D rightHit;
    RaycastHit2D groundHit;

    #endregion

    #region Unity Loop

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        UpdateRaycasts();

        if (isJumping)       TickAscend();
        else if (isFloating) TickFloat();
        else if (isFalling)  TickFall();
        else                 TickGround();

        UpdateWorldScroll();
    }

    #endregion

    #region Sensing

    void UpdateRaycasts()
    {
        // Raycast suelo — sin cambios
        Vector2 groundOrigin = rb.position + Vector2.down * rayAbajoOffset;
        groundHit = Physics2D.Raycast(groundOrigin, Vector2.down, longitudAbajo, groundLayer);

        // Reemplaza el rightHit simple por un BoxCast frontal
        if (playerCollider != null)
        {
            Vector2 boxCenter = rb.position;
            Vector2 boxSize = playerCollider.size * 0.9f; // ligeramente menor para evitar falsos positivos
            rightHit = Physics2D.BoxCast(boxCenter, boxSize, 0f,
                                         Vector2.right, longitudDerecha, groundLayer);
        }
        else if (der != null)
        {
            rightHit = Physics2D.Raycast(der.transform.position,
                                         transform.right, longitudDerecha, groundLayer);
        }

        // Activar isOnRamp si el BoxCast toca algo con tag Ramp
        if (!isJumping && !isFloating && !isFalling)
        {
            bool rampAhead = rightHit.collider != null
                          && rightHit.collider.CompareTag("Ramp");
            if (rampAhead && !isOnRamp)
            {
                if (horizontalSpeed >= rampMinSpeed)
                {
                    isOnRamp = true;
                    rampContactCount = 1;
                }
                else
                {
                    OnBail();
                }
            }
        }
    }

    #endregion

    #region Ground State

    void TickGround()
    {
        bool groundedThisFrame = groundHit.collider != null;

        float horizontalVel = ComputeGroundHorizontalVelocity();
        if (playerInputs.isAccelerating)
            onAccelerating?.Invoke();
        horizontalVel = ApplyCameraClamp(horizontalVel);

        Vector2 delta;
        if (isOnRamp)
        {
            float rad = rampAngle * Mathf.Deg2Rad;
            Vector2 rampDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float rampSpeedCompensation = horizontalVel / Mathf.Cos(rad);
            delta = rampDir * rampSpeedCompensation * Time.fixedDeltaTime;

            // Limitar paso máximo para no traspasar colliders
            float maxStep = 0.3f;
            if (delta.magnitude > maxStep)
                delta = delta.normalized * maxStep;

            rb.MovePosition(rb.position + delta);

            // BoxCast hacia abajo para encontrar la superficie real de la rampa
            RaycastHit2D rampSurface = Physics2D.Raycast(
                rb.position,
                Vector2.down,
                rayAbajoOffset + 2f,
                groundLayer);

            if (rampSurface.collider != null)
            {
                float targetY = rampSurface.point.y + rayAbajoOffset + rampSurfaceOffset;
                if (rb.position.y < targetY)
                    rb.position = new Vector2(rb.position.x, targetY);
            }
            else
            {
                // No hay suelo bajo nosotros: fin de la rampa
                isOnRamp = false;
                rampContactCount = 0;
            }
        }
        else
        {
            float verticalVel = groundedThisFrame ? 0f : -gravity;
            delta = new Vector2(horizontalVel, verticalVel) * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + delta);
        }

        if (!isOnRamp && groundedThisFrame)
            ApplyAngleFriction();

        UpdateGroundRotation(groundedThisFrame);
        ApplySpeedWobble();

        if (groundedThisFrame && justJumped)
        {
            justJumped = false;
            OnLand();
        }

        isGrounded = groundedThisFrame;
    }

    float ComputeGroundHorizontalVelocity()
    {
        if (playerInputs.isAccelerating) return horizontalSpeed;
        if (isBraking) return -brakeRetreatSpeed;

        // Sin input: retroceso proporcional a la velocidad actual
        // A velocidad alta retrocedes más, a velocidad baja casi nada
        float retreat = Mathf.Lerp(passiveRetreatSpeed * 0.5f, passiveRetreatSpeed,
            horizontalSpeed / playerInputs.maxSpeedRef);
        return -retreat;
    }

    float ApplyCameraClamp(float horizontalVel)
    {
        float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0f, 0f)).x;
        float backX  = cam.ViewportToWorldPoint(new Vector3(backLimitPercent,  0f, 0f)).x;

        if (rb.position.x >= frontX && horizontalVel > 0f) horizontalVel = 0f;
        if (rb.position.x <= backX  && horizontalVel < 0f) horizontalVel = 0f;

        if (rb.position.x < backX - limitMargin)
            rb.position = new Vector2(backX, rb.position.y);
        else if (rb.position.x > frontX + limitMargin)
            rb.position = new Vector2(frontX, rb.position.y);

        return horizontalVel;
    }

    void ApplyAngleFriction()
    {
        Vector2 normal = groundHit.normal;
        float groundAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = NormalizeAngle(transform.eulerAngles.z);
        float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngle, groundAngle));

        if (diff <= comfortAngle) return;

        float t = Mathf.InverseLerp(comfortAngle, maxPenaltyAngle, diff);
        float friction = maxFrictionForce * t;
        horizontalSpeed = Mathf.Max(horizontalSpeed - friction * Time.fixedDeltaTime, minScrollSpeed);
    }

    void UpdateGroundRotation(bool groundedThisFrame)
    {
        // Ramps: force the canonical 40° orientation via a fast Lerp.
        if (isOnRamp)
        {
            Quaternion target = Quaternion.Euler(0f, 0f, rampAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, target,
                rampSnapSpeed * Time.fixedDeltaTime);
            return;
        }

        // Player is manually rotating: let PlayerInputs own the rotation this frame.
        if (playerInputs.IsRotating()) return;

        // Otherwise, align to the ground normal (smoothly).
        if (!groundedThisFrame) return;

        Vector2 normal = groundHit.normal;
        float targetAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot,
            groundAlignSpeed * Time.fixedDeltaTime);
    }

    #endregion

    #region Jump Phases

    public void ActivateJump()
    {
        isGrounded  = false;
        isJumping   = true;
        isFloating  = false;
        isFalling   = false;
        isOnRamp    = false;
        justJumped  = true;
        floatTimer  = 0f;
        verticalSpeed = jumpUpSpeed;
        rampContactCount = 0;

        // Air time scales with how fast we were going when we took off.
        float t = Mathf.Clamp01(horizontalSpeed / speedForMaxFloat);
        currentFloatDuration = Mathf.Lerp(baseFloatDuration, maxFloatDuration, t);
    }


    void ApplySpeedWobble()
    {
        float wobbleFactor = Mathf.InverseLerp(wobbleStartSpeed, wobbleMaxSpeed, horizontalSpeed);

        // Si no hay wobble o está agachado, resetear acumulador
        if (wobbleFactor <= 0f || isCrouching)
        {
            wobbleAccumulator = 0f;
            wobbleWarningActive = false;
            wobbleWarningTimer = 0f;
            return;
        }

        // Acumular tiempo en zona de wobble
        wobbleAccumulator += Time.fixedDeltaTime;

        float intensity = wobbleMaxIntensity * wobbleFactor;

        // Fase 3: si el aviso terminó y sigue en wobble, bail
        if (wobbleWarningActive)
        {
            wobbleWarningTimer += Time.fixedDeltaTime;
            intensity *= wobbleWarningMultiplier;

            if (wobbleWarningTimer >= wobbleWarningDuration)
            {
                OnBail();
                return;
            }
        }
        // Fase 2: activar aviso
        else if (wobbleAccumulator >= wobbleWarningTime)
        {
            wobbleWarningActive = true;
            wobbleWarningTimer = 0f;
        }

        // Aplicar temblor
        wobbleTimer += Time.fixedDeltaTime * wobbleFrequency;
        float noise = Mathf.PerlinNoise(wobbleTimer, 0f) * 2f - 1f;
        float wobbleAngle = noise * intensity;

        transform.Rotate(0f, 0f, wobbleAngle * Time.fixedDeltaTime * wobbleFrequency);
    }

    void TickAscend()
    {
        Vector2 delta = new Vector2(0f, verticalSpeed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + delta);

        verticalSpeed -= ascendGravity * Time.fixedDeltaTime;

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity,
            jumpRotationResetSpeed * Time.fixedDeltaTime);

        if (verticalSpeed <= 0f)
        {
            isJumping = false;
            isFloating = true;
            floatTimer = 0f;
            verticalSpeed = 0f;
            transform.rotation = Quaternion.identity;
        }
    }

    // Phase 2: suspended in the air while the trick window is open.
    // Future trick combos will be read during this state; rotation is locked here.
    void TickFloat()
    {
        floatTimer += Time.fixedDeltaTime;

        if (floatTimer >= currentFloatDuration)
        {
            isFloating = false;
            isFalling = true;
            verticalSpeed = 0f;
        }
    }

    // Phase 3: falling. Gravity accumulates and the ground raycast triggers landing.
    void TickFall()
    {
        verticalSpeed += landingGravity * Time.fixedDeltaTime;
        Vector2 delta = new Vector2(0f, -verticalSpeed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + delta);

        // Usar CircleCast con radio pequeño para no perderse el suelo
        Vector2 groundOrigin = rb.position + Vector2.down * rayAbajoOffset;
        RaycastHit2D fallHit = Physics2D.CircleCast(
            groundOrigin, 0.15f, Vector2.down, longitudAbajo + 0.5f, groundLayer);

        if (fallHit.collider != null)
        {
            isFalling = false;
            isGrounded = true;
            verticalSpeed = 0f;
            rb.position = new Vector2(rb.position.x, fallHit.point.y + rayAbajoOffset);

            // Reducir velocidad al aterrizar para que no salga disparado
            horizontalSpeed = Mathf.Min(horizontalSpeed, minScrollSpeed * 3f);

            OnLand();
            justJumped = false;
        }
    }

    #endregion

    #region Scroll

    void UpdateWorldScroll()
    {
        // El scroll parte del base, y sube con la velocidad del player
        float scroll = baseScrollSpeed + (horizontalSpeed * scrollMultiplier);

        if (playerInputs.isAccelerating)
            scroll += scrollAccelBoost;
        else if (isBraking)
            scroll = baseScrollSpeed * scrollBrakeMultiplier;

        worldScroller.scrollSpeed = Mathf.Max(scroll, minScrollSpeed);
    }

    #endregion

    #region Ramp Collision

    void OnCollisionEnter2D(Collision2D collision) { }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Ramp")) return;
        rampContactCount = Mathf.Max(0, rampContactCount - 1);
        if (rampContactCount == 0 && isOnRamp)
        {
            isOnRamp = false;
            if (isGrounded && !isJumping && !isFloating && !isFalling)
                StartCoroutine(LerpRotationTo(0f, rampExitSnapSpeed));
        }
    }

    #endregion

    #region Brake & Landing

    public void OnBrakeReleased()
    {
        horizontalSpeed += brakeReleaseBoost;

        // Empujón físico hacia adelante para que se sienta el boost
        rb.MovePosition(rb.position + Vector2.right * brakeReleaseBoost * 0.05f);
    }

    void OnLand()
    {
        float angle = NormalizeAngle(transform.eulerAngles.z);

        if (angle >= safeLandingAngleMin && angle <= safeLandingAngleMax)
        {
            if (landingCoroutine != null) StopCoroutine(landingCoroutine);
            landingCoroutine = StartCoroutine(SnapToZero());
        }
        else
        {
            OnBail();
        }
    }

    void OnBail()
    {
        Debug.Log("HAS PERDIDO");
        Time.timeScale = 0f;
    }

    IEnumerator SnapToZero()
    {
        while (true)
        {
            float angle = NormalizeAngle(transform.eulerAngles.z);
            if (Mathf.Abs(angle) < 0.5f) break;

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity,
                landingSnapSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        landingCoroutine = null;
    }

    IEnumerator LerpRotationTo(float targetZ, float speed)
    {
        Quaternion target = Quaternion.Euler(0f, 0f, targetZ);
        while (Quaternion.Angle(transform.rotation, target) > 0.5f)
        {
            // Ramp re-entry or any state transition that needs rotation control
            // cancels this smoothing.
            if (isOnRamp || isJumping || isFloating || isFalling) yield break;

            transform.rotation = Quaternion.Lerp(transform.rotation, target,
                speed * Time.deltaTime);
            yield return null;
        }
        if (!isOnRamp && !isJumping && !isFloating && !isFalling)
            transform.rotation = target;
    }

    #endregion

    #region Helpers

    static float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    #endregion

    #region Debug

    [Header("Debug")]
    [SerializeField] bool showRaycasts = true;

    void OnDrawGizmos()
    {
        if (!showRaycasts) return;

        if (der != null)
        {
            Gizmos.color = Application.isPlaying && rightHit.collider != null ? Color.green : Color.red;
            Gizmos.DrawRay(der.transform.position, transform.right * longitudDerecha);
        }

        Vector2 groundOrigin = (Vector2)transform.position + Vector2.down * rayAbajoOffset;
        Gizmos.color = Application.isPlaying && groundHit.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(groundOrigin, Vector2.down * longitudAbajo);

        if (cam != null)
        {
            float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0f, 0f)).x;
            float backX  = cam.ViewportToWorldPoint(new Vector3(backLimitPercent,  0f, 0f)).x;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(frontX, -10f, 0f), new Vector3(frontX, 10f, 0f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(backX, -10f, 0f), new Vector3(backX, 10f, 0f));
        }
    }

    #endregion
}
