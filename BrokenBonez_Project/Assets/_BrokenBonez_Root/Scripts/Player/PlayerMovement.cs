using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float horizontalSpeed = 5f;
    [SerializeField] float verticalSpeed = 0f;
    [SerializeField] float gravity = 9.81f;
    [SerializeField] float gravityOnRotate = 5f;
    [SerializeField] float longitudDerecha = 0.5f;
    [SerializeField] float longitudAbajo = 0.5f;

    [Header("Screen Limits")]
    [SerializeField] Camera cam;
    [SerializeField][Range(0f, 1f)] float frontLimitPercent = 0.55f;
    [SerializeField][Range(0f, 1f)] float backLimitPercent = 0.25f;
    [SerializeField] float minScrollSpeed = 2f;

    [Header("Landing")]
    [SerializeField] float safeLandingAngleMin = -45f;
    [SerializeField] float safeLandingAngleMax = 45f;
    [SerializeField] float landingSnapSpeed = 10f;

    [Header("References")]
    [SerializeField] GameObject der;
    [SerializeField] GameObject down;
    [SerializeField] WorldScroller worldScroller;

    bool isGrounded = false;
    bool wasAirborne = false;
    RaycastHit2D rayDer;
    RaycastHit2D rayAbajo;

    void Update()
    {
        CheckRotation();
    }

    void CheckRotation()
    {
        // -- Límites de pantalla --
        float frontX = cam.ViewportToWorldPoint(new Vector3(frontLimitPercent, 0, 0)).x;
        float backX = cam.ViewportToWorldPoint(new Vector3(backLimitPercent, 0, 0)).x;

        float newX = transform.position.x + horizontalSpeed * Time.deltaTime;

        if (newX >= frontX)
        {
            transform.position = new Vector3(frontX, transform.position.y, transform.position.z);
            worldScroller.scrollSpeed = horizontalSpeed;
        }
        else if (newX <= backX)
        {
            transform.position = new Vector3(backX, transform.position.y, transform.position.z);
            worldScroller.scrollSpeed = minScrollSpeed;
        }
        else
        {
            transform.Translate(Vector3.right * horizontalSpeed * Time.deltaTime);
            worldScroller.scrollSpeed = Mathf.Max(horizontalSpeed, minScrollSpeed);
        }

        // -- Raycasts --
        rayDer = Physics2D.Raycast(der.transform.position, transform.right, longitudDerecha);
        rayAbajo = Physics2D.Raycast(down.transform.position, Vector2.down, longitudAbajo);

        // El suelo se confirma SOLO con el raycast de los pies
        bool groundedThisFrame = rayAbajo.collider != null;

        // -- Detección de aterrizaje --
        if (groundedThisFrame && wasAirborne)
        {
            OnLand();
        }

        // -- Gravedad en el aire --
        if (!groundedThisFrame)
        {
            verticalSpeed += gravity * Time.deltaTime;
            transform.Translate(Vector3.down * verticalSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            verticalSpeed = 0f;
            // Sin pegado forzado, el raycast ya confirma que está en suelo
        }

        // -- Rotación en suelo --
        if (groundedThisFrame)
        {
            AlignToGround();
        }

        // -- Actualizar estado al final --
        wasAirborne = !groundedThisFrame;
        isGrounded = groundedThisFrame;
    }

    void AlignToGround()
    {
        // La normal base siempre viene de los pies
        Vector2 normal = rayAbajo.normal;

        // Si rayDer también detecta algo, mezclamos las normales para anticipar la rampa
        if (rayDer.collider != null)
        {
            normal = Vector2.Lerp(normal, rayDer.normal, 0.4f);
        }

        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 15f * Time.deltaTime);
    }

    void OnLand()
    {
        float currentAngle = transform.eulerAngles.z;
        // Normalizar a -180..180 correctamente
        if (currentAngle > 180f) currentAngle -= 360f;

        if (currentAngle >= safeLandingAngleMin && currentAngle <= safeLandingAngleMax)
        {
            StartCoroutine(SnapToZero());
        }
        // Si está fuera del rango: bail (pendiente)
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
        Gizmos.color = rayAbajo.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(down.transform.position, -transform.up * longitudAbajo);

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