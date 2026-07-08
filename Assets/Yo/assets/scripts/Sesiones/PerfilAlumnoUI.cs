using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SlotMedalla
{
    public string   id;           // "matematicas", "ciencias", "lenguajes", "historia"
    public Image    imagen;       // Image del slot en la UI
    public Sprite[] niveles;      // [0]=vacío, [1]=nivel1, [2]=nivel2, [3]=nivel3
    public TextMeshProUGUI textoContador; // opcional: muestra "x3"
}

public class PerfilAlumnoUI : MonoBehaviour
{
    [Header("Datos fijos")]
    [SerializeField] private TextMeshProUGUI textoNombre;
    [SerializeField] private TextMeshProUGUI textoNivel;
    [SerializeField] private Slider barraXP;
    [SerializeField] private TextMeshProUGUI textoXP;
    [SerializeField] private TextMeshProUGUI textoRango;
    [SerializeField] private TextMeshProUGUI textoCombates;
    [SerializeField] private TextMeshProUGUI textoFechaRegistro;

    [Header("Personalización - Título")]
    [SerializeField] private TextMeshProUGUI textoTituloActual;
    [SerializeField] private Button botonEditarTitulo;
    [Tooltip("Añade aquí todos los títulos disponibles")]
    public string[] titulosDisponibles = { "Explorador", "Guerrero Estelar", "Maestro del Cosmos" };

    [Header("Personalización - Imagen de perfil")]
    [SerializeField] private Image imagenPerfilActual;
    [SerializeField] private Button botonEditarImagen;
    [Tooltip("Añade aquí todos los sprites de perfil disponibles")]
    public Sprite[] imagenesDisponibles;

    [Header("Personalización - Color de acento")]
    [SerializeField] private Image previsualizacionColor;
    [SerializeField] private Button botonEditarColor;
    public Color[] coloresDisponibles = { Color.cyan, Color.green, Color.yellow, Color.red, Color.magenta, Color.white };

    [Header("Personalización - Lema")]
    [SerializeField] private TMP_InputField inputLema;

    [Header("Panel Selector - Vertical (títulos)")]
    [SerializeField] private GameObject panelSelectorVertical;
    [SerializeField] private Transform contenedorVertical;
    [SerializeField] private GameObject prefabOpcionTexto;

    [Header("Panel Selector - Grid (imágenes y colores)")]
    [SerializeField] private GameObject panelSelectorGrid;
    [SerializeField] private Transform contenedorGrid;
    [SerializeField] private GameObject prefabOpcionImagen;
    [SerializeField] private GameObject prefabOpcionColor;

    [Header("Insignias automáticas")]
    [SerializeField] private Transform contenedorInsignias;
    [SerializeField] private GameObject prefabInsignia;

    [Header("Medallas de categoría (profesor)")]
    [SerializeField] private SlotMedalla[] slotsMedallas;
    [Tooltip("Número total de estados visuales por medalla, incluyendo el estado bloqueado (mínimo 2)")]
    public int numNiveles = 4;
    [Tooltip("Medallas necesarias para alcanzar cada nivel. Se ajusta automáticamente al cambiar Num Niveles")]
    public int[] umbrales = { 1, 5, 10 };

    [Header("Guardar")]
    [SerializeField] private Button botonGuardar;
    [SerializeField] private TextMeshProUGUI textoEstado;

    private int indiceTitulo = 0;
    private int indiceImagen = 0;
    private int indiceColor  = 0;

    private static readonly Dictionary<string, string> NombresInsignias = new Dictionary<string, string>
    {
        { "precision_perfecta", "Precisión Perfecta" },
        { "rango_oro",          "Rango Oro"           },
        { "racha_5",            "Racha x5"            }
    };


    // Se ejecuta en el Editor cada vez que cambias un valor en el Inspector
    private void OnValidate()
    {
        numNiveles = Mathf.Max(2, numNiveles); // mínimo: bloqueado + 1 nivel
        int numUmbrales = numNiveles - 1;      // un umbral por cada nivel por encima del 0

        // Redimensionar umbrales conservando los valores existentes
        if (umbrales == null || umbrales.Length != numUmbrales)
        {
            var nuevosUmbrales = new int[numUmbrales];
            for (int i = 0; i < numUmbrales; i++)
                nuevosUmbrales[i] = (umbrales != null && i < umbrales.Length) ? umbrales[i] : (i + 1) * 5;
            umbrales = nuevosUmbrales;
        }

        // Redimensionar niveles de cada slot conservando los sprites existentes
        if (slotsMedallas == null) return;
        foreach (var slot in slotsMedallas)
        {
            if (slot == null) continue;
            if (slot.niveles == null || slot.niveles.Length != numNiveles)
            {
                var nuevos = new Sprite[numNiveles];
                if (slot.niveles != null)
                    for (int i = 0; i < Mathf.Min(slot.niveles.Length, numNiveles); i++)
                        nuevos[i] = slot.niveles[i];
                slot.niveles = nuevos;
            }
        }
    }

    public void IniciarPerfil() => CargarPerfil();

    // ── Carga ────────────────────────────────────────────────────────────────

    private void CargarPerfil()
    {
        AulaDataManager.Instance.CargarPerfilAlumno(datos =>
        {
            if (datos == null) return;

            string nombre = datos.ContainsKey("nombre") ? datos["nombre"].ToString() : "?";
            if (textoNombre != null) textoNombre.text = nombre;

            int xpTotal = datos.ContainsKey("xpTotal") ? Convert.ToInt32(datos["xpTotal"]) : 0;
            int nivel   = AulaDataManager.CalcularNivel(xpTotal);
            var (xpEnNivel, xpNecesaria) = AulaDataManager.CalcularProgresoNivel(xpTotal);

            if (textoNivel != null) textoNivel.text = $"Nivel {nivel}";
            if (textoXP    != null) textoXP.text    = $"{xpEnNivel} / {xpNecesaria} XP";
            if (barraXP    != null) { barraXP.maxValue = xpNecesaria; barraXP.value = xpEnNivel; }

            int combates = datos.ContainsKey("combatesJugados") ? Convert.ToInt32(datos["combatesJugados"]) : 0;
            string rango = combates >= 30 ? "Comandante" :
                           combates >= 20 ? "Capitán"    :
                           combates >= 13 ? "Teniente"   :
                           combates >=  7 ? "Sargento"   :
                           combates >=  3 ? "Cabo"       : "Tripulante";
            if (textoRango    != null) textoRango.text    = $"Rango: {rango}";
            if (textoCombates != null) textoCombates.text = $"Combates: {combates}";

            if (textoFechaRegistro != null)
            {
                // Los alumnos antiguos pueden no tener el campo fechaRegistro; en ese
                // caso mostramos un guion en vez de dejar el "New Text" por defecto.
                if (datos.ContainsKey("fechaRegistro") && datos["fechaRegistro"] is Firebase.Firestore.Timestamp ts)
                    textoFechaRegistro.text = $"Desde: {ts.ToDateTime().ToLocalTime():dd/MM/yyyy}";
                else
                    textoFechaRegistro.text = "Desde: —";
            }

            // Título
            indiceTitulo = 0;
            if (datos.ContainsKey("tituloEquipado"))
                for (int i = 0; i < titulosDisponibles.Length; i++)
                    if (titulosDisponibles[i] == datos["tituloEquipado"].ToString()) { indiceTitulo = i; break; }

            indiceImagen = datos.ContainsKey("imagenPerfil") ? Convert.ToInt32(datos["imagenPerfil"]) : 0;
            indiceColor  = datos.ContainsKey("colorAcento")  ? Convert.ToInt32(datos["colorAcento"])  : 0;

            if (inputLema != null)
                inputLema.text = datos.ContainsKey("lema") ? datos["lema"].ToString() : "";

            ActualizarVistaTitulo();
            ActualizarVistaImagen();
            ActualizarVistaColor();
            CargarInsignias(datos);

            CerrarSelectores();
        });
    }

    private void CargarInsignias(Dictionary<string, object> datos)
    {
        if (contenedorInsignias == null || prefabInsignia == null) return;
        foreach (Transform hijo in contenedorInsignias) Destroy(hijo.gameObject);

        if (datos.ContainsKey("insignias") && datos["insignias"] is List<object> lista)
            foreach (var item in lista)
            {
                string id     = item.ToString();
                string nombre = NombresInsignias.ContainsKey(id) ? NombresInsignias[id] : id;
                GameObject go = Instantiate(prefabInsignia, contenedorInsignias);
                TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = nombre;
            }

        ActualizarSlotsMedallas(datos);
    }

    private void ActualizarSlotsMedallas(Dictionary<string, object> datos)
    {
        if (slotsMedallas == null) return;

        Dictionary<string, object> mapa = null;
        if (datos.ContainsKey("medallasProfesor") && datos["medallasProfesor"] is Dictionary<string, object> m)
            mapa = m;

        foreach (var slot in slotsMedallas)
        {
            if (slot.imagen == null || slot.niveles == null || slot.niveles.Length == 0) continue;

            int count = 0;
            if (mapa != null && mapa.ContainsKey(slot.id))
                count = Convert.ToInt32(mapa[slot.id]);

            int maxNivel = slot.niveles.Length - 1;
            int nivel = 0;
            if (umbrales != null)
                for (int i = umbrales.Length - 1; i >= 0; i--)
                    if (count >= umbrales[i]) { nivel = i + 1; break; }
            nivel = Mathf.Min(nivel, maxNivel);

            slot.imagen.sprite = slot.niveles[nivel];
            slot.imagen.color  = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.25f);

            if (slot.textoContador != null)
                slot.textoContador.text = count > 0 ? $"x{count}" : "";
        }
    }

    // ── Botones de editar ────────────────────────────────────────────────────

    public void ClickEditarTitulo()
    {
        CerrarSelectores();
        foreach (Transform hijo in contenedorVertical) Destroy(hijo.gameObject);
        panelSelectorVertical?.SetActive(true);

        for (int i = 0; i < titulosDisponibles.Length; i++)
        {
            int idx = i;
            GameObject item = Instantiate(prefabOpcionTexto, contenedorVertical);
            TextMeshProUGUI tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = titulosDisponibles[i];
            Button btn = item.GetComponent<Button>() ?? item.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                indiceTitulo = idx;
                ActualizarVistaTitulo();
                CerrarSelectores();
            });
        }
    }

    public void ClickEditarImagen()
    {
        if (imagenesDisponibles == null || imagenesDisponibles.Length == 0) return;
        CerrarSelectores();
        foreach (Transform hijo in contenedorGrid) Destroy(hijo.gameObject);
        panelSelectorGrid?.SetActive(true);

        for (int i = 0; i < imagenesDisponibles.Length; i++)
        {
            int idx = i;
            GameObject item = Instantiate(prefabOpcionImagen, contenedorGrid);
            Image img = item.GetComponentInChildren<Image>();
            if (img != null) img.sprite = imagenesDisponibles[i];
            Button btn = item.GetComponent<Button>() ?? item.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                indiceImagen = idx;
                ActualizarVistaImagen();
                CerrarSelectores();
            });
        }
    }

    public void ClickEditarColor()
    {
        CerrarSelectores();
        foreach (Transform hijo in contenedorGrid) Destroy(hijo.gameObject);
        panelSelectorGrid?.SetActive(true);

        for (int i = 0; i < coloresDisponibles.Length; i++)
        {
            int idx = i;
            GameObject item = Instantiate(prefabOpcionColor, contenedorGrid);
            Image img = item.GetComponentInChildren<Image>();
            if (img != null) img.color = coloresDisponibles[i];
            Button btn = item.GetComponent<Button>() ?? item.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                indiceColor = idx;
                ActualizarVistaColor();
                CerrarSelectores();
            });
        }
    }

    public void CerrarSelectores()
    {
        panelSelectorVertical?.SetActive(false);
        panelSelectorGrid?.SetActive(false);
    }

    // ── Actualizar vistas ────────────────────────────────────────────────────

    private void ActualizarVistaTitulo()
    {
        if (textoTituloActual != null && titulosDisponibles.Length > 0)
            textoTituloActual.text = titulosDisponibles[indiceTitulo];
    }

    private void ActualizarVistaImagen()
    {
        if (imagenPerfilActual != null && imagenesDisponibles != null && imagenesDisponibles.Length > 0)
            imagenPerfilActual.sprite = imagenesDisponibles[indiceImagen];
    }

    private void ActualizarVistaColor()
    {
        if (previsualizacionColor != null && coloresDisponibles.Length > 0)
            previsualizacionColor.color = coloresDisponibles[indiceColor];
    }

    // ── Guardar ──────────────────────────────────────────────────────────────

    public void ClickGuardar()
    {
        string lema = inputLema != null ? inputLema.text.Trim() : "";
        if (lema.Length > 60)
        {
            if (textoEstado != null) textoEstado.text = "El lema no puede superar 60 caracteres.";
            Toast.Show("El lema no puede superar 60 caracteres.", 3f, Toast.Tipo.Error);
            return;
        }

        var campos = new Dictionary<string, object>
        {
            { "tituloEquipado", titulosDisponibles.Length > 0 ? titulosDisponibles[indiceTitulo] : "" },
            { "imagenPerfil",   indiceImagen },
            { "colorAcento",    indiceColor  },
            { "lema",           lema         }
        };

        AulaDataManager.Instance.GuardarCamposPerfil(campos);
        if (textoEstado != null) textoEstado.text = "Perfil guardado.";
        Toast.Show("Cambios guardados.", 2.5f, Toast.Tipo.Exito);
    }
}
