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
    [SerializeField] float longitudDerecha = 1.5f;
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
    [SerializeField] float rampMaxStep = 0.3f;

    [Header("Screen Limits")]
    [SerializeField, Range(0f, 1f)] float frontLimitPercent = 0.55f;
    [SerializeField, Range(0f, 1f)] float backLimitPercent = 0.25f;
    [SerializeField] float limitMargin = 0.05f;

    [Header("Speed Wobble")]
    [SerializeField] float wobbleStartSpeed = 15f;
    [SerializeField] float wobbleTimeToStart = 2f;
    [SerializeField] float wobbleRampUpTime = 1.5f;
    [SerializeField] float wobbleMaxIntensity = 8f;
    [SerializeField] float wobbleFrequency = 15f;
    [SerializeField] float wobbleWarningTime = 3f;
    [SerializeField] float wobbleWarningDuration = 0.5f;
    [SerializeField] float wobbleWarningMultiplier = 3f;

    [Header("World Scroll")]
    [SerializeField] float minScrollSpeed = 2f;
    [SerializeField] float baseScrollSpeed = 5f;
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
    [SerializeField] float landingSpeedMultiplier = 6f;

    [Header("References")]
    [SerializeField] GameOverScreen gameOverScreen; // en la región References
    [SerializeField] Camera cam;
    [SerializeField] GameObject der;
    [SerializeField] WorldScroller worldScroller;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] PlayerInputs playerInputs;
    [SerializeField] BoxCollider2D playerCollider;
    [SerializeField] PlayerAnimator playerAnimator;

    [SerializeField] Vector2 normalColliderSize;
    [SerializeField] Vector2 normalColliderOffset;
    [SerializeField] Vector2 crouchColliderSize;
    [SerializeField] Vector2 crouchColliderOffset;

    #endregion

    #region Public State

    public float GetVerticalSpeed() => verticalSpeed;
    public bool isGrounded;
    public bool isJumping;
    public bool isFloating;
    public bool isFalling;
    public bool isOnRamp;
    public bool isBraking;
    public bool isCrouching;
    public System.Action onAccelerating;
    public System.Action OnStartFalling;
    public System.Action<bool> OnLanded;

    #endregion

    #region Private State

    Rigidbody2D rb;
    float verticalSpeed;
    float floatTimer;
    float wobbleTimer;
    float wobbleAccumulator;
    bool wobbleWarningActive;
    float wobbleWarningTimer;
    float currentFloatDuration;
    bool justJumped;
    Coroutine landingCoroutine;
    RaycastHit2D rightHit;
    RaycastHit2D groundHit;

    #endregion

    #region Unity Loop

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameOver += OnBail;
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnGameOver -= OnBail;
    }

    void FixedUpdate()
    {
        UpdateRaycasts();
        UpdateCrouchCollider();

        if (isJumping) TickAscend();
        else if (isFloating) TickFloat();
        else if (isFalling) TickFall();
        else TickGround();

        UpdateWorldScroll();
    }

    #endregion

    #region Sensing

    void UpdateRaycasts()
    {
        Vector2 groundOrigin = rb.position + Vector2.down * rayAbajoOffset;
        groundHit = Physics2D.Raycast(groundOrigin, Vector2.down, longitudAbajo, groundLayer);

        // Raycast simple horizontal, más predecible que BoxCast
        rightHit = Physics2D.Raycast(rb.position, Vector2.right, longitudDerecha, groundLayer);

        if (!isJumping && !isFloating && !isFalling)
        {
            bool rampAhead = rightHit.collider != null
                          && rightHit.collider.CompareTag("Ramp");

            if (rampAhead && !isOnRamp)
            {
                if (horizontalSpeed >= rampMinSpeed)
                {
                    isOnRamp = true;
                }
                else
                {
                    OnBail();
                }
            }

            // Salir de rampa si ya no hay rampa ni delante ni debajo
            if (isOnRamp && !rampAhead)
            {
                RaycastHit2D rampBelow = Physics2D.Raycast(
                    rb.position, Vector2.down, rayAbajoOffset + 1f, groundLayer);

                bool rampStillBelow = rampBelow.collider != null
                                   && rampBelow.collider.CompareTag("Ramp");

                if (!rampStillBelow)
                {
                    isOnRamp = false;
                    if (isGrounded && !isJumping && !isFloating && !isFalling)
                        StartCoroutine(LerpRotationTo(0f, rampExitSnapSpeed));
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

        if (isOnRamp)
            MoveOnRamp(horizontalVel);
        else
            MoveOnFlat(horizontalVel, groundedThisFrame);

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

    void MoveOnRamp(float horizontalVel)
    {
        float rad = rampAngle * Mathf.Deg2Rad;
        Vector2 rampDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float rampSpeedCompensation = horizontalVel / Mathf.Cos(rad);
        Vector2 delta = rampDir * rampSpeedCompensation * Time.fixedDeltaTime;

        if (delta.magnitude > rampMaxStep)
            delta = delta.normalized * rampMaxStep;

        rb.MovePosition(rb.position + delta);

        RaycastHit2D rampSurface = Physics2D.Raycast(
            rb.position, Vector2.down, rayAbajoOffset + 2f, groundLayer);

        if (rampSurface.collider != null)
        {
            float targetY = rampSurface.point.y + rayAbajoOffset + rampSurfaceOffset;
            if (rb.position.y < targetY)
                rb.position = new Vector2(rb.position.x, targetY);
        }
        else
        {
            isOnRamp = false;
        }
    }

    void MoveOnFlat(float horizontalVel, bool grounded)
    {
        float verticalVel = grounded ? 0f : -gravity;
        Vector2 delta = new Vector2(horizontalVel, verticalVel) * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + delta);
    }

    float ComputeGroundHorizontalVelocity()
    {
        if (playerInputs.isAccelerating) return horizontalSpeed;
        if (isBraking) return -brakeRetreatSpeed;

        float retreat = Mathf.Lerp(passiveRetreatSpeed * 0.5f, passiveRetreatSpeed,
            horizontalSpeed / playerInputs.maxSpeedRef);
        return -retreat;
    }

    float ApplyCameraClamp(float horizontalVel)
    {
        float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0f, 0f)).x;
        float backX = cam.ViewportToWorldPoint(new Vector3(backLimitPercent, 0f, 0f)).x;

        if (rb.position.x >= frontX && horizontalVel > 0f) horizontalVel = 0f;
        if (rb.position.x <= backX && horizontalVel < 0f) horizontalVel = 0f;

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
        if (isOnRamp)
        {
            Quaternion target = Quaternion.Euler(0f, 0f, rampAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, target,
                rampSnapSpeed * Time.fixedDeltaTime);
            return;
        }

        if (playerInputs.IsRotating()) return;
        if (!groundedThisFrame) return;

        Vector2 normal = groundHit.normal;
        float targetAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot,
            groundAlignSpeed * Time.fixedDeltaTime);
    }

    #endregion

    #region Speed Wobble

    void ApplySpeedWobble()
    {
        if (isOnRamp || isCrouching)
        {
            ResetWobble();
            return;
        }

        if (horizontalSpeed >= wobbleStartSpeed)
        {
            wobbleAccumulator += Time.fixedDeltaTime;
        }
        else
        {
            ResetWobble();
            return;
        }

        if (wobbleAccumulator < wobbleTimeToStart) return;

        float timeInWobble = wobbleAccumulator - wobbleTimeToStart;
        float wobbleFactor = Mathf.Clamp01(timeInWobble / wobbleRampUpTime);
        float intensity = wobbleMaxIntensity * wobbleFactor;

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
        else if (wobbleAccumulator >= wobbleTimeToStart + wobbleWarningTime)
        {
            wobbleWarningActive = true;
            wobbleWarningTimer = 0f;
        }

        wobbleTimer += Time.fixedDeltaTime * wobbleFrequency;
        float noise = Mathf.PerlinNoise(wobbleTimer, 0f) * 2f - 1f;
        transform.Rotate(0f, 0f, noise * intensity * Time.fixedDeltaTime * wobbleFrequency);
    }

    void ResetWobble()
    {
        wobbleAccumulator = 0f;
        wobbleWarningActive = false;
        wobbleWarningTimer = 0f;
    }

    #endregion

    #region Jump Phases

    public void ActivateJump()
    {
        isGrounded = false;
        isJumping = true;
        isFloating = false;
        isFalling = false;
        isOnRamp = false;
        justJumped = true;
        floatTimer = 0f;
        verticalSpeed = jumpUpSpeed;

        CancelLandingCoroutine();

        float t = Mathf.Clamp01(horizontalSpeed / speedForMaxFloat);
        currentFloatDuration = Mathf.Lerp(baseFloatDuration, maxFloatDuration, t);
    }

    void TickAscend()
    {
        rb.MovePosition(rb.position + Vector2.up * verticalSpeed * Time.fixedDeltaTime);
        verticalSpeed -= ascendGravity * Time.fixedDeltaTime;

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity,
            jumpRotationResetSpeed * Time.fixedDeltaTime);

        if (verticalSpeed <= 0f)
        {
            isJumping = false;
            isFloating = true;
            isFalling = false;
            floatTimer = 0f;
            verticalSpeed = 0f;
            transform.rotation = Quaternion.identity;
        }
    }

    void TickFloat()
    {
        floatTimer += Time.fixedDeltaTime;

        if (floatTimer >= currentFloatDuration)
        {
            isFloating = false;
            isFalling = true;
            verticalSpeed = 0f;
            OnStartFalling?.Invoke();
        }
    }

    void TickFall()
    {
        verticalSpeed += landingGravity * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + Vector2.down * verticalSpeed * Time.fixedDeltaTime);

        Vector2 groundOrigin = rb.position + Vector2.down * rayAbajoOffset;
        RaycastHit2D fallHit = Physics2D.CircleCast(
            groundOrigin, 0.15f, Vector2.down, longitudAbajo + 0.5f, groundLayer);

        if (fallHit.collider != null)
        {
            isFalling = false;
            isGrounded = true;
            verticalSpeed = 0f;
            rb.position = new Vector2(rb.position.x, fallHit.point.y + rayAbajoOffset);
            horizontalSpeed = Mathf.Min(horizontalSpeed, minScrollSpeed * landingSpeedMultiplier);

            float angle = NormalizeAngle(transform.eulerAngles.z);
            bool safe = angle >= safeLandingAngleMin && angle <= safeLandingAngleMax;
            OnLanded?.Invoke(safe);
            OnLand();
            justJumped = false;
        }
    }

    void UpdateCrouchCollider()
    {
        if (playerCollider == null) return;

        if (isCrouching)
        {
            playerCollider.size = crouchColliderSize;
            playerCollider.offset = crouchColliderOffset;
        }
        else
        {
            playerCollider.size = normalColliderSize;
            playerCollider.offset = normalColliderOffset;
        }
    }

    #endregion

    #region Scroll

    void UpdateWorldScroll()
    {
        float scroll = baseScrollSpeed + horizontalSpeed * scrollMultiplier;

        if (playerInputs.isAccelerating)
            scroll += scrollAccelBoost;
        else if (isBraking)
            scroll = baseScrollSpeed * scrollBrakeMultiplier;

        worldScroller.scrollSpeed = Mathf.Max(scroll, minScrollSpeed);
    }

    #endregion

    #region Ramp Collision

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Ramp")) return;

        isOnRamp = false;
        if (isGrounded && !isJumping && !isFloating && !isFalling)
            StartCoroutine(LerpRotationTo(0f, rampExitSnapSpeed));
    }

    #endregion

    #region Landing & Bail

    public void OnBrakeReleased()
    {
        horizontalSpeed += brakeReleaseBoost;
        rb.MovePosition(rb.position + Vector2.right * brakeReleaseBoost * 0.05f);
    }

    void OnLand()
    {
        float angle = NormalizeAngle(transform.eulerAngles.z);

        if (angle >= safeLandingAngleMin && angle <= safeLandingAngleMax)
        {
            CancelLandingCoroutine();
            landingCoroutine = StartCoroutine(SnapToZero());
        }
        else
        {
            OnBail();
        }
    }

    void OnBail()
    {
        if (playerAnimator != null)
            StartCoroutine(DefeatSequence());
        else
            Time.timeScale = 0f;
    }

    void CancelLandingCoroutine()
    {
        if (landingCoroutine != null)
        {
            StopCoroutine(landingCoroutine);
            landingCoroutine = null;
        }
    }

    IEnumerator DefeatSequence()
    {
        AudioManager.Instance?.PlayGameOver();
        Time.timeScale = 0f;
        if (worldScroller != null) worldScroller.enabled = false;
        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;

        playerAnimator.SetAnimatorUnscaled(true);
        playerAnimator.TriggerDefeat();

        yield return new WaitForSecondsRealtime(1f);

        playerAnimator.FreezeAnimator();

        // Avisar al ScoreManager por si no lo ha lanzado él (muerte por ángulo/wobble)
        ScoreManager.Instance?.ForceGameOver();
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

        Gizmos.color = Application.isPlaying && rightHit.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(rb != null ? (Vector3)rb.position : transform.position, Vector2.right * longitudDerecha);

        Vector2 groundOrigin = (Vector2)transform.position + Vector2.down * rayAbajoOffset;
        Gizmos.color = Application.isPlaying && groundHit.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(groundOrigin, Vector2.down * longitudAbajo);

        if (cam != null)
        {
            float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0f, 0f)).x;
            float backX = cam.ViewportToWorldPoint(new Vector3(backLimitPercent, 0f, 0f)).x;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(frontX, -10f, 0f), new Vector3(frontX, 10f, 0f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(backX, -10f, 0f), new Vector3(backX, 10f, 0f));
        }
    }

    #endregion
}