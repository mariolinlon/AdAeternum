using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class FlotaUI : MonoBehaviour
{
    [Header("Componentes Visuales")]
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoContador;
    public Transform contenedorTripulacion;
    public GameObject prefabMiembroFlota;

    private string idFlota;
    private int maxAlumnos;

    public void Configurar(string _id, string nombre, int max, List<string> alumnosIds, string idLider)
    {
        idFlota = _id;
        maxAlumnos = max;
        textoNombre.text = nombre;
        textoContador.text = alumnosIds.Count + " / " + max;

        for (int i = contenedorTripulacion.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedorTripulacion.GetChild(i);
            hijo.SetParent(null);
            Destroy(hijo.gameObject);
        }

        foreach (string id in alumnosIds)
        {
            string nombreAlumno = BuscarNombrePorId(id);
            string prefijo = (id == idLider) ? "[Lider] " : "- ";

            GameObject item = Instantiate(prefabMiembroFlota, contenedorTripulacion);
            item.GetComponentInChildren<TMP_Text>().text = prefijo + nombreAlumno;

            string idCapturado = id;

            Button btnQuitar = item.transform.Find("BotonQuitar")?.GetComponent<Button>();
            if (btnQuitar != null)
                btnQuitar.onClick.AddListener(() => AulaDataManager.Instance.QuitarAlumnoDeFlota(idCapturado, idFlota));

            Button btnLider = item.transform.Find("BotonLider")?.GetComponent<Button>();
            if (btnLider != null)
                btnLider.onClick.AddListener(() => AulaDataManager.Instance.DefinirLiderDeFlota(idFlota, idCapturado));
        }
    }

    public void ClickAsignarAlumnoAEstaFlota()
    {
        string idSel = PanelControlFlotas.Instance?.idAlumnoSeleccionado;

        if (string.IsNullOrEmpty(idSel))
        {
            Toast.Show("Selecciona un alumno de la lista primero.", 3f, Toast.Tipo.Aviso);
            return;
        }

        Flota flota = AulaDataManager.Instance.flotasActivas.Find(f => f.id == idFlota);
        if (flota != null)
        {
            // Tope efectivo: nunca más de MAX_ALUMNOS_POR_FLOTA, aunque una flota
            // antigua tuviera guardado un maxAlumnos mayor en Firebase.
            int cap = Mathf.Min(flota.maxAlumnos, PanelControlFlotas.MAX_ALUMNOS_POR_FLOTA);
            if (flota.alumnos.Count >= cap)
            {
                Toast.Show($"La flota '{flota.nombre}' está llena.", 3f, Toast.Tipo.Aviso);
                return;
            }
        }

        AulaDataManager.Instance.AsignarAlumnoAFlota(idSel, idFlota);
        PanelControlFlotas.Instance.idAlumnoSeleccionado = "";
    }

    public void ClickBorrarFlota()
    {
        string nombre = textoNombre != null ? textoNombre.text : "esta flota";
        ConfirmDialog.Show(
            $"¿Borrar la flota \"{nombre}\"?\nLos alumnos asignados quedarán sin flota.",
            () =>
            {
                AulaDataManager.Instance.BorrarFlota(idFlota);
                Toast.Show($"Flota '{nombre}' eliminada.", 2f, Toast.Tipo.Exito);
            },
            textoConfirmar: "Borrar",
            textoCancelar: "Cancelar"
        );
    }

    private string BuscarNombrePorId(string id)
    {
        foreach (var datos in AulaDataManager.Instance.alumnosDisponibles)
        {
            if (datos["id"].ToString() == id)
                return datos["nombre"].ToString();
        }
        return "Desconectado";
    }
}
