using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PantallaFlotaAlumnoUI : MonoBehaviour
{
    [Header("Contenedor donde se instancian las filas")]
    [SerializeField] private Transform contenido;

    [Header("Prefab de fila (con FlotaMemberRow)")]
    [SerializeField] private GameObject prefabFila;

    [Header("Texto cabecera (opcional, muestra nombre flota)")]
    [SerializeField] private TextMeshProUGUI textoNombreFlota;

    [Header("Estrellas de puntuacion (1-5)")]
    [SerializeField] private Image[] estrellas;
    [SerializeField] private Sprite spriteEstrellaLlena;
    [SerializeField] private Sprite spriteEstrellaVacia;

    private bool _suscrito = false;

    /// <summary>Llamar desde LoginAlumnoUI.EntrarAlJuego() — patrón habitual del proyecto.</summary>
    public void IniciarFlotaAlumno()
    {
        if (AulaDataManager.Instance == null)
        {
            Debug.LogWarning("[PantallaFlotaAlumno] AulaDataManager no disponible.");
            return;
        }

        if (!_suscrito)
        {
            AulaDataManager.Instance.EscucharAlumnos(Refrescar);
            AulaDataManager.Instance.EscucharFlotas(Refrescar);
            _suscrito = true;
        }
        else
        {
            Refrescar();
        }
    }

    private void Refrescar()
    {
        if (contenido == null || prefabFila == null) return;
        if (AulaDataManager.Instance == null) return;

        string idAlumnoLocal = AulaDataManager.Instance.GetIdAlumnoLocal();
        if (string.IsNullOrEmpty(idAlumnoLocal)) return;

        // Buscar mi propia entrada para conocer mi flota y mi rol
        var miAlumno = AulaDataManager.Instance.alumnosDisponibles
            .FirstOrDefault(a => a.ContainsKey("id") && a["id"].ToString() == idAlumnoLocal);
        if (miAlumno == null) return;

        string idMiFlota = miAlumno.ContainsKey("idFlota") ? miAlumno["idFlota"].ToString() : "";
        string miRol     = miAlumno.ContainsKey("rol")     ? miAlumno["rol"].ToString()     : "miembro";
        bool somosLider  = miRol == "lider";

        // Cabecera con nombre de la flota
        if (textoNombreFlota != null)
        {
            Flota miFlota = AulaDataManager.Instance.flotasActivas.FirstOrDefault(f => f.id == idMiFlota);
            textoNombreFlota.text = miFlota != null ? miFlota.nombre : "Sin nave";
        }

        // Calcular y pintar estrellas de puntuacion
        if (!string.IsNullOrEmpty(idMiFlota) && estrellas != null && estrellas.Length > 0)
        {
            AulaDataManager.Instance.CalcularPuntuacionFlota(idMiFlota, PintarEstrellas);
        }

        // Limpiar filas anteriores
        for (int i = contenido.childCount - 1; i >= 0; i--)
            Destroy(contenido.GetChild(i).gameObject);

        if (string.IsNullOrEmpty(idMiFlota)) return;

        // Filtrar alumnos que pertenecen a mi flota
        var miembrosFlota = AulaDataManager.Instance.alumnosDisponibles
            .Where(a => a.ContainsKey("idFlota") && a["idFlota"].ToString() == idMiFlota)
            .ToList();

        foreach (var alumno in miembrosFlota)
        {
            string id      = alumno.ContainsKey("id")      ? alumno["id"].ToString()     : "";
            string nombre  = alumno.ContainsKey("nombre")  ? alumno["nombre"].ToString() : "?";
            string rolJer  = alumno.ContainsKey("rol")     ? alumno["rol"].ToString()    : "miembro";
            string rolComb = alumno.ContainsKey("rolCombate") ? alumno["rolCombate"].ToString() : "";

            string prefijo = (rolJer == "lider") ? "[Lider] " : "";
            string sufijo  = "";
            if (rolComb == "atacante")  sufijo = "  [ATK]";
            else if (rolComb == "defensor") sufijo = "  [DEF]";

            string nombreMostrado = $"{prefijo}{nombre}{sufijo}";

            GameObject filaGO = Instantiate(prefabFila, contenido);
            FlotaMemberRow fila = filaGO.GetComponent<FlotaMemberRow>();
            if (fila != null)
            {
                fila.Configurar(id, nombreMostrado, rolComb, somosLider);
            }
        }
    }

    private void PintarEstrellas(int n)
    {
        if (estrellas == null) return;
        for (int i = 0; i < estrellas.Length; i++)
        {
            if (estrellas[i] == null) continue;
            bool llena = i < n;
            if (spriteEstrellaLlena != null && spriteEstrellaVacia != null)
            {
                estrellas[i].sprite = llena ? spriteEstrellaLlena : spriteEstrellaVacia;
                estrellas[i].color = Color.white;
            }
            else
            {
                // Placeholder: cambia de color si no hay sprites asignados aún
                estrellas[i].color = llena ? new Color(1f, 0.85f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
            }
        }
    }
}
