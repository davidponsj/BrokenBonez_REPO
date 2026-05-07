using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject leaderboardPanel;

    [Header("Entries (5)")]
    [SerializeField] TMP_Text[] entryTexts;   // nombres con ":"
    [SerializeField] TMP_Text[] scoreTexts;   // puntuaciones

    [Header("Buttons")]
    [SerializeField] Button btnAbrir;
    [SerializeField] Button btnCerrar;

    void Awake()
    {
        leaderboardPanel.SetActive(false);
        btnAbrir.onClick.AddListener(Abrir);
        btnCerrar.onClick.AddListener(Cerrar);
    }

    void Abrir()
    {
        leaderboardPanel.SetActive(true);
        var entries = LeaderboardManager.Instance?.Entries;

        for (int i = 0; i < entryTexts.Length; i++)
        {
            if (entries != null && i < entries.Count)
            {
                entryTexts[i].text = $"{entries[i].name}:";
                scoreTexts[i].text = entries[i].score.ToString();
            }
            else
            {
                entryTexts[i].text = "---:";
                scoreTexts[i].text = "0";
            }
        }
    }

    void Cerrar()
    {
        leaderboardPanel.SetActive(false);
    }
}