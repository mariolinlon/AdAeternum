using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

/// <summary>
/// Componente que se añade al GameObject quizmanager.
/// Rellena los textos del briefing dentro del panelEspera con info del alumno
/// y del planeta que el profesor tenga seleccionado en ese momento.
/// El flujo de combate no se modifica.
/// </summary>
public class PantallaBriefing : MonoBehaviour
{
    [Header("Textos del planeta (rellenados cuando el profesor selecciona uno)")]
    [SerializeField] private TextMeshProUGUI textoNombrePlaneta;
    [SerializeField] private TextMeshProUGUI textoDescripcionPlaneta;
    [SerializeField] private TextMeshProUGUI textoSinPlaneta; // Mensaje cuando aún no hay planeta seleccionado

    [Header("Textos del alumno")]
    [SerializeField] private TextMeshProUGUI textoNombreAlumno;
    [SerializeField] private TextMeshProUGUI textoFlota;
    [SerializeField] private TextMeshProUGUI textoLider;
    [SerializeField] private TextMeshProUGUI textoRolCombate;
    [SerializeField] private TextMeshProUGUI textoNivel;
    [SerializeField] private TextMeshProUGUI textoRango;
    [SerializeField] private TextMeshProUGUI textoMejorPuntuacion;

    [Header("Estrellas de la flota (1-5)")]
    [SerializeField] private Image[] estrellasFlota;
    [SerializeField] private Sprite spriteEstrellaLlena;
    [SerializeField] private Sprite spriteEstrellaVacia;

    [Header("Referencia a CreadorPreguntas (para contar preguntas del planeta)")]
    [SerializeField] private CreadorPreguntas creadorPreguntas;
    [SerializeField] private TextMeshProUGUI textoNumPreguntas;
    [SerializeField] private TextMeshProUGUI textoPuntosMaximos;

    private string idPlanetaActual = "";
    private bool _suscrito = false;
    private Coroutine _esperaPreguntas;

    /// <summary>Llamada desde LoginAlumnoUI.EntrarAlJuego() — patrón del proyecto.</summary>
    public void IniciarBriefing()
    {
        if (AulaDataManager.Instance == null)
        {
            Debug.LogWarning("[PantallaBriefing] AulaDataManager no disponible.");
            return;
        }

        if (!_suscrito)
        {
            // Listener del planeta seleccionado por el profesor
            AulaDataManager.Instance.EscucharPlanetaSeleccionado(OnPlanetaSeleccionadoCambio);
            // Listeners para refrescar info del alumno cuando cambia algo en Firebase
            AulaDataManager.Instance.EscucharAlumnos(RefrescarInfoAlumno);
            AulaDataManager.Instance.EscucharFlotas(RefrescarInfoAlumno);
            _suscrito = true;
        }

        RefrescarInfoAlumno();
    }

    private void OnPlanetaSeleccionadoCambio(string idPlaneta)
    {
        idPlanetaActual = idPlaneta ?? "";
        RefrescarInfoPlaneta();
    }

    private void RefrescarInfoPlaneta()
    {
        if (string.IsNullOrEmpty(idPlanetaActual))
        {
            // No hay planeta seleccionado todavía
            if (textoNombrePlaneta != null)      textoNombrePlaneta.text = "";
            if (textoDescripcionPlaneta != null) textoDescripcionPlaneta.text = "";
            if (textoNumPreguntas != null)       textoNumPreguntas.text = "";
            if (textoPuntosMaximos != null)      textoPuntosMaximos.text = "";
            if (textoMejorPuntuacion != null)    textoMejorPuntuacion.text = "";
            if (textoSinPlaneta != null)         textoSinPlaneta.gameObject.SetActive(true);
            return;
        }

        if (textoSinPlaneta != null) textoSinPlaneta.gameObject.SetActive(false);

        // Buscar el PlanetSelectable correspondiente en escena (todos los clientes los cargan)
        PlanetSelectable[] planetas = FindObjectsByType<PlanetSelectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        PlanetSelectable elPlaneta = planetas.FirstOrDefault(p => p.IdUnico == idPlanetaActual);

        if (elPlaneta != null)
        {
            if (textoNombrePlaneta != null)      textoNombrePlaneta.text = elPlaneta.NombrePlaneta;
            if (textoDescripcionPlaneta != null) textoDescripcionPlaneta.text = elPlaneta.DescripcionPlaneta;
        }

        // Contar preguntas del planeta y puntos máximos.
        // Las preguntas se cargan async desde PlanetSpawner; si aún no están,
        // arrancamos una corutina que reintenta hasta verlas.
        if (_esperaPreguntas != null) StopCoroutine(_esperaPreguntas);
        _esperaPreguntas = StartCoroutine(EsperarYContarPreguntas(idPlanetaActual));

        // Mejor puntuación previa del alumno en este planeta
        if (textoMejorPuntuacion != null)
        {
            string idAlumno = AulaDataManager.Instance.GetIdAlumnoLocal();
            AulaDataManager.Instance.ObtenerMejorPuntuacionPlaneta(idAlumno, idPlanetaActual, mejor =>
            {
                if (textoMejorPuntuacion == null) return;
                textoMejorPuntuacion.text = mejor < 0 ? "Primera vez" : $"Récord: {mejor} pts";
            });
        }
    }

    private void RefrescarInfoAlumno()
    {
        if (AulaDataManager.Instance == null) return;
        string idAlumno = AulaDataManager.Instance.GetIdAlumnoLocal();
        if (string.IsNullOrEmpty(idAlumno)) return;

        var alumno = AulaDataManager.Instance.alumnosDisponibles
            .FirstOrDefault(a => a.ContainsKey("id") && a["id"].ToString() == idAlumno);
        if (alumno == null) return;

        // Nombre
        if (textoNombreAlumno != null && alumno.ContainsKey("nombre"))
            textoNombreAlumno.text = alumno["nombre"].ToString();

        // Rol de combate
        if (textoRolCombate != null)
        {
            string rolComb = alumno.ContainsKey("rolCombate") ? alumno["rolCombate"].ToString() : "";
            if (rolComb == "atacante")      textoRolCombate.text = "[ATK] Atacante";
            else if (rolComb == "defensor") textoRolCombate.text = "[DEF] Defensor";
            else                             textoRolCombate.text = "Sin rol asignado";
        }

        // Nivel y rango
        if (textoNivel != null)
            textoNivel.text = alumno.ContainsKey("nivel") ? "Nivel " + alumno["nivel"] : "Nivel 1";
        if (textoRango != null)
            textoRango.text = alumno.ContainsKey("tituloEquipado") ? alumno["tituloEquipado"].ToString() : "";

        // Flota: nombre + líder + estrellas
        string idFlota = alumno.ContainsKey("idFlota") ? alumno["idFlota"].ToString() : "";
        Flota flota = AulaDataManager.Instance.flotasActivas.FirstOrDefault(f => f.id == idFlota);

        if (textoFlota != null)
            textoFlota.text = flota != null ? flota.nombre : "Sin nave";

        if (textoLider != null && flota != null)
        {
            var lider = AulaDataManager.Instance.alumnosDisponibles
                .FirstOrDefault(a => a.ContainsKey("id") && a["id"].ToString() == flota.liderID);
            string nombreLider = lider != null && lider.ContainsKey("nombre") ? lider["nombre"].ToString() : "—";
            textoLider.text = "[Lider] " + nombreLider;
        }

        // Estrellas de la flota
        if (!string.IsNullOrEmpty(idFlota))
        {
            AulaDataManager.Instance.CalcularPuntuacionFlota(idFlota, PintarEstrellas);
        }
        else
        {
            PintarEstrellas(0);
        }
    }

    private IEnumerator EsperarYContarPreguntas(string idPlaneta)
    {
        // Pre-pinta provisional mientras esperamos
        if (textoNumPreguntas != null)  textoNumPreguntas.text  = "...";
        if (textoPuntosMaximos != null) textoPuntosMaximos.text = "...";

        // Reintentar hasta 20 veces cada 0.5s (10s máximo)
        for (int intento = 0; intento < 20; intento++)
        {
            // Si el planeta cambió en mitad de la espera, abortamos
            if (idPlaneta != idPlanetaActual) yield break;

            if (creadorPreguntas != null && creadorPreguntas.bibliotecaLocal != null)
            {
                var preguntas = creadorPreguntas.bibliotecaLocal.Where(p => p.idPlaneta == idPlaneta).ToList();
                if (preguntas.Count > 0)
                {
                    int n = preguntas.Count;
                    int puntosMax = preguntas.Sum(p => p.puntosPorAcierto);
                    if (textoNumPreguntas != null)  textoNumPreguntas.text  = n + (n == 1 ? " pregunta" : " preguntas");
                    if (textoPuntosMaximos != null) textoPuntosMaximos.text = puntosMax + " pts máx";
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Si tras los reintentos sigue vacío, mostramos 0
        if (textoNumPreguntas != null)  textoNumPreguntas.text  = "0 preguntas";
        if (textoPuntosMaximos != null) textoPuntosMaximos.text = "0 pts máx";
    }

    private void PintarEstrellas(int n)
    {
        if (estrellasFlota == null) return;
        for (int i = 0; i < estrellasFlota.Length; i++)
        {
            if (estrellasFlota[i] == null) continue;
            bool llena = i < n;
            if (spriteEstrellaLlena != null && spriteEstrellaVacia != null)
            {
                estrellasFlota[i].sprite = llena ? spriteEstrellaLlena : spriteEstrellaVacia;
                estrellasFlota[i].color = Color.white;
            }
            else
            {
                estrellasFlota[i].color = llena ? new Color(1f, 0.85f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
            }
        }
    }
}
