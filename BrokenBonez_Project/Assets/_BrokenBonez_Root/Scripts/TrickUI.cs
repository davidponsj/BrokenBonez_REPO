using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SETUP en Inspector:
/// - trickWindowRoot → TrickWindow (GameObject)
/// - barVerde        → BarVerde    (Image, Filled, Horizontal, Origin Right) — Trick1 Seguro
/// - barAzul         → BarAzul     (Image, Filled, Horizontal, Origin Right) — Trick2 Técnico
/// - barAmarilla     → BarAmarilla (Image, Filled, Horizontal, Origin Right) — Trick3 Freestyle
/// - barRoja         → BarRoja     (Image, Filled, Horizontal, Origin Right) — Trick4 Agresivo
/// - scoreFill       → tu Image de relleno de la barra de vida (Filled)
/// - multiplierText  → TMP_Text opcional para mostrar el multiplicador
/// - trickManager    → TrickManager del player
/// - scoreManager    → ScoreManager de la escena
/// - playerMovement  → PlayerMovement del player
/// </summary>
public class TrickUI : MonoBehaviour
{
    [Header("Trick Window")]
    [SerializeField] GameObject trickWindowRoot;
    [SerializeField] Image barVerde;
    [SerializeField] Image barAzul;
    [SerializeField] Image barAmarilla;
    [SerializeField] Image barRoja;

    [Header("Score / Life Bar")]
    [SerializeField] Image scoreFill; // tu Image de relleno directamente

    [Header("Multiplier (opcional)")]
    [SerializeField] TMP_Text multiplierText;

    [Header("References")]
    [SerializeField] TrickManager trickManager;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] PlayerMovement playerMovement;

    Image barActual;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged += UpdateScoreBar;
            scoreManager.OnMultiplierChanged += UpdateMultiplier;
            scoreManager.OnGameOver += OnGameOver;
        }
        if (trickManager != null)
        {
            trickManager.OnTrickWindowOpen += OnTrickOpen;
            trickManager.OnTrickWindowTick += OnTrickTick;
            trickManager.OnTrickWindowClose += OnTrickClose;
        }
        if (playerMovement != null)
        {
            playerMovement.OnStartFalling += OnFallStart;
            playerMovement.OnLanded += OnLandedResult;
        }
    }

    void Start()
    {
        if (trickWindowRoot != null) trickWindowRoot.SetActive(false);
        UpdateMultiplier(1);
    }

    void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged -= UpdateScoreBar;
            scoreManager.OnMultiplierChanged -= UpdateMultiplier;
            scoreManager.OnGameOver -= OnGameOver;
        }
        if (trickManager != null)
        {
            trickManager.OnTrickWindowOpen -= OnTrickOpen;
            trickManager.OnTrickWindowTick -= OnTrickTick;
            trickManager.OnTrickWindowClose -= OnTrickClose;
        }
        if (playerMovement != null)
        {
            playerMovement.OnStartFalling -= OnFallStart;
            playerMovement.OnLanded -= OnLandedResult;
        }
    }

    // ── Score Bar ─────────────────────────────────────────────────────────────
    void UpdateScoreBar(float current, float max)
    {
        if (scoreFill == null) return;
        scoreFill.fillAmount = max > 0f ? current / max : 0f;
    }

    void UpdateMultiplier(int mult)
    {
        if (multiplierText != null)
            multiplierText.text = $"x{mult}";
    }

    void OnGameOver()
    {
        if (trickWindowRoot != null) trickWindowRoot.SetActive(false);
    }

    // ── Trick Window ──────────────────────────────────────────────────────────
    void OnTrickOpen(int trickIndex, bool initiatorIsP1, float duration)
    {
        if (trickWindowRoot == null) return;

        SetAllBarsInactive();

        barActual = GetBar(trickIndex);
        if (barActual != null)
        {
            barActual.gameObject.SetActive(true);
            barActual.fillAmount = 1f;
        }

        trickWindowRoot.SetActive(true);
    }

    void OnTrickTick(float timeLeft, float duration)
    {
        if (barActual == null) return;
        barActual.fillAmount = duration > 0f ? Mathf.Clamp01(timeLeft / duration) : 0f;
    }

    void OnTrickClose()
    {
        if (trickWindowRoot != null) trickWindowRoot.SetActive(false);
        barActual = null;
    }

    // ── Landing ───────────────────────────────────────────────────────────────
    void OnFallStart() { }
    void OnLandedResult(bool safe) { }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void SetAllBarsInactive()
    {
        if (barVerde != null) barVerde.gameObject.SetActive(false);
        if (barAzul != null) barAzul.gameObject.SetActive(false);
        if (barAmarilla != null) barAmarilla.gameObject.SetActive(false);
        if (barRoja != null) barRoja.gameObject.SetActive(false);
    }

    Image GetBar(int trickIndex) => trickIndex switch
    {
        1 => barVerde,
        2 => barAzul,
        3 => barAmarilla,
        4 => barRoja,
        _ => null
    };
}