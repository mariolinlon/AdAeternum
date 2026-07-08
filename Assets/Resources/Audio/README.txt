═══════════════════════════════════════════════════════════════════════
  AUDIO OVERRIDE — Cómo sustituir los sonidos procedurales por archivos
═══════════════════════════════════════════════════════════════════════

El AudioManager genera todos los SFX y la música proceduralmente al
arrancar. Suena retro pero funcional. Si quieres mejorar la calidad,
basta con dejar archivos de audio en estas carpetas con los nombres
EXACTOS que se listan abajo y el AudioManager los usará automáticamente
en lugar de los procedurales — no hace falta tocar código.

Formatos aceptados: .wav, .ogg, .mp3 (Unity los carga todos como AudioClip).
Recomendación: .ogg para música (comprime bien, suena bien en loop),
.wav para SFX cortos (sin compresión, sin latencia).


─── SFX  →  Assets/Resources/Audio/SFX/ ──────────────────────────────────

  click_boton            ← click en cualquier botón
  respuesta_correcta     ← contestar bien una pregunta
  respuesta_incorrecta   ← contestar mal una pregunta
  disparo                ← atacante dispara un ataque
  impacto                ← (libre por ahora, hookable)
  alarma_ataque          ← aparece un ataque entrante al defensor
  escudo_recarga         ← (libre por ahora, hookable)
  energia_cargada        ← (libre por ahora, hookable)
  victoria               ← fin de combate con victoria
  derrota                ← fin de combate con derrota
  zona_destruida         ← (libre por ahora, hookable)
  toast_info             ← toast tipo Info
  toast_exito            ← toast tipo Exito
  toast_error            ← toast tipo Error
  toast_aviso            ← toast tipo Aviso
  dialogo_abrir          ← ConfirmDialog aparece


─── Música  →  Assets/Resources/Audio/Music/ ─────────────────────────────

  musica_menu            ← se reproduce en login + pantallas no-combate
  musica_combate         ← se reproduce durante combate Tipo 1

  IMPORTANTE: deben ser archivos LOOPABLE (sin silencio al inicio/fin,
  preferiblemente con compas que cuadre). 30-90 segundos está bien.


─── Dónde conseguir audios CC0 (sin atribución requerida) ────────────────

  SFX:
    https://kenney.nl/assets/category:Audio        (packs sci-fi gratis)
    https://freesound.org                          (filtra por CC0)
    https://jsfxr.me                               (genera SFX 8-bit online)
    https://sfxr.me                                (lo mismo, otra UI)

  Música:
    https://pixabay.com/music/                     (royalty-free, filtra "space" o "sci-fi")
    https://opengameart.org/art-search?keys=ambient (CC0/CC-BY space ambient)
    https://incompetech.com                        (Kevin MacLeod, CC-BY)
    https://www.youtube.com/audiolibrary           (sin licencia, descargar wav)


─── Volúmenes ────────────────────────────────────────────────────────────

  Por código:
    AudioManager.VolumenMaster = 0.8f;   // 0-1
    AudioManager.VolumenMusica = 0.4f;
    AudioManager.VolumenSFX    = 0.9f;

  Se guardan automáticamente en PlayerPrefs y persisten entre sesiones.
  Si quieres sliders en la pantalla de ajustes, enlaza un Slider.onValueChanged
  a estas propiedades.

═══════════════════════════════════════════════════════════════════════
