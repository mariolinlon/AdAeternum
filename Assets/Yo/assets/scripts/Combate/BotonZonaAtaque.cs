using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Botón de selección de zona objetivo (vista del atacante).
/// Muestra nombre+vida y se resalta cuando está seleccionado.
/// </summary>
public class BotonZonaAtaque : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI texto;
    [SerializeField] private Image fondoImage;
    [SerializeField] private Button boton;

    private int indiceZona;
    private Action<int> onSeleccionar;

    public int IndiceZona => indiceZona;

    public void Configurar(int indice, string nombre, float vidaActual, float vidaMaxima, bool seleccionado, Action<int> onSeleccionar)
    {
        indiceZona = indice;
        this.onSeleccionar = onSeleccionar;

        bool destruida = vidaActual <= 0f;
        if (texto != null)
        {
            string txt = destruida ? $"<s>{nombre}</s>" : $"{nombre}  {Mathf.RoundToInt(vidaActual)}/{Mathf.RoundToInt(vidaMaxima)}";
            texto.text = txt;
        }
        if (boton != null)
        {
            boton.interactable = !destruida;
            boton.onClick.RemoveAllListeners();
            boton.onClick.AddListener(() => onSeleccionar?.Invoke(indiceZona));
        }
        if (fondoImage != null)
        {
            if (destruida) fondoImage.color = new Color(0.25f, 0.25f, 0.25f);
            else if (seleccionado) fondoImage.color = new Color(0.95f, 0.55f, 0.15f); // naranja resaltado
            else fondoImage.color = new Color(0.5f, 0.2f, 0.2f); // rojo apagado
        }
    }
}
