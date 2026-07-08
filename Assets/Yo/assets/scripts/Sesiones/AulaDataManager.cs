using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using System.Linq;

public class AulaDataManager : MonoBehaviour
{
    public static AulaDataManager Instance;

    private string codigoAulaActual;
    private string idAlumnoLocal;
    private string nombreAlumnoLocal;
    private string idSesionActual;

    private ListenerRegistration listenerAlumnos;
    private ListenerRegistration listenerFlotas;
    private ListenerRegistration listenerSesion;
    private ListenerRegistration listenerMensajes;

    [Header("Referencias de Escena")]
    [SerializeField] private PlanetSpawner spawner;
    [SerializeField] private CreadorPreguntas creador;

    public List<Dictionary<string, object>> alumnosDisponibles = new List<Dictionary<string, object>>();
    public List<Flota> flotasActivas = new List<Flota>();

    private bool SinAula => string.IsNullOrEmpty(codigoAulaActual);

    /// <summary>True solo si el SDK de Firebase está listo. Evita NullReferenceException
    /// si se llama a una operación mientras Firebase reconecta o aún no ha inicializado.</summary>
    private bool FirebaseListo => FirebaseManager.Instance != null && FirebaseManager.Instance.db != null;

    private DocumentReference AulaDocRef =>
        FirebaseManager.Instance.db
            .Collection("artifacts").Document("adaeternum")
            .Collection("public").Document("data")
            .Collection("Aulas").Document(codigoAulaActual);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetCodigoAula(string codigo) => codigoAulaActual = codigo;
    public string GetCodigoAula() => codigoAulaActual;
    public string GetIdAlumnoLocal() => idAlumnoLocal;
    public bool EsAlumno => !string.IsNullOrEmpty(idAlumnoLocal);

    public (string nombre, string idFlota, string nombreFlota) GetDatosAlumnoLocal()
    {
        // Primero buscar en la lista local (lado profesor)
        var alumno = alumnosDisponibles.FirstOrDefault(a => a.ContainsKey("id") && a["id"].ToString() == idAlumnoLocal);
        if (alumno != null)
        {
            string idFlotaL    = alumno.ContainsKey("idFlota") ? alumno["idFlota"].ToString() : "";
            string nombreFlotaL = "";
            if (!string.IsNullOrEmpty(idFlotaL))
            {
                Flota flota = flotasActivas.FirstOrDefault(f => f.id == idFlotaL);
                nombreFlotaL = flota != null ? flota.nombre : "";
            }
            return (nombreAlumnoLocal, idFlotaL, nombreFlotaL);
        }

        // Lado alumno: usar nombre guardado localmente, flota vacía (se consulta async en GuardarHistorialCombate)
        return (nombreAlumnoLocal ?? "Desconocido", "", "");
    }

    // ── Planetas ────────────────────────────────────────────────────────────

    public void GuardarPlanetaEnFirebase(PlanetSelectable planeta)
    {
        if (SinAula) { Debug.LogError("No se puede guardar: no hay código de aula activo."); return; }

        ConfigCombatePlaneta cfg = planeta.ConfigCombate ?? ConfigCombatePlaneta.ConfigDefault();

        Dictionary<string, object> datos = new Dictionary<string, object>
        {
            { "idUnico",         planeta.IdUnico },
            { "nombre",          planeta.NombrePlaneta },
            { "descripcion",    planeta.DescripcionPlaneta },
            { "posX",            planeta.transform.position.x },
            { "posY",            planeta.transform.position.y },
            { "posZ",            planeta.transform.position.z },
            { "bloqueado",       planeta.Bloqueado },
            { "configCombate",   cfg.ToDict() },
            { "idModeloVisual",  planeta.IdModeloVisual }
        };

        AulaDocRef.Collection("planetas").Document(planeta.IdUnico)
            .SetAsync(datos, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al guardar planeta: " + task.Exception);
                    Toast.Show("Error al guardar el planeta. Reintenta.", 3f, Toast.Tipo.Error);
                }
            });
    }

    public void SetBloqueadoPlaneta(string idPlaneta, bool bloqueado)
    {
        if (SinAula || string.IsNullOrEmpty(idPlaneta)) return;
        AulaDocRef.Collection("planetas").Document(idPlaneta)
            .UpdateAsync(new Dictionary<string, object> { { "bloqueado", bloqueado } })
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Error al actualizar niebla: " + task.Exception);
            });
    }

    /// <summary>
    /// Borra un planeta de Firestore (doc completo + sub-colección de preguntas).
    /// Todos los clientes detectarán el borrado vía el listener de planetas y
    /// destruirán el GameObject local correspondiente.
    /// </summary>
    public void BorrarPlaneta(string idPlaneta)
    {
        if (SinAula || string.IsNullOrEmpty(idPlaneta)) return;
        var docRef = AulaDocRef.Collection("planetas").Document(idPlaneta);

        // Borrar la sub-colección "preguntas" del planeta primero (Firestore no
        // borra sub-colecciones en cascada — hay que hacerlo manual).
        docRef.Collection("preguntas").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && task.Result != null)
            {
                foreach (var pregDoc in task.Result.Documents)
                {
                    pregDoc.Reference.DeleteAsync(); // fire-and-forget
                }
            }
            // Tras intentar borrar preguntas, borrar el doc del planeta.
            docRef.DeleteAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("BorrarPlaneta error: " + t.Exception?.Message);
            });
        });
    }

    public void GuardarPreguntaEnFirebase(Pregunta preg)
    {
        if (SinAula) return;

        Dictionary<string, object> datos = new Dictionary<string, object>
        {
            { "id",          preg.id },
            { "idPlaneta",   preg.idPlaneta },
            { "enunciado",   preg.enunciado },
            { "opciones",    new List<string>(preg.opciones) },
            { "correcta",    preg.respuestaCorrecta },
            { "tiempoLimite",       preg.tiempoLimite },
            { "puntosPorAcierto",   preg.puntosPorAcierto }
        };

        AulaDocRef.Collection("planetas").Document(preg.idPlaneta)
            .Collection("preguntas").Document(preg.id)
            .SetAsync(datos).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Error al guardar pregunta: " + task.Exception);
            });
    }

    public void BorrarPreguntaDeFirebase(Pregunta preg)
    {
        if (SinAula) return;

        AulaDocRef.Collection("planetas").Document(preg.idPlaneta)
            .Collection("preguntas").Document(preg.id)
            .DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("Error al borrar pregunta: " + task.Exception);
            });
    }

    public void GuardarEscenaCompleta(Transform contenedorPlanetas, List<Pregunta> todasLasPreguntas)
    {
        if (SinAula) { Debug.LogError("No hay código de aula activo."); return; }

        WriteBatch batch = FirebaseManager.Instance.db.StartBatch();

        foreach (Transform t in contenedorPlanetas)
        {
            PlanetSelectable p = t.GetComponent<PlanetSelectable>();
            if (p == null) continue;

            DocumentReference pRef = AulaDocRef.Collection("planetas").Document(p.IdUnico);

            ConfigCombatePlaneta cfg = p.ConfigCombate ?? ConfigCombatePlaneta.ConfigDefault();

            batch.Set(pRef, new Dictionary<string, object>
            {
                { "idUnico",       p.IdUnico },
                { "nombre",        p.NombrePlaneta },
                { "descripcion",   p.DescripcionPlaneta },
                { "posX",          t.position.x },
                { "posY",          t.position.y },
                { "posZ",          t.position.z },
                { "bloqueado",     p.Bloqueado },
                { "configCombate", cfg.ToDict() }
            }, SetOptions.MergeAll);

            foreach (Pregunta preg in todasLasPreguntas.FindAll(pre => pre.idPlaneta == p.IdUnico))
            {
                batch.Set(pRef.Collection("preguntas").Document(preg.id), new Dictionary<string, object>
                {
                    { "id",          preg.id },
                    { "idPlaneta",   preg.idPlaneta },
                    { "enunciado",   preg.enunciado },
                    { "opciones",    new List<string>(preg.opciones) },
                    { "correcta",    preg.respuestaCorrecta },
                    { "tiempoLimite",       preg.tiempoLimite },
            { "puntosPorAcierto",   preg.puntosPorAcierto }
                });
            }
        }

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) Debug.LogError("Error al guardar escena: " + task.Exception);
            else Debug.Log("<color=cyan>Firebase: escena y preguntas guardadas.</color>");
        });
    }

    public void BotonMaestroGuardarTodo()
    {
        if (spawner != null && creador != null)
            GuardarEscenaCompleta(spawner.GetContenedor(), creador.bibliotecaLocal);
        else
            Debug.LogError("Falta el Spawner o el Creador en el Inspector de AulaDataManager.");
    }

    // ── Alumnos ─────────────────────────────────────────────────────────────

    public void ValidarYEntrarAulaAlumno(string codigoInput, Action<bool> callback)
    {
        FirebaseManager.Instance.db
            .Collection("artifacts").Document("adaeternum")
            .Collection("public").Document("data")
            .Collection("Aulas").Document(codigoInput)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) { Debug.LogError("ValidarYEntrarAulaAlumno error: " + task.Exception?.Message); Toast.Show("Error de conexión al validar el aula.", 3f, Toast.Tipo.Error); callback(false); return; }
                if (task.Result != null && task.Result.Exists)
                {
                    codigoAulaActual = codigoInput;
                    Debug.Log("<color=green>Aula encontrada. Cargando contenido...</color>");
                    PlanetSpawner.Instance?.CargarPlanetasDesdeNube();
                    callback(true);
                }
                else
                {
                    Debug.LogWarning("El código de aula no existe.");
                    callback(false);
                }
            });
    }

    public void RegistrarEstudianteEnNube(string nombreAlumno, Action<bool> callback)
    {
        if (SinAula) { callback(false); return; }

        string nombreNormalizado = nombreAlumno.ToLower().Trim();

        AulaDocRef.Collection("alumnos")
            .WhereEqualTo("nombre", nombreNormalizado)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) { Debug.LogError("RegistrarEstudianteEnNube error: " + task.Exception?.Message); callback(false); return; }

                if (task.Result.Count > 0)
                {
                    var doc = task.Result.Documents.First();
                    idAlumnoLocal = doc.Id;
                    nombreAlumnoLocal = doc.ContainsField("nombre") ? doc.GetValue<string>("nombre") : nombreNormalizado;
                    Debug.Log($"<color=yellow>Alumno detectado (insensible a mayúsculas). Usando ID: {idAlumnoLocal}</color>");
                    callback(true);
                }
                else
                {
                    CrearNuevoRegistroAlumno(nombreNormalizado, callback);
                }
            });
    }

    private void CrearNuevoRegistroAlumno(string nombreAlumno, Action<bool> callback)
    {
        idAlumnoLocal = Guid.NewGuid().ToString();
        nombreAlumnoLocal = nombreAlumno;

        AulaDocRef.Collection("alumnos").Document(idAlumnoLocal)
            .SetAsync(new Dictionary<string, object>
            {
                { "id",             idAlumnoLocal },
                { "nombre",         nombreAlumno },
                { "idFlota",        "" },
                { "rol",            "miembro" },
                { "fechaRegistro",  FieldValue.ServerTimestamp }
            }).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) { Debug.LogError("Error al crear alumno: " + task.Exception); callback(false); }
                else callback(true);
            });
    }

    private event Action _onAlumnosActualizado;

    public void EscucharAlumnos(Action onActualizado)
    {
        if (SinAula) return;
        if (onActualizado == null) return;

        // Multi-suscriptor: añadimos el callback al evento. Si el listener no existe aún,
        // lo creamos. Si ya existe, el nuevo suscriptor se entera con la próxima actualización
        // y de paso le disparamos uno inmediato si ya hay datos.
        _onAlumnosActualizado -= onActualizado;
        _onAlumnosActualizado += onActualizado;

        if (listenerAlumnos == null)
        {
            listenerAlumnos = AulaDocRef.Collection("alumnos").Listen(snapshot =>
            {
                if (snapshot == null) return;
                alumnosDisponibles.Clear();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                    alumnosDisponibles.Add(doc.ToDictionary());
                _onAlumnosActualizado?.Invoke();
            });
        }
        else if (alumnosDisponibles.Count > 0)
        {
            // Ya hay datos cacheados: avisa al nuevo suscriptor inmediatamente
            onActualizado.Invoke();
        }
    }

    // ── Flotas ──────────────────────────────────────────────────────────────

    public void CrearNuevaFlotaEnNube(string nombre, int max)
    {
        if (SinAula) return;

        // Salvaguarda de límites (por si se llama saltándose la validación de la UI).
        if (flotasActivas.Count >= PanelControlFlotas.MAX_FLOTAS)
        {
            Toast.Show($"Máximo {PanelControlFlotas.MAX_FLOTAS} flotas permitidas.", 3f, Toast.Tipo.Error);
            return;
        }
        max = Mathf.Clamp(max, 1, PanelControlFlotas.MAX_ALUMNOS_POR_FLOTA);

        string idFlota = Guid.NewGuid().ToString();
        Flota nueva = new Flota(idFlota, nombre, max);

        AulaDocRef.Collection("flotas").Document(idFlota)
            .SetAsync(new Dictionary<string, object>
            {
                { "id",         nueva.id },
                { "nombre",     nueva.nombre },
                { "maxAlumnos", nueva.maxAlumnos },
                { "liderID",    nueva.liderID },
                { "alumnos",    nueva.alumnos }
            }).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al crear flota: " + task.Exception);
                    Toast.Show("Error al crear la flota.", 3f, Toast.Tipo.Error);
                }
                else
                {
                    Toast.Show($"Flota '{nombre}' creada.", 2f, Toast.Tipo.Exito);
                }
            });
    }

    public void BorrarFlota(string idFlota)
    {
        if (SinAula) return;

        DocumentReference flotaRef = AulaDocRef.Collection("flotas").Document(idFlota);

        flotaRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists) return;

            WriteBatch batch = FirebaseManager.Instance.db.StartBatch();

            var data = task.Result.ToDictionary();
            if (data.ContainsKey("alumnos"))
            {
                foreach (var id in (List<object>)data["alumnos"])
                    batch.Update(AulaDocRef.Collection("alumnos").Document(id.ToString()), "idFlota", "");
            }

            batch.Delete(flotaRef);
            batch.CommitAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("Error al borrar flota: " + t.Exception);
            });
        });
    }

    public void BorrarTodasLasFlotas()
    {
        if (SinAula) return;

        AulaDocRef.Collection("flotas").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted) return;
            foreach (DocumentSnapshot doc in task.Result.Documents)
                doc.Reference.DeleteAsync();
        });
    }

    public void QuitarAlumnoDeFlota(string idAlumno, string idFlota)
    {
        if (SinAula) return;

        WriteBatch batch = FirebaseManager.Instance.db.StartBatch();
        batch.Update(AulaDocRef.Collection("alumnos").Document(idAlumno), "idFlota", "");
        batch.Update(AulaDocRef.Collection("flotas").Document(idFlota),
            new Dictionary<string, object> { { "alumnos", FieldValue.ArrayRemove(idAlumno) } });

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) Debug.LogError("Error al quitar alumno de flota: " + task.Exception);
        });
    }

    public void AsignarAlumnoAFlota(string idAlumno, string idFlota)
    {
        if (SinAula) return;

        // Salvaguarda de aforo (por si se llama saltándose la validación de la UI).
        Flota flotaDestino = flotasActivas.Find(f => f.id == idFlota);
        if (flotaDestino != null)
        {
            int cap = Mathf.Min(flotaDestino.maxAlumnos, PanelControlFlotas.MAX_ALUMNOS_POR_FLOTA);
            if (!flotaDestino.alumnos.Contains(idAlumno) && flotaDestino.alumnos.Count >= cap)
            {
                Toast.Show($"La flota '{flotaDestino.nombre}' está llena.", 3f, Toast.Tipo.Aviso);
                return;
            }
        }

        AulaDocRef.Collection("alumnos").Document(idAlumno)
            .UpdateAsync("idFlota", idFlota).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Error al actualizar flota del alumno: " + task.Exception);
            });

        AulaDocRef.Collection("flotas").Document(idFlota)
            .UpdateAsync("alumnos", FieldValue.ArrayUnion(idAlumno)).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Error al asignar alumno a flota: " + task.Exception);
            });
    }

    public void DefinirLiderDeFlota(string idFlota, string idNuevoLider)
    {
        if (SinAula) return;

        AulaDocRef.Collection("flotas").Document(idFlota)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted || !task.Result.Exists) return;

                string idLiderAnterior = task.Result.GetValue<string>("liderID");
                WriteBatch batch = FirebaseManager.Instance.db.StartBatch();

                if (!string.IsNullOrEmpty(idLiderAnterior))
                    batch.Update(AulaDocRef.Collection("alumnos").Document(idLiderAnterior), "rol", "miembro");

                batch.Update(AulaDocRef.Collection("alumnos").Document(idNuevoLider), "rol", "lider");
                batch.Update(AulaDocRef.Collection("flotas").Document(idFlota), "liderID", idNuevoLider);

                batch.CommitAsync().ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted) Debug.LogError("Error al definir líder: " + t.Exception);
                });
            });
    }

    /// <summary>
    /// Calcula 1-5 estrellas para una flota a partir de la precisión media (aciertos/total)
    /// de todos los registros de historial cuyo idFlota coincide.
    /// Si no hay datos, devuelve 1 estrella.
    /// </summary>
    public void CalcularPuntuacionFlota(string idFlota, Action<int> callback)
    {
        if (SinAula || string.IsNullOrEmpty(idFlota)) { callback?.Invoke(1); return; }

        AulaDocRef.Collection("historial")
            .WhereEqualTo("idFlota", idFlota)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("CalcularPuntuacionFlota error: " + task.Exception?.Message);
                    callback?.Invoke(1);
                    return;
                }

                long aciertosTotales = 0;
                long preguntasTotales = 0;

                foreach (var doc in task.Result.Documents)
                {
                    var d = doc.ToDictionary();
                    if (d.ContainsKey("aciertos")) aciertosTotales += Convert.ToInt64(d["aciertos"]);
                    if (d.ContainsKey("total"))    preguntasTotales += Convert.ToInt64(d["total"]);
                }

                int estrellas;
                if (preguntasTotales <= 0)
                {
                    estrellas = 1;
                }
                else
                {
                    float precision = (float)aciertosTotales / preguntasTotales; // 0..1
                    if      (precision >= 0.80f) estrellas = 5;
                    else if (precision >= 0.60f) estrellas = 4;
                    else if (precision >= 0.40f) estrellas = 3;
                    else if (precision >= 0.20f) estrellas = 2;
                    else                          estrellas = 1;
                }

                callback?.Invoke(estrellas);
            });
    }

    public void SetRolCombateAlumno(string idAlumno, string rolCombate)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumno)) return;

        AulaDocRef.Collection("alumnos").Document(idAlumno)
            .UpdateAsync("rolCombate", rolCombate)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("Error al asignar rolCombate: " + t.Exception);
            });
    }

    private event Action _onFlotasActualizado;

    public void EscucharFlotas(Action callback)
    {
        if (SinAula) return;
        if (callback == null) return;

        _onFlotasActualizado -= callback;
        _onFlotasActualizado += callback;

        if (listenerFlotas == null)
        {
            listenerFlotas = AulaDocRef.Collection("flotas").Listen(snapshot =>
            {
                if (snapshot == null) return;
                flotasActivas.Clear();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    var data = doc.ToDictionary();
                    Flota f = new Flota(doc.Id, data["nombre"].ToString(), Convert.ToInt32(data["maxAlumnos"]));
                    f.liderID = data.ContainsKey("liderID") ? data["liderID"].ToString() : "";
                    if (data.ContainsKey("alumnos"))
                        f.alumnos = ((List<object>)data["alumnos"]).ConvertAll(x => x.ToString());
                    flotasActivas.Add(f);
                }
                _onFlotasActualizado?.Invoke();
            });
        }
        else if (flotasActivas.Count > 0)
        {
            callback.Invoke();
        }
    }

    // ── Combate ─────────────────────────────────────────────────────────────

    public void SincronizarEstadoCombate()
    {
        if (SinAula) return;

        AulaDocRef.Collection("sesion").Document("combate")
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.Result.Exists) return;

                AulaDocRef.Collection("sesion").Document("combate")
                    .SetAsync(new Dictionary<string, object>
                    {
                        { "estado",    "esperando" },
                        { "idPlaneta", "" },
                        { "idSesion",  "" }
                    });
            });
    }

    public void IniciarCombateEnAula(string idPlaneta)
    {
        if (SinAula) return;

        idSesionActual = Guid.NewGuid().ToString();

        AulaDocRef.Collection("sesion").Document("combate")
            .SetAsync(new Dictionary<string, object>
            {
                { "estado",    "enCombate" },
                { "idPlaneta", idPlaneta },
                { "idSesion",  idSesionActual }
            }, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al iniciar combate: " + task.Exception);
                    Toast.Show("Error al iniciar el combate. Reintenta.", 3f, Toast.Tipo.Error);
                }
            });
    }

    public void DetenerCombateEnAula()
    {
        if (SinAula) return;

        AulaDocRef.Collection("sesion").Document("combate")
            .SetAsync(new Dictionary<string, object>
            {
                { "estado",    "esperando" },
                { "idPlaneta", "" },
                { "idSesion",  "" }
            }, SetOptions.MergeAll);
    }

    private event Action<string, string, string> _onEstadoCombate;
    private string _ultEstadoCombate = "", _ultIdPlanetaCombate = "", _ultIdSesionCombate = "";

    public void EscucharEstadoCombate(Action<string, string, string> callback)
    {
        if (SinAula || callback == null) return;

        // Multi-suscriptor: añadir callback al evento. Crear listener si aún no existe.
        _onEstadoCombate -= callback;
        _onEstadoCombate += callback;

        if (listenerSesion == null)
        {
            listenerSesion = AulaDocRef.Collection("sesion").Document("combate").Listen(snapshot =>
            {
                if (snapshot == null || !snapshot.Exists) return;
                string estado    = snapshot.ContainsField("estado")    ? snapshot.GetValue<string>("estado")    : "esperando";
                string idPlaneta = snapshot.ContainsField("idPlaneta") ? snapshot.GetValue<string>("idPlaneta") : "";
                string idSesion  = snapshot.ContainsField("idSesion")  ? snapshot.GetValue<string>("idSesion")  : "";
                idSesionActual = idSesion;
                _ultEstadoCombate = estado;
                _ultIdPlanetaCombate = idPlaneta;
                _ultIdSesionCombate = idSesion;
                _onEstadoCombate?.Invoke(estado, idPlaneta, idSesion);
            });
        }
        else if (!string.IsNullOrEmpty(_ultEstadoCombate))
        {
            // Avisa al nuevo suscriptor con el último estado conocido inmediatamente
            callback.Invoke(_ultEstadoCombate, _ultIdPlanetaCombate, _ultIdSesionCombate);
        }
    }

    // ── Combate Tipo 1 (Asalto planetario) ─────────────────────────────────
    // Estado autoritativo por flota durante un combate activo.
    // Profesor escribe (CombateAsaltoManager). Alumnos leen via listener
    // y escriben sus propias acciones (recarga escudo, energia ataque, etc.).

    private DocumentReference RefEstadoFlota(string idSesion, string idFlota) =>
        AulaDocRef.Collection("combateActivo").Document(idSesion)
            .Collection("flotas").Document(idFlota);

    /// <summary>Profesor: crea el documento inicial de estado de una flota.</summary>
    public void CrearEstadoFlotaCombate(EstadoFlotaCombate estado)
    {
        if (SinAula || estado == null) return;
        RefEstadoFlota(estado.idSesion, estado.idFlota)
            .SetAsync(estado.ToDict())
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("CrearEstadoFlotaCombate error: " + t.Exception?.Message);
            });
    }

    /// <summary>Marca solo el campo "estado" del documento de la flota (best-effort, sin transacción).</summary>
    public void MarcarEstadoFlotaCombate(string idSesion, string idFlota, string nuevoEstado)
    {
        if (SinAula || string.IsNullOrEmpty(idSesion) || string.IsNullOrEmpty(idFlota)) return;
        RefEstadoFlota(idSesion, idFlota)
            .UpdateAsync("estado", nuevoEstado)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("MarcarEstadoFlotaCombate error: " + t.Exception?.Message);
            });
    }

    /// <summary>Profesor: sobrescribe completamente el estado de una flota tras tick.</summary>
    public void EscribirEstadoFlotaCombate(EstadoFlotaCombate estado)
    {
        if (SinAula || estado == null) return;
        RefEstadoFlota(estado.idSesion, estado.idFlota)
            .SetAsync(estado.ToDict(), SetOptions.MergeAll)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("EscribirEstadoFlotaCombate error: " + t.Exception?.Message);
            });
    }

    /// <summary>
    /// Escritura PARCIAL del estado que hace el manager autoritativo (líder) cada tick.
    /// Excluye los campos que gestionan los alumnos via transacciones atómicas
    /// (zonas, energiaAtaquePorAlumno), para no pisar sus cambios concurrentes.
    /// Esto arregla el bug en el que un ataque del atacante se perdía si llegaba
    /// justo entre el GetSnapshot del listener del líder y la siguiente escritura.
    ///
    /// Incluye además el heartbeat del líder (timestamp Unix ms actual) para que
    /// otros miembros sepan que sigue vivo. Si llevas varios segundos sin escribir
    /// (porque te desconectaste), otro miembro puede reclamar el rol.
    /// </summary>
    public void EscribirEstadoCombateDelManager(EstadoFlotaCombate estado)
    {
        if (SinAula || estado == null) return;

        var listaAtaques = new List<object>();
        foreach (var a in estado.ataquesEntrantes) listaAtaques.Add(a.ToDict());

        long ahora = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // SOLO los campos que el manager gestiona: escudo, vida nave, ataques
        // entrantes, cadencia y estado. NO incluye "zonas" ni
        // "energiaAtaquePorAlumno" — esos los modifican los alumnos via tx.
        // Incluye heartbeat actualizado para que el failover pueda detectar
        // si el líder cae.
        var camposManager = new Dictionary<string, object>
        {
            { "escudoActual",      estado.escudoActual },
            { "escudoMaximo",      estado.escudoMaximo },
            { "escudoMinimo",      estado.escudoMinimo },
            { "vidaNave",          estado.vidaNave },
            { "vidaNaveMaxima",    estado.vidaNaveMaxima },
            { "ataquesEntrantes",  listaAtaques },
            { "cadenciaActual",    estado.cadenciaActual },
            { "estado",            estado.estado ?? "activo" },
            { "liderHeartbeatMs",  ahora }
        };

        // Actualizamos también el heartbeat local para que el manager pueda
        // detectar fácilmente cuándo fue su última escritura.
        estado.liderHeartbeatMs = ahora;

        RefEstadoFlota(estado.idSesion, estado.idFlota)
            .SetAsync(camposManager, SetOptions.MergeAll)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("EscribirEstadoCombateDelManager error: " + t.Exception?.Message);
            });
    }

    /// <summary>
    /// Intenta reclamar el rol de manager autoritativo de una flota.
    /// Si el documento de la flota NO existe (primera vez del combate), lo
    /// crea ATÓMICAMENTE en la misma transacción con el reclamante como líder.
    ///
    /// Procede si:
    ///   a) El doc no existe (combate recién iniciado), o
    ///   b) Nadie está liderando aún (liderActivoId vacío), o
    ///   c) El líder actual lleva > umbralCaidoMs sin heartbeat.
    ///
    /// Atómica (transacción). Si dos miembros intentan a la vez, solo uno gana.
    ///
    /// callback(true, nuevoEpoch)  → reclamación exitosa, ahora eres el líder activo
    /// callback(false, -1)         → otro miembro está liderando con heartbeat fresco
    /// </summary>
    public void ReclamarLiderato(string idSesion, string idFlota, string idPlaneta, string idAlumno,
                                  long umbralCaidoMs, ConfigCombatePlaneta cfgParaCrear,
                                  Action<bool, int> callback)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumno))
        {
            callback?.Invoke(false, -1);
            return;
        }
        var docRef = RefEstadoFlota(idSesion, idFlota);
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                long ahora = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // ── Caso A: el doc no existe → crear con yo como líder, epoch 1 ──
                if (!snap.Exists)
                {
                    if (cfgParaCrear == null) throw new Exception("ReclamarLiderato: doc no existe y no se pasó cfgParaCrear");
                    var inicial = EstadoFlotaCombate.Crear(idSesion, idFlota, idPlaneta, cfgParaCrear);
                    inicial.liderActivoId    = idAlumno;
                    inicial.liderHeartbeatMs = ahora;
                    inicial.liderEpoch       = 1;
                    tx.Set(docRef, inicial.ToDict());
                    return 1;
                }

                // ── Caso B: el doc existe → comprobar si puedo reclamar ──
                string liderActual = snap.ContainsField("liderActivoId")
                    ? snap.GetValue<string>("liderActivoId") : "";
                long heartbeatActual = snap.ContainsField("liderHeartbeatMs")
                    ? snap.GetValue<long>("liderHeartbeatMs") : 0L;
                int epochActual = snap.ContainsField("liderEpoch")
                    ? snap.GetValue<int>("liderEpoch") : 0;

                bool hayLiderVivo = !string.IsNullOrEmpty(liderActual)
                                    && (ahora - heartbeatActual) <= umbralCaidoMs;

                // Si yo ya soy el líder activo con heartbeat fresco, no hago nada.
                if (liderActual == idAlumno && hayLiderVivo)
                    return epochActual;

                // Si hay un líder vivo distinto a mí, no puedo reclamar.
                if (hayLiderVivo && liderActual != idAlumno)
                    return -1;

                // Reclamamos.
                int nuevoEpoch = epochActual + 1;
                var updates = new Dictionary<string, object>
                {
                    { "liderActivoId",    idAlumno },
                    { "liderHeartbeatMs", ahora },
                    { "liderEpoch",       nuevoEpoch }
                };
                tx.Update(docRef, updates);
                return nuevoEpoch;
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("ReclamarLiderato error: " + t.Exception?.Message);
                callback?.Invoke(false, -1);
                return;
            }
            int epoch = t.Result;
            callback?.Invoke(epoch >= 0, epoch);
        });
    }

    private readonly Dictionary<string, ListenerRegistration> _listenersFlotaCombate = new Dictionary<string, ListenerRegistration>();
    private readonly Dictionary<string, Action<EstadoFlotaCombate>> _callbacksFlotaCombate = new Dictionary<string, Action<EstadoFlotaCombate>>();
    private readonly Dictionary<string, EstadoFlotaCombate> _ultimoEstadoFlotaCombate = new Dictionary<string, EstadoFlotaCombate>();

    /// <summary>Multi-suscriptor: escucha cambios en el estado de su flota durante el combate.</summary>
    public void EscucharEstadoFlotaCombate(string idSesion, string idFlota, Action<EstadoFlotaCombate> callback)
    {
        if (SinAula || string.IsNullOrEmpty(idSesion) || string.IsNullOrEmpty(idFlota) || callback == null) return;
        string key = idSesion + "|" + idFlota;

        // Añadir el callback al multicast de esta key
        if (_callbacksFlotaCombate.ContainsKey(key))
        {
            _callbacksFlotaCombate[key] -= callback; // evitar duplicados
            _callbacksFlotaCombate[key] += callback;
        }
        else
        {
            _callbacksFlotaCombate[key] = callback;
        }

        // Si todavía no hay listener para esta key, crearlo
        if (!_listenersFlotaCombate.ContainsKey(key))
        {
            var listener = RefEstadoFlota(idSesion, idFlota).Listen(snapshot =>
            {
                if (snapshot == null || !snapshot.Exists)
                {
                    if (_callbacksFlotaCombate.ContainsKey(key))
                        _callbacksFlotaCombate[key]?.Invoke(null);
                    return;
                }
                var dict = snapshot.ToDictionary();
                EstadoFlotaCombate e = EstadoFlotaCombate.FromDict(dict);
                _ultimoEstadoFlotaCombate[key] = e;
                if (_callbacksFlotaCombate.ContainsKey(key))
                    _callbacksFlotaCombate[key]?.Invoke(e);
            });
            _listenersFlotaCombate[key] = listener;
        }
        else if (_ultimoEstadoFlotaCombate.TryGetValue(key, out var cached) && cached != null)
        {
            // Ya hay listener: avisa al nuevo suscriptor con el último estado conocido
            callback.Invoke(cached);
        }
    }

    public void DetenerListenerEstadoFlota(string idSesion, string idFlota)
    {
        string key = idSesion + "|" + idFlota;
        if (_listenersFlotaCombate.ContainsKey(key))
        {
            _listenersFlotaCombate[key].Stop();
            _listenersFlotaCombate.Remove(key);
        }
        _callbacksFlotaCombate.Remove(key);
        _ultimoEstadoFlotaCombate.Remove(key);
    }

    /// <summary>Alumno (defensor): suma X al escudoActual, capped a escudoMaximo. Atómico.</summary>
    public void RecargarEscudo(string idSesion, string idFlota, float cantidad)
    {
        if (SinAula || cantidad <= 0f) return;
        if (!FirebaseListo) { Debug.LogWarning("RecargarEscudo: Firebase no está listo; se omite la recarga."); return; }
        var docRef = RefEstadoFlota(idSesion, idFlota);
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists) return;
                float actual = snap.ContainsField("escudoActual") ? snap.GetValue<float>("escudoActual") : 0f;
                float max    = snap.ContainsField("escudoMaximo") ? snap.GetValue<float>("escudoMaximo") : 100f;
                float nuevo  = Mathf.Min(max, actual + cantidad);
                tx.Update(docRef, "escudoActual", (object)nuevo);
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("RecargarEscudo error: " + t.Exception?.Message);
        });
    }

    /// <summary>Alumno (atacante): suma X a su energía de ataque, capped a energiaAtaqueMaxima.</summary>
    public void IncrementarEnergiaAtaque(string idSesion, string idFlota, string idAlumno, float cantidad, float maxima)
    {
        if (SinAula || cantidad <= 0f || string.IsNullOrEmpty(idAlumno)) return;
        if (!FirebaseListo) { Debug.LogWarning("IncrementarEnergiaAtaque: Firebase no está listo; se omite."); return; }
        var docRef = RefEstadoFlota(idSesion, idFlota);
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists) return;
                Dictionary<string, object> energias = null;
                if (snap.ContainsField("energiaAtaquePorAlumno"))
                {
                    energias = snap.GetValue<Dictionary<string, object>>("energiaAtaquePorAlumno");
                }
                if (energias == null) energias = new Dictionary<string, object>();
                float actual = energias.ContainsKey(idAlumno) ? Convert.ToSingle(energias[idAlumno]) : 0f;
                float nuevo  = Mathf.Min(maxima, actual + cantidad);
                energias[idAlumno] = nuevo;
                tx.Update(docRef, "energiaAtaquePorAlumno", (object)energias);
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("IncrementarEnergiaAtaque error: " + t.Exception?.Message);
        });
    }

    /// <summary>
    /// Alumno (atacante): dispara — consume `costeEnergia` de su energía y aplica `dañoZona` a la zona indicada.
    /// Solo procede si tiene al menos `costeEnergia`. Si la zona ya está destruida, redirecciona a la primera viva.
    /// La energía NO se pone a 0; solo se descuenta el coste (permite acumular hasta el máximo y disparar varias veces).
    /// </summary>
    public void DispararAtaque(string idSesion, string idFlota, string idAlumno, int indiceZona, float dañoZona, float costeEnergia)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumno)) return;
        if (!FirebaseListo) { Debug.LogWarning("DispararAtaque: Firebase no está listo; se omite el disparo."); return; }
        var docRef = RefEstadoFlota(idSesion, idFlota);
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists) return;

                Dictionary<string, object> energias = snap.ContainsField("energiaAtaquePorAlumno")
                    ? snap.GetValue<Dictionary<string, object>>("energiaAtaquePorAlumno") : new Dictionary<string, object>();
                float energia = energias.ContainsKey(idAlumno) ? Convert.ToSingle(energias[idAlumno]) : 0f;
                if (energia < costeEnergia - 0.01f) return; // sin energía suficiente, abortar

                List<object> rawZonas = snap.ContainsField("zonas") ? snap.GetValue<List<object>>("zonas") : new List<object>();
                if (rawZonas == null || rawZonas.Count == 0) return;

                int objetivo = indiceZona;
                if (objetivo < 0 || objetivo >= rawZonas.Count) objetivo = 0;

                // Si está destruida, busca primera viva
                Dictionary<string, object> zObj = rawZonas[objetivo] as Dictionary<string, object>;
                float vidaSel = zObj != null && zObj.ContainsKey("vidaActual") ? Convert.ToSingle(zObj["vidaActual"]) : 0f;
                if (vidaSel <= 0f)
                {
                    objetivo = -1;
                    for (int i = 0; i < rawZonas.Count; i++)
                    {
                        var zd = rawZonas[i] as Dictionary<string, object>;
                        float v = zd != null && zd.ContainsKey("vidaActual") ? Convert.ToSingle(zd["vidaActual"]) : 0f;
                        if (v > 0f) { objetivo = i; break; }
                    }
                    if (objetivo < 0) return; // todas destruidas
                    zObj = rawZonas[objetivo] as Dictionary<string, object>;
                    vidaSel = Convert.ToSingle(zObj["vidaActual"]);
                }

                float vidaNueva = Mathf.Max(0f, vidaSel - dañoZona);
                zObj["vidaActual"] = vidaNueva;
                rawZonas[objetivo] = zObj;

                // Descontar coste de energía (permite acumular y hacer múltiples disparos)
                energias[idAlumno] = Mathf.Max(0f, energia - costeEnergia);

                tx.Update(docRef, "zonas", (object)rawZonas);
                tx.Update(docRef, "energiaAtaquePorAlumno", (object)energias);
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("DispararAtaque error: " + t.Exception?.Message);
        });
    }

    /// <summary>Alumno (defensor): asume un ataque entrante en exclusiva. Solo procede si idAsumidoPor está vacío.</summary>
    public void AsumirAtaqueEntrante(string idSesion, string idFlota, string idAtaque, string idAlumno)
    {
        if (SinAula || string.IsNullOrEmpty(idAtaque) || string.IsNullOrEmpty(idAlumno)) return;
        var docRef = RefEstadoFlota(idSesion, idFlota);
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists) return;
                List<object> raw = snap.ContainsField("ataquesEntrantes") ? snap.GetValue<List<object>>("ataquesEntrantes") : new List<object>();
                bool cambiado = false;
                for (int i = 0; i < raw.Count; i++)
                {
                    var a = raw[i] as Dictionary<string, object>;
                    if (a == null) continue;
                    string id = a.ContainsKey("id") ? a["id"].ToString() : "";
                    string asumido = a.ContainsKey("idAsumidoPor") ? a["idAsumidoPor"].ToString() : "";
                    if (id == idAtaque && string.IsNullOrEmpty(asumido))
                    {
                        a["idAsumidoPor"] = idAlumno;
                        raw[i] = a;
                        cambiado = true;
                        break;
                    }
                }
                if (cambiado) tx.Update(docRef, "ataquesEntrantes", (object)raw);
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("AsumirAtaqueEntrante error: " + t.Exception?.Message);
        });
    }

    /// <summary>
    /// Alumno (defensor): resuelve un ataque entrante.
    /// Si éxito → retira el ataque. Si fallo → retira el ataque y aplica daño a la nave.
    /// </summary>
    public void ResolverAtaqueEntrante(string idSesion, string idFlota, string idAtaque, bool exito, float dañoNaveSiFalla)
    {
        if (SinAula || string.IsNullOrEmpty(idAtaque)) return;
        var docRef = RefEstadoFlota(idSesion, idFlota);
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists) return;
                List<object> raw = snap.ContainsField("ataquesEntrantes") ? snap.GetValue<List<object>>("ataquesEntrantes") : new List<object>();
                List<object> nueva = new List<object>();
                bool encontrado = false;
                foreach (var a in raw)
                {
                    var ad = a as Dictionary<string, object>;
                    if (ad == null) continue;
                    string id = ad.ContainsKey("id") ? ad["id"].ToString() : "";
                    if (id == idAtaque) { encontrado = true; continue; } // se elimina
                    nueva.Add(ad);
                }
                // Si el ataque ya no existía (el manager lo eliminó por timeout), no aplicar daño extra
                if (!encontrado) return;

                tx.Update(docRef, "ataquesEntrantes", (object)nueva);

                if (!exito)
                {
                    // El daño va PRIMERO al escudo, sobrante a la nave (consistente con manager)
                    float vida   = snap.ContainsField("vidaNave")     ? snap.GetValue<float>("vidaNave")     : 0f;
                    float escudo = snap.ContainsField("escudoActual") ? snap.GetValue<float>("escudoActual") : 0f;
                    float dañoEscudo = Mathf.Min(escudo, dañoNaveSiFalla);
                    float nuevoEscudo = Mathf.Max(0f, escudo - dañoEscudo);
                    float sobrante = dañoNaveSiFalla - dañoEscudo;
                    float nuevaVida = Mathf.Max(0f, vida - sobrante);
                    tx.Update(docRef, "escudoActual", (object)nuevoEscudo);
                    if (sobrante > 0f)
                        tx.Update(docRef, "vidaNave", (object)nuevaVida);
                }
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("ResolverAtaqueEntrante error: " + t.Exception?.Message);
        });
    }

    /// <summary>
    /// El profesor llama a esto cuando selecciona un planeta. Solo escribe el
    /// campo idPlanetaSeleccionado del documento sesion/combate, sin afectar al estado.
    /// Lo usa el alumno para mostrar el briefing en la pantalla de espera.
    /// </summary>
    public void SetPlanetaSeleccionadoProfesor(string idPlaneta)
    {
        if (SinAula) return;
        AulaDocRef.Collection("sesion").Document("combate")
            .SetAsync(new Dictionary<string, object> { { "idPlanetaSeleccionado", idPlaneta ?? "" } }, SetOptions.MergeAll)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("Error al actualizar planeta seleccionado: " + t.Exception);
            });
    }

    /// <summary>
    /// Listener independiente para el campo idPlanetaSeleccionado.
    /// Lo usa la PantallaBriefing del alumno.
    /// </summary>
    private ListenerRegistration listenerPlanetaSel;
    public void EscucharPlanetaSeleccionado(Action<string> callback)
    {
        if (SinAula) return;
        listenerPlanetaSel?.Stop();
        listenerPlanetaSel = AulaDocRef.Collection("sesion").Document("combate").Listen(snapshot =>
        {
            if (snapshot == null || !snapshot.Exists) return;
            string idSel = snapshot.ContainsField("idPlanetaSeleccionado")
                ? snapshot.GetValue<string>("idPlanetaSeleccionado")
                : "";
            callback?.Invoke(idSel);
        });
    }

    /// <summary>
    /// Devuelve la mejor puntuación que el alumno haya obtenido en un planeta concreto.
    /// Si nunca ha jugado ese planeta, devuelve -1.
    /// </summary>
    public void ObtenerMejorPuntuacionPlaneta(string idAlumno, string idPlaneta, Action<int> callback)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumno) || string.IsNullOrEmpty(idPlaneta))
        {
            callback?.Invoke(-1); return;
        }

        AulaDocRef.Collection("historial")
            .WhereEqualTo("idAlumno", idAlumno)
            .WhereEqualTo("idPlaneta", idPlaneta)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) { callback?.Invoke(-1); return; }
                int max = -1;
                foreach (var doc in task.Result.Documents)
                {
                    var d = doc.ToDictionary();
                    if (d.ContainsKey("puntos"))
                    {
                        int p = Convert.ToInt32(d["puntos"]);
                        if (p > max) max = p;
                    }
                }
                callback?.Invoke(max);
            });
    }

    // ── Historial ────────────────────────────────────────────────────────────

    public void ObtenerHistorial(Action<List<Dictionary<string, object>>> callback)
    {
        if (SinAula) { callback(new List<Dictionary<string, object>>()); return; }

        AulaDocRef.Collection("historial").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) { Debug.LogError("Error al obtener historial: " + task.Exception); callback(new List<Dictionary<string, object>>()); return; }
            var lista = new List<Dictionary<string, object>>();
            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                var d = doc.ToDictionary();
                d["_docId"] = doc.Id;
                lista.Add(d);
            }
            callback(lista);
        });
    }

    public void GuardarHistorialCombate(string idPlaneta, string nombrePlaneta,
        int puntos, int aciertos, int fallos, int total,
        float precision, int racha, float tiempoTotal, float tiempoMedio, string rango,
        List<Dictionary<string, object>> detalle = null)
    {
        if (SinAula) return;

        // Resolver nombre del planeta si no viene informado
        if (string.IsNullOrEmpty(nombrePlaneta) || nombrePlaneta == idPlaneta)
        {
            PlanetSelectable p = FindPlanetaPorId(idPlaneta);
            if (p != null) nombrePlaneta = p.NombrePlaneta;
        }

        // Consultar el documento del alumno en Firestore para obtener flota actualizada
        AulaDocRef.Collection("alumnos").Document(idAlumnoLocal)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                string idFlota = "";
                string nombreFlota = "";

                if (!task.IsFaulted && task.Result.Exists)
                    idFlota = task.Result.ContainsField("idFlota") ? task.Result.GetValue<string>("idFlota") : "";

                // Buscar nombre en lista local primero
                if (!string.IsNullOrEmpty(idFlota))
                {
                    Flota flotaLocal = flotasActivas.FirstOrDefault(f => f.id == idFlota);
                    nombreFlota = flotaLocal != null ? flotaLocal.nombre : "";
                }

                if (!string.IsNullOrEmpty(idFlota) && string.IsNullOrEmpty(nombreFlota))
                {
                    // Consultar Firestore si no está en la lista local (lado alumno)
                    AulaDocRef.Collection("flotas").Document(idFlota)
                        .GetSnapshotAsync().ContinueWithOnMainThread(flotaTask =>
                        {
                            string nombre = (!flotaTask.IsFaulted && flotaTask.Result.Exists && flotaTask.Result.ContainsField("nombre"))
                                ? flotaTask.Result.GetValue<string>("nombre") : "";
                            GuardarDocumentoHistorial(idPlaneta, nombrePlaneta, idFlota, nombre,
                                puntos, aciertos, fallos, total, precision, racha, tiempoTotal, tiempoMedio, rango, detalle);
                        });
                    return;
                }

                GuardarDocumentoHistorial(idPlaneta, nombrePlaneta, idFlota, nombreFlota,
                    puntos, aciertos, fallos, total, precision, racha, tiempoTotal, tiempoMedio, rango, detalle);
            });
    }

    private void GuardarDocumentoHistorial(string idPlaneta, string nombrePlaneta,
        string idFlota, string nombreFlota,
        int puntos, int aciertos, int fallos, int total,
        float precision, int racha, float tiempoTotal, float tiempoMedio, string rango,
        List<Dictionary<string, object>> detalle = null)
    {
        string id = Guid.NewGuid().ToString();
        AulaDocRef.Collection("historial").Document(id)
            .SetAsync(new Dictionary<string, object>
            {
                { "id",            id },
                { "idSesion",      idSesionActual },
                { "idAlumno",      idAlumnoLocal },
                { "nombreAlumno",  nombreAlumnoLocal ?? "Desconocido" },
                { "idFlota",       idFlota },
                { "nombreFlota",   nombreFlota },
                { "idPlaneta",     idPlaneta },
                { "nombrePlaneta", nombrePlaneta },
                { "timestamp",     FieldValue.ServerTimestamp },
                { "puntos",        puntos },
                { "aciertos",      aciertos },
                { "fallos",        fallos },
                { "total",         total },
                { "precision",     Math.Round(precision, 1) },
                { "racha",         racha },
                { "tiempoTotal",   Math.Round(tiempoTotal, 1) },
                { "tiempoMedio",       Math.Round(tiempoMedio, 1) },
                { "rango",             rango },
                { "detallePreguntas",  detalle ?? new List<Dictionary<string, object>>() }
            }).ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) Debug.LogError("Error al guardar historial: " + t.Exception);
            });
    }

    private PlanetSelectable FindPlanetaPorId(string idPlaneta)
    {
        foreach (PlanetSelectable p in FindObjectsByType<PlanetSelectable>(FindObjectsSortMode.None))
            if (p.IdUnico == idPlaneta) return p;
        return null;
    }

    // ── Perfil ───────────────────────────────────────────────────────────────

    public void CargarPerfilAlumno(Action<Dictionary<string, object>> callback)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumnoLocal)) { callback(null); return; }

        AulaDocRef.Collection("alumnos").Document(idAlumnoLocal)
            .GetSnapshotAsync().ContinueWithOnMainThread(alumTask =>
            {
                if (alumTask.IsFaulted || !alumTask.Result.Exists) { callback(null); return; }

                var datos = alumTask.Result.ToDictionary();

                AulaDocRef.Collection("historial")
                    .WhereEqualTo("idAlumno", idAlumnoLocal)
                    .GetSnapshotAsync().ContinueWithOnMainThread(histTask =>
                    {
                        if (!histTask.IsFaulted)
                        {
                            int xpCalculado      = 0;
                            int combatesCalculado = 0;
                            var medallas = new Dictionary<string, object>();
                            foreach (DocumentSnapshot doc in histTask.Result.Documents)
                            {
                                var h = doc.ToDictionary();
                                if (h.ContainsKey("puntos")) xpCalculado += Convert.ToInt32(h["puntos"]);
                                combatesCalculado++;
                                if (h.ContainsKey("medallaAsignada"))
                                {
                                    string cat = h["medallaAsignada"].ToString();
                                    if (!string.IsNullOrEmpty(cat))
                                    {
                                        int actual = medallas.ContainsKey(cat) ? Convert.ToInt32(medallas[cat]) : 0;
                                        medallas[cat] = actual + 1;
                                    }
                                }
                            }
                            datos["xpTotal"]          = xpCalculado;
                            datos["combatesJugados"]  = combatesCalculado;
                            datos["medallasProfesor"] = medallas;
                        }

                        callback(datos);
                    });
            });
    }

    public void OtorgarMedallaCategoria(string docIdHistorial, string categoria)
    {
        if (SinAula || string.IsNullOrEmpty(docIdHistorial)) return;

        AulaDocRef.Collection("historial").Document(docIdHistorial)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "medallaAsignada", categoria }
            })
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Error al otorgar medalla: " + task.Exception);
            });
    }

    public void GuardarCamposPerfil(Dictionary<string, object> campos)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumnoLocal)) return;

        AulaDocRef.Collection("alumnos").Document(idAlumnoLocal)
            .UpdateAsync(campos).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Error al guardar perfil: " + task.Exception);
            });
    }

    public void AñadirXPYActualizarPerfil(int xpGanada, int combatesJugados, List<string> nuevasInsignias)
    {
        if (SinAula || string.IsNullOrEmpty(idAlumnoLocal)) return;

        var docRef = AulaDocRef.Collection("alumnos").Document(idAlumnoLocal);

        // ============================================================
        // Antes esto era un read-modify-write sin transacción:
        //   GetSnapshot → calcular xpNueva → UpdateAsync.
        // Si dos escrituras concurrentes ocurrían (p.ej. dos combates
        // terminando casi a la vez, o un click duplicado), ambas leían
        // el mismo xpActual y la última escribía pisando a la primera
        // → un acierto/combate se perdía silenciosamente.
        // Ahora usamos RunTransactionAsync: si Firestore detecta cambio
        // concurrente, reintenta automáticamente. Aritmética garantizada.
        // ============================================================
        FirebaseManager.Instance.db.RunTransactionAsync(tx =>
        {
            return tx.GetSnapshotAsync(docRef).ContinueWith(snapTask =>
            {
                var snap = snapTask.Result;
                if (!snap.Exists) return;

                int xpActual      = snap.ContainsField("xpTotal")         ? snap.GetValue<int>("xpTotal")         : 0;
                int combatesTotal = snap.ContainsField("combatesJugados") ? snap.GetValue<int>("combatesJugados") : 0;

                int xpNueva    = xpActual + xpGanada;
                int nivelNuevo = CalcularNivel(xpNueva);

                var campos = new Dictionary<string, object>
                {
                    { "xpTotal",         xpNueva },
                    { "nivel",           nivelNuevo },
                    { "combatesJugados", combatesTotal + combatesJugados }
                };

                if (nuevasInsignias != null && nuevasInsignias.Count > 0)
                    campos["insignias"] = FieldValue.ArrayUnion(nuevasInsignias.Cast<object>().ToArray());

                tx.Update(docRef, campos);
            });
        }).ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) Debug.LogError("Error al actualizar XP: " + t.Exception);
        });
    }

    public static int CalcularNivel(int xpTotal)
    {
        int nivel = 1;
        int xpAcumulada = 0;
        while (true)
        {
            int xpNecesaria = 500 + (nivel * 80);
            if (xpAcumulada + xpNecesaria > xpTotal) break;
            xpAcumulada += xpNecesaria;
            nivel++;
        }
        return nivel;
    }

    public static (int xpEnNivel, int xpNecesaria) CalcularProgresoNivel(int xpTotal)
    {
        int nivel = 1;
        int xpAcumulada = 0;
        while (true)
        {
            int xpNecesaria = 500 + (nivel * 80);
            if (xpAcumulada + xpNecesaria > xpTotal)
                return (xpTotal - xpAcumulada, xpNecesaria);
            xpAcumulada += xpNecesaria;
            nivel++;
        }
    }

    // ── Progreso Global ──────────────────────────────────────────────────────

    public void ObtenerDatosProgreso(Action<List<Dictionary<string, object>>, List<Dictionary<string, object>>> callback)
    {
        if (SinAula) { callback(new List<Dictionary<string, object>>(), new List<Dictionary<string, object>>()); return; }

        AulaDocRef.Collection("historial").GetSnapshotAsync().ContinueWithOnMainThread(histTask =>
        {
            var historial = new List<Dictionary<string, object>>();
            if (!histTask.IsFaulted)
                foreach (DocumentSnapshot doc in histTask.Result.Documents)
                    historial.Add(doc.ToDictionary());

            AulaDocRef.Collection("alumnos").GetSnapshotAsync().ContinueWithOnMainThread(alumTask =>
            {
                var alumnos = new List<Dictionary<string, object>>();
                if (!alumTask.IsFaulted)
                {
                    var mapaFlotas = new Dictionary<string, string>();
                    foreach (var f in flotasActivas) mapaFlotas[f.id] = f.nombre;

                    // XP calculado desde historial: idAlumno → suma de puntos
                    var xpPorAlumno = historial
                        .Where(r => r.ContainsKey("idAlumno") && r.ContainsKey("puntos"))
                        .GroupBy(r => r["idAlumno"].ToString())
                        .ToDictionary(g => g.Key, g => g.Sum(r => Convert.ToInt32(r["puntos"])));

                    var combatesPorAlumno = historial
                        .Where(r => r.ContainsKey("idAlumno"))
                        .GroupBy(r => r["idAlumno"].ToString())
                        .ToDictionary(g => g.Key, g => g.Count());

                    foreach (DocumentSnapshot doc in alumTask.Result.Documents)
                    {
                        var d = doc.ToDictionary();
                        string idFlota = d.ContainsKey("idFlota") ? d["idFlota"].ToString() : "";
                        d["nombreFlota"] = (!string.IsNullOrEmpty(idFlota) && mapaFlotas.ContainsKey(idFlota))
                            ? mapaFlotas[idFlota] : "";

                        // Sobrescribir xpTotal y combatesJugados con los valores reales del historial
                        string idAlumno = doc.Id;
                        d["xpTotal"]         = xpPorAlumno.ContainsKey(idAlumno)   ? xpPorAlumno[idAlumno]   : 0;
                        d["combatesJugados"]  = combatesPorAlumno.ContainsKey(idAlumno) ? combatesPorAlumno[idAlumno] : 0;

                        alumnos.Add(d);
                    }
                }
                callback(historial, alumnos);
            });
        });
    }

    // ── Mensajes ─────────────────────────────────────────────────────────────

    public void EnviarMensaje(string texto)
    {
        if (SinAula || string.IsNullOrWhiteSpace(texto)) return;

        string id = System.Guid.NewGuid().ToString();
        AulaDocRef.Collection("mensajes").Document(id)
            .SetAsync(new Dictionary<string, object>
            {
                { "id",        id },
                { "texto",     texto.Trim() },
                { "timestamp", FieldValue.ServerTimestamp }
            }).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) Debug.LogError("Error al enviar mensaje: " + task.Exception);
            });
    }

    public void EscucharMensajes(Action<List<Dictionary<string, object>>> callback)
    {
        if (SinAula) return;

        listenerMensajes?.Stop();

        listenerMensajes = AulaDocRef.Collection("mensajes").Listen(snapshot =>
        {
            if (snapshot == null) return;
            var lista = new List<Dictionary<string, object>>();
            foreach (DocumentSnapshot doc in snapshot.Documents)
                lista.Add(doc.ToDictionary());
            callback?.Invoke(lista);
        });
    }

    private void OnDestroy()
    {
        listenerAlumnos?.Stop();
        listenerFlotas?.Stop();
        listenerSesion?.Stop();
        listenerMensajes?.Stop();
    }
}
