using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Gestiona el sistema de trucos cooperativo.
/// 
/// - Cualquiera de los dos jugadores inicia pulsando Trick1-4
/// - Se detecta P1 (Keyboard) vs P2 (Gamepad) por dispositivo
/// - El otro jugador confirma pulsando el MISMO botón en la ventana
/// - Ventana más corta = truco más difícil
/// 
/// SETUP:
/// - Añadir al mismo GameObject que PlayerMovement
/// - Asignar referencias en el Inspector
/// - En el PlayerInput (único, Behavior: Unity Events) conectar
///   OnTrick1..4 a TrickManager.OnTrick1..4
/// </summary>
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

    // ── Eventos para TrickUI ──────────────────────────────────────────────────
    /// <summary>(trickIndex 1-4, initiatorIsP1, windowDuration)</summary>
    public System.Action<int, bool, float> OnTrickWindowOpen;
    /// <summary>(timeRemaining, windowDuration)</summary>
    public System.Action<float, float> OnTrickWindowTick;
    /// <summary>Ventana cerrada (éxito o fallo)</summary>
    public System.Action OnTrickWindowClose;

    // ── Estado interno ────────────────────────────────────────────────────────
    bool windowOpen;
    int pendingTrick;
    bool initiatedByP1;
    float windowDuration;
    float windowTimer;
    bool trickLocked;

    // ── Propiedades públicas para UI ──────────────────────────────────────────
    public bool WindowOpen => windowOpen;
    public int PendingTrick => pendingTrick;
    public bool InitiatedByP1 => initiatedByP1;
    public float WindowTimeLeft => windowDuration - windowTimer;
    public float WindowDuration => windowDuration;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!windowOpen) return;

        windowTimer += Time.deltaTime;
        OnTrickWindowTick?.Invoke(windowDuration - windowTimer, windowDuration);

        if (windowTimer >= windowDuration)
            FailWindow();
    }

    // ── Input Actions — conectar desde PlayerInput en el Inspector ────────────
    // Un solo PlayerInput con bindings de teclado Y mando en cada acción.
    // Detectamos quién pulsó por el tipo de dispositivo.

    public void OnTrick1(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) HandleInput(1, IsKeyboard(ctx));
    }
    public void OnTrick2(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) HandleInput(2, IsKeyboard(ctx));
    }
    public void OnTrick3(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) HandleInput(3, IsKeyboard(ctx));
    }
    public void OnTrick4(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) HandleInput(4, IsKeyboard(ctx));
    }

    // ── Lógica central ────────────────────────────────────────────────────────
    void HandleInput(int trickIndex, bool isP1)
    {
        if (!playerMovement.isFloating && !playerMovement.isOnRamp) return;
        if (trickLocked) return;

        if (!windowOpen)
        {
            OpenWindow(trickIndex, isP1);
        }
        else
        {
            bool isConfirmer = (isP1 != initiatedByP1);

            if (!isConfirmer) return; // el mismo que inició vuelve a pulsar, ignorar

            if (trickIndex == pendingTrick)
                CompleteWindow();
            else
                FailWindowHard(); // botón incorrecto
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
        trickLocked = true;
        windowOpen = false;
        OnTrickWindowClose?.Invoke();

        scoreManager.RegisterTrick(pendingTrick);
        TriggerTrickAnimation(pendingTrick);

        StartCoroutine(UnlockAfterFrame());
    }

    void FailWindow()
    {
        // Tiempo agotado completamente → FailHard
        windowOpen = false;
        OnTrickWindowClose?.Invoke();

        scoreManager.RegisterFailHard();
        playerAnimator.TriggerFailHard();
    }

    void FailWindowHard()
    {
        // Botón incorrecto → FailHard
        windowOpen = false;
        OnTrickWindowClose?.Invoke();

        scoreManager.RegisterFailHard();
        playerAnimator.TriggerFailHard();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static bool IsKeyboard(InputAction.CallbackContext ctx)
    {
        return ctx.control.device is Keyboard;
    }

    float GetWindowDuration(int trickIndex) => trickIndex switch
    {
        1 => windowTrick1,
        2 => windowTrick2,
        3 => windowTrick3,
        4 => windowTrick4,
        _ => windowTrick1
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

    IEnumerator UnlockAfterFrame()
    {
        yield return null;
        trickLocked = false;
    }


}