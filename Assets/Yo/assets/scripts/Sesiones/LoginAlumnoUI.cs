using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class LoginAlumnoUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_InputField inputCodigoAula;
    [SerializeField] private TMP_InputField inputNombreAlumno;
    [SerializeField] private Button botonEntrar;
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelJuego;
    [SerializeField] private GameObject pantallaInicioJugador;
    [SerializeField] private TextMeshProUGUI textoError;
    [SerializeField] private PanelMensajesAlumno panelMensajesAlumno;
    [SerializeField] private PerfilAlumnoUI perfilAlumnoUI;
    [SerializeField] private ProgresoGlobalUI progresoGlobalUI;
    [SerializeField] private PantallaFlotaAlumnoUI pantallaFlotaAlumnoUI;
    [SerializeField] private PantallaBriefing pantallaBriefing;
    [SerializeField] private SistemaCombateAlumno sistemaCombateAlumno;

    public void BotonEntrarAlAula()
    {
        string codigo = inputCodigoAula != null ? inputCodigoAula.text.Trim() : "";
        string nombre = inputNombreAlumno != null ? inputNombreAlumno.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
        {
            if (textoError != null) textoError.text = "Introduce el código y tu nombre.";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
            return;
        }

        if (nombre.Contains(" "))
        {
            if (textoError != null) textoError.text = "El nombre no puede contener espacios.";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
            return;
        }

        if (nombre.Length < 2)
        {
            if (textoError != null) textoError.text = "El nombre debe tener al menos 2 caracteres.";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
            return;
        }

        if (botonEntrar != null) botonEntrar.interactable = false;
        if (textoError != null) textoError.text = "Verificando...";

        // OJO: el SFX de Login se reproduce SOLO si entra al juego con éxito
        // (al final del callback, dentro de EntrarAlJuego). Si hay error,
        // se reproduce ToastError en su lugar. El click en sí ya suena por el
        // AudioManagerScene (ClickAvanzar auto-detectado).

        AulaDataManager.Instance.ValidarYEntrarAulaAlumno(codigo, (existe) =>
        {
            if (!existe)
            {
                if (textoError != null) textoError.text = "Código de aula incorrecto.";
                AudioManager.PlaySFX(AudioManager.SFX.ToastError);
                if (botonEntrar != null) botonEntrar.interactable = true;
                return;
            }

            AulaDataManager.Instance.RegistrarEstudianteEnNube(nombre, (registrado) =>
            {
                if (!registrado)
                {
                    if (textoError != null) textoError.text = "Error al registrarse en el aula.";
                    AudioManager.PlaySFX(AudioManager.SFX.ToastError);
                    if (botonEntrar != null) botonEntrar.interactable = true;
                    return;
                }

                StartCoroutine(EntrarAlJuego());
            });
        });
    }

    private IEnumerator EntrarAlJuego()
    {
        yield return null; // salir del callback de Firebase antes de activar paneles

        // SFX de Login: solo cuando se entra al juego con éxito.
        AudioManager.PlaySFX(AudioManager.SFX.Login);

        if (textoError != null) textoError.text = "";
        if (panelLogin != null) panelLogin.SetActive(false);
        if (panelJuego != null) panelJuego.SetActive(true);
        if (pantallaInicioJugador != null) pantallaInicioJugador.SetActive(true);
        panelMensajesAlumno?.IniciarEscucha();
        perfilAlumnoUI?.IniciarPerfil();
        progresoGlobalUI?.IniciarProgreso();
        pantallaFlotaAlumnoUI?.IniciarFlotaAlumno();
        pantallaBriefing?.IniciarBriefing();
        sistemaCombateAlumno?.SuscribirListenerCombate();

        // Música de menú al entrar al juego.
        AudioManager.PlayMusic(AudioManager.Music.Menu);
    }
}
