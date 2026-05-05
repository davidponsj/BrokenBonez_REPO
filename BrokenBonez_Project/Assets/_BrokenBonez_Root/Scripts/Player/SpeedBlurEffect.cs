using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SpeedBlurEffect : MonoBehaviour
{
    [Header("Tunnel Vision")]
    [SerializeField] float effectStartSpeed = 15f;
    [SerializeField] float effectMaxSpeed = 25f;
    [SerializeField] float maxVignetteIntensity = 0.5f;
    [SerializeField] float maxChromaticIntensity = 0.3f;
    [SerializeField] float smoothSpeed = 5f;

    [Header("References")]
    [SerializeField] Volume volume;
    [SerializeField] PlayerMovement playerMovement;

    Vignette vignette;
    ChromaticAberration chromatic;
    float currentIntensity;

    void Start()
    {
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);
    }

    void Update()
    {
        float speedFactor = Mathf.InverseLerp(effectStartSpeed, effectMaxSpeed, playerMovement.horizontalSpeed);
        currentIntensity = Mathf.Lerp(currentIntensity, speedFactor, smoothSpeed * Time.deltaTime);

        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.value = currentIntensity * maxVignetteIntensity;
        }

        if (chromatic != null)
        {
            chromatic.active = true;
            chromatic.intensity.value = currentIntensity * maxChromaticIntensity;
        }
    }
}