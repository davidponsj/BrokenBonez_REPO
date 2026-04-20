using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float horizontalSpeed = 5f;
    [SerializeField] float gravity = 20f;
    [SerializeField] float longitudDerecha = 0.5f;
    [SerializeField] float longitudAbajo = 0.5f;
    [SerializeField] float rayAbajoOffset = 0.95f;
    [SerializeField] float passiveRetreatSpeed = 2f;

    [Header("Ramp Alignment")]
    [SerializeField] float rampAlignTolerance = 25f;
    [SerializeField] float rampSnapSpeed = 20f;

    [Header("Screen Limits")]
    [SerializeField] Camera cam;
    [SerializeField][Range(0f, 1f)] float frontLimitPercent = 0.55f;
    [SerializeField][Range(0f, 1f)] float backLimitPercent = 0.25f;
    [SerializeField] float minScrollSpeed = 2f;

    [Header("Jump / Trick")]
    [SerializeField] float jumpUpSpeed = 8f;
    [SerializeField] float ascendGravity = 15f;
    [SerializeField] float floatDuration = 2.5f;
    [SerializeField] float landingGravity = 30f;

    [Header("Landing")]
    [SerializeField] float safeLandingAngleMin = -45f;
    [SerializeField] float safeLandingAngleMax = 45f;
    [SerializeField] float landingSnapSpeed = 10f;

    [Header("References")]
    [SerializeField] GameObject der;
    [SerializeField] GameObject down;
    [SerializeField] WorldScroller worldScroller;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] PlayerInputs playerInputs;

    // Estados
    public bool isGrounded = false;
    public bool isJumping = false;
    public bool isFloating = false;
    public bool isFalling = false;

    bool wasAirborne = false;
    bool justJumped = false;
    float floatTimer = 0f;
    float verticalSpeed = 0f;

    RaycastHit2D rayDer;
    RaycastHit2D rayAbajo;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (isJumping) HandleJump();
        else if (isFloating) HandleFloat();
        else if (isFalling) HandleFall();
        else HandleGround();
    }

    // ── Suelo ───────────────────────────────────────────────────
    void HandleGround()
    {
        rayDer = Physics2D.Raycast(der.transform.position, transform.right, longitudDerecha, groundLayer);

        Vector2 rayOrigin = rb.position + Vector2.down * rayAbajoOffset;
        rayAbajo = Physics2D.Raycast(rayOrigin, Vector2.down, longitudAbajo, groundLayer);

        bool groundedThisFrame = rayAbajo.collider != null;

        float horizontalVel = playerInputs.isAccelerating ? horizontalSpeed : -passiveRetreatSpeed;

        // Límites de cámara: forzar dentro si se sale y cortar velocidad
        float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0, 0)).x;
        float backX = cam.ViewportToWorldPoint(new Vector3(backLimitPercent, 0, 0)).x;

        if (rb.position.x < backX)
        {
            rb.position = new Vector2(backX, rb.position.y);
            horizontalVel = 0f;
        }
        else if (rb.position.x > frontX)
        {
            rb.position = new Vector2(frontX, rb.position.y);
            horizontalVel = 0f;
        }

        if (rb.position.x >= frontX && horizontalVel > 0) horizontalVel = 0f;
        if (rb.position.x <= backX && horizontalVel < 0) horizontalVel = 0f;

        // Gravedad constante, el motor físico detiene al tocar suelo
        float newVelY = rb.linearVelocity.y - gravity * Time.fixedDeltaTime;
        if (newVelY < -gravity) newVelY = -gravity;

        rb.linearVelocity = new Vector2(horizontalVel, newVelY);

        worldScroller.scrollSpeed = Mathf.Max(horizontalSpeed, minScrollSpeed);

        if (groundedThisFrame && !playerInputs.IsRotating())
            AlignToGround();

        // Solo evaluamos aterrizaje si venimos de un salto real
        if (groundedThisFrame && wasAirborne && justJumped)
        {
            OnLand();
            justJumped = false;
        }

        wasAirborne = !groundedThisFrame;
        isGrounded = groundedThisFrame;
    }

    // ── Fase 1: ascenso ─────────────────────────────────────────
    void HandleJump()
    {
        isGrounded = false;
        KeepWorldScrolling();

        rb.linearVelocity = new Vector2(0f, verticalSpeed);
        verticalSpeed -= ascendGravity * Time.fixedDeltaTime;

        if (verticalSpeed <= 0f)
        {
            isJumping = false;
            isFloating = true;
            floatTimer = 0f;
            verticalSpeed = 0f;
        }
    }

    // ── Fase 2: flotación ───────────────────────────────────────
    void HandleFloat()
    {
        isGrounded = false;
        KeepWorldScrolling();

        rb.linearVelocity = Vector2.zero;
        floatTimer += Time.fixedDeltaTime;

        if (floatTimer >= floatDuration)
        {
            isFloating = false;
            isFalling = true;
            verticalSpeed = 0f;
        }
    }

    // ── Fase 3: caída ───────────────────────────────────────────
    void HandleFall()
    {
        isGrounded = false;
        KeepWorldScrolling();

        verticalSpeed += landingGravity * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(0f, -verticalSpeed);

        if (verticalSpeed < 2f) return;

        Vector2 rayOrigin = rb.position + Vector2.down * rayAbajoOffset;
        rayAbajo = Physics2D.Raycast(rayOrigin, Vector2.down, longitudAbajo, groundLayer);

        if (rayAbajo.collider != null)
        {
            isFalling = false;
            isGrounded = true;
            verticalSpeed = 0f;
            OnLand();
            justJumped = false;
        }
    }

    // ── Activación del salto desde RampTrigger ──────────────────
    public void ActivateJump()
    {
        isGrounded = false;
        isJumping = true;
        isFloating = false;
        isFalling = false;
        wasAirborne = true;
        justJumped = true;
        floatTimer = 0f;
        verticalSpeed = jumpUpSpeed;
    }

    void KeepWorldScrolling()
    {
        worldScroller.scrollSpeed = Mathf.Max(horizontalSpeed, minScrollSpeed);
    }

    // ── Alineación con el suelo ────────────────────────────────
    void AlignToGround()
    {
        Vector2 normal = rayAbajo.normal;
        if (rayDer.collider != null)
            normal = Vector2.Lerp(normal, rayDer.normal, 0.4f);

        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 15f * Time.fixedDeltaTime);
    }

    // ── Colisión contra rampas ─────────────────────────────────
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0) return;
        if (isJumping || isFloating) return;

        Vector2 normal = collision.GetContact(0).normal;
        float surfaceAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        float angleDiff = Mathf.DeltaAngle(currentAngle, surfaceAngle);

        if (Mathf.Abs(surfaceAngle) < 5f) return;

        if (Mathf.Abs(angleDiff) <= rampAlignTolerance)
            StartCoroutine(SnapToAngle(surfaceAngle));
        else
            OnBail();
    }

    System.Collections.IEnumerator SnapToAngle(float targetAngle)
    {
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);

        while (Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rampSnapSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRot;
    }

    // ── Aterrizaje ─────────────────────────────────────────────
    void OnLand()
    {
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        if (currentAngle >= safeLandingAngleMin && currentAngle <= safeLandingAngleMax)
            StartCoroutine(SnapToZero());
        else
            OnBail();
    }

    void OnBail()
    {
        Debug.Log("HAS PERDIDO");
        Time.timeScale = 0f;
    }

    System.Collections.IEnumerator SnapToZero()
    {
        while (isGrounded)
        {
            float currentAngle = transform.eulerAngles.z;
            if (currentAngle > 180f) currentAngle -= 360f;
            if (Mathf.Abs(currentAngle) < 0.5f) break;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.identity,
                landingSnapSpeed * Time.deltaTime
            );
            yield return null;
        }
        if (isGrounded)
            transform.rotation = Quaternion.identity;
    }

    #region Debug
    [Header("Debug")]
    [SerializeField] bool showRaycasts = true;

    void OnDrawGizmos()
    {
        if (!showRaycasts || der == null || down == null) return;

        Gizmos.color = rayDer.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(der.transform.position, transform.right * longitudDerecha);

        Vector2 rayOrigin = (Vector2)transform.position + Vector2.down * rayAbajoOffset;
        Gizmos.color = rayAbajo.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(rayOrigin, Vector2.down * longitudAbajo);

        if (cam != null)
        {
            float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0, 0)).x;
            float backX = cam.ViewportToWorldPoint(new Vector3(backLimitPercent, 0, 0)).x;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(frontX, -10, 0), new Vector3(frontX, 10, 0));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(backX, -10, 0), new Vector3(backX, 10, 0));
        }
    }
    #endregion
}