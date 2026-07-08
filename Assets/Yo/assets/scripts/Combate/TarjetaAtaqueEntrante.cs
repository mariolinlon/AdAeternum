using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tarjeta de un ataque entrante (vista del defensor):
///   [Tipo Normal/Agravado] [Tiempo restante] [Botón Defender]
/// </summary>
public class TarjetaAtaqueEntrante : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoTipo;
    [SerializeField] private TextMeshProUGUI textoTimer;
    [SerializeField] private Image fondoImage;
    [SerializeField] private Button botonDefender;

    private string idAtaque;
    private Action<string> onDefender;

    public string IdAtaque => idAtaque;

    public void Configurar(AtaqueEntrante ataque, float dañoEsperado, Action<string> onDefender)
    {
        if (ataque == null) return;
        idAtaque = ataque.id;
        this.onDefender = onDefender;

        string tipoStr = ataque.tipo == TipoAtaqueEntrante.Agravado ? "AGRAVADO" : "Normal";
        if (textoTipo != null)
            textoTipo.text = dañoEsperado > 0f
                ? $"{tipoStr}  ({Mathf.RoundToInt(dañoEsperado)} daño)"
                : tipoStr;
        if (fondoImage != null)
            fondoImage.color = ataque.tipo == TipoAtaqueEntrante.Agravado
                ? new Color(0.7f, 0.1f, 0.1f)
                : new Color(0.7f, 0.45f, 0.1f);

        ActualizarTimer(ataque.tiempoRestante);

        // El botón Defender ya no se usa: los ataques se gestionan automáticamente
        // (impactan contra el escudo, si lo hay; si no, dañan la nave).
        // El defensor solo recarga el escudo respondiendo preguntas cíclicas.
        if (botonDefender != null) botonDefender.gameObject.SetActive(false);
    }

    public void ActualizarTimer(float tiempoRestante)
    {
        if (textoTimer != null) textoTimer.text = Mathf.Max(0f, tiempoRestante).ToString("F1") + "s";
    }
}
