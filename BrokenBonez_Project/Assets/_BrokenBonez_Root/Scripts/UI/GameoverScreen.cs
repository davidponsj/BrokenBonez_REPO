using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject gameOverPanel;

    [Header("Texts")]
    [SerializeField] TMP_Text puntuacionText;
    [SerializeField] TMP_Text[] letterTexts;  // 3 textos, uno por letra

    [Header("Buttons")]
    [SerializeField] Button btnConfirmName;

    [Header("References")]
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] GameObject hud;

    char[] letters = new char[3] { 'A', 'A', 'A' };
    int selectedIndex = 0;
    int finalScore;
    bool inputActive = false;

    GameOver input;

    void Awake()
    {
        gameOverPanel.SetActive(false);

        if (scoreManager != null)
            scoreManager.OnGameOver += OnGameOver;

        btnConfirmName.onClick.AddListener(ConfirmName);

        input = new GameOver();

        input.UI.Izq.performed += _ =>
        {
            if (!inputActive) return;
            selectedIndex = Mathf.Max(0, selectedIndex - 1);
            UpdateLetterDisplay();
        };
        input.UI.Der.performed += _ =>
        {
            if (!inputActive) return;
            selectedIndex = Mathf.Min(2, selectedIndex + 1);
            UpdateLetterDisplay();
        };
        input.UI.Arriba.performed += _ =>
        {
            if (!inputActive) return;
            letters[selectedIndex] = (char)(((letters[selectedIndex] - 'A' + 1) % 26) + 'A');
            UpdateLetterDisplay();
        };
        input.UI.Abajo.performed += _ =>
        {
            if (!inputActive) return;
            letters[selectedIndex] = (char)(((letters[selectedIndex] - 'A' - 1 + 26) % 26) + 'A');
            UpdateLetterDisplay();
        };
    }

    void OnEnable() => input?.Enable();
    void OnDisable() => input?.Disable();

    void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.OnGameOver -= OnGameOver;
        input?.Dispose();
    }

    void OnGameOver()
    {
        finalScore = Mathf.RoundToInt(ScoreManager.Instance.TotalPoints);
        StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (hud != null) hud.SetActive(false);
        gameOverPanel.SetActive(true);
        if (puntuacionText != null)
            puntuacionText.text = $"{finalScore}";
        letters = new char[3] { 'A', 'A', 'A' };
        selectedIndex = 0;
        inputActive = true;
        UpdateLetterDisplay();
    }

    void UpdateLetterDisplay()
    {
        for (int i = 0; i < letterTexts.Length; i++)
        {
            if (letterTexts[i] == null) continue;
            letterTexts[i].text = letters[i].ToString();
            letterTexts[i].color = i == selectedIndex ? Color.yellow : Color.white;
        }
    }

    void ConfirmName()
    {
        AudioManager.Instance?.PlayButton();
        inputActive = false;
        string playerName = new string(letters);
        LeaderboardManager.Instance?.AddEntry(playerName, finalScore);
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}