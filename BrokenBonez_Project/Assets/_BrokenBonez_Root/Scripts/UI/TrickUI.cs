using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SETUP en Inspector:
/// - trickWindowRoot    → TrickWindow (desactivado por defecto)
/// - barVerde_L/R       → mitad izquierda/derecha de la barra verde   (Trick1)
/// - barAzul_L/R        → mitad izquierda/derecha de la barra azul    (Trick2)
/// - barAmarilla_L/R    → mitad izquierda/derecha de la barra amarilla(Trick3)
/// - barRoja_L/R        → mitad izquierda/derecha de la barra roja    (Trick4)
///
/// Cada _L tiene Fill Origin: Left, cada _R tiene Fill Origin: Right.
/// Cada par ocupa su mitad del RectTransform (anchors 0-0.5 y 0.5-1).
/// Ambas empiezan con fillAmount=1 y bajan a 0 → efecto de cierre hacia el centro.
/// </summary>
public class TrickUI : MonoBehaviour
{
    [Header("Trick Window")]
    [SerializeField] GameObject trickWindowRoot;

    [SerializeField] Image barVerde_L;
    [SerializeField] Image barVerde_R;
    [SerializeField] Image barAzul_L;
    [SerializeField] Image barAzul_R;
    [SerializeField] Image barAmarilla_L;
    [SerializeField] Image barAmarilla_R;
    [SerializeField] Image barRoja_L;
    [SerializeField] Image barRoja_R;

    [Header("Score / Life Bar")]
    [SerializeField] Image scoreFill;

    [Header("Multiplier (opcional)")]
    [SerializeField] TMP_Text multiplierText;

    [Header("Total Points")]
    [SerializeField] TMP_Text totalPointsText;

    [Header("References")]
    [SerializeField] TrickManager trickManager;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] PlayerMovement playerMovement;

    Image barActualL;
    Image barActualR;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (scoreManager != null)
        {
            scoreManager.OnLifeChanged += UpdateScoreBar;
            scoreManager.OnTotalPointsChanged += UpdateTotalPoints;
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
        SetAllBarsInactive();
        UpdateMultiplier(1);
    }

    void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnLifeChanged -= UpdateScoreBar;
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

    void UpdateTotalPoints(float total)
    {
        if (totalPointsText != null)
            totalPointsText.text = Mathf.RoundToInt(total).ToString();
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

        GetBarPair(trickIndex, out barActualL, out barActualR);

        if (barActualL != null) { barActualL.gameObject.SetActive(true); barActualL.fillAmount = 1f; }
        if (barActualR != null) { barActualR.gameObject.SetActive(true); barActualR.fillAmount = 1f; }

        trickWindowRoot.SetActive(true);
    }

    void OnTrickTick(float timeLeft, float duration)
    {
        if (duration <= 0f) return;
        float fill = Mathf.Clamp01(timeLeft / duration);
        if (barActualL != null) barActualL.fillAmount = fill;
        if (barActualR != null) barActualR.fillAmount = fill;
    }

    void OnTrickClose()
    {
        if (trickWindowRoot != null) trickWindowRoot.SetActive(false);
        barActualL = null;
        barActualR = null;
    }

    // ── Landing ───────────────────────────────────────────────────────────────
    void OnFallStart() { }
    void OnLandedResult(bool safe) { }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void SetAllBarsInactive()
    {
        if (barVerde_L != null) barVerde_L.gameObject.SetActive(false);
        if (barVerde_R != null) barVerde_R.gameObject.SetActive(false);
        if (barAzul_L != null) barAzul_L.gameObject.SetActive(false);
        if (barAzul_R != null) barAzul_R.gameObject.SetActive(false);
        if (barAmarilla_L != null) barAmarilla_L.gameObject.SetActive(false);
        if (barAmarilla_R != null) barAmarilla_R.gameObject.SetActive(false);
        if (barRoja_L != null) barRoja_L.gameObject.SetActive(false);
        if (barRoja_R != null) barRoja_R.gameObject.SetActive(false);
    }

    void GetBarPair(int trickIndex, out Image left, out Image right)
    {
        switch (trickIndex)
        {
            case 1: left = barVerde_L; right = barVerde_R; break;
            case 2: left = barAzul_L; right = barAzul_R; break;
            case 3: left = barAmarilla_L; right = barAmarilla_R; break;
            case 4: left = barRoja_L; right = barRoja_R; break;
            default: left = null; right = null; break;
        }
    }
}