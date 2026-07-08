#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera previews de música procedural como archivos WAV fuera de Assets/.
/// Permite escuchar variantes sin meter nada en el juego para decidir cuál gusta.
/// Output: D:/TFG/AdAeternum.unity/AdAeternum 0.0.1/PreviewMusica/*.wav
/// </summary>
public static class PreviewMusicaProcedural
{
    const int SR = 44100;
    const float DUR = 16f; // 16 segundos por variante

    [MenuItem("AdAeternum/Generar previews de música")]
    public static void Generar()
    {
        string carpeta = Path.Combine(Application.dataPath, "..", "PreviewMusica");
        Directory.CreateDirectory(carpeta);

        // Variantes individuales
        SaveMono(carpeta, "00_baseline_pad_estatico", BaselinePad(DUR));
        SaveStereo(carpeta, "A_estereo_panning", EstereoPanning(DUR));
        SaveMono(carpeta, "B_detuning_shimmer", DetuningShimmer(DUR));
        SaveMono(carpeta, "C_progresion_acordes", ProgresionAcordes(DUR));
        SaveMono(carpeta, "D_arpegio_sobre_pad", ArpegioSobrepad(DUR));
        SaveMono(carpeta, "E_brillo_estrellas", BrilloEstrellas(DUR));
        SaveMono(carpeta, "F_melodia_simple", MelodiaSimple(DUR));

        // Mezclas
        SaveStereo(carpeta, "MIX_1_C+A+B_recomendado", Mix_C_A_B(DUR));
        SaveStereo(carpeta, "MIX_2_C+A+B+D_rica", Mix_C_A_B_D(DUR));
        SaveStereo(carpeta, "MIX_3_C+A+B+E_cinematica", Mix_C_A_B_E(DUR));
        SaveStereo(carpeta, "MIX_4_TODO_C+A+B+D+E", Mix_TODO(DUR));

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Previews de música generadas en: {carpeta}</color>");
        EditorUtility.RevealInFinder(carpeta);
    }

    // ════════════════════════ V2: NUEVAS VARIANTES (feedback usuario) ════════════════════════
    // C es el base. Sin arpegio (D), sin estrellas (E). Detuning más sutil.
    // Probando melodía simple en versiones más suaves.

    [MenuItem("AdAeternum/Generar previews de música v2")]
    public static void GenerarV2()
    {
        string carpeta = Path.Combine(Application.dataPath, "..", "PreviewMusica");
        Directory.CreateDirectory(carpeta);

        SaveStereo(carpeta, "v2_05_C+A_sin_detuning",          Mix_v2_CA(DUR));
        SaveStereo(carpeta, "v2_06_C+A+B_muy_sutil",           Mix_v2_CA_BSutil(DUR));
        SaveStereo(carpeta, "v2_07_C+A+F_melodia_grave",       Mix_v2_CA_FGrave(DUR));
        SaveStereo(carpeta, "v2_08_C+A+F_melodia_lenta",       Mix_v2_CA_FLenta(DUR));
        SaveStereo(carpeta, "v2_09_C+A+F_melodia_espaciada",   Mix_v2_CA_FEspaciada(DUR));
        SaveStereo(carpeta, "v2_10_C+A+B_sutil+F_grave",       Mix_v2_CA_BSutil_FGrave(DUR));
        SaveStereo(carpeta, "v2_11_C+A+B_sutil+F_lenta",       Mix_v2_CA_BSutil_FLenta(DUR));
        SaveStereo(carpeta, "v2_12_C+A+F_susurro",             Mix_v2_CA_FSusurro(DUR));

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Previews v2 generadas en: {carpeta}</color>");
        EditorUtility.RevealInFinder(carpeta);
    }

    // ─── BASE: progresión de acordes + estéreo (helper común) ───
    private static (float[] L, float[] R) BaseProgresionEstereo(float duracion, float detunHz = 0f, float detunAmp = 0f)
    {
        int n = (int)(duracion * SR);
        var L = new float[n];
        var R = new float[n];

        var acordes = new[] { CM, ABMAJ, EBMAJ, BBMAJ };
        float[] pans = { -1f, -0.4f, 0.4f, 1f };
        float duracionAcorde = duracion / acordes.Length;
        float crossfade = 0.6f;

        for (int idx = 0; idx < acordes.Length; idx++)
        {
            var acorde = acordes[idx];
            float inicio = idx * duracionAcorde;
            float fin    = inicio + duracionAcorde;
            int iInicio = (int)(inicio * SR);
            int iFin    = (int)(fin * SR);
            int iCrossIn  = (int)((inicio + crossfade) * SR);
            int iCrossOut = (int)((fin - crossfade) * SR);

            for (int k = 0; k < acorde.Length; k++)
            {
                float pan = pans[k];
                float gL = Mathf.Cos(Mathf.PI * (pan + 1f) / 4f);
                float gR = Mathf.Sin(Mathf.PI * (pan + 1f) / 4f);

                var tmp = new float[n];
                AddSeno(tmp, acorde[k], 0.13f, inicio, duracionAcorde, SR);
                if (detunHz > 0f && detunAmp > 0f)
                    AddSeno(tmp, acorde[k] + detunHz, detunAmp, inicio, duracionAcorde, SR);

                for (int i = iInicio; i < iFin && i < n; i++)
                {
                    float env = 1f;
                    if (i < iCrossIn) env = (i - iInicio) / (float)(iCrossIn - iInicio);
                    else if (i > iCrossOut) env = 1f - (i - iCrossOut) / (float)(iFin - iCrossOut);
                    env = Mathf.Clamp01(env);
                    L[i] += tmp[i] * gL * env;
                    R[i] += tmp[i] * gR * env;
                }
            }
        }

        AplicarLfo(L, 0.22f, 0.35f);
        AplicarLfo(R, 0.22f, 0.35f);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA(float duracion)
    {
        var (L, R) = BaseProgresionEstereo(duracion);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA_BSutil(float duracion)
    {
        // Detuning muy reducido: solo +1.5 Hz, amplitud 30% del original
        var (L, R) = BaseProgresionEstereo(duracion, detunHz: 1.5f, detunAmp: 0.04f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    // ─── Melodía: 3 variantes ───

    private static void AñadirMelodia(float[] L, float[] R, float duracion, float[] motif, float duracionNota, float amp, float decay)
    {
        float t = 0f;
        int idx = 0;
        while (t < duracion)
        {
            float f = motif[idx % motif.Length];
            // Pannear suavemente al centro (60% del peso al centro)
            float pan = 0.15f * Mathf.Sin(idx * 0.7f);
            float gL = Mathf.Cos(Mathf.PI * (pan + 1f) / 4f);
            float gR = Mathf.Sin(Mathf.PI * (pan + 1f) / 4f);

            int inicio = (int)(t * SR);
            int fin = Mathf.Min((int)((t + duracionNota) * SR), L.Length);
            for (int i = inicio; i < fin; i++)
            {
                float dt = (i - inicio) / (float)SR;
                float env = Mathf.Exp(-decay * dt) * Mathf.Min(1f, dt * 15f); // attack rápido para suavizar
                float s = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SR)) * amp * env;
                L[i] += s * gL;
                R[i] += s * gR;
            }
            t += duracionNota;
            idx++;
        }
    }

    private static (float[], float[]) Mix_v2_CA_FGrave(float duracion)
    {
        var (L, R) = BaseProgresionEstereo(duracion);
        // Melodía una octava abajo (G3-Bb3-C4-...) y volumen reducido
        float[] motif = { 196f, 233.08f, 261.63f, 233.08f, 196f, 174.61f, 196f, 155.56f };
        AñadirMelodia(L, R, duracion, motif, duracionNota: 0.5f, amp: 0.12f, decay: 2.5f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA_FLenta(float duracion)
    {
        var (L, R) = BaseProgresionEstereo(duracion);
        // Notas el doble de largas (1s) en octava media, envolvente lenta = más "cantarina"
        float[] motif = { 392f, 466.16f, 523.25f, 466.16f, 392f, 349.23f, 392f, 311.13f };
        AñadirMelodia(L, R, duracion, motif, duracionNota: 1f, amp: 0.13f, decay: 1.2f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA_FEspaciada(float duracion)
    {
        var (L, R) = BaseProgresionEstereo(duracion);
        // Solo 4 notas espaciadas: una cada 2s con silencio entre medias
        float[] motif = { 523.25f, 466.16f, 392f, 466.16f };
        float duracionNota = 0.6f;
        float intervalo = 2f;
        float t = 0f;
        int idx = 0;
        while (t < duracion)
        {
            float f = motif[idx % motif.Length];
            int inicio = (int)(t * SR);
            int fin = Mathf.Min((int)((t + duracionNota) * SR), L.Length);
            for (int i = inicio; i < fin; i++)
            {
                float dt = (i - inicio) / (float)SR;
                float env = Mathf.Exp(-1.5f * dt) * Mathf.Min(1f, dt * 10f);
                float s = Mathf.Sin(2f * Mathf.PI * f * (i / (float)SR)) * 0.14f * env;
                L[i] += s * 0.6f;
                R[i] += s * 0.6f;
            }
            t += intervalo;
            idx++;
        }
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA_BSutil_FGrave(float duracion)
    {
        var (L, R) = BaseProgresionEstereo(duracion, detunHz: 1.5f, detunAmp: 0.04f);
        float[] motif = { 196f, 233.08f, 261.63f, 233.08f, 196f, 174.61f, 196f, 155.56f };
        AñadirMelodia(L, R, duracion, motif, duracionNota: 0.5f, amp: 0.11f, decay: 2.5f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA_BSutil_FLenta(float duracion)
    {
        var (L, R) = BaseProgresionEstereo(duracion, detunHz: 1.5f, detunAmp: 0.04f);
        float[] motif = { 392f, 466.16f, 523.25f, 466.16f, 392f, 349.23f, 392f, 311.13f };
        AñadirMelodia(L, R, duracion, motif, duracionNota: 1f, amp: 0.12f, decay: 1.2f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static (float[], float[]) Mix_v2_CA_FSusurro(float duracion)
    {
        // Melodía SUPER tenue, casi como un susurro mental
        var (L, R) = BaseProgresionEstereo(duracion);
        float[] motif = { 392f, 466.16f, 523.25f, 466.16f };
        AñadirMelodia(L, R, duracion, motif, duracionNota: 1.5f, amp: 0.07f, decay: 0.8f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    // ════════════════════════ V3: PISTAS LARGAS PERSONALIZADAS ════════════════════════
    // Progresión extendida con 8 acordes (no solo 4), melodía que sigue los acordes,
    // y versiones largas que loopean bien.

    // 8 acordes distintos, 2 secciones (A: home, B: aleja+vuelve).
    // Sección A (4 acordes): Cm  - Ab  - Eb  - Bb
    // Sección B (4 acordes): Fm  - Db  - Ab  - Cm   ← termina en Cm para loopear sin costura
    private static readonly float[] FM     = { 87.31f, 174.61f, 207.65f, 261.63f };   // Fa menor
    private static readonly float[] DBMAJ  = { 69.30f, 138.59f, 174.61f, 207.65f };   // Re♭ mayor

    // Melodía coherente con cada acorde (2 notas tenidas por acorde + descanso)
    // 8 acordes × 2 notas = 16 notas total
    private static readonly float[] MELODIA_PROG = {
        // Cm     Ab    Eb     Bb    Fm     Db    Ab     Cm
        196f, 155.56f,    // Cm:  G3, Eb3
        261.63f, 207.65f, // Ab:  C4, Ab3
        196f, 233.08f,    // Eb:  G3, Bb3
        293.66f, 174.61f, // Bb:  D4, F3
        261.63f, 207.65f, // Fm:  C4, Ab3
        174.61f, 277.18f, // Db:  F3, Db4
        261.63f, 311.13f, // Ab:  C4, Eb4
        196f, 261.63f     // Cm:  G3, C4
    };

    [MenuItem("AdAeternum/Generar previews de música v3 (LARGAS)")]
    public static void GenerarV3()
    {
        string carpeta = Path.Combine(Application.dataPath, "..", "PreviewMusica");
        Directory.CreateDirectory(carpeta);

        // 56 segundos: 8 acordes × 7s cada uno
        SaveStereo(carpeta, "v3_01_largo_progresion_solo_detuning_sutil",   PistaLarga(56f, detuning: true, melodia: false));
        SaveStereo(carpeta, "v3_02_largo_progresion_solo_melodia",           PistaLarga(56f, detuning: false, melodia: true));
        SaveStereo(carpeta, "v3_03_largo_progresion_detuning_y_melodia",     PistaLarga(56f, detuning: true, melodia: true));
        SaveStereo(carpeta, "v3_04_largo_solo_progresion_sin_extras",        PistaLarga(56f, detuning: false, melodia: false));

        // Versión EXTRA larga 80s con 8 acordes a 10s cada uno (más respirar)
        SaveStereo(carpeta, "v3_05_extra_largo_80s_melodia_detuning",        PistaLarga(80f, detuning: true, melodia: true));

        // Versión con DOBLE progresión (16 acordes, ~90s): repite las 2 secciones cambiando intensidad
        SaveStereo(carpeta, "v3_06_doble_seccion_90s",                       PistaDobleSeccion(90f));

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Previews v3 (largas) generadas en: {carpeta}</color>");
        EditorUtility.RevealInFinder(carpeta);
    }

    private static (float[], float[]) PistaLarga(float duracion, bool detuning, bool melodia)
    {
        int n = (int)(duracion * SR);
        var L = new float[n];
        var R = new float[n];

        var acordes = new[] { CM, ABMAJ, EBMAJ, BBMAJ, FM, DBMAJ, ABMAJ, CM };
        float[] pans = { -1f, -0.4f, 0.4f, 1f };
        float duracionAcorde = duracion / acordes.Length;
        float crossfade = 0.8f;

        float detunHz  = detuning ? 1.5f : 0f;
        float detunAmp = detuning ? 0.04f : 0f;

        for (int idx = 0; idx < acordes.Length; idx++)
        {
            var acorde = acordes[idx];
            float inicio = idx * duracionAcorde;
            float fin    = inicio + duracionAcorde;
            int iInicio = (int)(inicio * SR);
            int iFin    = (int)(fin * SR);
            int iCrossIn  = (int)((inicio + crossfade) * SR);
            int iCrossOut = (int)((fin - crossfade) * SR);

            for (int k = 0; k < acorde.Length; k++)
            {
                float pan = pans[k];
                float gL = Mathf.Cos(Mathf.PI * (pan + 1f) / 4f);
                float gR = Mathf.Sin(Mathf.PI * (pan + 1f) / 4f);

                var tmp = new float[n];
                AddSeno(tmp, acorde[k], 0.13f, inicio, duracionAcorde, SR);
                if (detunHz > 0f) AddSeno(tmp, acorde[k] + detunHz, detunAmp, inicio, duracionAcorde, SR);

                for (int i = iInicio; i < iFin && i < n; i++)
                {
                    float env = 1f;
                    if (i < iCrossIn) env = (i - iInicio) / (float)(iCrossIn - iInicio);
                    else if (i > iCrossOut) env = 1f - (i - iCrossOut) / (float)(iFin - iCrossOut);
                    env = Mathf.Clamp01(env);
                    L[i] += tmp[i] * gL * env;
                    R[i] += tmp[i] * gR * env;
                }
            }
        }

        AplicarLfo(L, 0.18f, 0.30f);
        AplicarLfo(R, 0.18f, 0.30f);

        // Melodía: 16 notas, 2 por acorde, espaciadas dentro del acorde
        if (melodia)
        {
            float duracionAcorde2 = duracionAcorde;
            for (int idx = 0; idx < acordes.Length; idx++)
            {
                float chordStart = idx * duracionAcorde2;
                // 2 notas por acorde: una en el 15% del acorde (~1s), otra en el 55%
                float n1Inicio = chordStart + duracionAcorde2 * 0.15f;
                float n2Inicio = chordStart + duracionAcorde2 * 0.55f;
                float duracionNota = duracionAcorde2 * 0.32f;

                float f1 = MELODIA_PROG[idx * 2];
                float f2 = MELODIA_PROG[idx * 2 + 1];

                AddMelodyNote(L, R, f1, n1Inicio, duracionNota, amp: 0.10f, decay: 1.3f, panOffset: -0.15f);
                AddMelodyNote(L, R, f2, n2Inicio, duracionNota, amp: 0.10f, decay: 1.3f, panOffset:  0.15f);
            }
        }

        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    private static void AddMelodyNote(float[] L, float[] R, float freq, float t0, float duracion, float amp, float decay, float panOffset)
    {
        float gL = Mathf.Cos(Mathf.PI * (panOffset + 1f) / 4f);
        float gR = Mathf.Sin(Mathf.PI * (panOffset + 1f) / 4f);

        int inicio = (int)(t0 * SR);
        int fin    = Mathf.Min((int)((t0 + duracion) * SR), L.Length);

        for (int i = inicio; i < fin; i++)
        {
            float dt = (i - inicio) / (float)SR;
            float attack = Mathf.Min(1f, dt * 12f);
            float env = Mathf.Exp(-decay * dt) * attack;
            float s = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)SR)) * amp * env;
            L[i] += s * gL;
            R[i] += s * gR;
        }
    }

    // ════════════════════════ V4: MELODÍA AUDIBLE ════════════════════════
    // El problema en v3: melodía a 0.10 amp, notas largas (2.2s) y mismas frecuencias
    // que el pad → se camufla. Esta tanda la hace claramente audible.

    [MenuItem("AdAeternum/Generar previews de música v4 (melodía audible)")]
    public static void GenerarV4()
    {
        string carpeta = Path.Combine(Application.dataPath, "..", "PreviewMusica");
        Directory.CreateDirectory(carpeta);

        SaveStereo(carpeta, "v4_01_melodia_clara_volumen",      PistaLargaMelodiaV4(56f, opt: 1));
        SaveStereo(carpeta, "v4_02_melodia_octava_media",       PistaLargaMelodiaV4(56f, opt: 2));
        SaveStereo(carpeta, "v4_03_melodia_pulsada_corta",      PistaLargaMelodiaV4(56f, opt: 3));
        SaveStereo(carpeta, "v4_04_melodia_con_armonicos",      PistaLargaMelodiaV4(56f, opt: 4));
        SaveStereo(carpeta, "v4_05_melodia_4_notas_por_acorde", PistaLargaMelodiaV4(56f, opt: 5));
        SaveStereo(carpeta, "v4_06_completo_recomendado",       PistaLargaMelodiaV4(56f, opt: 6));

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Previews v4 generadas en: {carpeta}</color>");
        EditorUtility.RevealInFinder(carpeta);
    }

    // Mismas progresiones que v3 pero con la melodía configurable
    private static (float[], float[]) PistaLargaMelodiaV4(float duracion, int opt)
    {
        // PAD: base progresión + detuning sutil (lo que te gusta)
        var (L, R) = PistaLarga(duracion, detuning: true, melodia: false);

        // Bajar un poco el pad para acoger la melodía sin saturación
        for (int i = 0; i < L.Length; i++) { L[i] *= 0.80f; R[i] *= 0.80f; }

        // Definir la melodía adaptada a cada acorde, dependiendo de opt
        // Cada chord: array de tuplas (frecuencia, tiempoRelativo, duracion, ampMul)
        var acordes = new[] { CM, ABMAJ, EBMAJ, BBMAJ, FM, DBMAJ, ABMAJ, CM };
        float duracionAcorde = duracion / acordes.Length; // 7s

        // Frecuencias adaptadas por acorde (notas que suenan bien sobre cada acorde)
        // Formato: dos notas grave + dos notas media (para que las opt elijan rangos)
        float[][] notasGrave = new[] {
            new[] { 196f, 233.08f },     // Cm: G3, Bb3
            new[] { 207.65f, 261.63f },  // Ab: Ab3, C4
            new[] { 233.08f, 311.13f },  // Eb: Bb3, Eb4
            new[] { 174.61f, 220f },     // Bb: F3, A3
            new[] { 174.61f, 220f },     // Fm: F3, A3 (Ab3=207)
            new[] { 277.18f, 174.61f },  // Db: Db4, F3
            new[] { 261.63f, 207.65f },  // Ab: C4, Ab3
            new[] { 196f, 261.63f }      // Cm: G3, C4
        };

        float[][] notasMedia = new[] {
            new[] { 392f, 466.16f },     // Cm: G4, Bb4
            new[] { 415.30f, 523.25f },  // Ab: Ab4, C5
            new[] { 466.16f, 622.25f },  // Eb: Bb4, Eb5
            new[] { 349.23f, 440f },     // Bb: F4, A4
            new[] { 349.23f, 440f },     // Fm: F4, A4
            new[] { 554.37f, 349.23f },  // Db: Db5, F4
            new[] { 523.25f, 415.30f },  // Ab: C5, Ab4
            new[] { 392f, 523.25f }      // Cm: G4, C5
        };

        for (int idx = 0; idx < acordes.Length; idx++)
        {
            float chordStart = idx * duracionAcorde;

            switch (opt)
            {
                case 1: // melodía clara: amp 0.22, notas 0.8s, decay 1.5
                    AddMelodyNoteHarm(L, R, notasGrave[idx][0], chordStart + 0.5f,  0.9f, 0.22f, 1.5f,  0f, harm: false);
                    AddMelodyNoteHarm(L, R, notasGrave[idx][1], chordStart + 3.5f,  0.9f, 0.22f, 1.5f,  0f, harm: false);
                    break;

                case 2: // melodía en octava media (4-5) clara
                    AddMelodyNoteHarm(L, R, notasMedia[idx][0], chordStart + 0.5f,  0.9f, 0.18f, 1.5f,  0f, harm: false);
                    AddMelodyNoteHarm(L, R, notasMedia[idx][1], chordStart + 3.5f,  0.9f, 0.18f, 1.5f,  0f, harm: false);
                    break;

                case 3: // pulsada: notas cortas tipo "pluck", más rítmicas
                    AddMelodyNoteHarm(L, R, notasGrave[idx][0], chordStart + 0.0f,  0.4f, 0.25f, 4f,   0f, harm: false);
                    AddMelodyNoteHarm(L, R, notasGrave[idx][1], chordStart + 1.5f,  0.4f, 0.25f, 4f,   0f, harm: false);
                    AddMelodyNoteHarm(L, R, notasGrave[idx][0], chordStart + 3.5f,  0.4f, 0.25f, 4f,   0f, harm: false);
                    AddMelodyNoteHarm(L, R, notasGrave[idx][1], chordStart + 5.0f,  0.4f, 0.25f, 4f,   0f, harm: false);
                    break;

                case 4: // con armónicos: timbre distinto al pad para destacar
                    AddMelodyNoteHarm(L, R, notasGrave[idx][0], chordStart + 0.5f,  0.9f, 0.20f, 1.8f, 0f, harm: true);
                    AddMelodyNoteHarm(L, R, notasGrave[idx][1], chordStart + 3.5f,  0.9f, 0.20f, 1.8f, 0f, harm: true);
                    break;

                case 5: // 4 notas por acorde (más densa)
                    AddMelodyNoteHarm(L, R, notasGrave[idx][0], chordStart + 0.3f,  1.3f, 0.18f, 1.5f, -0.2f, harm: false);
                    AddMelodyNoteHarm(L, R, notasGrave[idx][1], chordStart + 2.0f,  1.3f, 0.18f, 1.5f,  0.2f, harm: false);
                    AddMelodyNoteHarm(L, R, notasMedia[idx][0], chordStart + 4.0f,  1.3f, 0.16f, 1.5f, -0.2f, harm: false);
                    AddMelodyNoteHarm(L, R, notasMedia[idx][1], chordStart + 5.5f,  1.3f, 0.16f, 1.5f,  0.2f, harm: false);
                    break;

                case 6: // RECOMENDADO: octava media con armónicos, notas medias 1s, 2 notas por acorde, pannéo sutil
                    AddMelodyNoteHarm(L, R, notasMedia[idx][0], chordStart + 0.7f,  1.0f, 0.16f, 1.6f, -0.25f, harm: true);
                    AddMelodyNoteHarm(L, R, notasMedia[idx][1], chordStart + 4.0f,  1.0f, 0.16f, 1.6f,  0.25f, harm: true);
                    break;
            }
        }

        Normalizar(L, 0.85f); Normalizar(R, 0.85f);
        return (L, R);
    }

    /// <summary>Como AddMelodyNote pero con opción de añadir 2º armónico para distinguir timbre del pad.</summary>
    private static void AddMelodyNoteHarm(float[] L, float[] R, float freq, float t0, float duracion, float amp, float decay, float pan, bool harm)
    {
        float gL = Mathf.Cos(Mathf.PI * (pan + 1f) / 4f);
        float gR = Mathf.Sin(Mathf.PI * (pan + 1f) / 4f);

        int inicio = (int)(t0 * SR);
        int fin    = Mathf.Min((int)((t0 + duracion) * SR), L.Length);

        for (int i = inicio; i < fin; i++)
        {
            float dt = (i - inicio) / (float)SR;
            float attack = Mathf.Min(1f, dt * 20f);
            float env = Mathf.Exp(-decay * dt) * attack;
            float t = i / (float)SR;
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            if (harm)
            {
                // 2º armónico (octava) y 3º armónico, dan brillo y distinción
                s += 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                s += 0.15f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);
            }
            s *= amp * env;
            L[i] += s * gL;
            R[i] += s * gR;
        }
    }

    // ════════════════════════ V5: MELODÍA MÁS DENSA ════════════════════════
    // En vez de 2 notas tenidas por acorde, frases melódicas reales con varias notas por compás.

    [MenuItem("AdAeternum/Generar previews de música v5 (melodía densa)")]
    public static void GenerarV5()
    {
        string carpeta = Path.Combine(Application.dataPath, "..", "PreviewMusica");
        Directory.CreateDirectory(carpeta);

        SaveStereo(carpeta, "v5_01_4_notas_por_acorde",     PistaMelodiaDensa(56f, notasPorAcorde: 4));
        SaveStereo(carpeta, "v5_02_8_notas_por_acorde",     PistaMelodiaDensa(56f, notasPorAcorde: 8));
        SaveStereo(carpeta, "v5_03_8_notas_con_ritmo",      PistaMelodiaDensa(56f, notasPorAcorde: 8, conRitmo: true));
        SaveStereo(carpeta, "v5_04_16_notas_dense",         PistaMelodiaDensa(56f, notasPorAcorde: 16));
        SaveStereo(carpeta, "v5_05_8_notas_grave",          PistaMelodiaDensa(56f, notasPorAcorde: 8, octavaArriba: false));
        SaveStereo(carpeta, "v5_06_4_notas_con_silencio",   PistaMelodiaDensa(56f, notasPorAcorde: 4, conSilencio: true));

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>Previews v5 generadas en: {carpeta}</color>");
        EditorUtility.RevealInFinder(carpeta);
    }

    // Frases melódicas: 16 notas por acorde (semicorcheas si pensamos 4/4 con cada acorde = 1 compás).
    // Cada array tiene 16 grados de la escala que casan con el acorde. Los modos 4 y 8 notas
    // submuestrean este array (cogen 1 cada 4 o 1 cada 2).
    private static readonly float[][] FRASES_16 = new float[][] {
        // Cm: G Bb C Eb D C Bb G | F G Bb C Bb G F Eb
        new[] { 196f, 233.08f, 261.63f, 311.13f, 293.66f, 261.63f, 233.08f, 196f,
                174.61f, 196f, 233.08f, 261.63f, 233.08f, 196f, 174.61f, 155.56f },
        // Ab: C Eb G C Bb G Eb C | Db C Eb G F Eb C Ab
        new[] { 261.63f, 311.13f, 392f, 523.25f, 466.16f, 392f, 311.13f, 261.63f,
                277.18f, 261.63f, 311.13f, 392f, 349.23f, 311.13f, 261.63f, 207.65f },
        // Eb: G Bb D F Eb D Bb G | F G Bb D C Bb G Eb
        new[] { 196f, 233.08f, 293.66f, 349.23f, 311.13f, 293.66f, 233.08f, 196f,
                174.61f, 196f, 233.08f, 293.66f, 261.63f, 233.08f, 196f, 155.56f },
        // Bb: F Bb D F G F D Bb | A Bb D F Eb D Bb F
        new[] { 174.61f, 233.08f, 293.66f, 349.23f, 392f, 349.23f, 293.66f, 233.08f,
                220f, 233.08f, 293.66f, 349.23f, 311.13f, 293.66f, 233.08f, 174.61f },
        // Fm: C Eb F Ab G F Eb C | Db C Eb F Eb C Ab F
        new[] { 261.63f, 311.13f, 349.23f, 415.30f, 392f, 349.23f, 311.13f, 261.63f,
                277.18f, 261.63f, 311.13f, 349.23f, 311.13f, 261.63f, 207.65f, 174.61f },
        // Db: F Ab Db F Eb Db Ab F | Eb F Ab Db C Ab F Db
        new[] { 174.61f, 207.65f, 277.18f, 349.23f, 311.13f, 277.18f, 207.65f, 174.61f,
                155.56f, 174.61f, 207.65f, 277.18f, 261.63f, 207.65f, 174.61f, 138.59f },
        // Ab: C Eb Ab C Bb Ab Eb C | Db C Eb Ab G Eb C Ab
        new[] { 261.63f, 311.13f, 415.30f, 523.25f, 466.16f, 415.30f, 311.13f, 261.63f,
                277.18f, 261.63f, 311.13f, 415.30f, 392f, 311.13f, 261.63f, 207.65f },
        // Cm (vuelta): G Bb C Eb F Eb C Bb | C Eb G C Bb G Eb C
        new[] { 196f, 233.08f, 261.63f, 311.13f, 349.23f, 311.13f, 261.63f, 233.08f,
                261.63f, 311.13f, 392f, 523.25f, 466.16f, 392f, 311.13f, 261.63f }
    };

    private static (float[], float[]) PistaMelodiaDensa(float duracion, int notasPorAcorde, bool octavaArriba = true, bool conRitmo = false, bool conSilencio = false)
    {
        // Pad de fondo + detuning sutil
        var (L, R) = PistaLarga(duracion, detuning: true, melodia: false);
        // Bajar el pad para que la melodía se siente sobre él
        for (int i = 0; i < L.Length; i++) { L[i] *= 0.70f; R[i] *= 0.70f; }

        float duracionAcorde = duracion / 8f;
        int paso = 16 / notasPorAcorde; // 16→1, 8→2, 4→4

        for (int idx = 0; idx < 8; idx++)
        {
            float chordStart = idx * duracionAcorde;
            float duracionNota = duracionAcorde / notasPorAcorde;

            for (int n = 0; n < notasPorAcorde; n++)
            {
                int faseIdx = n * paso;
                float f = FRASES_16[idx][faseIdx];

                // Subir una octava si toca (la melodía suena más "presente" arriba del pad)
                if (octavaArriba && f < 250f) f *= 2f;

                // Patrón rítmico: alternar notas largas con notas cortas
                float durNotaReal = duracionNota * 0.9f;
                if (conRitmo && (n % 2 == 1)) durNotaReal *= 0.5f; // contratiempos más cortos

                // Silencios: cada 4ª nota se queda en silencio
                if (conSilencio && (n % 4 == 3)) continue;

                float pan = 0.2f * Mathf.Sin(idx * 0.6f + n * 0.3f);

                float amp = notasPorAcorde >= 8 ? 0.13f : 0.16f;
                float decay = notasPorAcorde >= 8 ? 2.2f : 1.6f;

                AddMelodyNoteHarm(L, R, f, chordStart + n * duracionNota, durNotaReal, amp, decay, pan, harm: true);
            }
        }

        Normalizar(L, 0.88f); Normalizar(R, 0.88f);
        return (L, R);
    }

    private static (float[], float[]) PistaDobleSeccion(float duracion)
    {
        // 90s: primera mitad (45s) más íntima (solo detuning sutil, sin melodía)
        //       segunda mitad (45s) más completa (detuning + melodía)
        int n = (int)(duracion * SR);
        var L = new float[n];
        var R = new float[n];

        var (l1, r1) = PistaLarga(duracion / 2f, detuning: true, melodia: false);
        var (l2, r2) = PistaLarga(duracion / 2f, detuning: true, melodia: true);

        int mitad = n / 2;
        System.Array.Copy(l1, 0, L, 0, Mathf.Min(l1.Length, mitad));
        System.Array.Copy(r1, 0, R, 0, Mathf.Min(r1.Length, mitad));
        System.Array.Copy(l2, 0, L, mitad, Mathf.Min(l2.Length, n - mitad));
        System.Array.Copy(r2, 0, R, mitad, Mathf.Min(r2.Length, n - mitad));

        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    // ════════════════════════ HELPERS DE ESCRITURA WAV ════════════════════════

    private static void SaveMono(string carpeta, string nombre, float[] samples)
    {
        WriteWav(Path.Combine(carpeta, nombre + ".wav"), samples, SR, 1);
    }

    private static void SaveStereo(string carpeta, string nombre, (float[] L, float[] R) stereo)
    {
        int n = stereo.L.Length;
        var inter = new float[n * 2];
        for (int i = 0; i < n; i++)
        {
            inter[i * 2]     = stereo.L[i];
            inter[i * 2 + 1] = stereo.R[i];
        }
        WriteWav(Path.Combine(carpeta, nombre + ".wav"), inter, SR, 2);
    }

    private static void WriteWav(string path, float[] samples, int sampleRate, int channels)
    {
        int byteCount = samples.Length * 2;
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + byteCount);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1); // PCM
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((short)(channels * 2));
            bw.Write((short)16);
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(byteCount);
            foreach (var s in samples)
            {
                float clamped = Mathf.Clamp(s, -1f, 1f);
                bw.Write((short)Mathf.RoundToInt(clamped * 32767f));
            }
        }
    }

    // ════════════════════════ HELPERS DE SÍNTESIS ════════════════════════

    private static void AddSeno(float[] data, float freq, float amp, float t0, float dur, float SRf)
    {
        int inicio = Mathf.Max(0, (int)(t0 * SRf));
        int fin    = Mathf.Min(data.Length, (int)((t0 + dur) * SRf));
        for (int i = inicio; i < fin; i++)
        {
            float t = i / SRf;
            data[i] += Mathf.Sin(2f * Mathf.PI * freq * t) * amp;
        }
    }

    private static void AddSenoEnv(float[] data, float freq, float amp, float t0, float dur, float decay, float SRf)
    {
        int inicio = Mathf.Max(0, (int)(t0 * SRf));
        int fin    = Mathf.Min(data.Length, (int)((t0 + dur) * SRf));
        for (int i = inicio; i < fin; i++)
        {
            float t = (i - inicio) / SRf;
            float env = Mathf.Exp(-decay * t);
            data[i] += Mathf.Sin(2f * Mathf.PI * freq * (i / SRf)) * amp * env;
        }
    }

    private static void AplicarLfo(float[] data, float lfoHz, float profundidad)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)SR;
            float lfo = (1f - profundidad) + profundidad * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * lfoHz * t));
            data[i] *= lfo;
        }
    }

    private static void Normalizar(float[] data, float max = 0.85f)
    {
        float pico = 0f;
        for (int i = 0; i < data.Length; i++) pico = Mathf.Max(pico, Mathf.Abs(data[i]));
        if (pico > max)
        {
            float k = max / pico;
            for (int i = 0; i < data.Length; i++) data[i] *= k;
        }
    }

    private static float[] Sumar(params float[][] arrays)
    {
        int n = arrays[0].Length;
        var r = new float[n];
        foreach (var a in arrays)
            for (int i = 0; i < n; i++) r[i] += a[i];
        return r;
    }

    // Acordes (cuádruples para tener bajos)
    private static readonly float[] CM    = { 130.81f, 155.56f, 196.00f, 233.08f };  // Do menor
    private static readonly float[] ABMAJ = { 103.83f, 130.81f, 155.56f, 196.00f };  // La♭ mayor
    private static readonly float[] EBMAJ = { 155.56f, 196.00f, 233.08f, 293.66f };  // Mi♭ mayor
    private static readonly float[] BBMAJ = { 116.54f, 146.83f, 174.61f, 207.65f };  // Si♭ mayor

    // ════════════════════════ VARIANTES INDIVIDUALES ════════════════════════

    /// <summary>Lo que tienes ahora: Cm7 estático con LFO suave.</summary>
    private static float[] BaselinePad(float duracion)
    {
        int n = (int)(duracion * SR);
        var data = new float[n];
        foreach (var f in CM) AddSeno(data, f, 0.18f / CM.Length, 0f, duracion, SR);
        AplicarLfo(data, 0.18f, 0.7f);
        Normalizar(data);
        return data;
    }

    /// <summary>A: cada nota del acorde panneada a una posición distinta.</summary>
    private static (float[], float[]) EstereoPanning(float duracion)
    {
        int n = (int)(duracion * SR);
        var L = new float[n];
        var R = new float[n];
        // Repartir 4 notas en estéreo
        float[] pans = { -1f, -0.4f, 0.4f, 1f }; // C izq, Eb izq-medio, G der-medio, Bb der
        for (int k = 0; k < CM.Length; k++)
        {
            float pan = pans[k];
            float gL = Mathf.Cos(Mathf.PI * (pan + 1f) / 4f);
            float gR = Mathf.Sin(Mathf.PI * (pan + 1f) / 4f);
            var tmp = new float[n];
            AddSeno(tmp, CM[k], 0.20f, 0f, duracion, SR);
            for (int i = 0; i < n; i++)
            {
                L[i] += tmp[i] * gL;
                R[i] += tmp[i] * gR;
            }
        }
        AplicarLfo(L, 0.18f, 0.7f);
        AplicarLfo(R, 0.18f, 0.7f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    /// <summary>B: cada nota duplicada y desafinada 5 Hz para movimiento orgánico.</summary>
    private static float[] DetuningShimmer(float duracion)
    {
        int n = (int)(duracion * SR);
        var data = new float[n];
        foreach (var f in CM)
        {
            AddSeno(data, f,        0.10f, 0f, duracion, SR);
            AddSeno(data, f + 4.5f, 0.10f, 0f, duracion, SR); // desafinado
        }
        AplicarLfo(data, 0.18f, 0.5f);
        Normalizar(data);
        return data;
    }

    /// <summary>C: 4 acordes cambiando cada 4s con crossfade. Progresión vi-IV-I-V en Mi♭ mayor.</summary>
    private static float[] ProgresionAcordes(float duracion)
    {
        int n = (int)(duracion * SR);
        var data = new float[n];
        var acordes = new[] { CM, ABMAJ, EBMAJ, BBMAJ };

        float duracionAcorde = duracion / acordes.Length;
        float crossfade = 0.5f;

        for (int idx = 0; idx < acordes.Length; idx++)
        {
            var acorde = acordes[idx];
            float inicio = idx * duracionAcorde;
            float fin    = inicio + duracionAcorde;

            int iInicio = (int)(inicio * SR);
            int iFin    = (int)(fin * SR);
            int iCrossIn  = (int)((inicio + crossfade) * SR);
            int iCrossOut = (int)((fin - crossfade) * SR);

            // Sintetizar este acorde y mezclarlo con envolvente trapezoidal (fade in/out)
            var tmp = new float[n];
            foreach (var f in acorde) AddSeno(tmp, f, 0.18f / acorde.Length, inicio, duracionAcorde, SR);

            for (int i = iInicio; i < iFin && i < n; i++)
            {
                float env = 1f;
                if (i < iCrossIn) env = (i - iInicio) / (float)(iCrossIn - iInicio);
                else if (i > iCrossOut) env = 1f - (i - iCrossOut) / (float)(iFin - iCrossOut);
                env = Mathf.Clamp01(env);
                data[i] += tmp[i] * env;
            }
        }

        AplicarLfo(data, 0.25f, 0.4f);
        Normalizar(data);
        return data;
    }

    /// <summary>D: pad Cm + arpegio agudo (Cm7) ascendente-descendente continuo.</summary>
    private static float[] ArpegioSobrepad(float duracion)
    {
        int n = (int)(duracion * SR);
        var data = BaselinePad(duracion);
        // Atenuar el pad para dejar espacio
        for (int i = 0; i < data.Length; i++) data[i] *= 0.5f;

        // Arpegio octava 5 (Cm7): C5, Eb5, G5, Bb5, G5, Eb5
        float[] arp = { 523.25f, 622.25f, 783.99f, 932.33f, 783.99f, 622.25f };
        float duracionNota = 0.30f;
        float t = 0f;
        int idx = 0;
        while (t < duracion)
        {
            float f = arp[idx % arp.Length];
            AddSenoEnv(data, f, 0.18f, t, duracionNota, 5f, SR);
            t += duracionNota;
            idx++;
        }
        Normalizar(data);
        return data;
    }

    /// <summary>E: pad Cm + nota muy aguda con shimmer + "estrellas" blips aleatorios.</summary>
    private static float[] BrilloEstrellas(float duracion)
    {
        int n = (int)(duracion * SR);
        var data = BaselinePad(duracion);
        for (int i = 0; i < data.Length; i++) data[i] *= 0.7f;

        // Nota aguda sostenida con LFO lento propio (C7 = 2093 Hz)
        var brillo = new float[n];
        AddSeno(brillo, 2093f, 0.06f, 0f, duracion, SR);
        AplicarLfo(brillo, 0.1f, 0.8f);
        for (int i = 0; i < n; i++) data[i] += brillo[i];

        // Estrellas aleatorias: blips cortos a frecuencias agudas
        var rng = new System.Random(42);
        float t = 1f;
        while (t < duracion - 1f)
        {
            float interval = 0.8f + (float)rng.NextDouble() * 1.2f;
            float freq = 1500f + (float)rng.NextDouble() * 2500f;
            AddSenoEnv(data, freq, 0.05f, t, 0.25f, 15f, SR);
            t += interval;
        }

        Normalizar(data);
        return data;
    }

    /// <summary>F: pad Cm + melodía repetitiva de 8 notas.</summary>
    private static float[] MelodiaSimple(float duracion)
    {
        int n = (int)(duracion * SR);
        var data = BaselinePad(duracion);
        for (int i = 0; i < data.Length; i++) data[i] *= 0.5f;

        // Melodía: G4, Bb4, C5, Bb4, G4, F4, G4, Eb4 (en Cm)
        float[] motif = { 392f, 466.16f, 523.25f, 466.16f, 392f, 349.23f, 392f, 311.13f };
        float duracionNota = 0.5f;
        float t = 0f;
        int idx = 0;
        while (t < duracion)
        {
            float f = motif[idx % motif.Length];
            AddSenoEnv(data, f, 0.20f, t, duracionNota, 3f, SR);
            t += duracionNota;
            idx++;
        }
        Normalizar(data);
        return data;
    }

    // ════════════════════════ MEZCLAS ════════════════════════

    /// <summary>MIX_1: C (progresión) + A (estéreo) + B (detuning). Lo recomendado base.</summary>
    private static (float[], float[]) Mix_C_A_B(float duracion)
    {
        int n = (int)(duracion * SR);
        var L = new float[n];
        var R = new float[n];

        var acordes = new[] { CM, ABMAJ, EBMAJ, BBMAJ };
        float[] pans = { -1f, -0.4f, 0.4f, 1f };
        float duracionAcorde = duracion / acordes.Length;
        float crossfade = 0.5f;

        for (int idx = 0; idx < acordes.Length; idx++)
        {
            var acorde = acordes[idx];
            float inicio = idx * duracionAcorde;
            float fin    = inicio + duracionAcorde;
            int iInicio = (int)(inicio * SR);
            int iFin    = (int)(fin * SR);
            int iCrossIn  = (int)((inicio + crossfade) * SR);
            int iCrossOut = (int)((fin - crossfade) * SR);

            for (int k = 0; k < acorde.Length; k++)
            {
                float pan = pans[k];
                float gL = Mathf.Cos(Mathf.PI * (pan + 1f) / 4f);
                float gR = Mathf.Sin(Mathf.PI * (pan + 1f) / 4f);

                var tmp = new float[n];
                AddSeno(tmp, acorde[k],        0.09f, inicio, duracionAcorde, SR);
                AddSeno(tmp, acorde[k] + 4.5f, 0.09f, inicio, duracionAcorde, SR); // detuning

                for (int i = iInicio; i < iFin && i < n; i++)
                {
                    float env = 1f;
                    if (i < iCrossIn) env = (i - iInicio) / (float)(iCrossIn - iInicio);
                    else if (i > iCrossOut) env = 1f - (i - iCrossOut) / (float)(iFin - iCrossOut);
                    env = Mathf.Clamp01(env);
                    L[i] += tmp[i] * gL * env;
                    R[i] += tmp[i] * gR * env;
                }
            }
        }

        AplicarLfo(L, 0.25f, 0.4f);
        AplicarLfo(R, 0.25f, 0.4f);
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    /// <summary>MIX_2: + arpegio (más movimiento).</summary>
    private static (float[], float[]) Mix_C_A_B_D(float duracion)
    {
        var (L, R) = Mix_C_A_B(duracion);
        // Bajar volumen de la base para acoger el arpegio
        for (int i = 0; i < L.Length; i++) { L[i] *= 0.65f; R[i] *= 0.65f; }

        float[] arp = { 523.25f, 622.25f, 783.99f, 932.33f, 783.99f, 622.25f };
        float dn = 0.30f;
        float t = 0f;
        int idx = 0;
        while (t < duracion)
        {
            float f = arp[idx % arp.Length];
            // Pannear el arpegio alternando L/R
            bool izq = (idx % 2) == 0;
            var canal = izq ? L : R;
            AddSenoEnv(canal, f, 0.18f, t, dn, 5f, SR);
            t += dn;
            idx++;
        }
        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    /// <summary>MIX_3: + brillo (más cinematográfico/atmosférico).</summary>
    private static (float[], float[]) Mix_C_A_B_E(float duracion)
    {
        var (L, R) = Mix_C_A_B(duracion);

        int n = (int)(duracion * SR);
        var brillo = new float[n];
        AddSeno(brillo, 2093f, 0.06f, 0f, duracion, SR);
        AplicarLfo(brillo, 0.1f, 0.8f);
        for (int i = 0; i < n; i++) { L[i] += brillo[i] * 0.7f; R[i] += brillo[i] * 0.7f; }

        var rng = new System.Random(42);
        float t = 1f;
        while (t < duracion - 1f)
        {
            float interval = 0.8f + (float)rng.NextDouble() * 1.2f;
            float freq = 1500f + (float)rng.NextDouble() * 2500f;
            bool izq = rng.NextDouble() < 0.5;
            var canal = izq ? L : R;
            AddSenoEnv(canal, freq, 0.05f, t, 0.25f, 15f, SR);
            t += interval;
        }

        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    /// <summary>MIX_4: TODO mezclado (progresión + estéreo + detuning + arpegio + brillo).</summary>
    private static (float[], float[]) Mix_TODO(float duracion)
    {
        var (L, R) = Mix_C_A_B_D(duracion);

        int n = L.Length;
        var brillo = new float[n];
        AddSeno(brillo, 2093f, 0.05f, 0f, duracion, SR);
        AplicarLfo(brillo, 0.1f, 0.8f);
        for (int i = 0; i < n; i++) { L[i] += brillo[i] * 0.6f; R[i] += brillo[i] * 0.6f; }

        var rng = new System.Random(42);
        float t = 1f;
        while (t < duracion - 1f)
        {
            float interval = 0.8f + (float)rng.NextDouble() * 1.2f;
            float freq = 1500f + (float)rng.NextDouble() * 2500f;
            bool izq = rng.NextDouble() < 0.5;
            var canal = izq ? L : R;
            AddSenoEnv(canal, freq, 0.04f, t, 0.25f, 15f, SR);
            t += interval;
        }

        Normalizar(L); Normalizar(R);
        return (L, R);
    }

    // ════════════════════════ EXPORT MIDI (editable) ════════════════════════
    // Exporta v5_02 (la que te gustó) como archivo MIDI multitrack para editar
    // notas, alturas y tiempos en cualquier DAW (Reaper, Audacity, BeepBox, etc.)

    [MenuItem("AdAeternum/Exportar MIDI de la pista v5_02")]
    public static void ExportarMidi()
    {
        string carpeta = Path.Combine(Application.dataPath, "..", "PreviewMusica");
        Directory.CreateDirectory(carpeta);

        float duracion = 56f;
        int notasPorAcorde = 8;
        bool octavaArriba = true;
        float duracionAcorde = duracion / 8f;
        int paso = 16 / notasPorAcorde;

        var acordes = new[] { CM, ABMAJ, EBMAJ, BBMAJ, FM, DBMAJ, ABMAJ, CM };

        // 480 ticks por negra, tempo 120 BPM (1s = 960 ticks)
        const int TPQ = 480;
        const int microsPerQuarter = 500000;

        var melodyEvents = new System.Collections.Generic.List<(int tick, byte[] msg)>();
        var padEvents    = new System.Collections.Generic.List<(int tick, byte[] msg)>();

        // MELODÍA: 8 notas por acorde
        for (int idx = 0; idx < 8; idx++)
        {
            float chordStart = idx * duracionAcorde;
            float dn = duracionAcorde / notasPorAcorde;
            for (int n2 = 0; n2 < notasPorAcorde; n2++)
            {
                int faseIdx = n2 * paso;
                float f = FRASES_16[idx][faseIdx];
                if (octavaArriba && f < 250f) f *= 2f;
                int note = FreqToMidi(f);
                float t0 = chordStart + n2 * dn;
                float t1 = t0 + dn * 0.9f;
                int tickOn  = (int)(t0 * 2f * TPQ);
                int tickOff = (int)(t1 * 2f * TPQ);
                melodyEvents.Add((tickOn,  new byte[] { 0x90, (byte)note, 90 }));
                melodyEvents.Add((tickOff, new byte[] { 0x80, (byte)note, 64 }));
            }
        }

        // PAD: 4 notas del acorde sostenidas todo el acorde
        for (int idx = 0; idx < 8; idx++)
        {
            float chordStart = idx * duracionAcorde;
            float chordEnd   = chordStart + duracionAcorde - 0.1f;
            int tickOn  = (int)(chordStart * 2f * TPQ);
            int tickOff = (int)(chordEnd   * 2f * TPQ);
            foreach (var f in acordes[idx])
            {
                int note = FreqToMidi(f);
                padEvents.Add((tickOn,  new byte[] { 0x90, (byte)note, 55 }));
                padEvents.Add((tickOff, new byte[] { 0x80, (byte)note, 64 }));
            }
        }

        melodyEvents.Sort((a, b) => a.tick.CompareTo(b.tick));
        padEvents.Sort((a, b) => a.tick.CompareTo(b.tick));

        // Construir archivo MIDI Format 1
        string ruta = Path.Combine(carpeta, "v5_02_melodia_editable.mid");
        using (var fs = new FileStream(ruta, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(Encoding.ASCII.GetBytes("MThd"));
            WriteIntBE(bw, 6);
            WriteShortBE(bw, 1);
            WriteShortBE(bw, 3);
            WriteShortBE(bw, (short)TPQ);

            // Track 0: tempo
            using (var ms = new MemoryStream())
            using (var tw = new BinaryWriter(ms))
            {
                WriteVLQ(tw, 0);
                byte[] nombre = Encoding.ASCII.GetBytes("Tempo");
                tw.Write(new byte[] { 0xFF, 0x03, (byte)nombre.Length });
                tw.Write(nombre);
                WriteVLQ(tw, 0);
                tw.Write(new byte[] { 0xFF, 0x51, 0x03 });
                tw.Write((byte)((microsPerQuarter >> 16) & 0xFF));
                tw.Write((byte)((microsPerQuarter >> 8) & 0xFF));
                tw.Write((byte)(microsPerQuarter & 0xFF));
                WriteVLQ(tw, 0);
                tw.Write(new byte[] { 0xFF, 0x2F, 0x00 });
                EscribirTrack(bw, ms.ToArray());
            }

            // Track 1: Melodia
            using (var ms = new MemoryStream())
            using (var tw = new BinaryWriter(ms))
            {
                WriteVLQ(tw, 0);
                byte[] nombre = Encoding.ASCII.GetBytes("Melodia");
                tw.Write(new byte[] { 0xFF, 0x03, (byte)nombre.Length });
                tw.Write(nombre);
                int prev = 0;
                foreach (var ev in melodyEvents)
                {
                    WriteVLQ(tw, ev.tick - prev);
                    tw.Write(ev.msg);
                    prev = ev.tick;
                }
                WriteVLQ(tw, 0);
                tw.Write(new byte[] { 0xFF, 0x2F, 0x00 });
                EscribirTrack(bw, ms.ToArray());
            }

            // Track 2: Pad acordes
            using (var ms = new MemoryStream())
            using (var tw = new BinaryWriter(ms))
            {
                WriteVLQ(tw, 0);
                byte[] nombre = Encoding.ASCII.GetBytes("PadAcordes");
                tw.Write(new byte[] { 0xFF, 0x03, (byte)nombre.Length });
                tw.Write(nombre);
                int prev = 0;
                foreach (var ev in padEvents)
                {
                    WriteVLQ(tw, ev.tick - prev);
                    tw.Write(ev.msg);
                    prev = ev.tick;
                }
                WriteVLQ(tw, 0);
                tw.Write(new byte[] { 0xFF, 0x2F, 0x00 });
                EscribirTrack(bw, ms.ToArray());
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>MIDI exportado: {ruta}</color>");
        EditorUtility.RevealInFinder(carpeta);
    }

    private static int FreqToMidi(float freq) => Mathf.RoundToInt(12f * Mathf.Log(freq / 440f, 2f) + 69f);

    private static void WriteIntBE(BinaryWriter bw, int v)
    {
        bw.Write((byte)((v >> 24) & 0xFF));
        bw.Write((byte)((v >> 16) & 0xFF));
        bw.Write((byte)((v >> 8) & 0xFF));
        bw.Write((byte)(v & 0xFF));
    }

    private static void WriteShortBE(BinaryWriter bw, short v)
    {
        bw.Write((byte)((v >> 8) & 0xFF));
        bw.Write((byte)(v & 0xFF));
    }

    private static void WriteVLQ(BinaryWriter bw, int v)
    {
        if (v < 0) v = 0;
        int buffer = v & 0x7F;
        while ((v >>= 7) > 0)
        {
            buffer <<= 8;
            buffer |= 0x80 | (v & 0x7F);
        }
        while (true)
        {
            bw.Write((byte)(buffer & 0xFF));
            if ((buffer & 0x80) != 0) buffer >>= 8;
            else break;
        }
    }

    private static void EscribirTrack(BinaryWriter bw, byte[] data)
    {
        bw.Write(Encoding.ASCII.GetBytes("MTrk"));
        WriteIntBE(bw, data.Length);
        bw.Write(data);
    }
}
#endif
