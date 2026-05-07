using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] PlayerInputs playerInputs;

    [Header("Crouch Delay")]
    [Tooltip("Segundos que tarda en agacharse tras pulsar el botón")]
    [SerializeField] float crouchDelay = 0.15f;

    // Hashes
    static readonly int H_IsGrounded = Animator.StringToHash("isGrounded");
    static readonly int H_IsBraking = Animator.StringToHash("isBraking");
    static readonly int H_IsCrouching = Animator.StringToHash("isCrouching");
    static readonly int H_CanDoTrick = Animator.StringToHash("canDoTrick");
    static readonly int H_IsFailing = Animator.StringToHash("isFailing");
    static readonly int H_IsRunning = Animator.StringToHash("isRunning");
    static readonly int H_Speed = Animator.StringToHash("speed");
    static readonly int H_VerticalSpeed = Animator.StringToHash("verticalSpeed");

    static readonly int T_Impulse = Animator.StringToHash("Impulse");
    static readonly int T_Trick1 = Animator.StringToHash("Trick1");
    static readonly int T_Trick2 = Animator.StringToHash("Trick2");
    static readonly int T_Trick3 = Animator.StringToHash("Trick3");
    static readonly int T_Trick4 = Animator.StringToHash("Trick4");
    static readonly int T_Defeat = Animator.StringToHash("Defeat");
    static readonly int T_LandGood = Animator.StringToHash("LandGood");
    static readonly int T_LandBad = Animator.StringToHash("LandBad");
    static readonly int T_FailEasy = Animator.StringToHash("FailEasy");  // NUEVO
    static readonly int T_FailHard = Animator.StringToHash("FailHard");  // NUEVO

    // Estado interno
    Animator anim;
    bool wasAccelerating;
    bool wasGrounded;
    bool wasCrouchInput;
    bool crouchPending;
    Coroutine crouchCoroutine;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerInputs == null) playerInputs = GetComponentInParent<PlayerInputs>();
    }

    void Update()
    {
        if (anim.runtimeAnimatorController == null) return;

        // ── Floats ────────────────────────────────────────────────────────────
        anim.SetFloat(H_Speed, playerMovement.horizontalSpeed);

        float vSpeed = playerMovement.isFalling ? -playerMovement.GetVerticalSpeed()
                     : playerMovement.isJumping ? playerMovement.GetVerticalSpeed()
                     : 0f;
        anim.SetFloat(H_VerticalSpeed, vSpeed);

        // ── Bools ─────────────────────────────────────────────────────────────
        anim.SetBool(H_IsGrounded, playerMovement.isGrounded);
        anim.SetBool(H_IsBraking, playerMovement.isBraking);
        anim.SetBool(H_CanDoTrick, playerMovement.isOnRamp || playerMovement.isFloating);

        bool isRunning = playerMovement.isGrounded
                      && playerInputs.isAccelerating
                      && !playerMovement.isFalling
                      && !playerMovement.isJumping;
        anim.SetBool(H_IsRunning, isRunning);

        // ── Impulse ───────────────────────────────────────────────────────────
        bool isAcceleratingNow = playerInputs.isAccelerating;
        if (isAcceleratingNow && !wasAccelerating && playerMovement.isGrounded)
            anim.SetTrigger(T_Impulse);
        wasAccelerating = isAcceleratingNow;

        // ── Crouch con delay ──────────────────────────────────────────────────
        HandleCrouch();

        // ── Aterrizaje ────────────────────────────────────────────────────────
        bool groundedNow = playerMovement.isGrounded;
        if (groundedNow && !wasGrounded)
        {
            anim.SetTrigger(T_LandGood);
            wasAccelerating = false; // ← AÑADIR — fuerza re-detección del impulso
        }
        wasGrounded = groundedNow;
    }

    void HandleCrouch()
    {
        bool crouchInput = playerMovement.isCrouching;

        if (crouchInput && !wasCrouchInput)
        {
            crouchPending = true;
            if (crouchCoroutine != null) StopCoroutine(crouchCoroutine);
            crouchCoroutine = StartCoroutine(CrouchAfterDelay());
        }

        if (!crouchInput && wasCrouchInput)
        {
            crouchPending = false;
            if (crouchCoroutine != null) { StopCoroutine(crouchCoroutine); crouchCoroutine = null; }
            anim.SetBool(H_IsCrouching, false);
        }

        wasCrouchInput = crouchInput;
    }

    IEnumerator CrouchAfterDelay()
    {
        yield return new WaitForSeconds(crouchDelay);
        if (crouchPending)
            anim.SetBool(H_IsCrouching, true);
        crouchCoroutine = null;
    }

    public void SetAnimatorUnscaled(bool unscaled)
    {
        anim.updateMode = unscaled
            ? AnimatorUpdateMode.UnscaledTime
            : AnimatorUpdateMode.Normal;
    }

    public void FreezeAnimator()
    {
        anim.speed = 0f;
    }

    // ── API pública ───────────────────────────────────────────────────────────
    public void TriggerTrick1() => anim.SetTrigger(T_Trick1);
    public void TriggerTrick2() => anim.SetTrigger(T_Trick2);
    public void TriggerTrick3() => anim.SetTrigger(T_Trick3);
    public void TriggerTrick4() => anim.SetTrigger(T_Trick4);
    public void TriggerDefeat() => anim.SetTrigger(T_Defeat);
    public void TriggerLandBad() => anim.SetTrigger(T_LandBad);
    public void TriggerFailEasy() => anim.SetTrigger(T_FailEasy); // NUEVO
    public void TriggerFailHard() => anim.SetTrigger(T_FailHard); // NUEVO
    public void SetFailing(bool value) => anim.SetBool(H_IsFailing, value);
}