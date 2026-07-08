using UnityEngine;

public class PlanetSelectable : MonoBehaviour
{
    [Header("Identidad Única")]
    [SerializeField] private string idUnico; // El ID que vincula con las preguntas

    [Header("Vista del planeta")]
    [SerializeField] private Transform puntoVistaPlaneta;
    [SerializeField] private Transform puntoVistaCamaraCompartido;

    [Header("Visual de selección")]
    [SerializeField] private GameObject indicadorSeleccion;

    [Header("Niebla de Guerra")]
    [SerializeField] private GameObject nieblaFog;

    [Header("Datos del planeta")]
    [SerializeField] private string nombrePlaneta;

    [TextArea(3, 8)]
    [SerializeField] private string descripcionPlaneta;

    [Header("Configuración de combate")]
    [SerializeField] private ConfigCombatePlaneta configCombate = new ConfigCombatePlaneta();

    [Header("Modelo visual (Low Poly Cosmos)")]
    [Tooltip("Índice del modelo low poly asignado a este planeta (se persiste en Firebase). -1 = no asignado.")]
    [SerializeField] private int idModeloVisual = -1;
    [Tooltip("Opcional: GameObject del mesh visual ORIGINAL del prefab. Se desactivará al aplicar un modelo low poly. Si se deja vacío, no se desactiva nada.")]
    [SerializeField] private GameObject meshVisualBase;
    [Tooltip("Opcional: Transform donde se instanciará el modelo low poly. Si se deja vacío, se instancia como hijo directo de este GameObject.")]
    [SerializeField] private Transform contenedorModeloVisual;

    private GameObject _modeloVisualInstanciado;

    // Propiedades
    public string IdUnico => idUnico;
    public string NombrePlaneta => nombrePlaneta;
    public string DescripcionPlaneta => descripcionPlaneta;
    public bool Bloqueado => _bloqueado;
    public ConfigCombatePlaneta ConfigCombate => configCombate;
    public int IdModeloVisual => idModeloVisual;

    public void AsignarConfigCombate(ConfigCombatePlaneta cfg)
    {
        configCombate = cfg ?? ConfigCombatePlaneta.ConfigDefault();
    }

    private bool _bloqueado = true;

    private void Awake()
    {
        // Si el planeta no tiene ID (es nuevo), le generamos uno
        if (string.IsNullOrEmpty(idUnico))
        {
            idUnico = System.Guid.NewGuid().ToString();
        }

        // Garantizar que siempre haya una config de combate (defaults si no la cargan de Firebase)
        if (configCombate == null) configCombate = ConfigCombatePlaneta.ConfigDefault();
        if (configCombate.zonas == null || configCombate.zonas.Count == 0)
        {
            configCombate.zonas = new System.Collections.Generic.List<ZonaPlaneta>
            {
                new ZonaPlaneta("Núcleo", 100f)
            };
        }

        // La niebla es puramente visual: desactivar TODOS sus colliders para que
        // los clicks lleguen al planeta y la lógica EsAlumno decida si los acepta.
        DesactivarColliderNiebla();
    }

    private void DesactivarColliderNiebla()
    {
        if (nieblaFog == null) return;
        foreach (Collider c in nieblaFog.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
    }

    private void Start()
    {
        DeseleccionarVisual();
    }

    private void OnMouseDown()
    {
        // Los alumnos no pueden interactuar con planetas bloqueados
        if (_bloqueado && AulaDataManager.Instance != null && AulaDataManager.Instance.EsAlumno) return;
        SeleccionarPlaneta();
    }

    public void SeleccionarPlaneta()
    {
        if (puntoVistaPlaneta != null && puntoVistaCamaraCompartido != null)
        {
            puntoVistaCamaraCompartido.position = puntoVistaPlaneta.position;
            puntoVistaCamaraCompartido.rotation = puntoVistaPlaneta.rotation;
        }

        if (PlanetSelectionManager.Instance != null)
        {
            PlanetSelectionManager.Instance.Seleccionar(this);
        }
    }

    public void AsignarPuntoVistaCompartido(Transform nuevoPuntoVista)
    {
        puntoVistaCamaraCompartido = nuevoPuntoVista;
    }

    public void CambiarNombre(string nuevoNombre)
    {
        nombrePlaneta = nuevoNombre;
    }

    public void CambiarDescripcion(string nuevaDescripcion)
    {
        descripcionPlaneta = nuevaDescripcion;
    }

    public void CambiarNombreYActualizarUI(string nuevoNombre)
    {
        nombrePlaneta = nuevoNombre;

        if (PlanetSelectionManager.Instance != null &&
            PlanetSelectionManager.Instance.ObtenerPlanetaActual() == this)
        {
            PlanetSelectionManager.Instance.ActualizarTextosPlaneta();
        }
    }

    public void CambiarDescripcionYActualizarUI(string nuevaDescripcion)
    {
        descripcionPlaneta = nuevaDescripcion;

        if (PlanetSelectionManager.Instance != null &&
            PlanetSelectionManager.Instance.ObtenerPlanetaActual() == this)
        {
            PlanetSelectionManager.Instance.ActualizarTextosPlaneta();
        }
    }

    public void SeleccionarVisual()
    {
        if (indicadorSeleccion != null)
            indicadorSeleccion.SetActive(true);
    }

    public void DeseleccionarVisual()
    {
        if (indicadorSeleccion != null)
            indicadorSeleccion.SetActive(false);
    }

    public void Bloquear()
    {
        _bloqueado = true;
        if (nieblaFog != null) nieblaFog.SetActive(true);
    }

    public void Desbloquear()
    {
        _bloqueado = false;
        if (nieblaFog != null) nieblaFog.SetActive(false);
    }

    public void CargarDesdeFirebase(string id, string nombre, string desc, bool bloqueado = true)
    {
        idUnico = id;
        nombrePlaneta = nombre;
        descripcionPlaneta = desc;
        if (bloqueado) Bloquear(); else Desbloquear();
    }

    /// <summary>
    /// Instancia el prefab visual low poly como hijo de este planeta y guarda
    /// el índice (para persistirlo luego en Firebase). Si ya había un modelo
    /// instanciado, lo destruye antes. Desactiva el meshVisualBase original
    /// (si está asignado en el Inspector) para que no se solape.
    /// </summary>
    public void AplicarModeloLowPoly(GameObject modeloPrefab, int idModelo)
    {
        if (modeloPrefab == null) return;

        // Destruir el modelo low poly anterior (si lo había) para evitar duplicados
        // al recargar planetas o cuando el listener de Firebase fire varias veces.
        if (_modeloVisualInstanciado != null)
        {
            Destroy(_modeloVisualInstanciado);
            _modeloVisualInstanciado = null;
        }

        // Ocultar el mesh base del prefab original (el placeholder hardcoded).
        // Si el usuario asignó meshVisualBase en el Inspector, usamos ese.
        // Si no, auto-detect en este orden:
        //   1. LODGroup en hijos → desactivar el GO que lo contiene (típico
        //      cuando el prefab tiene Jowisz_LOD0/1/2 hardcoded).
        //   2. MeshRenderer en el root → desactivar.
        // Importante: NO desactivamos MeshRenderers de hijos arbitrariamente
        // porque podríamos cargarnos el indicador de selección, la niebla, etc.
        if (meshVisualBase != null)
        {
            meshVisualBase.SetActive(false);
        }
        else
        {
            var lodGroup = GetComponentInChildren<LODGroup>(true);
            if (lodGroup != null && lodGroup.gameObject != gameObject)
            {
                lodGroup.gameObject.SetActive(false);
            }
            else
            {
                var mrRoot = GetComponent<MeshRenderer>();
                if (mrRoot != null) mrRoot.enabled = false;
            }
        }

        Transform parent = contenedorModeloVisual != null ? contenedorModeloVisual : transform;
        _modeloVisualInstanciado = Instantiate(modeloPrefab, parent);
        _modeloVisualInstanciado.transform.localPosition = Vector3.zero;
        _modeloVisualInstanciado.transform.localRotation = Quaternion.identity;
        // El scale lo hereda del parent (que ya viene escalado desde PlanetSpawner).
        _modeloVisualInstanciado.transform.localScale = Vector3.one;

        // El modelo low poly puede traer su propio LODGroup interno (con LOD0,
        // LOD1, LOD2). Cuando lo instanciamos como hijo de un planeta con
        // escala distinta a la original, el LODGroup falla calculando la
        // distancia de transición y termina ocultando todos los renderers
        // (planeta invisible). Solución: quitar los LODGroup del modelo y
        // dejar visible SOLO el LOD0 (el más detallado).
        AplastarLODGroups(_modeloVisualInstanciado);

        idModeloVisual = idModelo;
    }

    /// <summary>
    /// Elimina los componentes LODGroup del modelo y deja activos únicamente
    /// los Renderers del LOD0. Los GameObjects de LOD1+ se desactivan para
    /// evitar mallas duplicadas.
    /// </summary>
    private void AplastarLODGroups(GameObject modelo)
    {
        if (modelo == null) return;
        var grupos = modelo.GetComponentsInChildren<LODGroup>(true);
        foreach (var lg in grupos)
        {
            if (lg == null) continue;
            var lods = lg.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                bool esLOD0 = (i == 0);
                foreach (var r in lods[i].renderers)
                {
                    if (r == null) continue;
                    r.enabled = esLOD0;
                    r.gameObject.SetActive(esLOD0);
                }
            }
            // Quitar el componente LODGroup ya inútil.
            Destroy(lg);
        }
    }

    /// <summary>Asigna sólo el id (sin instanciar). Útil cuando aún no se conoce el prefab.</summary>
    public void AsignarIdModelo(int idModelo)
    {
        idModeloVisual = idModelo;
    }
}