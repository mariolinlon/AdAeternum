using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PanelMensajesProfesor : MonoBehaviour
{
    [Header("Envío")]
    [SerializeField] private TMP_InputField inputMensaje;
    [SerializeField] private Button botonEnviar;
    [SerializeField] private TextMeshProUGUI textoEstado;

    private IEnumerator Start()
    {
        // El botón "Enviar mensaje" no suena con el ClickAvanzar genérico,
        // sino con el SFX de Login (más impactante, ya que es una acción que
        // afecta a todos los alumnos).
        while (AudioManagerScene.Instance == null) yield return null;
        if (botonEnviar != null)
            AudioManagerScene.Instance.RegistrarBotonConSonido(botonEnviar, "login");
    }

    public void ClickEnviar()
    {
        if (inputMensaje == null || string.IsNullOrWhiteSpace(inputMensaje.text)) return;

        string texto = inputMensaje.text.Trim();
        if (botonEnviar != null) botonEnviar.interactable = false;

        AulaDataManager.Instance.EnviarMensaje(texto);

        inputMensaje.text = "";
        if (textoEstado != null) textoEstado.text = "Comunicado enviado.";
        if (botonEnviar != null) botonEnviar.interactable = true;
    }
}
