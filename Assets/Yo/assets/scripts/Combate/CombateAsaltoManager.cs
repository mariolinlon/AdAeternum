using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manager autoritativo del combate Tipo 1 (Asalto planetario).
/// Distribuido: cualquier alumno de la flota puede ser el manager autoritativo
/// de SU propia flota. El profesor solo dispara el inicio del combate
/// (sesion/combate.estado=enCombate) pero NO ejecuta tick.
///
/// FAILOVER por heartbeat + epoch:
///  - El líder activo escribe un heartbeat (timestamp Unix ms) cada vez que
///    persiste el estado (~0.5s). Si pasa más de UMBRAL_LIDER_CAIDO_MS sin
///    heartbeat fresco, otro miembro puede reclamar el rol via transacción
///    atómica. La transacción incrementa un epoch — un ex-líder reconectándose
///    detecta el cambio de epoch y cede el rol inmediatamente.
///  - Cualquier miembro puede ser candidato; la transacción decide quién gana
///    cuando varios intentan reclamar a la vez.
///
/// Responsabilidades cuando soy líder ACTIVO de mi flota:
///  - Crear el documento de estado en Firebase (combateActivo/{idSesion}/flotas/{miIdFlota}).
///  - Tick continuo: descargar escudo, generar ataques entrantes, aplicar timeout de ataques no defendidos.
///  - Heartbeat continuo (en cada EscribirEstadoCombateDelManager).
///  - Detectar fin de combate (zonas destruidas o nave a 0) y marcar estado.
///  - Limpieza al detenerse el combate.
///
/// Cuando NO soy líder activo pero hay combate activo:
///  - Cada INTERVALO_INTENTO_RECLAMAR segundos verifico si el líder cayó
///    (heartbeat antiguo) y, de ser así, intento reclamar el rol.
///
/// Va en el GameObject quizmanager.
/// </summary>
public class CombateAsaltoManager : MonoBehaviour
{
    // ── Parámetros del failover ──────────────────────────────────────────────
    /// <summary>Tiempo que debe pasar sin heartbeat para considerar al líder caído.</summary>
    private const long UMBRAL_LIDER_CAIDO_MS = 8000L;
    /// <summary>Cada cuánto un miembro NO-líder verifica si puede reclamar.</summary>
    private const float INTERVALO_INTENTO_RECLAMAR = 3f;

    private bool activo = false;
    private string idSesionActiva = "";
    private string idPlanetaActivo = "";
    private ConfigCombatePlaneta cfgActiva;

    /// <summary>
    /// Epoch que gané al reclamar el liderato. Si el listener trae un epoch
    /// distinto, significa que otro miembro me ha sustituido y debo ceder.
    /// -1 = aún no he reclamado.
    /// </summary>
    private int _miEpochLider = -1;

    /// <summary>Acumulador para el loop periódico de intento de reclamación.</summary>
    private float _acumIntentoReclamar = 0f;

    /// <summary>Evita lanzar transacciones de reclamación concurrentes desde el mismo cliente.</summary>
    private bool _reclamacionEnVuelo = false;

    /// <summary>
    /// True cuando ya hemos recibido el primer snapshot del listener tras
    /// reclamar. En ese primer snapshot SÍ aceptamos el estado de Firebase
    /// como autoridad (sincronización inicial). En snapshots posteriores
    /// preservamos los campos del manager (escudo, ataques, vidaNave...).
    /// Crítico para el failover: el nuevo líder NO debe pisar el estado real
    /// con su copia local recién creada (que está limpia).
    /// </summary>
    private bool _primerSnapshotRecibido = false;

    // Estado en memoria por flota (espejo del documento Firebase, mantenido por el tick)
    private readonly Dictionary<string, EstadoFlotaCombate> estadosPorFlota = new Dictionary<string, EstadoFlotaCombate>();

    // Listeners para captar acciones de los alumnos (que escriben directamente al doc)
    private readonly Dictionary<string, Firebase.Firestore.ListenerRegistration> _listeners
        = new Dictionary<string, Firebase.Firestore.ListenerRegistration>();

    private float acumuladorEscritura = 0f; // para no escribir cada frame
    private bool suscritoEstadoCombate = false;

    private void OnEnable()
    {
        // Auto-suscripción: cubre el caso de domain reload donde activo se resetea pero
        // el combate sigue vivo en Firestore. Multi-suscriptor en EscucharEstadoCombate
        // permite que profesor + alumno coexistan.
        Invoke(nameof(SuscribirEstadoCombate), 1.5f); // espera a que AulaDataManager esté listo y SetCodigoAula
    }

    // Último estado de combate observado (para reevaluar si soy líder cuando cambian flotas/alumnos)
    private string _ultEstadoCombate = "";
    private string _ultIdPlanetaCombate = "";
    private string _ultIdSesionCombate = "";

    private void SuscribirEstadoCombate()
    {
        if (suscritoEstadoCombate) return;
        if (AulaDataManager.Instance == null || string.IsNullOrEmpty(AulaDataManager.Instance.GetCodigoAula()))
        {
            // todavía no hay aula; reintenta más tarde
            Invoke(nameof(SuscribirEstadoCombate), 1.5f);
            return;
        }
        AulaDataManager.Instance.EscucharEstadoCombate(OnEstadoCombateCambiado);
        // Suscribimos también a flotas y alumnos para reevaluar si soy líder cuando cambian
        AulaDataManager.Instance.EscucharAlumnos(ReevaluarLiderato);
        AulaDataManager.Instance.EscucharFlotas(ReevaluarLiderato);
        suscritoEstadoCombate = true;
    }

    private void OnEstadoCombateCambiado(string estado, string idPlaneta, string idSesion)
    {
        _ultEstadoCombate = estado ?? "";
        _ultIdPlanetaCombate = idPlaneta ?? "";
        _ultIdSesionCombate = idSesion ?? "";

        if (estado != "enCombate" && activo)
        {
            DetenerCombate();
            return;
        }

        ReevaluarLiderato();
    }

    /// <summary>
    /// Reevalúa si yo (alumno local) puedo ser manager autoritativo de mi flota.
    /// Cualquier miembro es candidato — la transacción de reclamación decide.
    /// Se llama cuando cambia el estado de combate, cuando cambian flotas/alumnos,
    /// y periódicamente desde Update si no soy líder y hay combate activo.
    /// </summary>
    public void ReevaluarLiderato()
    {
        if (AulaDataManager.Instance == null) return;
        if (_ultEstadoCombate != "enCombate" || string.IsNullOrEmpty(_ultIdPlanetaCombate) || string.IsNullOrEmpty(_ultIdSesionCombate)) return;
        if (!AulaDataManager.Instance.EsAlumno) return; // el profesor no actúa como manager

        string idAlumno = AulaDataManager.Instance.GetIdAlumnoLocal();
        if (string.IsNullOrEmpty(idAlumno)) return;

        // Encontrar mi flota
        var miAlumno = AulaDataManager.Instance.alumnosDisponibles.FirstOrDefault(a =>
            a.ContainsKey("id") && a["id"].ToString() == idAlumno);
        if (miAlumno == null) return; // todavía no se han cargado los alumnos
        string idFlota = miAlumno.ContainsKey("idFlota") ? miAlumno["idFlota"].ToString() : "";
        if (string.IsNullOrEmpty(idFlota)) return;

        // Verificar que mi flota existe (si no, todavía no se han cargado).
        // OJO: ya NO miramos miFlota.liderID — cualquier miembro puede reclamar.
        var miFlota = AulaDataManager.Instance.flotasActivas.FirstOrDefault(f => f.id == idFlota);
        if (miFlota == null) return;

        // Si ya estoy liderando esta sesión, no rehacer setup.
        if (activo && idSesionActiva == _ultIdSesionCombate && idPlanetaActivo == _ultIdPlanetaCombate)
            return;

        // Buscar planeta y config
        PlanetSelectable planeta = null;
        foreach (var p in FindObjectsByType<PlanetSelectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (p.IdUnico == _ultIdPlanetaCombate) { planeta = p; break; }
        ConfigCombatePlaneta cfg = planeta != null ? planeta.ConfigCombate : ConfigCombatePlaneta.ConfigDefault();
        if (cfg == null || cfg.tipo != TipoCombate.AsaltoPlanetario) return;

        IntentarReclamarYArrancar(_ultIdSesionCombate, _ultIdPlanetaCombate, idFlota, idAlumno, cfg);
    }

    /// <summary>
    /// Lanza una transacción ReclamarLiderato. La transacción crea el doc si
    /// no existía (combate recién iniciado). Si gano, asumo el rol con el
    /// epoch obtenido. Si pierdo (alguien ya está liderando con heartbeat
    /// fresco), no hago nada — el siguiente ciclo del Update lo reintentará.
    /// </summary>
    private void IntentarReclamarYArrancar(string idSesion, string idPlaneta, string idFlota,
                                            string idAlumno, ConfigCombatePlaneta cfg)
    {
        if (_reclamacionEnVuelo) return;
        _reclamacionEnVuelo = true;

        AulaDataManager.Instance.ReclamarLiderato(idSesion, idFlota, idPlaneta, idAlumno,
            UMBRAL_LIDER_CAIDO_MS, cfg,
            (gane, nuevoEpoch) =>
            {
                _reclamacionEnVuelo = false;
                if (!gane) return; // hay un líder vivo con heartbeat fresco; reintento en el siguiente ciclo
                _miEpochLider = nuevoEpoch;
                IniciarComoLider(idSesion, idPlaneta, idFlota, cfg);
            });
    }

    private void Update()
    {
        // ── FAILOVER: si NO soy líder activo pero hay combate, intentar reclamar ──
        // Cada INTERVALO_INTENTO_RECLAMAR segundos hacemos un intento. Si nadie
        // está liderando o el líder ha caído (heartbeat antiguo), ganaremos la
        // transacción. Si hay un líder vivo, la transacción retorna false y
        // esperamos al siguiente ciclo.
        if (!activo)
        {
            if (_ultEstadoCombate == "enCombate" && !string.IsNullOrEmpty(_ultIdSesionCombate))
            {
                _acumIntentoReclamar += Time.deltaTime;
                if (_acumIntentoReclamar >= INTERVALO_INTENTO_RECLAMAR)
                {
                    _acumIntentoReclamar = 0f;
                    ReevaluarLiderato();
                }
            }
            return;
        }

        float dt = Time.deltaTime;

        // Log de diagnóstico cada 3s para ver si el manager está corriendo y qué valores tiene
        bool huboCambios = false;
        bool huboCambioTerminal = false;
        bool huboCambioCriticoAtaques = false; // ataques generados/timeout → escribir YA para no perderlos por race con listener
        var estadosAEscribir = new List<EstadoFlotaCombate>(); // referencias locales con cambios
        // Snapshot de las claves para evitar InvalidOperationException si modificamos el dict dentro
        var snapshotPares = new List<KeyValuePair<string, EstadoFlotaCombate>>(estadosPorFlota);
        foreach (var kv in snapshotPares)
        {
            var estado = kv.Value;
            if (estado.estado != "activo") continue;

            // 1. Descargar escudo
            if (estado.escudoActual > 0f)
            {
                estado.escudoActual = Mathf.Max(0f, estado.escudoActual - cfgActiva.tasaDescargaEscudo * dt);
                huboCambios = true;
            }

            // 2. Tick de ataques entrantes
            // Si tiempoRestante <= 0 → daño a la nave y retirar, INDEPENDIENTE de si fue asumido.
            // El defensor tiene que resolverlo dentro del tiempo del ataque o impacta.
            for (int i = estado.ataquesEntrantes.Count - 1; i >= 0; i--)
            {
                var ataque = estado.ataquesEntrantes[i];
                ataque.tiempoRestante -= dt;
                if (ataque.tiempoRestante <= 0f)
                {
                    float daño = ataque.tipo == TipoAtaqueEntrante.Agravado
                        ? cfgActiva.dañoAtaqueAgravado
                        : cfgActiva.dañoAtaqueNormal;

                    // El daño va PRIMERO al escudo. Si el daño supera al escudo, el sobrante va a la nave.
                    float dañoAlEscudo = Mathf.Min(estado.escudoActual, daño);
                    estado.escudoActual = Mathf.Max(0f, estado.escudoActual - dañoAlEscudo);
                    float dañoSobrante = daño - dañoAlEscudo;
                    if (dañoSobrante > 0f)
                        estado.vidaNave = Mathf.Max(0f, estado.vidaNave - dañoSobrante);

                    estado.ataquesEntrantes.RemoveAt(i);
                    huboCambios = true;
                    huboCambioCriticoAtaques = true;
                }
            }

            // 3. Generar ataques entrantes según cadencia
            estado.cadenciaActual -= dt;
            if (estado.cadenciaActual <= 0f)
            {
                TipoAtaqueEntrante tipo = UnityEngine.Random.value < 0.25f ? TipoAtaqueEntrante.Agravado : TipoAtaqueEntrante.Normal;
                AtaqueEntrante nuevo = new AtaqueEntrante(
                    Guid.NewGuid().ToString(),
                    tipo,
                    tipo == TipoAtaqueEntrante.Agravado ? 8f : 12f
                );
                estado.ataquesEntrantes.Add(nuevo);
                estado.cadenciaActual = cfgActiva.cadenciaAtaquesEntrantes;
                huboCambios = true;
                huboCambioCriticoAtaques = true;
                estadosPorFlota[kv.Key] = estado; // reasignar por si el listener replaza el value entre frames
            }

            // 4. Comprobar fin
            if (estado.NaveDestruida)
            {
                estado.estado = "eliminado";
                huboCambios = true;
                huboCambioTerminal = true;
                estadosAEscribir.Add(estado);
                // Reasignar al diccionario por si el listener replaza el value entre ticks
                estadosPorFlota[kv.Key] = estado;
            }
            else if (estado.TodasLasZonasDestruidas)
            {
                estado.estado = "completado";
                huboCambios = true;
                huboCambioTerminal = true;
                estadosAEscribir.Add(estado);
                estadosPorFlota[kv.Key] = estado;
            }
        }

        // Si hay cambio terminal, persistir YA (escribir las referencias locales, no del diccionario)
        if (huboCambioTerminal)
        {
            acumuladorEscritura = 0f;
            foreach (var e in estadosAEscribir)
                AulaDataManager.Instance?.EscribirEstadoCombateDelManager(e);
            return;
        }

        // Si hubo cambio crítico de ataques entrantes (generado o timeout), persistir YA
        // para evitar que el listener nos pise el estado con un snapshot anterior antes de
        // que hayamos escrito el cambio.
        if (huboCambioCriticoAtaques)
        {
            acumuladorEscritura = 0f;
            foreach (var kv in estadosPorFlota)
                AulaDataManager.Instance?.EscribirEstadoCombateDelManager(kv.Value);
            return;
        }

        // Persistir cambios cada 0.5s para no saturar Firebase
        acumuladorEscritura += dt;
        if (huboCambios && acumuladorEscritura >= 0.5f)
        {
            acumuladorEscritura = 0f;
            foreach (var kv in estadosPorFlota)
                AulaDataManager.Instance?.EscribirEstadoCombateDelManager(kv.Value);
        }
    }

    /// <summary>
    /// Activo el manager autoritativo de MI flota tras ganar la transacción de
    /// reclamación. El documento ya existe en Firestore (lo creó la transacción
    /// si era la primera vez, o ya estaba creado por un líder previo).
    /// </summary>
    private void IniciarComoLider(string idSesion, string idPlaneta, string idFlota, ConfigCombatePlaneta cfg)
    {
        if (cfg == null) { Debug.LogError("[CombateAsaltoManager] cfg null."); return; }
        if (AulaDataManager.Instance == null) return;

        // Idempotencia
        if (activo && idSesionActiva == idSesion && idPlanetaActivo == idPlaneta && estadosPorFlota.ContainsKey(idFlota))
            return;

        // OJO: NO llamamos DetenerCombate aquí — eso resetearía _miEpochLider
        // que acabamos de ganar. Solo limpiamos estado local.
        estadosPorFlota.Clear();
        acumuladorEscritura = 0f;
        _acumIntentoReclamar = 0f;
        _primerSnapshotRecibido = false;

        idSesionActiva = idSesion;
        idPlanetaActivo = idPlaneta;
        cfgActiva = cfg;

        // Estado local inicial. Se sincronizará con el de Firebase en el primer
        // fire del listener (puede haber estado ya en curso si soy un líder
        // que sustituye a otro caído).
        EstadoFlotaCombate e = EstadoFlotaCombate.Crear(idSesion, idFlota, idPlaneta, cfg);
        e.liderEpoch = _miEpochLider; // mi epoch ganado en la reclamación
        estadosPorFlota[idFlota] = e;

        // Suscribir listener para captar acciones de los alumnos (sus transacciones
        // modifican el doc) Y para detectar si alguien me roba el rol (epoch).
        string flotaKey = idFlota;
        AulaDataManager.Instance.EscucharEstadoFlotaCombate(idSesion, idFlota, estadoActualizado =>
        {
            if (estadoActualizado == null) return;

            // ============================================================
            // DETECCIÓN DE ROBO: si el epoch del snapshot ≠ el mío, alguien
            // me ha sustituido como líder activo. Cedo el rol inmediatamente.
            // ============================================================
            if (estadoActualizado.liderEpoch != _miEpochLider)
            {
                Debug.Log($"[CombateAsaltoManager] Cedo el rol (epoch local={_miEpochLider} remoto={estadoActualizado.liderEpoch}).");
                DetenerCombate();
                return;
            }

            if (estadosPorFlota.TryGetValue(flotaKey, out var local) && local != null && _primerSnapshotRecibido)
            {
                // Si nuestro estado local ya es terminal, no permitir que un snapshot
                // más antiguo lo revierta a "activo"
                if ((local.estado == "completado" || local.estado == "eliminado")
                    && estadoActualizado.estado == "activo")
                    return;

                // ============================================================
                // PRESERVAR los campos que GESTIONA el manager. El snapshot
                // los trae con el valor que Firebase tiene "ahora", pero como
                // el manager interpola en memoria entre escrituras (cada 0.5s),
                // si dejamos que el snapshot pise el estado local perdemos:
                //   - ataquesEntrantes que el manager generó tras la última escritura
                //   - decrementos de cadenciaActual / escudoActual / tiempoRestante
                //
                // El snapshot SÍ aporta los campos que gestionan los alumnos:
                //   - zonas (DispararAtaque las modifica)
                //   - energiaAtaquePorAlumno (IncrementarEnergiaAtaque / DispararAtaque)
                // Esos sí los aceptamos del snapshot.
                //
                // EXCEPCIÓN: en el PRIMER snapshot tras reclamar (cuando soy
                // un líder de failover sustituyendo al caído), NO preservamos
                // local — es la sincronización inicial con el estado real de
                // Firebase. Sin esto, nuestro local recién creado (limpio)
                // pisaría el estado acumulado del combate en curso.
                // ============================================================
                estadoActualizado.cadenciaActual    = local.cadenciaActual;
                estadoActualizado.escudoActual      = local.escudoActual;
                estadoActualizado.vidaNave          = local.vidaNave;
                estadoActualizado.ataquesEntrantes  = local.ataquesEntrantes;
            }

            estadosPorFlota[flotaKey] = estadoActualizado;
            _primerSnapshotRecibido = true;
        });

        activo = true;
    }

    /// <summary>Detiene el manager: para listeners y deja de escribir. No borra los docs (sirven de histórico).</summary>
    public void DetenerCombate()
    {
        activo = false;
        if (AulaDataManager.Instance != null)
        {
            foreach (var idFlota in estadosPorFlota.Keys)
                AulaDataManager.Instance.DetenerListenerEstadoFlota(idSesionActiva, idFlota);
        }
        estadosPorFlota.Clear();
        idSesionActiva = "";
        idPlanetaActivo = "";
        cfgActiva = null;
        _miEpochLider = -1;       // dejo de "ser" líder en mi memoria
        _acumIntentoReclamar = 0f;
    }
}
