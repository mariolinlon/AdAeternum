using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SistemaCombateAlumno : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public CreadorPreguntas creadorPreguntas;
    public PantallaResultados pantallaResultados;

    [Header("Pantalla Inicio Jugador")]
    public GameObject pantallaInicioJugador;

    [Header("Sala de Espera")]
    public GameObject panelEspera;
    public TextMeshProUGUI textoEspera;

    [Header("UI del Combate")]
    public GameObject panelCombate;
    public TextMeshProUGUI textoEnunciado;
    public TextMeshProUGUI resultado;
    public TextMeshProUGUI final;
    public Button[] botonesOpciones;
    public TextMeshProUGUI[] textosBotones;
    public GameObject BotonResultados;

    [Header("Temporizador")]
    public TextMeshProUGUI textoTiempo;
    public Slider sliderTiempo;

    [Header("Combate Tipo 1 (refactor)")]
    public MotorPreguntas motorPreguntas;
    public CombateAsaltoManager combateAsaltoManager;

    [Header("HUDs Tipo 1")]
    public HUDCombateCompartido hudCompartido;
    public HUDCombateAtacante hudAtacante;
    public HUDCombateDefensor hudDefensor;

    private Coroutine coroutineEspera;
    private bool combateEnProgreso = false;

    // Stats internas
    private int aciertos;
    private int fallos;
    private int rachaActual;
    private int rachaMaxima;
    private float tiempoInicioCombate;
    private float tiempoInicioPregunta;
    private List<float> tiemposPorPregunta = new List<float>();
    private List<Dictionary<string, object>> detallePreguntas = new List<Dictionary<string, object>>();

    // Stats finales (guardadas para cuando se abra la pantalla)
    private int _total, _puntuacion, _rachaFinal;
    private int _puntosAcumulados;
    private float _porcentaje, _tiempoTotal, _tiempoMedio;
    private string _rango;

    private List<Pregunta> preguntasDelPlanetaActual = new List<Pregunta>();
    private string idPlanetaActual = "";
    private string nombrePlanetaActual = "";
    private int indicePreguntaActual = 0;
    private bool estaProcesandoRespuesta = false;
    private Coroutine coroutineTemporizador;

    // Estrategia del combate Tipo 1+ (null si estamos en modo fallback lineal)
    private EstrategiaCombate estrategiaActual;
    private string idSesionActual = "";

    // Caché del último estado de combate visto por el listener
    private string _ultEstadoVisto = "", _ultIdPlanetaVisto = "", _ultIdSesionVisto = "";

    private void Update()
    {
        if (estrategiaActual != null)
        {
            estrategiaActual.Tick(Time.deltaTime);
            if (estrategiaActual.EstaTerminado)
            {
                FinalizarCombatePorEstrategia();
            }
        }
    }

    /// <summary>
    /// Llamado cuando panelEspera se reactiva (alumno vuelve de otras pantallas).
    /// Si en ese momento ya estamos en estado "enCombate" y no estamos ya combatiendo, lanzamos el combate.
    /// </summary>
    public void IntentarArrancarSiHayCombateActivo()
    {
        if (combateEnProgreso) return;
        if (_ultEstadoVisto != "enCombate") return;
        if (string.IsNullOrEmpty(_ultIdPlanetaVisto)) return;
        if (panelEspera == null || !panelEspera.activeInHierarchy) return;

        combateEnProgreso = true;
        idSesionActual = _ultIdSesionVisto;
        if (coroutineEspera != null) StopCoroutine(coroutineEspera);
        coroutineEspera = StartCoroutine(EsperarPreguntasYComenzar(_ultIdPlanetaVisto));
    }

    private bool _listenerCombateSuscrito = false;

    /// <summary>
    /// Suscribe el listener de estado de combate. Idempotente — la primera llamada se suscribe,
    /// las siguientes no hacen nada. Se llama desde LoginAlumnoUI y desde SensorPanelEspera
    /// para asegurar que el listener está activo independientemente del flujo del alumno.
    /// </summary>
    public void SuscribirListenerCombate()
    {
        if (_listenerCombateSuscrito) return;
        if (AulaDataManager.Instance == null) return;
        if (string.IsNullOrEmpty(AulaDataManager.Instance.GetCodigoAula()))
        {
            // Aún no hay código de aula. Reintentar en 1s sin spamear el log.
            Invoke(nameof(SuscribirListenerCombate), 1f);
            return;
        }

        AulaDataManager.Instance.EscucharEstadoCombate(OnEstadoCombateCambio);
        _listenerCombateSuscrito = true;
    }

    private void OnEstadoCombateCambio(string estado, string idPlaneta, string idSesion)
    {
        _ultEstadoVisto = estado;
        _ultIdPlanetaVisto = idPlaneta;
        _ultIdSesionVisto = idSesion;

        if (estado == "enCombate" && !string.IsNullOrEmpty(idPlaneta) && !combateEnProgreso)
        {
            // El combate solo arranca para el alumno si está en la sala de espera.
            if (panelEspera == null || !panelEspera.activeInHierarchy) return;

            combateEnProgreso = true;
            idSesionActual = idSesion;
            if (coroutineEspera != null) StopCoroutine(coroutineEspera);
            coroutineEspera = StartCoroutine(EsperarPreguntasYComenzar(idPlaneta));
        }
        else if (estado == "esperando")
        {
            if (panelCombate != null && panelCombate.activeSelf) return;
            combateEnProgreso = false;
            if (coroutineEspera != null) { StopCoroutine(coroutineEspera); coroutineEspera = null; }

            // Solo redirigir a pantallaInicioJugador si el alumno estaba en panelEspera.
            // Si está en otra pantalla (mapa, flota, perfil...), no lo movemos.
            if (panelEspera != null && panelEspera.activeInHierarchy)
            {
                panelEspera.SetActive(false);
                if (pantallaInicioJugador != null) pantallaInicioJugador.SetActive(true);
            }
        }
    }

    public void EsperarInicioCombate()
    {
        // Si ya estamos combatiendo o ya tenemos una estrategia activa, no rearrancar.
        // Esto evita el bug de doble ejecución cuando el botón también dispara SensorPanelEspera.OnEnable.
        if (combateEnProgreso || estrategiaActual != null) return;

        if (coroutineEspera != null) { StopCoroutine(coroutineEspera); coroutineEspera = null; }

        // Ocultar pantalla inicio y mostrar sala de espera
        if (pantallaInicioJugador != null) pantallaInicioJugador.SetActive(false);
        if (panelEspera != null) panelEspera.SetActive(true);
        if (panelCombate != null) panelCombate.SetActive(false);
        if (textoEspera != null) textoEspera.text = "Esperando al profesor...";

        SuscribirListenerCombate();

        // Si al activar ya estamos en estado enCombate (cacheado), arrancar inmediatamente
        IntentarArrancarSiHayCombateActivo();
    }

    public void IniciarCombateConPlaneta(string idPlaneta)
    {
        estaProcesandoRespuesta = false;
        indicePreguntaActual = 0;
        preguntasDelPlanetaActual.Clear();
        aciertos = 0;
        fallos = 0;
        rachaActual = 0;
        rachaMaxima = 0;
        _puntosAcumulados = 0;
        tiemposPorPregunta.Clear();
        detallePreguntas.Clear();
        tiempoInicioCombate = Time.time;

        idPlanetaActual = idPlaneta;
        PlanetSelectable planetaObj = PlanetSelectionManager.Instance?.ObtenerPlanetaActual();
        if (planetaObj == null)
        {
            // Lado alumno: el planeta puede no estar "seleccionado", buscarlo por id
            foreach (var p in FindObjectsByType<PlanetSelectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (p.IdUnico == idPlaneta) { planetaObj = p; break; }
        }
        nombrePlanetaActual = planetaObj != null ? planetaObj.NombrePlaneta : idPlaneta;

        // Resolver tipo de combate del planeta. Si es AsaltoPlanetario y tenemos motor + manager, usar estrategia.
        ConfigCombatePlaneta cfg = planetaObj != null ? planetaObj.ConfigCombate : ConfigCombatePlaneta.ConfigDefault();
        bool usarEstrategia = motorPreguntas != null
                              && cfg != null
                              && cfg.tipo == TipoCombate.AsaltoPlanetario;

        if (usarEstrategia)
        {
            // El alumno inicia su estrategia local. El manager autoritativo lo arranca el profesor desde ControlCombateProfesor.
            string idAlumno = AulaDataManager.Instance != null ? AulaDataManager.Instance.GetIdAlumnoLocal() : "";
            string idFlota = "", rolComb = "";
            if (AulaDataManager.Instance != null && !string.IsNullOrEmpty(idAlumno))
            {
                var a = AulaDataManager.Instance.alumnosDisponibles
                    .FirstOrDefault(x => x.ContainsKey("id") && x["id"].ToString() == idAlumno);
                if (a != null)
                {
                    idFlota = a.ContainsKey("idFlota")    ? a["idFlota"].ToString()    : "";
                    rolComb = a.ContainsKey("rolCombate") ? a["rolCombate"].ToString() : "";
                }
            }

            if (panelCombate != null) panelCombate.SetActive(true);
            if (panelEspera != null) panelEspera.SetActive(false);
            if (BotonResultados != null) BotonResultados.SetActive(false);

            // Si el alumno no tiene rol o no está en una flota, no se puede jugar Tipo 1.
            // Caemos al fallback lineal (combate antiguo) para no dejarlo bloqueado.
            if (string.IsNullOrEmpty(idFlota) || (rolComb != "atacante" && rolComb != "defensor"))
            {
                Debug.LogWarning("[SistemaCombate] Alumno sin flota/rol; fallback al combate lineal antiguo.");
                IniciarCombateLineal(idPlaneta);
                return;
            }

            estrategiaActual = new EstrategiaAsaltoPlanetario();
            estrategiaActual.Iniciar(this, motorPreguntas, cfg, idPlaneta, nombrePlanetaActual,
                                     idAlumno, idFlota, rolComb, idSesionActual);
            return;
        }

        // Fallback: combate lineal (camino antiguo)
        IniciarCombateLineal(idPlaneta);
    }

    /// <summary>Combate lineal (modo viejo): bucle de preguntas hasta agotar la lista.</summary>
    private void IniciarCombateLineal(string idPlaneta)
    {

        var preguntasFiltradas = creadorPreguntas.bibliotecaLocal
            .Where(p => p.idPlaneta == idPlaneta).ToList();

        if (preguntasFiltradas.Count == 0) { Debug.LogWarning("[SistemaCombate] El planeta no tiene preguntas."); return; }

        preguntasDelPlanetaActual = new List<Pregunta>(preguntasFiltradas);
        ShufflePreguntas(preguntasDelPlanetaActual);

        if (BotonResultados != null) BotonResultados.SetActive(false);
        if (panelCombate != null) panelCombate.SetActive(true);
        if (panelEspera != null) panelEspera.SetActive(false);
        MostrarSiguientePregunta();
    }

    public void IniciarCombate()
    {
        estaProcesandoRespuesta = false;
        indicePreguntaActual = 0;
        preguntasDelPlanetaActual.Clear();
        aciertos = 0;
        fallos = 0;
        rachaActual = 0;
        rachaMaxima = 0;
        _puntosAcumulados = 0;
        tiemposPorPregunta.Clear();
        detallePreguntas.Clear();
        tiempoInicioCombate = Time.time;

        PlanetSelectable planeta = PlanetSelectionManager.Instance.ObtenerPlanetaActual();
        if (planeta == null) { Debug.LogWarning("[SistemaCombate] No hay planeta seleccionado."); return; }

        var preguntasFiltradas = creadorPreguntas.bibliotecaLocal
            .Where(p => p.idPlaneta == planeta.IdUnico).ToList();

        if (preguntasFiltradas.Count == 0) { Debug.LogWarning("[SistemaCombate] Este planeta no tiene preguntas."); return; }

        preguntasDelPlanetaActual = new List<Pregunta>(preguntasFiltradas);
        ShufflePreguntas(preguntasDelPlanetaActual);

        BotonResultados.SetActive(false);
        panelCombate.SetActive(true);
        MostrarSiguientePregunta();
    }

    void MostrarSiguientePregunta()
    {
        if (indicePreguntaActual >= preguntasDelPlanetaActual.Count)
        {
            FinalizarCombate();
            return;
        }

        Pregunta p = preguntasDelPlanetaActual[indicePreguntaActual];
        if (textoEnunciado == null) { Debug.LogError("[SistemaCombate] textoEnunciado no asignado en el Inspector."); return; }
        textoEnunciado.text = p.enunciado;
        if (resultado != null) resultado.text = "";

        for (int i = 0; i < botonesOpciones.Length; i++)
        {
            if (i < p.opciones.Length)
            {
                botonesOpciones[i].gameObject.SetActive(true);
                if (i < textosBotones.Length) textosBotones[i].text = p.opciones[i];
                int index = i;
                botonesOpciones[i].onClick.RemoveAllListeners();
                botonesOpciones[i].onClick.AddListener(() => Responder(index));
            }
            else
            {
                botonesOpciones[i].gameObject.SetActive(false);
            }
        }

        estaProcesandoRespuesta = false;
        tiempoInicioPregunta = Time.time;

        if (coroutineTemporizador != null) StopCoroutine(coroutineTemporizador);
        coroutineTemporizador = StartCoroutine(Temporizador(p.tiempoLimite));
    }

    IEnumerator Temporizador(float duracion)
    {
        float tiempoRestante = duracion;
        if (sliderTiempo != null) sliderTiempo.maxValue = duracion;

        while (tiempoRestante > 0f)
        {
            tiempoRestante -= Time.deltaTime;
            if (textoTiempo != null) textoTiempo.text = Mathf.CeilToInt(tiempoRestante).ToString();
            if (sliderTiempo != null) sliderTiempo.value = tiempoRestante;
            yield return null;
        }

        if (!estaProcesandoRespuesta)
        {
            estaProcesandoRespuesta = true;
            RegistrarRespuesta(false);
            if (resultado != null) resultado.text = "<color=red>¡Tiempo!</color>";
            indicePreguntaActual++;
            Invoke(nameof(MostrarSiguientePregunta), 1f);
        }
    }

    public void Responder(int indiceSeleccionado)
    {
        // Si la estrategia nueva está activa, MotorPreguntas se encarga de las respuestas.
        // Este método antiguo solo aplica al fallback lineal — protección contra persistent
        // listeners residuales en el Inspector que aún apuntan aquí.
        if (estrategiaActual != null) return;
        if (preguntasDelPlanetaActual == null || preguntasDelPlanetaActual.Count == 0) return;
        if (indicePreguntaActual < 0 || indicePreguntaActual >= preguntasDelPlanetaActual.Count) return;

        if (estaProcesandoRespuesta) return;
        estaProcesandoRespuesta = true;

        if (coroutineTemporizador != null)
        {
            StopCoroutine(coroutineTemporizador);
            coroutineTemporizador = null;
        }

        Pregunta p = preguntasDelPlanetaActual[indicePreguntaActual];
        bool correcto = indiceSeleccionado == p.respuestaCorrecta;

        RegistrarRespuesta(correcto);

        resultado.text = correcto ? "<color=green>Correcto</color>" : "<color=red>Incorrecto</color>";
        indicePreguntaActual++;
        Invoke(nameof(MostrarSiguientePregunta), 1f);
    }

    /// <summary>Permite a la estrategia mostrar un texto de estado en panelCombate (display temporal del Bloque 2).</summary>
    public void MostrarEstadoCombate(string texto)
    {
        if (final != null) final.text = texto ?? "";
    }

    /// <summary>Versión pública que llama la estrategia. Registra estadísticas del alumno.</summary>
    public void RegistrarRespuestaEstrategia(Pregunta p, bool correcto, float tiempoRespuesta)
    {
        if (p == null) return;

        // SFX inmediato según resultado
        AudioManager.PlaySFX(correcto
            ? AudioManager.SFX.RespuestaCorrecta
            : AudioManager.SFX.RespuestaIncorrecta);

        tiemposPorPregunta.Add(tiempoRespuesta);
        detallePreguntas.Add(new Dictionary<string, object>
        {
            { "idPregunta",      p.id },
            { "enunciado",       p.enunciado },
            { "correcto",        correcto },
            { "tiempoRespuesta", System.Math.Round(tiempoRespuesta, 1) }
        });
        if (correcto)
        {
            aciertos++;
            _puntosAcumulados += p.puntosPorAcierto;
            rachaActual++;
            if (rachaActual > rachaMaxima) rachaMaxima = rachaActual;
        }
        else
        {
            fallos++;
            rachaActual = 0;
        }
    }

    void RegistrarRespuesta(bool correcto)
    {
        AudioManager.PlaySFX(correcto
            ? AudioManager.SFX.RespuestaCorrecta
            : AudioManager.SFX.RespuestaIncorrecta);

        float tiempoRespuesta = Time.time - tiempoInicioPregunta;
        tiemposPorPregunta.Add(tiempoRespuesta);

        Pregunta p = preguntasDelPlanetaActual[indicePreguntaActual];

        detallePreguntas.Add(new Dictionary<string, object>
        {
            { "idPregunta",       p.id },
            { "enunciado",        p.enunciado },
            { "correcto",         correcto },
            { "tiempoRespuesta",  Math.Round(tiempoRespuesta, 1) }
        });

        if (correcto)
        {
            aciertos++;
            _puntosAcumulados += p.puntosPorAcierto;
            rachaActual++;
            if (rachaActual > rachaMaxima) rachaMaxima = rachaActual;
        }
        else
        {
            fallos++;
            rachaActual = 0;
        }
    }

    void FinalizarCombate()
    {
        if (coroutineTemporizador != null)
        {
            StopCoroutine(coroutineTemporizador);
            coroutineTemporizador = null;
        }

        if (textoTiempo != null) textoTiempo.text = "";
        if (sliderTiempo != null) sliderTiempo.value = 0;

        _total = preguntasDelPlanetaActual.Count;
        _tiempoTotal = Time.time - tiempoInicioCombate;
        _tiempoMedio = tiemposPorPregunta.Count > 0 ? tiemposPorPregunta.Average() : 0f;
        _porcentaje = _total > 0 ? (aciertos / (float)_total) * 100f : 0f;
        _puntuacion = _puntosAcumulados;
        _rachaFinal = rachaMaxima;
        _rango = _porcentaje >= 80f ? "Oro" : _porcentaje >= 50f ? "Plata" : "Bronce";

        AulaDataManager.Instance?.GuardarHistorialCombate(
            idPlanetaActual, nombrePlanetaActual,
            _puntuacion, aciertos, fallos, _total,
            _porcentaje, _rachaFinal, _tiempoTotal, _tiempoMedio, _rango,
            detallePreguntas);

        List<string> insignias = ComprobarInsignias();
        AulaDataManager.Instance?.AñadirXPYActualizarPerfil(_puntuacion, 1, insignias);

        BotonResultados.SetActive(true);
        final.text = "Fin del combate";
    }

    /// <summary>
    /// Llamado cuando la estrategia indica EstaTerminado. Cierra la estrategia,
    /// calcula stats y reutiliza el flujo de FinalizarCombate (guarda historial, etc.).
    /// </summary>
    private void FinalizarCombatePorEstrategia()
    {
        if (estrategiaActual == null) return;

        var est = estrategiaActual;
        estrategiaActual = null; // evita re-entrada
        est.Finalizar();

        if (motorPreguntas != null) motorPreguntas.DetenerYLimpiar();

        // Compatibilidad con la pantalla de resultados existente:
        // Calcular stats desde lo registrado (si la estrategia futura los lleva, mejor; ahora nada los suma).
        _total       = aciertos + fallos;
        _tiempoTotal = Time.time - tiempoInicioCombate;
        _tiempoMedio = tiemposPorPregunta.Count > 0 ? tiemposPorPregunta.Average() : 0f;
        _porcentaje  = _total > 0 ? (aciertos / (float)_total) * 100f : 0f;
        _puntuacion  = _puntosAcumulados;
        _rachaFinal  = rachaMaxima;
        _rango       = _porcentaje >= 80f ? "Oro" : _porcentaje >= 50f ? "Plata" : "Bronce";

        AulaDataManager.Instance?.GuardarHistorialCombate(
            idPlanetaActual, nombrePlanetaActual,
            _puntuacion, aciertos, fallos, _total,
            _porcentaje, _rachaFinal, _tiempoTotal, _tiempoMedio, _rango,
            detallePreguntas);

        List<string> insignias = ComprobarInsignias();
        AulaDataManager.Instance?.AñadirXPYActualizarPerfil(_puntuacion, 1, insignias);

        if (BotonResultados != null) BotonResultados.SetActive(true);
        if (final != null) final.text = "Fin del combate";
    }

    public void AbrirPantallaResultados()
    {
        if (pantallaResultados != null)
            pantallaResultados.MostrarResultados(aciertos, fallos, _total, _porcentaje, _rachaFinal, _tiempoTotal, _tiempoMedio, _puntuacion, _rango);
    }

    private IEnumerator EsperarPreguntasYComenzar(string idPlaneta)
    {
        if (textoEspera != null) textoEspera.text = "Cargando preguntas...";

        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (creadorPreguntas != null)
            {
                var preguntas = creadorPreguntas.bibliotecaLocal
                    .Where(p => p.idPlaneta == idPlaneta).ToList();

                if (preguntas.Count > 0)
                {
                    if (panelEspera != null) panelEspera.SetActive(false);
                    IniciarCombateConPlaneta(idPlaneta);
                    yield break;
                }
            }
            else
            {
                Debug.LogError("[SistemaCombate] creadorPreguntas es NULL — asígnalo en el Inspector.");
            }

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        Debug.LogError($"[SistemaCombate] Timeout: el planeta '{idPlaneta}' no tiene preguntas en Firebase.");
        if (textoEspera != null) textoEspera.text = "Error: el planeta no tiene preguntas.\nEl profesor debe añadir preguntas primero.";
    }

    List<string> ComprobarInsignias()
    {
        var nuevas = new List<string>();
        if (_porcentaje >= 100f)  nuevas.Add("precision_perfecta");
        if (_rango == "Oro")      nuevas.Add("rango_oro");
        if (_rachaFinal >= 5)     nuevas.Add("racha_5");
        return nuevas;
    }

    void ShufflePreguntas(List<Pregunta> lista)
    {
        System.Random rng = new System.Random();
        int n = lista.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Pregunta value = lista[k];
            lista[k] = lista[n];
            lista[n] = value;
        }
    }
}
