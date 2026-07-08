using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ProgresoGlobalUI : MonoBehaviour
{
    [Header("Estadísticas globales")]
    [SerializeField] private TextMeshProUGUI textoCombatesTotales;
    [SerializeField] private TextMeshProUGUI textoPrecisionMedia;
    [SerializeField] private TextMeshProUGUI textoPlanetaMasJugado;
    [SerializeField] private TextMeshProUGUI textoMejorAlumno;

    [Header("Ranking Flotas")]
    [SerializeField] private Transform contenedorFlotas;
    [SerializeField] private GameObject prefabFilaFlota;

    [Header("Ranking Alumnos")]
    [SerializeField] private Transform contenedorAlumnos;
    [SerializeField] private GameObject prefabFilaAlumno;

    // ── Inicialización ───────────────────────────────────────────────────────

    public void IniciarProgreso()
    {
        AulaDataManager.Instance.ObtenerDatosProgreso((historial, alumnos) =>
        {
            MostrarEstadisticas(historial, alumnos);
            MostrarFlotas(historial);
            MostrarAlumnos(alumnos);
        });
    }

    // ── Estadísticas globales ─────────────────────────────────────────────────

    private void MostrarEstadisticas(List<Dictionary<string, object>> historial,
                                     List<Dictionary<string, object>> alumnos)
    {
        int total = historial.Count;

        if (textoCombatesTotales != null)
            textoCombatesTotales.text = $"Combates jugados: {total}";

        if (textoPrecisionMedia != null)
            textoPrecisionMedia.text = total > 0
                ? $"Precisión media: {historial.Average(r => ObtenerFloat(r, "precision")):F1}%"
                : "Precisión media: —";

        if (textoPlanetaMasJugado != null)
        {
            var planetaTop = historial
                .Where(r => r.ContainsKey("nombrePlaneta"))
                .GroupBy(r => r["nombrePlaneta"].ToString())
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            textoPlanetaMasJugado.text = planetaTop != null
                ? $"Planeta más jugado: {planetaTop.Key} ({planetaTop.Count()}x)"
                : "Planeta más jugado: —";
        }

        if (textoMejorAlumno != null)
        {
            var mejor = alumnos.OrderByDescending(a => ObtenerInt(a, "xpTotal")).FirstOrDefault();
            if (mejor != null)
            {
                string nombre = mejor.ContainsKey("nombre") ? mejor["nombre"].ToString() : "?";
                int nivel     = AulaDataManager.CalcularNivel(ObtenerInt(mejor, "xpTotal"));
                textoMejorAlumno.text = $"Mayor XP: {nombre}  (Nv. {nivel})";
            }
            else textoMejorAlumno.text = "Mayor XP: —";
        }
    }

    // ── Ranking Flotas ────────────────────────────────────────────────────────

    private void MostrarFlotas(List<Dictionary<string, object>> historial)
    {
        if (contenedorFlotas == null || prefabFilaFlota == null) return;
        foreach (Transform hijo in contenedorFlotas) Destroy(hijo.gameObject);

        var porFlota = historial
            .Where(r => r.ContainsKey("nombreFlota") && !string.IsNullOrEmpty(r["nombreFlota"].ToString()))
            .GroupBy(r => r["nombreFlota"].ToString())
            .Select(g => new
            {
                nombre    = g.Key,
                puntos    = g.Sum(r => ObtenerFloat(r, "puntos")),
                precision = g.Average(r => ObtenerFloat(r, "precision")),
                combates  = g.Count()
            })
            .OrderByDescending(f => f.puntos)
            .ToList();

        float maxPuntos = porFlota.Count > 0 ? porFlota.Max(f => f.puntos) : 1f;

        for (int i = 0; i < porFlota.Count; i++)
        {
            var f = porFlota[i];
            GameObject fila = Instantiate(prefabFilaFlota, contenedorFlotas);

            TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"#{i + 1}  {f.nombre}   {f.puntos:F0} pts  |  {f.precision:F1}%  |  {f.combates} combates";

            Slider barra = fila.GetComponentInChildren<Slider>();
            if (barra != null)
            {
                barra.minValue     = 0f;
                barra.maxValue     = 1f;
                barra.value        = maxPuntos > 0 ? f.puntos / maxPuntos : 0f;
                barra.interactable = false;
            }
        }
    }

    // ── Ranking Alumnos ───────────────────────────────────────────────────────

    private void MostrarAlumnos(List<Dictionary<string, object>> alumnos)
    {
        if (contenedorAlumnos == null || prefabFilaAlumno == null) return;
        foreach (Transform hijo in contenedorAlumnos) Destroy(hijo.gameObject);

        var ordenados = alumnos.OrderByDescending(a => ObtenerInt(a, "xpTotal")).ToList();

        for (int i = 0; i < ordenados.Count; i++)
        {
            var a         = ordenados[i];
            string nombre = a.ContainsKey("nombre")      ? a["nombre"].ToString()      : "?";
            string flota  = a.ContainsKey("nombreFlota") ? a["nombreFlota"].ToString() : "";
            int xp        = ObtenerInt(a, "xpTotal");
            int nivel     = AulaDataManager.CalcularNivel(xp);

            GameObject fila = Instantiate(prefabFilaAlumno, contenedorAlumnos);
            TextMeshProUGUI tmp = fila.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = string.IsNullOrEmpty(flota)
                    ? $"#{i + 1}  {nombre}   Nv.{nivel}  —  {xp} XP"
                    : $"#{i + 1}  {nombre}   Nv.{nivel}  —  {xp} XP  [{flota}]";
        }
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

    private float ObtenerFloat(Dictionary<string, object> d, string key) =>
        d.ContainsKey(key) ? System.Convert.ToSingle(d[key]) : 0f;

    private int ObtenerInt(Dictionary<string, object> d, string key) =>
        d.ContainsKey(key) ? System.Convert.ToInt32(d[key]) : 0;
}
