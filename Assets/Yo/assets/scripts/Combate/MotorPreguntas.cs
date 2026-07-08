using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reproductor de preguntas reutilizable. Pinta preguntas en la UI existente
/// (textoEnunciado, botonesOpciones, temporizador) y notifica resultados.
///
/// Soporta dos modos:
///   - Modo "una vuelta": agotar la lista barajada y parar (combate viejo lineal).
///   - Modo "loop": al agotar, re-barajear y volver a empezar (combate Tipo 1).
///
/// El motor NO decide cuándo termina el combate: solo sirve preguntas y devuelve
/// resultados. La estrategia decide cuándo `Pausar()`, `Reanudar()` o `Detener()`.
/// </summary>
public class MotorPreguntas : MonoBehaviour
{
    [Header("UI ya existente del combate")]
    [SerializeField] private TextMeshProUGUI textoEnunciado;
    [SerializeField] private TextMeshProUGUI textoResultado;
    [SerializeField] private Button[] botonesOpciones;
    [SerializeField] private TextMeshProUGUI[] textosBotones;
    [SerializeField] private TextMeshProUGUI textoTiempo;
    [SerializeField] private Slider sliderTiempo;

    private List<Pregunta> poolBase = new List<Pregunta>();
    private Queue<Pregunta> colaActual = new Queue<Pregunta>();
    private bool modoLoop = true;
    private bool pausado = false;

    /// <summary>True si se acabó la lista y no estaba en modo loop. Útil para detectar fin del modo viejo.</summary>
    public bool Agotado { get; private set; }

    private Pregunta preguntaActual;
    private bool estaProcesandoRespuesta;
    private Coroutine coroutineTimer;
    private float tiempoInicioPregunta;

    private Action<Pregunta, bool, float> onResultado;
    private Action onAgotadoSinLoop;

    /// <summary>Configura el pool de preguntas y los callbacks. Empieza pausado.</summary>
    public void Configurar(
        List<Pregunta> preguntas,
        bool loop,
        Action<Pregunta, bool, float> onResultado,
        Action onAgotadoSinLoop = null)
    {
        DetenerYLimpiar();
        this.poolBase = new List<Pregunta>(preguntas ?? new List<Pregunta>());
        this.modoLoop = loop;
        this.onResultado = onResultado;
        this.onAgotadoSinLoop = onAgotadoSinLoop;
        this.Agotado = false;
        RellenarColaConBarajeo();
    }

    /// <summary>Empieza/continúa sirviendo preguntas. Si está agotado y no hay loop, no hace nada.</summary>
    public void Reanudar()
    {
        pausado = false;
        if (preguntaActual == null) ServirSiguiente();
    }

    /// <summary>Pausa: oculta UI de pregunta y para el temporizador (sin perder estado).</summary>
    public void Pausar()
    {
        pausado = true;
        if (coroutineTimer != null) { StopCoroutine(coroutineTimer); coroutineTimer = null; }
        OcultarBotones();
        if (textoEnunciado != null) textoEnunciado.text = "";
        if (textoTiempo != null)    textoTiempo.text    = "";
        if (sliderTiempo != null)   sliderTiempo.value  = 0f;
    }

    /// <summary>Detiene completamente: limpia estado interno.</summary>
    public void DetenerYLimpiar()
    {
        if (coroutineTimer != null) { StopCoroutine(coroutineTimer); coroutineTimer = null; }
        OcultarBotones();
        preguntaActual = null;
        estaProcesandoRespuesta = false;
        pausado = true;
        Agotado = false;
        if (textoEnunciado != null) textoEnunciado.text = "";
        if (textoResultado != null) textoResultado.text = "";
        if (textoTiempo != null)    textoTiempo.text    = "";
        if (sliderTiempo != null)   sliderTiempo.value  = 0f;
    }

    /// <summary>Cancela la pregunta en curso sin contar como respondida (usado p.ej. cuando el escudo cae).</summary>
    public void CancelarPreguntaActual()
    {
        if (coroutineTimer != null) { StopCoroutine(coroutineTimer); coroutineTimer = null; }
        OcultarBotones();
        preguntaActual = null;
        estaProcesandoRespuesta = false;
        if (textoEnunciado != null) textoEnunciado.text = "";
        if (textoTiempo != null)    textoTiempo.text    = "";
        if (sliderTiempo != null)   sliderTiempo.value  = 0f;
    }

    private void RellenarColaConBarajeo()
    {
        var lista = new List<Pregunta>(poolBase);
        Shuffle(lista);
        colaActual = new Queue<Pregunta>(lista);
    }

    private void ServirSiguiente()
    {
        if (pausado) return;

        if (colaActual.Count == 0)
        {
            if (modoLoop && poolBase.Count > 0)
            {
                RellenarColaConBarajeo();
            }
            else
            {
                Agotado = true;
                onAgotadoSinLoop?.Invoke();
                return;
            }
        }

        if (colaActual.Count == 0) { Agotado = true; return; }

        preguntaActual = colaActual.Dequeue();
        estaProcesandoRespuesta = false;
        tiempoInicioPregunta = Time.time;

        if (textoEnunciado != null) textoEnunciado.text = preguntaActual.enunciado;
        if (textoResultado != null) textoResultado.text = "";

        for (int i = 0; i < botonesOpciones.Length; i++)
        {
            if (i < preguntaActual.opciones.Length)
            {
                botonesOpciones[i].gameObject.SetActive(true);
                if (i < textosBotones.Length) textosBotones[i].text = preguntaActual.opciones[i];
                int idx = i;
                botonesOpciones[i].onClick.RemoveAllListeners();
                botonesOpciones[i].onClick.AddListener(() => Responder(idx));
            }
            else
            {
                botonesOpciones[i].gameObject.SetActive(false);
            }
        }

        if (coroutineTimer != null) StopCoroutine(coroutineTimer);
        coroutineTimer = StartCoroutine(TemporizadorCoroutine(preguntaActual.tiempoLimite));
    }

    private IEnumerator TemporizadorCoroutine(float duracion)
    {
        float restante = duracion;
        if (sliderTiempo != null) sliderTiempo.maxValue = duracion;
        while (restante > 0f && !pausado)
        {
            restante -= Time.deltaTime;
            if (textoTiempo != null) textoTiempo.text = Mathf.CeilToInt(restante).ToString();
            if (sliderTiempo != null) sliderTiempo.value = restante;
            yield return null;
        }
        if (!estaProcesandoRespuesta && !pausado)
        {
            estaProcesandoRespuesta = true;
            float t = Time.time - tiempoInicioPregunta;
            if (textoResultado != null) textoResultado.text = "<color=red>¡Tiempo!</color>";
            Pregunta p = preguntaActual;
            // Limpiar el estado ANTES de notificar: si el callback lanza una excepción,
            // el avance a la siguiente pregunta no debe quedar bloqueado.
            preguntaActual = null;
            NotificarResultado(p, false, t);
            ProgramarSiguiente();
        }
    }

    private void Responder(int indiceSeleccionado)
    {
        if (preguntaActual == null || estaProcesandoRespuesta || pausado) return;
        estaProcesandoRespuesta = true;
        if (coroutineTimer != null) { StopCoroutine(coroutineTimer); coroutineTimer = null; }

        bool correcto = indiceSeleccionado == preguntaActual.respuestaCorrecta;
        float t = Time.time - tiempoInicioPregunta;
        if (textoResultado != null) textoResultado.text = correcto ? "<color=green>Correcto</color>" : "<color=red>Incorrecto</color>";

        // Limpiar el estado ANTES de notificar: si onResultado lanza una excepción
        // (p.ej. un fallo transitorio de Firebase al recargar escudo/energía), el
        // bucle de preguntas debe seguir avanzando igualmente y no congelarse.
        Pregunta p = preguntaActual;
        preguntaActual = null;
        NotificarResultado(p, correcto, t);
        ProgramarSiguiente();
    }

    /// <summary>Invoca el callback de resultado de forma segura: una excepción del
    /// suscriptor nunca debe romper el ciclo de preguntas.</summary>
    private void NotificarResultado(Pregunta p, bool correcto, float t)
    {
        try { onResultado?.Invoke(p, correcto, t); }
        catch (Exception e)
        {
            Debug.LogError("[MotorPreguntas] onResultado lanzó una excepción; se continúa con la siguiente pregunta. " + e);
        }
    }

    /// <summary>Programa la siguiente pregunta de forma idempotente (cancela cualquier
    /// programación previa para no duplicar).</summary>
    private void ProgramarSiguiente()
    {
        CancelInvoke(nameof(ServirSiguiente));
        Invoke(nameof(ServirSiguiente), 1f);
    }

    /// <summary>
    /// Red de seguridad contra congelaciones: si quedamos con una respuesta ya
    /// procesada pero sin pregunta en pantalla y sin una siguiente programada
    /// (p.ej. porque el panel se desactivó y canceló el Invoke, o un callback
    /// abortó el flujo), volvemos a servir. Sin esto, el defensor —que no tiene
    /// ciclo de pausa/reanudación como el atacante— podía quedarse bloqueado.
    /// </summary>
    private void Update()
    {
        if (!pausado && !Agotado && preguntaActual == null && estaProcesandoRespuesta
            && !IsInvoking(nameof(ServirSiguiente)))
        {
            ServirSiguiente();
        }
    }

    private void OcultarBotones()
    {
        if (botonesOpciones == null) return;
        foreach (var b in botonesOpciones) if (b != null) b.gameObject.SetActive(false);
    }

    private static void Shuffle(List<Pregunta> lista)
    {
        System.Random rng = new System.Random();
        int n = lista.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (lista[k], lista[n]) = (lista[n], lista[k]);
        }
    }
}
