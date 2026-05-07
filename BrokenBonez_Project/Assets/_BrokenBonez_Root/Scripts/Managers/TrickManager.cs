using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TrickManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] PlayerAnimator playerAnimator;
    [SerializeField] ScoreManager scoreManager;

    [Header("Trick Windows (seconds)")]
    [SerializeField] float windowTrick1 = 2.0f;
    [SerializeField] float windowTrick2 = 1.5f;
    [SerializeField] float windowTrick3 = 1.0f;
    [SerializeField] float windowTrick4 = 0.6f;

    [Header("Fail Easy Thresholds (seconds remaining)")]
    [Tooltip("Trick1 Seguro — si quedan menos de X segundos al confirmar → FailEasy")]
    [SerializeField] float failEasyTrick1 = 0.8f;
    [Tooltip("Trick2 Técnico")]
    [SerializeField] float failEasyTrick2 = 0.6f;
    [Tooltip("Trick3 Freestyle")]
    [SerializeField] float failEasyTrick3 = 0.4f;
    [Tooltip("Trick4 Agresivo")]
    [SerializeField] float failEasyTrick4 = 0.2f;

    [Header("Confirm Delay")]
    [SerializeField] float minConfirmDelay = 0.1f;

    // ── Eventos para TrickUI ──────────────────────────────────────────────────
    public System.Action<int, bool, float> OnTrickWindowOpen;
    public System.Action<float, float> OnTrickWindowTick;
    public System.Action OnTrickWindowClose;

    // ── Estado interno ────────────────────────────────────────────────────────
    bool windowOpen;
    int pendingTrick;
    bool initiatedByP1;
    float windowDuration;
    float windowTimer;
    bool trickUsedThisJump;
    bool wasGrounded;

    public float WindowDuration => windowDuration;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Update()
    {
        bool groundedNow = playerMovement.isGrounded;
        if (groundedNow && !wasGrounded)
        {
            trickUsedThisJump = false;
            // Si aterriza con ventana abierta → FailHard
            if (windowOpen) FailWindow();
        }
        wasGrounded = groundedNow;

        if (!windowOpen) return;

        windowTimer += Time.deltaTime;
        OnTrickWindowTick?.Invoke(windowDuration - windowTimer, windowDuration);

        if (windowTimer >= windowDuration)
            FailWindow();
    }

    // ── Input Actions ─────────────────────────────────────────────────────────
    public void OnTrick1(InputAction.CallbackContext ctx) { if (ctx.performed) HandleInput(1, IsKeyboard(ctx)); }
    public void OnTrick2(InputAction.CallbackContext ctx) { if (ctx.performed) HandleInput(2, IsKeyboard(ctx)); }
    public void OnTrick3(InputAction.CallbackContext ctx) { if (ctx.performed) HandleInput(3, IsKeyboard(ctx)); }
    public void OnTrick4(InputAction.CallbackContext ctx) { if (ctx.performed) HandleInput(4, IsKeyboard(ctx)); }

    // ── Lógica central ────────────────────────────────────────────────────────
    void HandleInput(int trickIndex, bool isP1)
    {
        Debug.Log($"tick={windowTimer:F2} isJumping={playerMovement.isJumping} isFloating={playerMovement.isFloating} trickUsed={trickUsedThisJump} windowOpen={windowOpen} confirmDelay={windowTimer < minConfirmDelay} isConfirmer={windowOpen && (isP1 != initiatedByP1)}");

        if (!playerMovement.isJumping && !playerMovement.isFloating && !playerMovement.isFalling) return;
        if (trickUsedThisJump) return;

        if (!windowOpen)
        {
            OpenWindow(trickIndex, isP1);
        }
        else
        {
            if (windowTimer < minConfirmDelay) return;

            bool isConfirmer = (isP1 != initiatedByP1);
            if (!isConfirmer) return;

            if (trickIndex == pendingTrick)
                CompleteWindow();
            else
                FailWindowHard();
        }
    }

    void OpenWindow(int trickIndex, bool isP1)
    {
        windowOpen = true;
        pendingTrick = trickIndex;
        initiatedByP1 = isP1;
        windowDuration = GetWindowDuration(trickIndex);
        windowTimer = 0f;

        OnTrickWindowOpen?.Invoke(trickIndex, isP1, windowDuration);
    }

    void CompleteWindow()
    {
        windowOpen = false;
        trickUsedThisJump = true;
        OnTrickWindowClose?.Invoke();

        float timeLeft = windowDuration - windowTimer;
        float easyThresh = GetFailEasyThreshold(pendingTrick);

        if (timeLeft <= easyThresh)
        {
            scoreManager.RegisterFailEasy();
            playerAnimator.TriggerFailEasy();
        }
        else
        {
            AudioManager.Instance?.PlayTrick();  // ← añade aquí
            scoreManager.RegisterTrick(pendingTrick);
            TriggerTrickAnimation(pendingTrick);
        }
    }

    void FailWindow()
    {
        if (!windowOpen) return;
        windowOpen = false;
        trickUsedThisJump = true;
        OnTrickWindowClose?.Invoke();

        scoreManager.RegisterFailHard();
        playerAnimator.TriggerFailHard();
    }

    void FailWindowHard()
    {
        windowOpen = false;
        trickUsedThisJump = true;
        OnTrickWindowClose?.Invoke();

        scoreManager.RegisterFailHard();
        playerAnimator.TriggerFailHard();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static bool IsKeyboard(InputAction.CallbackContext ctx) => ctx.control.device is Keyboard;

    float GetWindowDuration(int trickIndex) => trickIndex switch
    {
        1 => windowTrick1,
        2 => windowTrick2,
        3 => windowTrick3,
        4 => windowTrick4,
        _ => windowTrick1
    };

    float GetFailEasyThreshold(int trickIndex) => trickIndex switch
    {
        1 => failEasyTrick1,
        2 => failEasyTrick2,
        3 => failEasyTrick3,
        4 => failEasyTrick4,
        _ => 0.4f
    };

    void TriggerTrickAnimation(int trickIndex)
    {
        switch (trickIndex)
        {
            case 1: playerAnimator.TriggerTrick1(); break;
            case 2: playerAnimator.TriggerTrick2(); break;
            case 3: playerAnimator.TriggerTrick3(); break;
            case 4: playerAnimator.TriggerTrick4(); break;
        }
    }
}