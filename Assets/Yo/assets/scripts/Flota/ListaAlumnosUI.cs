using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ListaAlumnosUI : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject prefabNombreAlumno;
    [SerializeField] private Transform contenedorAlumnos;

    [Header("Color de selección")]
    [SerializeField] private Color colorSeleccionado = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private Color colorNormal = Color.white;

    private bool escuchando = false;
    private GameObject itemSeleccionado;

    private void OnEnable()
    {
        UpdateList();
    }

    public void UpdateList()
    {
        if (escuchando) return;
        if (AulaDataManager.Instance == null) return;
        escuchando = true;
        AulaDataManager.Instance.EscucharAlumnos(ActualizarInterfaz);
    }

    public void ActualizarInterfaz()
    {
        for (int i = contenedorAlumnos.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedorAlumnos.GetChild(i);
            hijo.SetParent(null);
            Destroy(hijo.gameObject);
        }

        itemSeleccionado = null;
        if (PanelControlFlotas.Instance != null)
            PanelControlFlotas.Instance.idAlumnoSeleccionado = "";

        foreach (var datosAlumno in AulaDataManager.Instance.alumnosDisponibles)
        {
            string idFlota = datosAlumno.ContainsKey("idFlota") ? datosAlumno["idFlota"].ToString() : "";
            if (!string.IsNullOrEmpty(idFlota)) continue;

            string nombre = datosAlumno["nombre"].ToString();
            string idAlumno = datosAlumno["id"].ToString();

            GameObject item = Instantiate(prefabNombreAlumno, contenedorAlumnos);
            item.GetComponentInChildren<TMP_Text>().text = nombre;

            Button btn = item.GetComponentInChildren<Button>();
            if (btn != null)
            {
                GameObject itemRef = item;
                string idRef = idAlumno;
                btn.onClick.AddListener(() => SeleccionarAlumno(idRef, itemRef));
            }
        }
    }

    private void SeleccionarAlumno(string idAlumno, GameObject item)
    {
        if (itemSeleccionado != null)
        {
            Image imgAnterior = itemSeleccionado.GetComponentInChildren<Image>();
            if (imgAnterior != null) imgAnterior.color = colorNormal;
        }

        itemSeleccionado = item;
        Image img = item.GetComponentInChildren<Image>();
        if (img != null) img.color = colorSeleccionado;

        if (PanelControlFlotas.Instance != null)
            PanelControlFlotas.Instance.idAlumnoSeleccionado = idAlumno;
    }
}
