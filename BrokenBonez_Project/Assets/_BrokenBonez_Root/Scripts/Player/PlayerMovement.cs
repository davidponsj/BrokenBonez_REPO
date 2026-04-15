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

    [Header("References")]
    [SerializeField] GameObject der;
    [SerializeField] GameObject down;

    bool isGrounded = true;
    RaycastHit2D rayDer;
    RaycastHit2D rayIzq;

    void Update()
    {
        CheckRotation();
    }

    void CheckRotation()
    {
        // Movimiento horizontal constante
        transform.Translate(Vector3.right * horizontalSpeed * Time.deltaTime);

        // Raycasts
        rayDer = Physics2D.Raycast(der.transform.position, transform.right, longitudDerecha);
        rayIzq = Physics2D.Raycast(down.transform.position, -transform.up, longitudAbajo);

        // Gravedad cuando está en el aire
        if (!isGrounded)
        {
            verticalSpeed += gravity * Time.deltaTime;
            transform.Translate(Vector3.down * verticalSpeed * Time.deltaTime);
        }

        // Rotación y pegado a la superficie
        if (rayDer.collider != null)
        {
            float angleDer = Mathf.Atan2(rayDer.normal.y, rayDer.normal.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angleDer);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 15f * Time.deltaTime);
            transform.Translate(-rayDer.normal * horizontalSpeed * gravityOnRotate * Time.deltaTime, Space.World);
        }

        // Estado en suelo
        if (rayDer.collider != null || rayIzq.collider != null)
        {
            isGrounded = true;
            verticalSpeed = 0f;
        }
        else
        {
            isGrounded = false;
        }
    }

    #region Debug
    [Header("Debug")]
    [SerializeField] bool showRaycasts = true;

    void OnDrawGizmos()
    {
        if (!showRaycasts || der == null) return;

        Gizmos.color = rayDer.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(der.transform.position, transform.right * longitudDerecha);

        Gizmos.color = rayIzq.collider != null ? Color.green : Color.red;
        Gizmos.DrawRay(down.transform.position, -transform.up * longitudAbajo);
    }
    #endregion
}