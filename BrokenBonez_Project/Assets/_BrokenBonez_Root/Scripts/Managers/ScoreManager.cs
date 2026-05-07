using System;
using UnityEngine;

/// <summary>
/// Singleton. Gestiona la puntuación que funciona como barra de vida.
/// - Decay pasivo con el tiempo
/// - Los trucos añaden puntos
/// - Los fallos quitan puntos
/// - Si llega a 0 → OnGameOver
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score / Life Bar")]
    [SerializeField] float maxScore = 1000f;
    [SerializeField] float startScore = 500f;
    [Tooltip("Puntos que se pierden por segundo pasivamente")]
    [SerializeField] float decayPerSecond = 10f;

    [Header("Trick Points")]
    [SerializeField] float trick1Points = 100f;
    [SerializeField] float trick2Points = 300f;
    [SerializeField] float trick3Points = 700f;
    [SerializeField] float trick4Points = 500f;

    [Header("Penalties")]
    [SerializeField] float failEasyPenalty = 150f;
    [SerializeField] float failHardPenalty = 400f;

    // ── Estado público ────────────────────────────────────────────────────────
    public float CurrentScore { get; private set; }
    public float MaxScore => maxScore;
    public int Multiplier { get; private set; } = 1;

    // Eventos para la UI
    public event Action<float, float> OnScoreChanged;   // (current, max)
    public event Action<int> OnMultiplierChanged;
    public event Action OnGameOver;

    bool gameOver;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentScore = startScore;
        Multiplier = 1;
        NotifyScoreChanged();
    }

    void Update()
    {
        if (gameOver || Time.timeScale == 0f) return;

        ApplyDecay();
    }

    // ── Decay ─────────────────────────────────────────────────────────────────
    void ApplyDecay()
    {
        AddScore(-decayPerSecond * Time.deltaTime);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Llama al completar un truco correctamente.</summary>
    public void RegisterTrick(int trickIndex)
    {
        float points = trickIndex switch
        {
            1 => trick1Points,
            2 => trick2Points,
            3 => trick3Points,
            4 => trick4Points,
            _ => 0f
        };

        // Trick4 sube el multiplicador además de dar puntos
        if (trickIndex == 4)
        {
            Multiplier++;
            OnMultiplierChanged?.Invoke(Multiplier);
        }

        AddScore(points * Multiplier);
    }

    /// <summary>Fallo leve: tiempo casi agotado.</summary>
    public void RegisterFailEasy()
    {
        ResetMultiplier();
        AddScore(-failEasyPenalty);
    }

    /// <summary>Fallo grave: botón incorrecto, tiempo agotado del todo, aterrizaje malo.</summary>
    public void RegisterFailHard()
    {
        ResetMultiplier();
        AddScore(-failHardPenalty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void AddScore(float delta)
    {
        CurrentScore = Mathf.Clamp(CurrentScore + delta, 0f, maxScore);
        NotifyScoreChanged();

        if (CurrentScore <= 0f && !gameOver)
            TriggerGameOver();
    }

    void ResetMultiplier()
    {
        if (Multiplier == 1) return;
        Multiplier = 1;
        OnMultiplierChanged?.Invoke(Multiplier);
    }

    void NotifyScoreChanged()
    {
        OnScoreChanged?.Invoke(CurrentScore, maxScore);
    }

    void TriggerGameOver()
    {
        gameOver = true;
        OnGameOver?.Invoke();
    }

    /// <summary>Reinicia el estado (útil para restart).</summary>
    public void ResetScore()
    {
        gameOver = false;
        CurrentScore = startScore;
        Multiplier = 1;
        NotifyScoreChanged();
        OnMultiplierChanged?.Invoke(Multiplier);
    }
}