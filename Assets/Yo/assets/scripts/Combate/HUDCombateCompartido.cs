using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cabecera compartida del combate (visible para atacantes y defensores):
///   - Barra de escudo
///   - Barra de vida de la nave
///   - Lista de zonas con vida
/// </summary>
public class HUDCombateCompartido : MonoBehaviour
{
    [Header("Escudo")]
    [SerializeField] private Slider sliderEscudo;
    [SerializeField] private TextMeshProUGUI textoEscudo;
    [SerializeField] private Image marcadorEscudoMinimo; // pequeña marca vertical en el slider

    [Header("Nave")]
    [SerializeField] private Slider sliderNave;
    [SerializeField] private TextMeshProUGUI textoNave;
    [SerializeField] private Image naveFillImage;

    [Header("Zonas")]
    [SerializeField] private Transform contenedorZonas;
    [SerializeField] private GameObject prefabFilaZona; // con FilaZonaHUDCompartido

    private readonly List<FilaZonaHUDCompartido> filas = new List<FilaZonaHUDCompartido>();

    public void Mostrar() { gameObject.SetActive(true); }
    public void Ocultar() { gameObject.SetActive(false); }

    public void RefrescarEstado(EstadoFlotaCombate estado)
    {
        if (estado == null) return;

        // Escudo
        if (sliderEscudo != null)
        {
            sliderEscudo.maxValue = Mathf.Max(1f, estado.escudoMaximo);
            sliderEscudo.value = Mathf.Clamp(estado.escudoActual, 0f, sliderEscudo.maxValue);
        }
        if (textoEscudo != null)
        {
            string sufijo = estado.EscudoCaido ? "  [CAÍDO]" : "";
            textoEscudo.text = $"Escudo {Mathf.RoundToInt(estado.escudoActual)}/{Mathf.RoundToInt(estado.escudoMaximo)}{sufijo}";
        }
        if (marcadorEscudoMinimo != null && sliderEscudo != null && sliderEscudo.maxValue > 0f)
        {
            // Posiciona la marca del mínimo según porcentaje
            float pct = Mathf.Clamp01(estado.escudoMinimo / sliderEscudo.maxValue);
            RectTransform rtMarker = marcadorEscudoMinimo.rectTransform;
            RectTransform rtSlider = sliderEscudo.GetComponent<RectTransform>();
            if (rtSlider != null)
            {
                Vector2 anchorMin = rtMarker.anchorMin;
                Vector2 anchorMax = rtMarker.anchorMax;
                anchorMin.x = pct;
                anchorMax.x = pct;
                rtMarker.anchorMin = anchorMin;
                rtMarker.anchorMax = anchorMax;
                rtMarker.anchoredPosition = new Vector2(0f, rtMarker.anchoredPosition.y);
            }
        }

        // Nave
        if (sliderNave != null)
        {
            sliderNave.maxValue = Mathf.Max(1f, estado.vidaNaveMaxima);
            sliderNave.value = Mathf.Clamp(estado.vidaNave, 0f, sliderNave.maxValue);
        }
        if (textoNave != null) textoNave.text = $"Nave {Mathf.RoundToInt(estado.vidaNave)}/{Mathf.RoundToInt(estado.vidaNaveMaxima)}";
        if (naveFillImage != null)
        {
            float pct = estado.vidaNaveMaxima > 0f ? estado.vidaNave / estado.vidaNaveMaxima : 0f;
            naveFillImage.color = pct < 0.25f ? new Color(0.9f, 0.15f, 0.15f)
                                : pct < 0.6f ? new Color(0.95f, 0.7f, 0.2f)
                                              : new Color(0.25f, 0.75f, 0.35f);
        }

        // Zonas (recreamos si la cantidad cambia, si no, refrescamos in-place)
        SincronizarFilasZonas(estado.zonas);
    }

    private void SincronizarFilasZonas(List<ZonaPlaneta> zonas)
    {
        if (contenedorZonas == null || prefabFilaZona == null) return;
        if (zonas == null) zonas = new List<ZonaPlaneta>();

        // Ajustar nº de filas
        while (filas.Count < zonas.Count)
        {
            GameObject go = Instantiate(prefabFilaZona, contenedorZonas);
            FilaZonaHUDCompartido f = go.GetComponent<FilaZonaHUDCompartido>();
            if (f != null) filas.Add(f);
        }
        while (filas.Count > zonas.Count)
        {
            int last = filas.Count - 1;
            if (filas[last] != null) Destroy(filas[last].gameObject);
            filas.RemoveAt(last);
        }

        for (int i = 0; i < zonas.Count; i++)
        {
            if (filas[i] != null) filas[i].Actualizar(zonas[i].nombre, zonas[i].vidaActual, zonas[i].vidaMaxima);
        }
    }
}
