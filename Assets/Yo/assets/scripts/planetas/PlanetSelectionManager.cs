using UnityEngine;
using TMPro;
using System.Collections;

public class PlanetSelectionManager : MonoBehaviour
{
    public static PlanetSelectionManager Instance { get; private set; }

    [Header("UI de visualización")]
    [SerializeField] private TextMeshProUGUI textoNombrePlaneta;
    [SerializeField] private TextMeshProUGUI textoDescripcionPlaneta;

    [Header("UI de edición")]
    [SerializeField] private TMP_InputField inputNombrePlaneta;
    [SerializeField] private TMP_InputField inputDescripcionPlaneta;

    [Header("Botones de Acción (Se activan al seleccionar)")]
    [SerializeField] private GameObject[] botonesDeAccion;

    [Header("Niebla de Guerra")]
    [SerializeField] private TextMeshProUGUI textoEstadoNiebla;

    private PlanetSelectable planetaSeleccionado;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Al empezar, nos aseguramos de que los botones estén apagados
        DesactivarBotonesAccion();
    }

    public void Seleccionar(PlanetSelectable nuevoPlaneta)
    {
        if (nuevoPlaneta == null)
            return;

        bool esCambio = (planetaSeleccionado != nuevoPlaneta);

        if (planetaSeleccionado != null && esCambio)
        {
            planetaSeleccionado.DeseleccionarVisual();
        }

        planetaSeleccionado = nuevoPlaneta;
        planetaSeleccionado.SeleccionarVisual();

        // SFX al seleccionar un planeta (la selección viene por OnMouseDown sobre el
        // collider, no por un Button UI, así que el AudioManagerScene no la capta).
        // Solo suena cuando realmente cambias de planeta, no si vuelves a clicar el mismo.
        if (esCambio)
            AudioManager.PlaySFX(AudioManager.SFX.ClickAvanzar);

        // ACTIVAR BOTONES
        ActivarBotonesAccion();

        ActualizarTextosPlaneta();
        ActualizarTextoNiebla();

        // El profesor sincroniza el planeta seleccionado a Firebase para que el alumno
        // pueda mostrarlo en su pantalla de espera/briefing.
        if (AulaDataManager.Instance != null && !AulaDataManager.Instance.EsAlumno)
            AulaDataManager.Instance.SetPlanetaSeleccionadoProfesor(planetaSeleccionado.IdUnico);
    }

    public void ClickToggleNiebla()
    {
        if (planetaSeleccionado == null) return;
        bool nuevoEstado = !planetaSeleccionado.Bloqueado;
        if (nuevoEstado) planetaSeleccionado.Bloquear();
        else             planetaSeleccionado.Desbloquear();
        AulaDataManager.Instance?.SetBloqueadoPlaneta(planetaSeleccionado.IdUnico, nuevoEstado);
        ActualizarTextoNiebla();
    }

    private void ActualizarTextoNiebla()
    {
        if (textoEstadoNiebla == null || planetaSeleccionado == null) return;
        textoEstadoNiebla.text = planetaSeleccionado.Bloqueado ? "Bloqueado" : "Visible";
    }

    private void ActivarBotonesAccion()
    {
        foreach (GameObject boton in botonesDeAccion)
        {
            if (boton != null) boton.SetActive(true);
        }
    }

    private void DesactivarBotonesAccion()
    {
        foreach (GameObject boton in botonesDeAccion)
        {
            if (boton != null) boton.SetActive(false);
        }
    }

    public PlanetSelectable ObtenerPlanetaActual()
    {
        return planetaSeleccionado;
    }

    public void ActualizarTextosPlaneta()
    {
        if (planetaSeleccionado == null)
        {
            if (textoNombrePlaneta != null) textoNombrePlaneta.text = "";
            if (textoDescripcionPlaneta != null) textoDescripcionPlaneta.text = "";
            DesactivarBotonesAccion();
            return;
        }

        if (textoNombrePlaneta != null)
            textoNombrePlaneta.text = planetaSeleccionado.NombrePlaneta;

        if (textoDescripcionPlaneta != null)
            textoDescripcionPlaneta.text = planetaSeleccionado.DescripcionPlaneta;

        // También rellenar los InputFields con los valores actuales, para que al
        // guardar uno de los dos campos no se sobreescriba el otro con string vacío.
        if (inputNombrePlaneta != null)
            inputNombrePlaneta.text = planetaSeleccionado.NombrePlaneta;

        if (inputDescripcionPlaneta != null)
            inputDescripcionPlaneta.text = planetaSeleccionado.DescripcionPlaneta;
    }

    // ... (El resto de funciones como CargarDatosEnInputs, Guardar, etc., se mantienen igual)
    
    public void GuardarDatosDesdeInputFields()
    {
        if (planetaSeleccionado == null)
        {
            Toast.Show("Selecciona un planeta primero.", 3f, Toast.Tipo.Aviso);
            return;
        }

        if (inputNombrePlaneta != null)
            planetaSeleccionado.CambiarNombre(inputNombrePlaneta.text);

        if (inputDescripcionPlaneta != null)
            planetaSeleccionado.CambiarDescripcion(inputDescripcionPlaneta.text);

        ActualizarTextosPlaneta();
        AulaDataManager.Instance.GuardarPlanetaEnFirebase(planetaSeleccionado);
        Toast.Show("Planeta guardado.", 2f, Toast.Tipo.Exito);
    }

    public void LimpiarUI()
    {
        if (textoNombrePlaneta != null) textoNombrePlaneta.text = "";
        if (textoDescripcionPlaneta != null) textoDescripcionPlaneta.text = "";
        if (inputNombrePlaneta != null) inputNombrePlaneta.text = "";
        if (inputDescripcionPlaneta != null) inputDescripcionPlaneta.text = "";

        planetaSeleccionado = null;
        DesactivarBotonesAccion();
    }

    /// <summary>
    /// Llamado desde el botón "Borrar planeta" en la pantalla de mapa.
    /// Pide confirmación, borra el planeta de Firebase, destruye el GO local y
    /// recoloca los planetas restantes para cerrar el hueco. Todos los clientes
    /// detectan el borrado via listener y se sincronizan.
    /// </summary>
    public void ClickBorrarPlaneta()
    {
        if (planetaSeleccionado == null)
        {
            Toast.Show("Selecciona un planeta primero.", 3f, Toast.Tipo.Aviso);
            return;
        }

        // Solo el profesor puede borrar planetas (los alumnos no tienen permisos
        // de escritura sobre la colección "planetas").
        if (AulaDataManager.Instance != null && AulaDataManager.Instance.EsAlumno)
        {
            Toast.Show("Solo el profesor puede borrar planetas.", 3f, Toast.Tipo.Error);
            return;
        }

        string nombre = planetaSeleccionado.NombrePlaneta;
        string idAEliminar = planetaSeleccionado.IdUnico;

        ConfirmDialog.Show(
            $"¿Borrar el planeta \"{nombre}\"?\nSe perderán sus preguntas y configuración.",
            () =>
            {
                // 1. Borrar el doc + sub-preguntas en Firebase.
                AulaDataManager.Instance?.BorrarPlaneta(idAEliminar);

                // 2. El listener del PlanetSpawner detectará el Type.Removed y
                //    destruirá el GameObject local. Esperamos un par de frames
                //    y luego recolocamos los planetas restantes (el profesor
                //    persiste las nuevas posiciones en Firebase para que los
                //    alumnos reciban la recolocación via listener).
                StartCoroutine(RecolocarTrasBorrado());

                // 3. Feedback al usuario.
                Toast.Show($"Planeta '{nombre}' eliminado.", 2.5f, Toast.Tipo.Exito);

                // 4. Limpiar selección local.
                LimpiarUI();
            },
            textoConfirmar: "Borrar",
            textoCancelar: "Cancelar"
        );
    }

    private IEnumerator RecolocarTrasBorrado()
    {
        // Damos margen al listener de Firebase para procesar el Type.Removed
        // y destruir el GameObject local. Si recolocamos antes de que se
        // destruya, el planeta borrado entraría en el cálculo y dejaría un
        // hueco fantasma.
        yield return new WaitForSecondsRealtime(0.5f);
        PlanetSpawner.Instance?.RecolocarPlanetas();
    }
}