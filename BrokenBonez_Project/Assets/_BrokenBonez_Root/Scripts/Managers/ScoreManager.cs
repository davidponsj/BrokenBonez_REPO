using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Life Bar")]
    [SerializeField] float maxLife = 1000f;
    [SerializeField] float startLife = 500f;
    [SerializeField] float decayPerSecond = 10f;
    [SerializeField] float crouchDecayPerSecond = 30f;

    [Header("Trick Points")]
    [SerializeField] float trick1Points = 100f;
    [SerializeField] float trick2Points = 300f;
    [SerializeField] float trick3Points = 700f;
    [SerializeField] float trick4Points = 500f;

    [Header("Penalties")]
    [SerializeField] float failEasyPenalty = 150f;
    [SerializeField] float failHardPenalty = 400f;

    [Header("References")]
    [SerializeField] PlayerMovement playerMovement;

    // Estado público
    public float CurrentLife { get; private set; }
    public float MaxLife => maxLife;
    public float TotalPoints { get; private set; }
    public int Multiplier { get; private set; } = 1;

    // Eventos
    public event Action<float, float> OnLifeChanged;
    public event Action<float> OnTotalPointsChanged;
    public event Action<int> OnMultiplierChanged;
    public event Action OnGameOver;

    bool gameOver;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentLife = startLife;
        TotalPoints = 0f;
        Multiplier = 1;
        NotifyAll();
    }

    void Update()
    {
        if (gameOver || Time.timeScale == 0f) return;

        float decay = playerMovement != null && playerMovement.isCrouching
            ? crouchDecayPerSecond
            : decayPerSecond;

        AddLife(-decay * Time.deltaTime);
    }

    // ── API pública ──────────────────────────────────────────────────────────

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

        if (trickIndex == 4)
        {
            Multiplier++;
            OnMultiplierChanged?.Invoke(Multiplier);
        }

        float gained = points * Multiplier;
        AddLife(gained);
        AddTotalPoints(gained);
    }

    public void RegisterFailEasy()
    {
        ResetMultiplier();
        AddLife(-failEasyPenalty);
    }

    public void RegisterFailHard()
    {
        ResetMultiplier();
        AddLife(-failHardPenalty);
    }

    public void ForceGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        OnGameOver?.Invoke();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void AddLife(float delta)
    {
        CurrentLife = Mathf.Clamp(CurrentLife + delta, 0f, maxLife);
        OnLifeChanged?.Invoke(CurrentLife, maxLife);

        if (delta > 0f)
            AddTotalPoints(delta);

        if (CurrentLife <= 0f && !gameOver)
            TriggerGameOver();
    }

    void AddTotalPoints(float points)
    {
        if (points <= 0f) return;
        TotalPoints += points;
        OnTotalPointsChanged?.Invoke(TotalPoints);
    }

    void ResetMultiplier()
    {
        if (Multiplier == 1) return;
        Multiplier = 1;
        OnMultiplierChanged?.Invoke(Multiplier);
    }

    void NotifyAll()
    {
        OnLifeChanged?.Invoke(CurrentLife, maxLife);
        OnTotalPointsChanged?.Invoke(TotalPoints);
        OnMultiplierChanged?.Invoke(Multiplier);
    }

    void TriggerGameOver()
    {
        gameOver = true;
        OnGameOver?.Invoke();
    }

    public void ResetScore()
    {
        gameOver = false;
        CurrentLife = startLife;
        TotalPoints = 0f;
        Multiplier = 1;
        NotifyAll();
        OnMultiplierChanged?.Invoke(Multiplier);
    }
}