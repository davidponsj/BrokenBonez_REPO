using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFade : MonoBehaviour
{
    [SerializeField] Image fadeImage;
    [SerializeField] float fadeDuration = 1f;

    void Start()
    {
        fadeImage.gameObject.SetActive(true);
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }
}