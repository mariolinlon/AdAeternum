using UnityEngine;
using TMPro;

public class PantallaResultados : MonoBehaviour
{
    [Header("Referencias")]
    public SistemaCombateAlumno sistemaCombate;

    [Header("Panel")]
    public GameObject panelResultados;

    [Header("Básicas")]
    public TextMeshProUGUI textoAciertos;
    public TextMeshProUGUI textoFallos;
    public TextMeshProUGUI textoPorcentaje;
    public TextMeshProUGUI textoPuntuacion;
    public TextMeshProUGUI textoRango;

    [Header("Progresión")]
    public TextMeshProUGUI textoRachaMaxima;

    [Header("Tiempo")]
    public TextMeshProUGUI textoTiempoTotal;
    public TextMeshProUGUI textoTiempoMedio;

    private void OnEnable()
    {
        sistemaCombate?.AbrirPantallaResultados();
    }

    public void MostrarResultados(int aciertos, int fallos, int total, float porcentaje,
        int rachaMaxima, float tiempoTotal, float tiempoMedio, int puntuacion, string rango)
    {

        if (textoAciertos != null)   textoAciertos.text   = $"Aciertos: {aciertos} / {total}";
        if (textoFallos != null)     textoFallos.text     = $"Fallos: {fallos}";
        if (textoPorcentaje != null) textoPorcentaje.text = $"Precisión: {porcentaje:F1}%";
        if (textoPuntuacion != null) textoPuntuacion.text = $"Puntuación: {puntuacion}";
        if (textoRango != null)      textoRango.text      = $"Rango: {rango}";
        if (textoRachaMaxima != null) textoRachaMaxima.text = $"Racha máxima: {rachaMaxima}";
        if (textoTiempoTotal != null) textoTiempoTotal.text = $"Tiempo total: {FormatearTiempo(tiempoTotal)}";
        if (textoTiempoMedio != null) textoTiempoMedio.text = $"Tiempo medio/pregunta: {tiempoMedio:F1}s";
    }

    private string FormatearTiempo(float segundos)
    {
        int min = Mathf.FloorToInt(segundos / 60f);
        int seg = Mathf.FloorToInt(segundos % 60f);
        return min > 0 ? $"{min}m {seg}s" : $"{seg}s";
    }
}
