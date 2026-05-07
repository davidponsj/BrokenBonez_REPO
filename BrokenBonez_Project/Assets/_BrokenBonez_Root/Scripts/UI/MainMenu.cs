using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] Button btnJugar;
    [SerializeField] Button btnOpciones;
    [SerializeField] Button btnSalir;

    [Header("Opciones")]
    [SerializeField] GameObject panelOpciones;

    [Header("Fade")]
    [SerializeField] Image fadeImage;
    [SerializeField] float fadeDuration = 1f;

    [Header("Escala botones hover")]
    [SerializeField] float hoverScale = 1.15f;
    [SerializeField] float scaleSpeed = 8f;

    [Header("Player")]
    [SerializeField] Animator playerAnimator;
    [SerializeField] float playerMoveSpeed = 5f;

    // Hashes
    static readonly int T_Start = Animator.StringToHash("Start");
    static readonly int T_Impulse = Animator.StringToHash("Impulse");
    static readonly int H_IsRunning = Animator.StringToHash("isRunning");

    // Estado interno
    Button[] allButtons;
    Vector3[] originalScales;
    bool transitioning = false;

    void Start()
    {
        allButtons = new Button[] { btnJugar, btnOpciones, btnSalir };
        originalScales = new Vector3[allButtons.Length];
        for (int i = 0; i < allButtons.Length; i++)
            originalScales[i] = allButtons[i].transform.localScale;

        // Fade empieza transparente
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;

        // Asignar listeners
        btnJugar.onClick.AddListener(OnJugar);
        btnOpciones.onClick.AddListener(OnOpciones);
        btnSalir.onClick.AddListener(OnSalir);
    }

    void Update()
    {
        if (transitioning) return;
        HandleButtonScales();
    }

    // ── Hover scale ──────────────────────────────────────────────────────────
    void HandleButtonScales()
    {
        if (panelOpciones.activeSelf) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        for (int i = 0; i < allButtons.Length; i++)
        {
            RectTransform rt = allButtons[i].GetComponent<RectTransform>();
            bool isHovered = RectTransformUtility.RectangleContainsScreenPoint(
                rt, mousePos, null);

            Vector3 targetScale = isHovered
                ? originalScales[i] * hoverScale
                : originalScales[i];

            rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    // ── Botones ──────────────────────────────────────────────────────────────
    void OnJugar()
    {
        AudioManager.Instance?.PlayButton();
        if (transitioning) return;
        transitioning = true;
        StartCoroutine(PlaySequence());
    }

    void OnOpciones()
    {
        AudioManager.Instance?.PlayButton();
        if (transitioning) return;
        panelOpciones.SetActive(true);
        btnJugar.interactable = false;
        btnOpciones.interactable = false;
        btnSalir.interactable = false;
    }

    public void CerrarOpciones()
    {
        AudioManager.Instance?.PlayButton();
        panelOpciones.SetActive(false);
        btnJugar.interactable = true;
        btnOpciones.interactable = true;
        btnSalir.interactable = true;
    }

    void OnSalir()
    {
        AudioManager.Instance?.PlayButton();
        if (transitioning) return;
        Application.Quit();
    }

    // ── Secuencia al pulsar Jugar ─────────────────────────────────────────────
    IEnumerator PlaySequence()
    {
        // 1. Animación de montarse al skate
        playerAnimator.SetTrigger(T_Start);
        yield return new WaitForSeconds(0.3f); // duración del clip START

        // 2. Impulse
        playerAnimator.SetTrigger(T_Impulse);
        yield return new WaitForSeconds(0.5f); // duración del clip IMPULSE

        StartCoroutine(MovePlayerRight());
        // 3. Corre hacia la derecha durante X segundos
        playerAnimator.SetBool(H_IsRunning, true);
        yield return new WaitForSeconds(5f); // ← cuánto tiempo corre, cámbialo a tu gusto

        // 4. Fade y mover a la vez
        yield return StartCoroutine(FadeIn());

        // 5. Cambiar escena
        SceneManager.LoadScene(1);
    }

    IEnumerator MovePlayerRight()
    {
        Transform playerTransform = playerAnimator.transform.parent ?? playerAnimator.transform;
        while (true)
        {
            playerTransform.position += Vector3.right * playerMoveSpeed * Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }
}