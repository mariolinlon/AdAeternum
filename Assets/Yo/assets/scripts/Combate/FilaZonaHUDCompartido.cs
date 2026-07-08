using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Visualiza una zona en la cabecera compartida del combate:
/// [Nombre] [Slider de vida] [Texto X/Y]
/// </summary>
public class FilaZonaHUDCompartido : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private Slider sliderVida;
    [SerializeField] private TextMeshProUGUI textoVida;
    [SerializeField] private Image fillImage; // imagen del fill del slider, para tintar destrozado

    public void Actualizar(string nombre, float vidaActual, float vidaMaxima)
    {
        if (textoNombre != null) textoNombre.text = nombre;
        if (sliderVida != null)
        {
            sliderVida.maxValue = Mathf.Max(1f, vidaMaxima);
            sliderVida.value = Mathf.Clamp(vidaActual, 0f, sliderVida.maxValue);
        }
        if (textoVida != null) textoVida.text = $"{Mathf.RoundToInt(vidaActual)}/{Mathf.RoundToInt(vidaMaxima)}";
        if (fillImage != null)
        {
            bool destruida = vidaActual <= 0f;
            fillImage.color = destruida ? new Color(0.3f, 0.3f, 0.3f)
                                        : new Color(0.85f, 0.30f, 0.30f); // rojo (zonas son enemigas)
        }
    }
}
