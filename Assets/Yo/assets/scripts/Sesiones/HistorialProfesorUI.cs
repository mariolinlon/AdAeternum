using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Firestore;

public class HistorialProfesorUI : MonoBehaviour
{
    [Header("Panel 1 - Lista de Sesiones")]
    [SerializeField] private GameObject panelListaSesiones;
    [SerializeField] private Transform contenedorSesiones;
    [SerializeField] private GameObject prefabBotonSesion;
    [Tooltip("Botón externo (boton/historial) que se habilita cuando el usuario selecciona una sesión de la lista.")]
    [SerializeField] private Button botonHistorial;

    [Header("Panel 2 - Detalle de Sesion")]
    [SerializeField] private GameObject panelDetalleSesion;
    [SerializeField] private TextMeshProUGUI textoTituloSesion;
    [SerializeField] private TextMeshProUGUI textoNumAlumnos;
    [SerializeField] private TextMeshProUGUI textoPrecisionMedia;
    [SerializeField] private TextMeshProUGUI textoPuntuacionMedia;
    [SerializeField] private TextMeshProUGUI textoTiempoMedio;
    [SerializeField] private TextMeshProUGUI textoMejorAlumno;
    [SerializeField] private TextMeshProUGUI textoDistribucionRangos;
    [SerializeField] private Transform contenedorAlumnos;
    [SerializeField] private GameObject prefabBotonAlumno;

    [Header("Panel 3 - Detalle de Alumno")]
    [SerializeField] private GameObject panelDetalleAlumno;
    [SerializeField] private TextMeshProUGUI textoNombreAlumno;
    [SerializeField] private TextMeshProUGUI textoFlotaAlumno;
    [SerializeField] private TextMeshProUGUI textoRangoAlumno;
    [SerializeField] private TextMeshProUGUI textoPuntuacionAlumno;
    [SerializeField] private TextMeshProUGUI textoPrecisionAlumno;
    [SerializeField] private TextMeshProUGUI textoRachaAlumno;
    [SerializeField] private TextMeshProUGUI textoTiempoAlumno;

    [Header("Navegador de Preguntas")]
    [SerializeField] private TextMeshProUGUI textoIndicePregunta;
    [SerializeField] private TextMeshProUGUI textoEnunciadoPregunta;
    [SerializeField] private TextMeshProUGUI textoResultadoPregunta;
    [SerializeField] private TextMeshProUGUI textoTiempoPregunta;
    [SerializeField] private Button botonPreguntaAnterior;
    [SerializeField] private Button botonPreguntaSiguiente;

    [Header("Panel Medallas")]
    [SerializeField] private GameObject panelMedallas;
    [SerializeField] private Transform contenedorMedallas;
    [SerializeField] private GameObject prefabMedalla;
    [SerializeField] private TextMeshProUGUI textoEstadoMedalla;
    [Tooltip("Botón que abre/cierra el panel de medallas. Arrástralo aquí para que suene ClickAvanzar al abrir y ClickRetroceder al cerrar.")]
    [SerializeField] private Button botonToggleMedallas;

    [Header("Sprites de Medallas")]
    [Tooltip("Arrastra aqui los sprites en orden: Matematicas, Ciencias, Lenguajes, Historia")]
    [SerializeField] private Sprite[] spritesMedallas;

    // Categorias en orden fijo (mismo orden que spritesMedallas)
    private static readonly string[] MedallaIds     = { "matematicas", "ciencias", "lenguajes", "historia" };
    private static readonly string[] MedallaNombres = { "Matematicas", "Ciencias", "Lenguajes", "Historia" };

    private static readonly Color ColorNormal      = new Color(0.15f, 0.15f, 0.25f, 0.90f);
    private static readonly Color ColorSeleccionada = new Color(0.10f, 0.55f, 0.10f, 1.00f);

    private List<Dictionary<string, object>> todosLosRegistros = new List<Dictionary<string, object>>();
    private List<Dictionary<string, object>> preguntasAlumnoActual = new List<Dictionary<string, object>>();
    private int indicePregunta = 0;
    private string idAlumnoDetalle = "";

    // Estado radio-button medallas
    private GameObject _medallaSeleccionadaGO    = null;
    private string     _medallaSeleccionadaId    = "";
    private string     _medallaNombreSeleccionada = "";

    // ID del documento Firestore del registro actualmente visible
    private string _docIdRegistroActual = "";

    // ── Inicializacion ───────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (AulaDataManager.Instance != null && !string.IsNullOrEmpty(AulaDataManager.Instance.GetCodigoAula()))
            IniciarHistorial();
    }

    private IEnumerator Start()
    {
        // Excluimos el botón "Otorgar reconocimientos" del auto-detect del
        // AudioManagerScene, porque su sonido depende de si abre o cierra el
        // panel (avanzar/retroceder). El SFX se reproduce manualmente desde
        // ClickToggleMedallas.
        while (AudioManagerScene.Instance == null) yield return null;
        if (botonToggleMedallas != null)
            AudioManagerScene.Instance.ExcluirBotonDeAutoDetect(botonToggleMedallas);
    }

    public void IniciarHistorial()
    {
        AulaDataManager.Instance?.ObtenerHistorial(registros =>
        {
            todosLosRegistros = registros;
            CargarListaSesiones();
        });
    }

    // ── Panel 1: Lista de sesiones ───────────────────────────────────────────

    private void CargarListaSesiones()
    {
        foreach (Transform hijo in contenedorSesiones) Destroy(hijo.gameObject);

        // Reset: al recargar la lista no hay sesión seleccionada → ocultar el botón externo.
        if (botonHistorial != null) botonHistorial.gameObject.SetActive(false);

        var sesiones = todosLosRegistros
            .Where(r => r.ContainsKey("idSesion") && !string.IsNullOrEmpty(r["idSesion"].ToString()))
            .GroupBy(r => r["idSesion"].ToString())
            .OrderByDescending(g => ObtenerTimestamp(g.First()))
            .ToList();

        foreach (var grupo in sesiones)
        {
            var primero     = grupo.First();
            string planeta  = primero.ContainsKey("nombrePlaneta") ? primero["nombrePlaneta"].ToString() : "?";
            string fecha    = FormatearFecha(primero);
            string idSesion = grupo.Key;

            GameObject item = Instantiate(prefabBotonSesion, contenedorSesiones);
            TextMeshProUGUI tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"{planeta}  -  {fecha}";

            Button btn = item.GetComponentInChildren<Button>();
            if (btn != null)
                btn.onClick.AddListener(() =>
                {
                    // Al seleccionar una sesión, mostrar el botón externo
                    if (botonHistorial != null) botonHistorial.gameObject.SetActive(true);
                    AbrirDetalleSesion(idSesion);
                });
        }
    }

    // ── Panel 2: Detalle de sesion ───────────────────────────────────────────

    private void AbrirDetalleSesion(string idSesion)
    {
        panelListaSesiones?.SetActive(false);
        panelDetalleSesion?.SetActive(true);

        var registros = todosLosRegistros
            .Where(r => r.ContainsKey("idSesion") && r["idSesion"].ToString() == idSesion)
            .ToList();

        var primero    = registros.First();
        string planeta = primero.ContainsKey("nombrePlaneta") ? primero["nombrePlaneta"].ToString() : "?";
        string fecha   = FormatearFecha(primero);

        if (textoTituloSesion     != null) textoTituloSesion.text     = $"{planeta}  -  {fecha}";
        if (textoNumAlumnos       != null) textoNumAlumnos.text       = $"Participantes: {registros.Count}";
        if (textoPrecisionMedia   != null) textoPrecisionMedia.text   = $"Precision media: {registros.Average(r => ObtenerFloat(r, "precision")):F1}%";
        if (textoPuntuacionMedia  != null) textoPuntuacionMedia.text  = $"Puntuacion media: {registros.Average(r => ObtenerFloat(r, "puntos")):F0}";
        if (textoTiempoMedio      != null) textoTiempoMedio.text      = $"Tiempo medio: {FormatearTiempo(registros.Average(r => ObtenerFloat(r, "tiempoTotal")))}";

        var mejor = registros.OrderByDescending(r => ObtenerFloat(r, "puntos")).First();
        if (textoMejorAlumno != null)
            textoMejorAlumno.text = $"Mejor alumno: {mejor["nombreAlumno"]}  ({ObtenerFloat(mejor, "puntos"):F0} pts)";

        int oro    = registros.Count(r => r.ContainsKey("rango") && r["rango"].ToString() == "Oro");
        int plata  = registros.Count(r => r.ContainsKey("rango") && r["rango"].ToString() == "Plata");
        int bronce = registros.Count(r => r.ContainsKey("rango") && r["rango"].ToString() == "Bronce");
        if (textoDistribucionRangos != null)
            textoDistribucionRangos.text = $"Oro: {oro}  |  Plata: {plata}  |  Bronce: {bronce}";

        foreach (Transform hijo in contenedorAlumnos) Destroy(hijo.gameObject);

        foreach (var reg in registros.OrderByDescending(r => ObtenerFloat(r, "puntos")))
        {
            string nombre = reg.ContainsKey("nombreAlumno") ? reg["nombreAlumno"].ToString() : "?";
            string rango  = reg.ContainsKey("rango")        ? reg["rango"].ToString()        : "?";
            float  puntos = ObtenerFloat(reg, "puntos");

            GameObject item = Instantiate(prefabBotonAlumno, contenedorAlumnos);
            TextMeshProUGUI tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"{nombre}  -  {puntos:F0} pts  -  {rango}";

            var r = reg;
            Button btn = item.GetComponentInChildren<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => AbrirDetalleAlumno(r));
        }
    }

    public void ClickVolverAListaSesiones()
    {
        panelDetalleSesion?.SetActive(false);
        panelListaSesiones?.SetActive(true);
    }

    // ── Panel 3: Detalle de alumno ───────────────────────────────────────────

    private void AbrirDetalleAlumno(Dictionary<string, object> registro)
    {
        panelDetalleAlumno?.SetActive(true);
        panelMedallas?.SetActive(false);
        idAlumnoDetalle      = registro.ContainsKey("idAlumno") ? registro["idAlumno"].ToString() : "";
        _docIdRegistroActual = registro.ContainsKey("_docId")   ? registro["_docId"].ToString()   : "";

        // Mostrar medalla ya asignada si existe
        if (textoEstadoMedalla != null)
        {
            if (registro.ContainsKey("medallaAsignada") && !string.IsNullOrEmpty(registro["medallaAsignada"].ToString()))
                textoEstadoMedalla.text = $"Medalla asignada: {registro["medallaAsignada"]}";
            else
                textoEstadoMedalla.text = "";
        }

        string nombre  = registro.ContainsKey("nombreAlumno") ? registro["nombreAlumno"].ToString() : "?";
        string flota   = registro.ContainsKey("nombreFlota")  ? registro["nombreFlota"].ToString()  : "Sin nave";
        string rango   = registro.ContainsKey("rango")        ? registro["rango"].ToString()        : "?";

        if (textoNombreAlumno     != null) textoNombreAlumno.text     = nombre;
        if (textoFlotaAlumno      != null) textoFlotaAlumno.text      = $"Nave: {flota}";
        if (textoRangoAlumno      != null) textoRangoAlumno.text      = $"Rango: {rango}";
        if (textoPuntuacionAlumno != null) textoPuntuacionAlumno.text = $"Puntuacion: {ObtenerFloat(registro, "puntos"):F0}";
        if (textoPrecisionAlumno  != null) textoPrecisionAlumno.text  = $"Precision: {ObtenerFloat(registro, "precision"):F1}%  ({(int)ObtenerFloat(registro, "aciertos")}/{(int)ObtenerFloat(registro, "total")})";
        if (textoRachaAlumno      != null) textoRachaAlumno.text      = $"Racha maxima: {(int)ObtenerFloat(registro, "racha")}";
        if (textoTiempoAlumno     != null) textoTiempoAlumno.text     = $"Tiempo total: {FormatearTiempo(ObtenerFloat(registro, "tiempoTotal"))}  |  Medio: {ObtenerFloat(registro, "tiempoMedio"):F1}s";

        preguntasAlumnoActual.Clear();
        if (registro.ContainsKey("detallePreguntas") && registro["detallePreguntas"] is List<object> raw)
            foreach (var item in raw)
                if (item is Dictionary<string, object> d) preguntasAlumnoActual.Add(d);

        indicePregunta = 0;
        MostrarPregunta();
    }

    public void ClickCerrarDetalleAlumno()
    {
        panelDetalleAlumno?.SetActive(false);
        panelMedallas?.SetActive(false);
    }

    // ── Panel Medallas (ScrollView Grid, radio-button) ───────────────────────

    public void ClickToggleMedallas()
    {
        if (panelMedallas == null) return;
        bool abrir = !panelMedallas.activeSelf;
        // Sonido condicional: abrir → avanzar, cerrar → retroceder.
        AudioManager.PlaySFX(abrir
            ? AudioManager.SFX.ClickAvanzar
            : AudioManager.SFX.ClickRetroceder);
        panelMedallas.SetActive(abrir);
        if (abrir) PoblarMedallas();
    }

    private void PoblarMedallas()
    {
        if (contenedorMedallas == null || prefabMedalla == null) return;
        foreach (Transform hijo in contenedorMedallas) Destroy(hijo.gameObject);

        _medallaSeleccionadaGO      = null;
        _medallaSeleccionadaId      = "";
        _medallaNombreSeleccionada  = "";

        for (int i = 0; i < MedallaIds.Length; i++)
        {
            string id     = MedallaIds[i];
            string nombre = MedallaNombres[i];
            Sprite sprite = (spritesMedallas != null && i < spritesMedallas.Length) ? spritesMedallas[i] : null;

            GameObject item = Instantiate(prefabMedalla, contenedorMedallas);

            // Sprite de la medalla en el hijo "MedalImage"
            Transform imgChild = item.transform.Find("MedalImage");
            if (imgChild != null)
            {
                Image medalImg = imgChild.GetComponent<Image>();
                if (medalImg != null)
                {
                    if (sprite != null) medalImg.sprite = sprite;
                    else                medalImg.color  = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }

            // Etiqueta del nombre
            TextMeshProUGUI tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = nombre;

            // Color base del boton
            Image bgImg = item.GetComponent<Image>();
            if (bgImg != null) bgImg.color = ColorNormal;

            // Click: seleccion radio-button + otorgar
            Button btn = item.GetComponent<Button>() ?? item.AddComponent<Button>();
            btn.onClick.AddListener(() => SeleccionarYOtorgarMedalla(item, id, nombre));
            // Registramos el botón en el AudioManagerScene de inmediato (sin
            // esperar al escaneo de 1s) para que suene ClickAvanzar al instante.
            AudioManagerScene.Instance?.RegistrarBoton(btn);
        }
    }

    private void SeleccionarYOtorgarMedalla(GameObject item, string id, string nombre)
    {
        // Deseleccionar la anterior
        if (_medallaSeleccionadaGO != null)
        {
            Image prevBg = _medallaSeleccionadaGO.GetComponent<Image>();
            if (prevBg != null) prevBg.color = ColorNormal;
        }

        // Seleccionar la nueva
        _medallaSeleccionadaGO     = item;
        _medallaSeleccionadaId     = id;
        _medallaNombreSeleccionada = nombre;

        Image bg = item.GetComponent<Image>();
        if (bg != null) bg.color = ColorSeleccionada;

        // Otorgar reconocimiento
        OtorgarMedallaCategoria(id, nombre);
    }

    private void OtorgarMedallaCategoria(string id, string nombre)
    {
        if (string.IsNullOrEmpty(_docIdRegistroActual)) return;
        AulaDataManager.Instance.OtorgarMedallaCategoria(_docIdRegistroActual, id);
        panelMedallas?.SetActive(false);
        _medallaSeleccionadaGO = null;
        if (textoEstadoMedalla != null)
            textoEstadoMedalla.text = $"Medalla asignada: {id}";
    }

    // ── Navegador de preguntas ───────────────────────────────────────────────

    private void MostrarPregunta()
    {
        if (preguntasAlumnoActual.Count == 0) return;

        var p         = preguntasAlumnoActual[indicePregunta];
        bool correcto = p.ContainsKey("correcto") && (bool)p["correcto"];
        string enunc  = p.ContainsKey("enunciado")       ? p["enunciado"].ToString()                             : "?";
        float  tiempo = p.ContainsKey("tiempoRespuesta") ? System.Convert.ToSingle(p["tiempoRespuesta"])         : 0f;

        if (textoIndicePregunta    != null) textoIndicePregunta.text    = $"{indicePregunta + 1} / {preguntasAlumnoActual.Count}";
        if (textoEnunciadoPregunta != null) textoEnunciadoPregunta.text = enunc;
        if (textoResultadoPregunta != null) textoResultadoPregunta.text = correcto ? "<color=green>Correcta</color>" : "<color=red>Incorrecta</color>";
        if (textoTiempoPregunta    != null) textoTiempoPregunta.text    = $"{tiempo:F1}s";

        if (botonPreguntaAnterior  != null) botonPreguntaAnterior.interactable  = indicePregunta > 0;
        if (botonPreguntaSiguiente != null) botonPreguntaSiguiente.interactable = indicePregunta < preguntasAlumnoActual.Count - 1;
    }

    public void ClickPreguntaAnterior()  { if (indicePregunta > 0)                                    { indicePregunta--; MostrarPregunta(); } }
    public void ClickPreguntaSiguiente() { if (indicePregunta < preguntasAlumnoActual.Count - 1) { indicePregunta++; MostrarPregunta(); } }

    // ── Utilidades ───────────────────────────────────────────────────────────

    private float ObtenerFloat(Dictionary<string, object> d, string key) =>
        d.ContainsKey(key) ? System.Convert.ToSingle(d[key]) : 0f;

    private System.DateTime ObtenerTimestamp(Dictionary<string, object> d) =>
        d.ContainsKey("timestamp") && d["timestamp"] is Timestamp ts ? ts.ToDateTime() : System.DateTime.MinValue;

    private string FormatearFecha(Dictionary<string, object> d)
    {
        var dt = ObtenerTimestamp(d);
        return dt == System.DateTime.MinValue ? "Sin fecha" : dt.ToLocalTime().ToString("dd/MM/yyyy  HH:mm");
    }

    private string FormatearTiempo(float segundos)
    {
        int min = Mathf.FloorToInt(segundos / 60f);
        int seg = Mathf.FloorToInt(segundos % 60f);
        return min > 0 ? $"{min}m {seg}s" : $"{seg}s";
    }
}
