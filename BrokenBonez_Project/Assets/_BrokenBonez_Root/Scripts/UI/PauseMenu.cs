using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("Panel Pausa")]
    [SerializeField] GameObject pausePanel;
    [Header("Botones")]
    [SerializeField] Button btnReanudar;
    [SerializeField] Button btnSalir;
    [Header("HUD")]
    [SerializeField] GameObject hud;

    bool isPaused = false;

    void Start()
    {
        pausePanel.SetActive(false);
        btnReanudar.onClick.AddListener(Reanudar);
        btnSalir.onClick.AddListener(SalirAlMenu);
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Reanudar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        hud.SetActive(false);
    }

    public void Reanudar()
    {
        AudioManager.Instance?.PlayButton();
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        hud.SetActive(true);
    }

    void SalirAlMenu()
    {
        AudioManager.Instance?.PlayButton();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}