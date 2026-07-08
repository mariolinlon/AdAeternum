using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AudioManager singleton auto-spawn. Gestiona música de fondo y SFX one-shot.
///
/// Uso:
///   AudioManager.PlaySFX(AudioManager.SFX.ClickBoton);
///   AudioManager.PlayMusic(AudioManager.Music.Combate);
///   AudioManager.StopMusic();
///   AudioManager.VolumenMusica = 0.4f;
///
/// Carga automática: si existen archivos en Resources/Audio/SFX o Resources/Audio/Music
/// con el nombre del enum (snake_case), se usan en lugar de los procedurales.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public enum SFX
    {
        ClickBoton,           // 0  alias de ClickAvanzar (compatibilidad)
        RespuestaCorrecta,    // 1
        RespuestaIncorrecta,  // 2
        Disparo,              // 3  (normal/suave)
        Impacto,              // 4  (fuerte)
        AlarmaAtaque,         // 5  (recortado a 1.5s)
        EscudoRecarga,        // 6  (subida)
        EnergiaCargada,       // 7  (clímax)
        Victoria,             // 8
        Derrota,              // 9
        ZonaDestruida,        // 10
        ToastInfo,            // 11
        ToastExito,           // 12
        ToastError,           // 13
        ToastAviso,           // 14
        DialogoAbrir,         // 15
        ClickAvanzar,         // 16  NUEVO: click "adelante/confirmar"
        ClickRetroceder,      // 17  NUEVO: click "atrás/cancelar"
        DisparoCargado,       // 18  NUEVO: variante cargada del disparo (más potente)
        ImpactoSuave,         // 19  NUEVO: variante suave del impacto
        Login                 // 20  NUEVO: sonido de login exitoso
    }

    public enum Music
    {
        Ninguna,        // 0
        Menu,           // 1  — pantallas tranquilas (login, pantalla de inicio)
        Combate,        // 2  — combate normal
        Briefing,       // 3  — pantalla de briefing / preparación
        Mapa,           // 4  — vista de mapa / selección de planeta
        CombateIntenso  // 5  — combate con más tensión (jefe, último escudo, etc.)
    }

    // ─── Singleton ─────────────────────────────────────────────────────────────

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null) GetOrCreate();
            return _instance;
        }
    }

    private static AudioManager GetOrCreate()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("[AudioManager]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<AudioManager>();
        _instance.Inicializar();
        return _instance;
    }

    // Auto-arranque tras cargar la primera escena: música menú por defecto.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoArrancar()
    {
        var mgr = Instance;
        mgr.ReproducirMusica(Music.Menu);
    }

    // ─── Estado interno ────────────────────────────────────────────────────────

    private AudioSource sourceMusic;
    private AudioSource sourceSFX;

    private Dictionary<SFX, AudioClip>   clipsSFX   = new Dictionary<SFX, AudioClip>();
    private Dictionary<Music, AudioClip> clipsMusic = new Dictionary<Music, AudioClip>();

    private float volMaster = 1f;
    private float volMusica = 0.45f;
    private float volSFX    = 0.85f;

    private Music musicaActual = Music.Ninguna;
    private Coroutine fadeRutina;

    private HashSet<int> botonesRegistrados = new HashSet<int>();
    private const float INTERVALO_ESCANEO = 1.5f;

    // ─── PlayerPrefs keys ──────────────────────────────────────────────────────

    private const string KEY_MASTER = "audio.master";
    private const string KEY_MUSICA = "audio.musica";
    private const string KEY_SFX    = "audio.sfx";

    // ─── API estática ──────────────────────────────────────────────────────────

    public static float VolumenMaster
    {
        get => Instance.volMaster;
        set { Instance.volMaster = Mathf.Clamp01(value); Instance.AplicarVolumenes(); Instance.Guardar(); }
    }
    public static float VolumenMusica
    {
        get => Instance.volMusica;
        set { Instance.volMusica = Mathf.Clamp01(value); Instance.AplicarVolumenes(); Instance.Guardar(); }
    }
    public static float VolumenSFX
    {
        get => Instance.volSFX;
        set { Instance.volSFX = Mathf.Clamp01(value); Instance.Guardar(); }
    }

    public static void PlaySFX(SFX sfx) => Instance.ReproducirSFX(sfx);
    public static void PlayMusic(Music m) => Instance.ReproducirMusica(m);
    public static void StopMusic() => Instance.DetenerMusica();

    /// <summary>Llamar después de instanciar dinámicamente botones para que el AudioManager les enganche el click SFX.</summary>
    public static void ReescanearBotones() => Instance.EscanearYRegistrarBotones();

    // ─── Inicialización ────────────────────────────────────────────────────────

    private void Inicializar()
    {
        sourceMusic = gameObject.AddComponent<AudioSource>();
        sourceMusic.loop = true;
        sourceMusic.playOnAwake = false;
        sourceMusic.spatialBlend = 0f;

        sourceSFX = gameObject.AddComponent<AudioSource>();
        sourceSFX.loop = false;
        sourceSFX.playOnAwake = false;
        sourceSFX.spatialBlend = 0f;

        Cargar();

        // Cargar SFX (file override si existe en Resources, fallback procedural)
        clipsSFX[SFX.ClickBoton]          = CargarOSintetizar("click_boton",          SintetizadorAudio.Click);
        clipsSFX[SFX.RespuestaCorrecta]   = CargarOSintetizar("respuesta_correcta",   SintetizadorAudio.RespuestaCorrecta);
        clipsSFX[SFX.RespuestaIncorrecta] = CargarOSintetizar("respuesta_incorrecta", SintetizadorAudio.RespuestaIncorrecta);
        clipsSFX[SFX.Disparo]             = CargarOSintetizar("disparo",              SintetizadorAudio.Disparo);
        clipsSFX[SFX.Impacto]             = CargarOSintetizar("impacto",              SintetizadorAudio.Impacto);
        clipsSFX[SFX.AlarmaAtaque]        = CargarOSintetizar("alarma_ataque",        SintetizadorAudio.AlarmaAtaque);
        clipsSFX[SFX.EscudoRecarga]       = CargarOSintetizar("escudo_recarga",       SintetizadorAudio.EscudoRecarga);
        clipsSFX[SFX.EnergiaCargada]      = CargarOSintetizar("energia_cargada",      SintetizadorAudio.EnergiaCargada);
        clipsSFX[SFX.Victoria]            = CargarOSintetizar("victoria",             SintetizadorAudio.Victoria);
        clipsSFX[SFX.Derrota]             = CargarOSintetizar("derrota",              SintetizadorAudio.Derrota);
        clipsSFX[SFX.ZonaDestruida]       = CargarOSintetizar("zona_destruida",       SintetizadorAudio.ZonaDestruida);
        clipsSFX[SFX.ToastInfo]           = CargarOSintetizar("toast_info",           SintetizadorAudio.ToastInfo);
        clipsSFX[SFX.ToastExito]          = CargarOSintetizar("toast_exito",          SintetizadorAudio.ToastExito);
        clipsSFX[SFX.ToastError]          = CargarOSintetizar("toast_error",          SintetizadorAudio.ToastError);
        clipsSFX[SFX.ToastAviso]          = CargarOSintetizar("toast_aviso",          SintetizadorAudio.ToastAviso);
        clipsSFX[SFX.DialogoAbrir]        = CargarOSintetizar("dialogo_abrir",        SintetizadorAudio.DialogoAbrir);
        // SFX nuevos (fallback procedural si no hay archivo)
        clipsSFX[SFX.ClickAvanzar]        = CargarOSintetizar("click_avanzar",        SintetizadorAudio.Click);
        clipsSFX[SFX.ClickRetroceder]     = CargarOSintetizar("click_retroceder",     SintetizadorAudio.Click);
        clipsSFX[SFX.DisparoCargado]      = CargarOSintetizar("disparo_cargado",      SintetizadorAudio.Disparo);
        clipsSFX[SFX.ImpactoSuave]        = CargarOSintetizar("impacto_suave",        SintetizadorAudio.Impacto);
        clipsSFX[SFX.Login]               = CargarOSintetizar("login",                SintetizadorAudio.DialogoAbrir);

        // Música (siempre intenta archivo primero, fallback procedural)
        clipsMusic[Music.Menu]           = CargarMusicaOSintetizar("musica_menu",            SintetizadorAudio.MusicaMenu);
        clipsMusic[Music.Combate]        = CargarMusicaOSintetizar("musica_combate",         SintetizadorAudio.MusicaCombate);
        clipsMusic[Music.Briefing]       = CargarMusicaOSintetizar("musica_briefing",        SintetizadorAudio.MusicaMenu);    // fallback al menu
        clipsMusic[Music.Mapa]           = CargarMusicaOSintetizar("musica_mapa",            SintetizadorAudio.MusicaMenu);    // fallback al menu
        clipsMusic[Music.CombateIntenso] = CargarMusicaOSintetizar("musica_combate_intenso", SintetizadorAudio.MusicaCombate); // fallback al combate

        AplicarVolumenes();

        // El auto-scan de botones quedó deshabilitado: ahora los clicks se gestionan
        // desde AudioManagerScene (componente en escena, enlazado desde Inspector).
    }

    private AudioClip CargarOSintetizar(string nombre, System.Func<AudioClip> generador)
    {
        var clip = Resources.Load<AudioClip>($"Audio/SFX/{nombre}");
        return clip != null ? clip : generador();
    }

    private AudioClip CargarMusicaOSintetizar(string nombre, System.Func<AudioClip> generador)
    {
        var clip = Resources.Load<AudioClip>($"Audio/Music/{nombre}");
        return clip != null ? clip : generador();
    }

    // ─── Reproducción ──────────────────────────────────────────────────────────

    private void ReproducirSFX(SFX sfx)
    {
        if (!clipsSFX.TryGetValue(sfx, out var clip) || clip == null) return;
        float vol = volSFX * volMaster;
        if (vol <= 0.001f) return;
        sourceSFX.PlayOneShot(clip, vol);
    }

    private void ReproducirMusica(Music m)
    {
        if (m == musicaActual && sourceMusic.isPlaying) return;
        if (!clipsMusic.TryGetValue(m, out var clip) || clip == null)
        {
            DetenerMusica();
            return;
        }
        musicaActual = m;
        if (fadeRutina != null) StopCoroutine(fadeRutina);
        fadeRutina = StartCoroutine(CrossfadeAClip(clip));
    }

    private void DetenerMusica()
    {
        musicaActual = Music.Ninguna;
        if (fadeRutina != null) StopCoroutine(fadeRutina);
        fadeRutina = StartCoroutine(FadeOut());
    }

    private IEnumerator CrossfadeAClip(AudioClip nuevo)
    {
        // Fade out actual si suena
        if (sourceMusic.isPlaying)
        {
            float vol0 = sourceMusic.volume;
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                sourceMusic.volume = Mathf.Lerp(vol0, 0f, t / 0.4f);
                yield return null;
            }
            sourceMusic.Stop();
        }

        sourceMusic.clip = nuevo;
        sourceMusic.volume = 0f;
        sourceMusic.Play();

        float volTarget = volMusica * volMaster;
        float t2 = 0f;
        while (t2 < 0.6f)
        {
            t2 += Time.unscaledDeltaTime;
            sourceMusic.volume = Mathf.Lerp(0f, volTarget, t2 / 0.6f);
            yield return null;
        }
        sourceMusic.volume = volTarget;
        fadeRutina = null;
    }

    private IEnumerator FadeOut()
    {
        float vol0 = sourceMusic.volume;
        float t = 0f;
        while (t < 0.4f && sourceMusic.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            sourceMusic.volume = Mathf.Lerp(vol0, 0f, t / 0.4f);
            yield return null;
        }
        sourceMusic.Stop();
        fadeRutina = null;
    }

    private void AplicarVolumenes()
    {
        if (sourceMusic != null) sourceMusic.volume = volMusica * volMaster;
    }

    // ─── Persistencia ──────────────────────────────────────────────────────────

    private void Cargar()
    {
        volMaster = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        volMusica = PlayerPrefs.GetFloat(KEY_MUSICA, 0.45f);
        volSFX    = PlayerPrefs.GetFloat(KEY_SFX,    0.85f);
    }

    private void Guardar()
    {
        PlayerPrefs.SetFloat(KEY_MASTER, volMaster);
        PlayerPrefs.SetFloat(KEY_MUSICA, volMusica);
        PlayerPrefs.SetFloat(KEY_SFX,    volSFX);
        PlayerPrefs.Save();
    }

    // ─── Auto-registro de botones ──────────────────────────────────────────────

    private IEnumerator EscaneoPeriodico()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(INTERVALO_ESCANEO);
            EscanearYRegistrarBotones();
        }
    }

    private void EscanearYRegistrarBotones()
    {
        var botones = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in botones)
        {
            if (b == null) continue;
            int id = b.GetInstanceID();
            if (botonesRegistrados.Contains(id)) continue;
            botonesRegistrados.Add(id);
            // Auto-detección del tipo de click según el nombre del botón
            SFX tipoClick = EsBotonRetroceder(b) ? SFX.ClickRetroceder : SFX.ClickAvanzar;
            b.onClick.AddListener(() => PlaySFX(tipoClick));
        }
    }

    /// <summary>
    /// Detecta si un botón es de "atrás/cancelar/cerrar/volver" para usar
    /// el SFX ClickRetroceder en vez de ClickAvanzar.
    /// </summary>
    private static readonly string[] KEYWORDS_RETROCEDER = {
        "atras", "atrás", "back", "volver", "vuelta", "cancel", "cerrar",
        "close", "salir", "exit", "abandonar", "no", "retroceder", "rechazar"
    };
    private bool EsBotonRetroceder(Button b)
    {
        if (b == null) return false;
        string n = b.gameObject.name.ToLowerInvariant();
        foreach (var kw in KEYWORDS_RETROCEDER)
            if (n.Contains(kw)) return true;
        // También revisar el texto del botón (TMP o legacy)
        var tmp = b.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmp != null)
        {
            string t = tmp.text?.ToLowerInvariant() ?? "";
            foreach (var kw in KEYWORDS_RETROCEDER)
                if (t.Contains(kw)) return true;
        }
        return false;
    }
}
