using UnityEngine;
using Firebase.Firestore; // Para QuerySnapshot y Collection
using Firebase.Extensions; // ESTA es la que permite el ContinueWithOnMainThread
using System.Threading.Tasks;
using System.Collections.Generic; // Necesario para manejar las listas de Firebase
using System.Linq; // Necesario para conversiones de datos

public class PlanetSpawner : MonoBehaviour
{
    public static PlanetSpawner Instance;
    [Header("Prefab a instanciar")]
    [SerializeField] private GameObject planetaPrefab;

    [Header("Primer planeta ya existente en escena")]
    [SerializeField] private Transform primerPlaneta;

    [Header("Padre opcional para los nuevos planetas")]
    [SerializeField] private Transform contenedorPlanetas;

    [Header("Punto de vista compartido que se asignará a cada planeta")]
    [SerializeField] private Transform puntoVistaCamaraCompartido;

    [Header("Separación horizontal entre planetas")]
    [SerializeField] private float separacionX = 5f;

    [Header("Rango aleatorio en Z respecto al último planeta")]
    [SerializeField] private float zMin = -1.5f;
    [SerializeField] private float zMax = 1.5f;

    [Header("Límite total respecto al planeta original")]
    [SerializeField] private float limiteZDesdeOriginal = 200f;

    [Header("Multiplicador de escala del nuevo planeta")]
    [SerializeField] private float multiplicadorEscala = 5f;

    [Header("Variantes visuales (Low Poly Cosmos)")]
    [Tooltip("Arrastra aquí los prefabs de planetas del pack Low Poly Cosmos (ej. EA05_Planets_Earth_01a_Default.prefab, _Mars_01a_Default.prefab, etc.). Al crear un planeta nuevo se asignará uno aleatorio de esta lista y se persistirá en Firebase para que todos los clientes vean el mismo.")]
    [SerializeField] private GameObject[] modelosPlanetaLowPoly;

    private Transform ultimoPlaneta;
    private Firebase.Firestore.ListenerRegistration _listenerPlanetas;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        _listenerPlanetas?.Stop();
    }

    private void Start()
    {
        if (primerPlaneta == null)
        {
            Debug.LogWarning("PlanetSpawner: no se ha asignado el primer planeta.");
            return;
        }

        ultimoPlaneta = primerPlaneta;
    }

    public void InstanciarPlaneta()
    {
        if (planetaPrefab == null)
        {
            Debug.LogWarning("PlanetSpawner: no se ha asignado el prefab del planeta.");
            return;
        }

        if (primerPlaneta == null || ultimoPlaneta == null)
        {
            Debug.LogWarning("PlanetSpawner: faltan referencias de planeta.");
            return;
        }

        Vector3 posicionBase = ultimoPlaneta.position;

        float desplazamientoZ = Random.Range(zMin, zMax);
        float zTentativa = posicionBase.z + desplazamientoZ;

        float zMinPermitida = primerPlaneta.position.z - limiteZDesdeOriginal;
        float zMaxPermitida = primerPlaneta.position.z + limiteZDesdeOriginal;

        float zFinal = Mathf.Clamp(zTentativa, zMinPermitida, zMaxPermitida);

        Vector3 nuevaPosicion = new Vector3(
            posicionBase.x + separacionX,
            posicionBase.y,
            zFinal
        );

        GameObject nuevoPlaneta = Instantiate(
            planetaPrefab,
            nuevaPosicion,
            planetaPrefab.transform.rotation,
            contenedorPlanetas
        );

        nuevoPlaneta.transform.localScale = primerPlaneta.localScale * multiplicadorEscala;

        PlanetSelectable planetSelectable = nuevoPlaneta.GetComponent<PlanetSelectable>();

        if (planetSelectable != null)
        {
            planetSelectable.AsignarPuntoVistaCompartido(puntoVistaCamaraCompartido);

            // Asignar un modelo visual aleatorio del pack Low Poly Cosmos (si hay).
            // El idModelo se persistirá en Firebase para que todos los clientes vean
            // el mismo modelo.
            AplicarModeloAleatorio(planetSelectable);

            AulaDataManager.Instance.GuardarPlanetaEnFirebase(planetSelectable);
        }
        else
        {
            Debug.LogWarning("PlanetSpawner: el planeta instanciado no tiene el script PlanetSelectable.");
        }

        ultimoPlaneta = nuevoPlaneta.transform;
        CameraViewTransition.Instance?.RecalcularLimites();
    }

    /// <summary>
    /// Elige un índice aleatorio del array modelosPlanetaLowPoly y se lo aplica
    /// al planeta. Si el array está vacío, no hace nada (el planeta conserva el
    /// mesh por defecto del prefab).
    /// </summary>
    private void AplicarModeloAleatorio(PlanetSelectable planeta)
    {
        if (modelosPlanetaLowPoly == null || modelosPlanetaLowPoly.Length == 0) return;
        int idx = Random.Range(0, modelosPlanetaLowPoly.Length);
        var prefab = modelosPlanetaLowPoly[idx];
        if (prefab == null) return;
        planeta.AplicarModeloLowPoly(prefab, idx);
    }

    /// <summary>
    /// Aplica un modelo low poly específico (por índice del array). Usado al
    /// cargar planetas desde Firebase: si el doc tiene idModeloVisual, se usa.
    /// Si idModelo está fuera de rango o array vacío, no aplica nada.
    /// </summary>
    private void AplicarModeloPorIndice(PlanetSelectable planeta, int idModelo)
    {
        if (modelosPlanetaLowPoly == null || modelosPlanetaLowPoly.Length == 0) return;
        if (idModelo < 0 || idModelo >= modelosPlanetaLowPoly.Length) return;
        var prefab = modelosPlanetaLowPoly[idModelo];
        if (prefab == null) return;
        planeta.AplicarModeloLowPoly(prefab, idModelo);
    }

    public void CargarPlanetasDesdeNube()
    {
        string codigo = AulaDataManager.Instance.GetCodigoAula();
        if (string.IsNullOrEmpty(codigo)) return;
        

        FirebaseManager.Instance.db
            .Collection("artifacts").Document("adaeternum")
            .Collection("public").Document("data")
            .Collection("Aulas").Document(codigo)
            .Collection("planetas").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { Debug.LogError("CargarPlanetasDesdeNube error: " + task.Exception?.Message); return; }
            if (task.IsCompleted)
            {
                foreach (Transform hijo in contenedorPlanetas) { Destroy(hijo.gameObject); }

                CreadorPreguntas creador = FindFirstObjectByType<CreadorPreguntas>(FindObjectsInactive.Include);
                if (creador == null) Debug.LogWarning("[PlanetSpawner] CreadorPreguntas no encontrado en la escena.");

                foreach (var doc in task.Result.Documents)
                {
                    var datos = doc.ToDictionary();
                    Vector3 pos = new Vector3(
                        float.Parse(datos["posX"].ToString()),
                        float.Parse(datos["posY"].ToString()),
                        float.Parse(datos["posZ"].ToString())
                    );

                    GameObject nuevoP = Instantiate(planetaPrefab, pos, Quaternion.identity, contenedorPlanetas);
                    nuevoP.transform.localScale = primerPlaneta.localScale * multiplicadorEscala;

                    // Actualizamos el ultimoPlaneta para que los nuevos que creemos se pongan al final
                    if(pos.x > ultimoPlaneta.position.x) ultimoPlaneta = nuevoP.transform;

                    PlanetSelectable scriptP = nuevoP.GetComponent<PlanetSelectable>();
                    if (scriptP != null)
                    {
                        bool bloqueado = datos.ContainsKey("bloqueado") && (bool)datos["bloqueado"];
                        scriptP.CargarDesdeFirebase(
                            datos["idUnico"].ToString(),
                            datos["nombre"].ToString(),
                            datos["descripcion"].ToString(),
                            bloqueado
                        );
                        scriptP.AsignarPuntoVistaCompartido(puntoVistaCamaraCompartido);

                        // Modelo visual: si el doc trae idModeloVisual, lo aplicamos.
                        // Si NO lo trae (planeta antiguo creado antes de esta feature),
                        // generamos uno aleatorio aquí Y lo persistimos en Firebase para
                        // que en la siguiente carga todos los clientes vean el mismo.
                        if (datos.ContainsKey("idModeloVisual"))
                        {
                            int idModelo = System.Convert.ToInt32(datos["idModeloVisual"]);
                            AplicarModeloPorIndice(scriptP, idModelo);
                        }
                        else if (AulaDataManager.Instance != null && !AulaDataManager.Instance.EsAlumno)
                        {
                            // Migración: solo el profesor escribe (evita escrituras concurrentes
                            // desde múltiples clientes).
                            AplicarModeloAleatorio(scriptP);
                            AulaDataManager.Instance.GuardarPlanetaEnFirebase(scriptP);
                        }
                        else
                        {
                            // Alumno con doc antiguo: aplica un modelo aleatorio local
                            // (visible solo en su cliente hasta que el profesor lo persista).
                            AplicarModeloAleatorio(scriptP);
                        }

                        // Cargar configuración de combate (con defaults si no existe en Firebase)
                        bool tieneConfigCombate = datos.ContainsKey("configCombate") && datos["configCombate"] is Dictionary<string, object>;
                        if (tieneConfigCombate)
                            scriptP.AsignarConfigCombate(ConfigCombatePlaneta.FromDict(datos["configCombate"] as Dictionary<string, object>));
                        else
                            scriptP.AsignarConfigCombate(ConfigCombatePlaneta.ConfigDefault());

                        // Migración automática silenciosa: si el planeta no tiene configCombate
                        // y el cliente local es el profesor, escribimos los defaults en Firebase.
                        if (!tieneConfigCombate && AulaDataManager.Instance != null && !AulaDataManager.Instance.EsAlumno)
                        {
                            doc.Reference.SetAsync(
                                new Dictionary<string, object> { { "configCombate", scriptP.ConfigCombate.ToDict() } },
                                Firebase.Firestore.SetOptions.MergeAll
                            ).ContinueWithOnMainThread(t =>
                            {
                                if (t.IsFaulted) Debug.LogWarning("[PlanetSpawner] Migración configCombate falló para " + scriptP.IdUnico + ": " + t.Exception?.Message);
                            });
                        }

                        // --- CARGA DE PREGUNTAS ---
                        doc.Reference.Collection("preguntas").GetSnapshotAsync().ContinueWithOnMainThread(pregTask =>
                        {
                            if (pregTask.IsFaulted) { Debug.LogError("CargarPreguntas error: " + pregTask.Exception?.Message); return; }
                            if (pregTask.IsCompleted)
                            {
                                if (creador != null)
                                {
                                    foreach (var pregDoc in pregTask.Result.Documents)
                                    {
                                        var d = pregDoc.ToDictionary();

                                        // 1. Extraemos y convertimos las opciones
                                        var opcionesRaw = (List<object>)d["opciones"];
                                        string[] opcionesArray = opcionesRaw.ConvertAll(x => x.ToString()).ToArray();

                                        // 2. Creamos la pregunta usando tu constructor de 5 parámetros
                                        float tiempo  = d.ContainsKey("tiempoLimite")     ? System.Convert.ToSingle(d["tiempoLimite"])  : 30f;
                                        int puntos    = d.ContainsKey("puntosPorAcierto") ? System.Convert.ToInt32(d["puntosPorAcierto"]) : 10;

                                        Pregunta nuevaP = new Pregunta(
                                            d["id"].ToString(),
                                            d["idPlaneta"].ToString(),
                                            d["enunciado"].ToString(),
                                            opcionesArray,
                                            System.Convert.ToInt32(d["correcta"]),
                                            tiempo,
                                            puntos
                                        );

                                        // 3. Añadimos a la biblioteca local
                                        if (!creador.bibliotecaLocal.Exists(p => p.id == nuevaP.id))
                                        {
                                            creador.bibliotecaLocal.Add(nuevaP);
                                        }
                                    }
                                    
                                }
                            }
                        });
                    }
                }

                CameraViewTransition.Instance?.RecalcularLimites();
                IniciarListenerNiebla(codigo);
            }
        });
    }

    private void IniciarListenerNiebla(string codigo)
    {
        _listenerPlanetas?.Stop();
        _listenerPlanetas = FirebaseManager.Instance.db
            .Collection("artifacts").Document("adaeternum")
            .Collection("public").Document("data")
            .Collection("Aulas").Document(codigo)
            .Collection("planetas")
            .Listen(snapshot =>
            {
                foreach (var change in snapshot.GetChanges())
                {
                    // ── BORRADO: destruir el planeta local ─────────────────
                    if (change.ChangeType == Firebase.Firestore.DocumentChange.Type.Removed)
                    {
                        string idBorrado = change.Document.Id; // doc id = idUnico
                        foreach (Transform t in contenedorPlanetas)
                        {
                            var p = t.GetComponent<PlanetSelectable>();
                            if (p != null && p.IdUnico == idBorrado)
                            {
                                // Si era el seleccionado, limpiar selección.
                                if (PlanetSelectionManager.Instance != null
                                    && PlanetSelectionManager.Instance.ObtenerPlanetaActual() == p)
                                {
                                    PlanetSelectionManager.Instance.LimpiarUI();
                                }
                                Destroy(t.gameObject);
                                break;
                            }
                        }
                        CameraViewTransition.Instance?.RecalcularLimites();
                        continue;
                    }

                    // ── MODIFICADO: bloqueado y/o posición ─────────────────
                    if (change.ChangeType != Firebase.Firestore.DocumentChange.Type.Modified) continue;
                    var datos = change.Document.ToDictionary();
                    if (!datos.ContainsKey("idUnico")) continue;
                    string id       = datos["idUnico"].ToString();
                    bool bloqueado  = datos.ContainsKey("bloqueado") && (bool)datos["bloqueado"];

                    // Posición actualizada (tras recolocación por borrado).
                    bool tienePos = datos.ContainsKey("posX") && datos.ContainsKey("posY") && datos.ContainsKey("posZ");
                    Vector3 nuevaPos = Vector3.zero;
                    if (tienePos)
                    {
                        nuevaPos = new Vector3(
                            System.Convert.ToSingle(datos["posX"]),
                            System.Convert.ToSingle(datos["posY"]),
                            System.Convert.ToSingle(datos["posZ"])
                        );
                    }

                    foreach (Transform t in contenedorPlanetas)
                    {
                        PlanetSelectable p = t.GetComponent<PlanetSelectable>();
                        if (p != null && p.IdUnico == id)
                        {
                            if (bloqueado) p.Bloquear(); else p.Desbloquear();
                            if (tienePos && (t.position - nuevaPos).sqrMagnitude > 0.001f)
                                t.position = nuevaPos;
                            break;
                        }
                    }
                }
            });
    }

    public Transform GetContenedor()
    {
        return contenedorPlanetas;
    }

    /// <summary>
    /// Reorganiza los planetas para que estén espaciados uniformemente en X,
    /// "cerrando" el hueco si alguno se borró desde el medio. El profesor
    /// persiste las nuevas posiciones en Firebase para que los alumnos las
    /// reciban via listener. Idempotente — si ya estaban bien colocados, no
    /// cambia nada.
    /// </summary>
    public void RecolocarPlanetas()
    {
        if (contenedorPlanetas == null || primerPlaneta == null) return;

        // Recolectar todos los planetas del contenedor, ordenados por X actual.
        var planetas = new List<PlanetSelectable>();
        foreach (Transform t in contenedorPlanetas)
        {
            var p = t.GetComponent<PlanetSelectable>();
            if (p != null) planetas.Add(p);
        }
        planetas.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        if (planetas.Count == 0)
        {
            ultimoPlaneta = primerPlaneta;
            CameraViewTransition.Instance?.RecalcularLimites();
            return;
        }

        // El primer planeta del contenedor pasa a la posición inicial (tras el primerPlaneta de escena).
        float baseX = primerPlaneta.position.x + separacionX;
        bool soyProfesor = AulaDataManager.Instance != null && !AulaDataManager.Instance.EsAlumno;

        for (int i = 0; i < planetas.Count; i++)
        {
            Vector3 pos = planetas[i].transform.position;
            float nuevaX = baseX + i * separacionX;
            if (Mathf.Abs(pos.x - nuevaX) > 0.001f)
            {
                pos.x = nuevaX;
                planetas[i].transform.position = pos;
                // Solo el profesor escribe — los alumnos reciben las nuevas posiciones por listener.
                if (soyProfesor)
                    AulaDataManager.Instance.GuardarPlanetaEnFirebase(planetas[i]);
            }
        }

        ultimoPlaneta = planetas[planetas.Count - 1].transform;
        CameraViewTransition.Instance?.RecalcularLimites();
    }
}