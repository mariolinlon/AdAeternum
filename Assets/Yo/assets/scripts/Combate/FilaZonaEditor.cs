using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una fila editable del panel de zonas del planeta:
/// [InputField nombre]  [InputField vida (float)]  [Botón X eliminar]
/// </summary>
public class FilaZonaEditor : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputNombre;
    [SerializeField] private TMP_InputField inputVida;
    [SerializeField] private Button botonEliminar;

    private Action<FilaZonaEditor> onCambio;
    private Action<FilaZonaEditor> onEliminar;

    public void Configurar(string nombre, float vidaMaxima,
                           Action<FilaZonaEditor> onCambio,
                           Action<FilaZonaEditor> onEliminar)
    {
        this.onCambio = onCambio;
        this.onEliminar = onEliminar;

        if (inputNombre != null)
        {
            inputNombre.SetTextWithoutNotify(nombre ?? "");
            inputNombre.onValueChanged.RemoveAllListeners();
            inputNombre.onEndEdit.RemoveAllListeners();
            inputNombre.onEndEdit.AddListener(_ => onCambio?.Invoke(this));
        }
        if (inputVida != null)
        {
            inputVida.SetTextWithoutNotify(vidaMaxima.ToString(System.Globalization.CultureInfo.InvariantCulture));
            inputVida.onValueChanged.RemoveAllListeners();
            inputVida.onEndEdit.RemoveAllListeners();
            inputVida.onEndEdit.AddListener(_ => onCambio?.Invoke(this));
        }
        if (botonEliminar != null)
        {
            botonEliminar.onClick.RemoveAllListeners();
            botonEliminar.onClick.AddListener(() => onEliminar?.Invoke(this));
        }
    }

    public string Nombre => inputNombre != null ? inputNombre.text : "";

    public float VidaMaxima
    {
        get
        {
            if (inputVida == null) return 0f;
            if (float.TryParse(inputVida.text, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out float v))
                return Mathf.Max(0f, v);
            return 0f;
        }
    }
}
