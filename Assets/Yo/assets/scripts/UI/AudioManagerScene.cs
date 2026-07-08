using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manager de audio "de escena": MonoBehaviour pensado para enlazarse desde el
/// Inspector en eventos Button.onClick. No es singleton (usa el GameObject
/// llamado "AudioManagerScene" en la escena).
///
/// Uso:
///   1. Hay un GameObject "AudioManagerScene" en la escena con este componente
///   2. El array "efectos" contiene todos los SFX disponibles (rellenable en Inspector)
///   3. Para cada Button:
///        - Inspector > OnClick > + > arrastra el GameObject AudioManagerScene
///        - En el dropdown elige el método: AudioManagerScene → PlayClickAvanzar
///        - (o el que corresponda)
///   4. Si quieres cambiar el sonido de un botón, basta con cambiar el método
///      seleccionado en el dropdown del Inspector. Sin tocar código.
///   5. Si quieres cambiar QUÉ AudioClip se reproduce para PlayClickAvanzar,
///      basta con cambiar el clip en el array "efectos" (entrada "click_avanzar").
/// </summary>
public class AudioManagerScene : MonoBehaviour
{
    // Singleton ligero: NO usa DontDestroyOnLoad, vive en su escena.
    // Sirve para que otros scripts puedan llamar a RegistrarBoton() en
    // botones que se instancian en runtime sin esperar al escaneo de 1s.
    public static AudioManagerScene Instance { get; private set; }

    [System.Serializable]
    public class EfectoSonido
    {
        [Tooltip("Identificador del efecto (debe coincidir con el nombre usado en los métodos PlayX). Ej: click_avanzar")]
        public string nombre;

        [Tooltip("AudioClip a reproducir. Si está vacío, se ignora la llamada.")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("Volumen multiplicador (0-1). Se multiplica también por el volumen SFX global del AudioManager.")]
        public float volumen = 1f;
    }

    [Header("AudioSource")]
    [Tooltip("Si no se asigna, se crea uno automáticamente en este GameObject.")]
    [SerializeField] private AudioSource source;

    [Header("Efectos de sonido")]
    [Tooltip("Lista de SFX disponibles. Edita el clip de cualquier entrada para cambiar lo que suena al llamar el método correspondiente.")]
    [SerializeField] private EfectoSonido[] efectos;

    private Dictionary<string, EfectoSonido> _indice;
    private HashSet<int> _botonesAutoRegistrados = new HashSet<int>();

    // Keywords para auto-detectar botones de "atrás/cancelar/destructivo".
    // OJO: el matching es por PALABRA COMPLETA (no substring). "no" NO matchea
    // dentro de "alumno", pero sí matchea "Boton No" o "BotonNo" (split CamelCase).
    private static readonly string[] KW_RETROCEDER = {
        "atras", "atrás", "back", "volver", "cancel", "cancelar", "cerrar",
        "close", "salir", "exit", "abandonar", "no", "retroceder", "rechazar",
        "borrar", "eliminar", "quitar", "remove", "delete",   // destructivos
        "guardar", "save",                                     // confirmar/finalizar edición
        "detener", "parar", "stop", "finalizar"                // parar acciones (combate, etc.)
    };

    // Regex que tokeniza por: 1) caracteres no-letra, 2) frontera camelCase
    // "BotonAtras"      → ["boton", "atras"]
    // "Registrar Alumno"→ ["registrar", "alumno"]
    // "alumno"          → ["alumno"]  (ya NO matchea "no")
    // "BotonNo"         → ["boton", "no"]  (matchea "no")
    private static readonly System.Text.RegularExpressions.Regex SplitTokens =
        new System.Text.RegularExpressions.Regex(
            @"(?<=[a-záéíóúñ])(?=[A-ZÁÉÍÓÚÑ])|[^\p{L}]+",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private void Awake()
    {
        Instance = this;

        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;

        ConstruirIndice();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Escanear botones dinámicos cada 1s. SOLO afecta a los que NO tienen
        // ya un listener persistente apuntando a este manager (los que están
        // enlazados via Inspector se mantienen como están).
        StartCoroutine(EscaneoBotonesDinamicos());
    }

    private IEnumerator EscaneoBotonesDinamicos()
    {
        // Esperamos 1 frame para que otros scripts puedan registrar sus
        // botones específicos (vía RegistrarBoton / RegistrarBotonConSonido)
        // ANTES del primer auto-detect. Así el auto-detect no les añade
        // ClickAvanzar y luego se solapa con el sonido específico.
        yield return null;
        while (true)
        {
            EscanearYRegistrarDinamicos();
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    private void EscanearYRegistrarDinamicos()
    {
        var botones = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in botones)
        {
            if (b == null) continue;
            int id = b.GetInstanceID();
            if (_botonesAutoRegistrados.Contains(id)) continue;
            _botonesAutoRegistrados.Add(id);

            // ¿Ya tiene un listener PERSISTENTE apuntando a este manager?
            // (los enlazados via Inspector tienen esto). Si sí → respetar y no añadir.
            if (TienePersistentListenerAEsteManager(b)) continue;

            // Auto-detectar tipo de click
            bool esRetro = EsBotonRetroceder(b);
            string nombre = esRetro ? "click_retroceder" : "click_avanzar";
            b.onClick.AddListener(() => Play(nombre));
        }
    }

    private bool TienePersistentListenerAEsteManager(Button b)
    {
        int count = b.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (b.onClick.GetPersistentTarget(i) is AudioManagerScene)
                return true;
        }
        return false;
    }

    private bool EsBotonRetroceder(Button b)
    {
        if (ContieneKeywordRetroceder(b.gameObject.name)) return true;
        var tmp = b.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmp != null && ContieneKeywordRetroceder(tmp.text)) return true;
        return false;
    }

    private bool ContieneKeywordRetroceder(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return false;
        // Tokenizamos antes de pasar a minúsculas para que la regla CamelCase
        // funcione, luego comparamos cada token en minúscula contra las keywords.
        var tokens = SplitTokens.Split(texto);
        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token)) continue;
            string t = token.ToLowerInvariant();
            foreach (var kw in KW_RETROCEDER)
                if (t == kw) return true;
        }
        return false;
    }

    /// <summary>
    /// Registra un botón concreto inmediatamente (en vez de esperar al escaneo periódico).
    /// Útil al instanciar prefabs si quieres que suenen de inmediato.
    /// </summary>
    public void RegistrarBoton(Button b)
    {
        if (b == null) return;
        int id = b.GetInstanceID();
        if (_botonesAutoRegistrados.Contains(id)) return;
        _botonesAutoRegistrados.Add(id);
        if (TienePersistentListenerAEsteManager(b)) return;
        bool esRetro = EsBotonRetroceder(b);
        string nombre = esRetro ? "click_retroceder" : "click_avanzar";
        b.onClick.AddListener(() => Play(nombre));
    }

    /// <summary>
    /// Registra un botón con un sonido SFX específico (por nombre del array efectos).
    /// Útil cuando quieres que un botón concreto suene distinto al auto-detect
    /// (avanzar/retroceder). Ejemplo: el botón "Enviar mensaje" → "login".
    /// Marca el botón como ya procesado para que el escaneo automático NO le
    /// añada el sonido por defecto encima.
    /// </summary>
    public void RegistrarBotonConSonido(Button b, string nombreSfx)
    {
        if (b == null || string.IsNullOrEmpty(nombreSfx)) return;
        int id = b.GetInstanceID();
        if (_botonesAutoRegistrados.Contains(id)) return;
        _botonesAutoRegistrados.Add(id);
        b.onClick.AddListener(() => Play(nombreSfx));
    }

    /// <summary>
    /// Excluye un botón del auto-detect: lo marca como ya procesado SIN
    /// añadir ningún listener. Úsalo cuando el sonido del click se reproduce
    /// manualmente desde código (ej. botones toggle que suenan avanzar/retroceder
    /// según abran o cierren un panel).
    /// </summary>
    public void ExcluirBotonDeAutoDetect(Button b)
    {
        if (b == null) return;
        int id = b.GetInstanceID();
        if (!_botonesAutoRegistrados.Contains(id))
            _botonesAutoRegistrados.Add(id);
    }

    private void OnValidate()
    {
        // Rebuild en edición para que cambios al inspector se reflejen
        ConstruirIndice();
    }

    private void ConstruirIndice()
    {
        _indice = new Dictionary<string, EfectoSonido>();
        if (efectos == null) return;
        foreach (var e in efectos)
        {
            if (e != null && !string.IsNullOrEmpty(e.nombre) && !_indice.ContainsKey(e.nombre))
                _indice[e.nombre] = e;
        }
    }

    // ═══════════════ Métodos públicos para llamar desde Inspector ═══════════════
    // Cada método reproduce el SFX con el nombre indicado. El nombre coincide
    // con la entrada del array "efectos".

    public void PlayClickAvanzar()        => Play("click_avanzar");
    public void PlayClickRetroceder()     => Play("click_retroceder");
    public void PlayDialogoAbrir()        => Play("dialogo_abrir");
    public void PlayLogin()               => Play("login");
    public void PlayRespuestaCorrecta()   => Play("respuesta_correcta");
    public void PlayRespuestaIncorrecta() => Play("respuesta_incorrecta");
    public void PlayDisparo()             => Play("disparo");
    public void PlayDisparoCargado()      => Play("disparo_cargado");
    public void PlayImpacto()             => Play("impacto");
    public void PlayImpactoSuave()        => Play("impacto_suave");
    public void PlayAlarmaAtaque()        => Play("alarma_ataque");
    public void PlayEscudoRecarga()       => Play("escudo_recarga");
    public void PlayEnergiaCargada()      => Play("energia_cargada");
    public void PlayZonaDestruida()       => Play("zona_destruida");
    public void PlayVictoria()            => Play("victoria");
    public void PlayDerrota()             => Play("derrota");
    public void PlayToastInfo()           => Play("toast_info");
    public void PlayToastExito()          => Play("toast_exito");
    public void PlayToastError()          => Play("toast_error");
    public void PlayToastAviso()          => Play("toast_aviso");

    /// <summary>Llamada genérica por nombre. Útil si añades un SFX nuevo y aún no tienes método.</summary>
    public void PlayByName(string nombre) => Play(nombre);

    /// <summary>Llamada genérica por índice del array.</summary>
    public void PlayByIndex(int index)
    {
        if (efectos == null || index < 0 || index >= efectos.Length) return;
        ReproducirEntry(efectos[index]);
    }

    // ═══════════════ Implementación ═══════════════

    private void Play(string nombre)
    {
        if (_indice == null || _indice.Count == 0) ConstruirIndice();
        if (_indice == null) return;
        if (!_indice.TryGetValue(nombre, out var e)) return;
        ReproducirEntry(e);
    }

    private void ReproducirEntry(EfectoSonido e)
    {
        if (e == null || e.clip == null || source == null) return;
        // Respeta los volúmenes globales del AudioManager singleton si existe
        float vol = e.volumen * AudioManager.VolumenSFX * AudioManager.VolumenMaster;
        if (vol <= 0.001f) return;
        source.PlayOneShot(e.clip, vol);
    }
}
