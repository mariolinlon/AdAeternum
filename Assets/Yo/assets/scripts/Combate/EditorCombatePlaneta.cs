using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel del profesor para configurar el combate del planeta seleccionado.
/// Auto-guarda al cambiar cualquier campo (sin botón explícito).
/// Se abre desde un botón en la pantalla del editor de planetas.
/// </summary>
public class EditorCombatePlaneta : MonoBehaviour
{
    [Header("Panel raíz (se activa/desactiva al abrir/cerrar)")]
    [SerializeField] private GameObject panelRaiz;

    [Header("Cabecera")]
    [SerializeField] private TextMeshProUGUI textoTitulo; // muestra nombre del planeta seleccionado

    [Header("Selector de tipo de combate")]
    [SerializeField] private TMP_Dropdown dropdownTipo;

    [Header("Zonas")]
    [SerializeField] private Transform contenedorZonas;   // padre donde se instancian las filas
    [SerializeField] private GameObject prefabFilaZona;   // prefab con FilaZonaEditor
    [SerializeField] private Button botonAñadirZona;

    private PlanetSelectable planetaActual;
    private readonly List<FilaZonaEditor> filas = new List<FilaZonaEditor>();

    // Máximo de zonas que el profesor puede crear por planeta.
    private const int MAX_ZONAS = 6;

    private void Awake()
    {
        // Configurar dropdown con los nombres de los tipos de combate
        if (dropdownTipo != null)
        {
            dropdownTipo.ClearOptions();
            dropdownTipo.AddOptions(new List<string>
            {
                "1 — Asalto Planetario",
                "2 — Exploración Pura (próximamente)",
                "3 — PvP entre flotas (próximamente)"
            });
            dropdownTipo.onValueChanged.RemoveAllListeners();
            dropdownTipo.onValueChanged.AddListener(_ => GuardarCambios());
        }

        if (botonAñadirZona != null)
        {
            botonAñadirZona.onClick.RemoveAllListeners();
            botonAñadirZona.onClick.AddListener(() =>
            {
                if (filas.Count >= MAX_ZONAS)
                {
                    Toast.Show($"Máximo {MAX_ZONAS} zonas por planeta.", 3f, Toast.Tipo.Aviso);
                    return;
                }
                AñadirFila("Zona " + (filas.Count + 1), 100f);
                GuardarCambios();
            });
        }
    }

    /// <summary>Abre el panel con el planeta indicado (rellena los campos con su config).</summary>
    public void AbrirParaPlaneta(PlanetSelectable planeta)
    {
        if (planeta == null) return;
        planetaActual = planeta;

        if (panelRaiz != null) panelRaiz.SetActive(true);

        if (textoTitulo != null) textoTitulo.text = "Combate: " + planeta.NombrePlaneta;

        ConfigCombatePlaneta cfg = planeta.ConfigCombate ?? ConfigCombatePlaneta.ConfigDefault();

        // Dropdown
        if (dropdownTipo != null)
        {
            int idx = ((int)cfg.tipo) - 1; // enum empieza en 1
            if (idx < 0) idx = 0;
            if (idx >= dropdownTipo.options.Count) idx = 0;
            dropdownTipo.SetValueWithoutNotify(idx);
        }

        // Limpiar filas anteriores
        for (int i = filas.Count - 1; i >= 0; i--)
            if (filas[i] != null) Destroy(filas[i].gameObject);
        filas.Clear();

        // Crear filas con las zonas de la config
        foreach (var z in cfg.zonas)
            AñadirFila(z.nombre, z.vidaMaxima);
    }

    public void Cerrar()
    {
        if (panelRaiz != null) panelRaiz.SetActive(false);
    }

    /// <summary>Botón "Editar Combate" en el panel principal: abre con el planeta actual.</summary>
    public void ClickBotonEditarCombate()
    {
        PlanetSelectable p = PlanetSelectionManager.Instance?.ObtenerPlanetaActual();
        if (p == null)
        {
            Toast.Show("Selecciona un planeta antes de editar el combate.", 3f, Toast.Tipo.Aviso);
            return;
        }
        AbrirParaPlaneta(p);
    }

    private void AñadirFila(string nombre, float vida)
    {
        if (prefabFilaZona == null || contenedorZonas == null) return;
        GameObject go = Instantiate(prefabFilaZona, contenedorZonas);
        FilaZonaEditor fila = go.GetComponent<FilaZonaEditor>();
        if (fila == null) return;
        fila.Configurar(nombre, vida, OnCambioFila, OnEliminarFila);
        filas.Add(fila);
    }

    private void OnCambioFila(FilaZonaEditor fila)
    {
        GuardarCambios();
    }

    private void OnEliminarFila(FilaZonaEditor fila)
    {
        if (fila == null) return;
        filas.Remove(fila);
        Destroy(fila.gameObject);
        GuardarCambios();
    }

    private void GuardarCambios()
    {
        if (planetaActual == null) return;

        ConfigCombatePlaneta cfg = planetaActual.ConfigCombate ?? new ConfigCombatePlaneta();

        // Tipo
        if (dropdownTipo != null)
            cfg.tipo = (TipoCombate)(dropdownTipo.value + 1);

        // Zonas (preservando vidaActual si ya existía en runtime)
        cfg.zonas = new List<ZonaPlaneta>();
        foreach (var f in filas)
        {
            if (f == null) continue;
            string nombre = string.IsNullOrEmpty(f.Nombre) ? "Zona" : f.Nombre;
            float vida = f.VidaMaxima > 0f ? f.VidaMaxima : 1f;
            cfg.zonas.Add(new ZonaPlaneta(nombre, vida));
        }
        if (cfg.zonas.Count == 0)
            cfg.zonas.Add(new ZonaPlaneta("Núcleo", 100f));

        planetaActual.AsignarConfigCombate(cfg);

        // Guardar SOLO el campo configCombate del documento del planeta (no toca el resto)
        if (AulaDataManager.Instance != null)
            AulaDataManager.Instance.GuardarPlanetaEnFirebase(planetaActual);
    }
}
