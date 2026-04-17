using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Parameters")]
    [SerializeField] float speedMultiplier;
    [SerializeField] float yOffset;
    [SerializeField] float xOffset;
    float speed;
    [HideInInspector] public float speedLayer;

    [Header("Parallax References")]
    [SerializeField] Sprite[] sprites; // Array de 5 sprites distintos
    [SerializeField] SpriteRenderer[] copias;

    [Header("Debug")]
    [SerializeField] bool mostrarDebug = false;

    float anchoCopia;
    private int currentSpriteIndex = 0;
    private int imagenesReproducidas = 0;

    private void Start()
    {
        // Validar que tenemos sprites
        if (sprites.Length == 0)
        {
            Debug.LogError("ParallaxLayer: No hay sprites asignados!");
            return;
        }

        anchoCopia = copias[0].bounds.size.x;

        // Inicializar posiciones y sprites
        for (int i = 0; i < copias.Length; i++)
        {
            copias[i].transform.position = new Vector3(i * anchoCopia + xOffset, yOffset, 0);
            copias[i].sprite = sprites[i % sprites.Length];
        }

        currentSpriteIndex = copias.Length % sprites.Length;
    }

    private void Update()
    {
        if (sprites.Length == 0) return;

        speed = speedMultiplier * speedLayer;
        float maxX = float.MinValue;

        // Mover todas las copias
        foreach (SpriteRenderer copia in copias)
        {
            copia.transform.position += new Vector3(-speed * Time.deltaTime, 0, 0);
        }

        // Encontrar la posición máxima
        foreach (SpriteRenderer copia in copias)
        {
            if (copia.transform.position.x > maxX)
            {
                maxX = copia.transform.position.x;
            }
        }

        // Reciclar copias
        foreach (SpriteRenderer copia in copias)
        {
            if (copia.transform.position.x < -anchoCopia * 2f)
            {
                copia.transform.position = new Vector3(maxX + anchoCopia, yOffset, 0);

                // Cambiar sprite
                int spriteIndex = currentSpriteIndex % sprites.Length;
                copia.sprite = sprites[spriteIndex];

                if (mostrarDebug)
                    Debug.Log($"Sprite {spriteIndex + 1}/5 ({sprites[spriteIndex].name})");

                currentSpriteIndex++;
                imagenesReproducidas++;

                // Callback cuando completa una secuencia completa de 5
                if (imagenesReproducidas % sprites.Length == 0)
                {
                    OnCycleComplete();
                }
            }
        }
    }

    private void OnCycleComplete()
    {
        if (mostrarDebug)
            Debug.Log("Ciclo completo de imagenes reproducido!");
    }
}