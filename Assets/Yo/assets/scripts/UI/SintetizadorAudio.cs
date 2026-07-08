using UnityEngine;

/// <summary>
/// Generador procedural de AudioClips. Usado como fallback cuando no hay
/// archivos en Resources/Audio. Genera SFX cortos sintéticos y dos pistas
/// de música ambiental loopable.
///
/// Si en algún momento quieres reemplazar un sonido por uno real, basta con
/// dejar el .wav/.ogg en Resources/Audio/SFX|Music con el nombre indicado
/// en AudioManager — el AudioManager lo detectará y lo usará automáticamente.
/// </summary>
public static class SintetizadorAudio
{
    const int SR = 44100; // sample rate

    // ===================== HELPERS =====================

    private static AudioClip CrearClip(string nombre, float[] data)
    {
        var clip = AudioClip.Create(nombre, data.Length, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void MezclarSeno(float[] data, float freq, float amp, float t0, float duracion, float decay)
    {
        int inicio = Mathf.Max(0, (int)(t0 * SR));
        int fin    = Mathf.Min(data.Length, (int)((t0 + duracion) * SR));
        for (int i = inicio; i < fin; i++)
        {
            float t = (i - inicio) / (float)SR;
            float env = Mathf.Exp(-decay * t);
            data[i] += Mathf.Sin(2f * Mathf.PI * freq * t) * amp * env;
        }
    }

    private static void MezclarCuadrada(float[] data, float freq, float amp, float t0, float duracion, float decay)
    {
        int inicio = Mathf.Max(0, (int)(t0 * SR));
        int fin    = Mathf.Min(data.Length, (int)((t0 + duracion) * SR));
        for (int i = inicio; i < fin; i++)
        {
            float t = (i - inicio) / (float)SR;
            float env = Mathf.Exp(-decay * t);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t) >= 0 ? 1f : -1f;
            data[i] += s * amp * env;
        }
    }

    private static void MezclarRuido(float[] data, float amp, float t0, float duracion, float decay)
    {
        int inicio = Mathf.Max(0, (int)(t0 * SR));
        int fin    = Mathf.Min(data.Length, (int)((t0 + duracion) * SR));
        for (int i = inicio; i < fin; i++)
        {
            float t = (i - inicio) / (float)SR;
            float env = Mathf.Exp(-decay * t);
            data[i] += (Random.value * 2f - 1f) * amp * env;
        }
    }

    private static void Normalizar(float[] data, float maxAbs = 0.9f)
    {
        float pico = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float a = Mathf.Abs(data[i]);
            if (a > pico) pico = a;
        }
        if (pico > maxAbs)
        {
            float k = maxAbs / pico;
            for (int i = 0; i < data.Length; i++) data[i] *= k;
        }
    }

    // ===================== SFX =====================

    public static AudioClip Click()
    {
        float[] data = new float[(int)(0.06f * SR)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Exp(-40f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env * 0.4f;
        }
        return CrearClip("click", data);
    }

    public static AudioClip RespuestaCorrecta()
    {
        // C5 (523) → G5 (784), dos notas tipo "ding-ding" ascendente
        float[] data = new float[(int)(0.35f * SR)];
        MezclarSeno(data, 523.25f, 0.5f, 0.00f, 0.16f, 8f);
        MezclarSeno(data, 783.99f, 0.5f, 0.12f, 0.20f, 6f);
        Normalizar(data);
        return CrearClip("correcta", data);
    }

    public static AudioClip RespuestaIncorrecta()
    {
        // G3 (196) → C3 (130), dos notas descendentes con cuadrada
        float[] data = new float[(int)(0.35f * SR)];
        MezclarCuadrada(data, 196f, 0.25f, 0.00f, 0.16f, 8f);
        MezclarCuadrada(data, 130.81f, 0.25f, 0.12f, 0.20f, 6f);
        Normalizar(data);
        return CrearClip("incorrecta", data);
    }

    public static AudioClip Disparo()
    {
        // Sweep de frecuencia 1500Hz → 200Hz en 0.25s, tipo láser
        float dur = 0.25f;
        float[] data = new float[(int)(dur * SR)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)SR;
            float prog = t / dur;
            float freq = Mathf.Lerp(1500f, 200f, prog);
            float env = Mathf.Exp(-5f * t);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            // un toque de saw para brillo
            s += 0.3f * (2f * (freq * t - Mathf.Floor(freq * t + 0.5f)));
            data[i] = s * env * 0.4f;
        }
        Normalizar(data);
        return CrearClip("disparo", data);
    }

    public static AudioClip Impacto()
    {
        // Explosión: ruido blanco con envolvente larga + sub-bajo
        float[] data = new float[(int)(0.45f * SR)];
        MezclarRuido(data, 0.7f, 0f, 0.45f, 6f);
        MezclarSeno(data, 60f, 0.7f, 0f, 0.30f, 8f);
        Normalizar(data);
        return CrearClip("impacto", data);
    }

    public static AudioClip AlarmaAtaque()
    {
        // 2 beeps cuadrados a 700Hz separados
        float[] data = new float[(int)(0.55f * SR)];
        MezclarCuadrada(data, 700f, 0.25f, 0.00f, 0.15f, 3f);
        MezclarCuadrada(data, 700f, 0.25f, 0.25f, 0.15f, 3f);
        Normalizar(data);
        return CrearClip("alarma", data);
    }

    public static AudioClip EscudoRecarga()
    {
        // Sweep ascendente 200 → 1000Hz, sensación "powerup"
        float dur = 0.35f;
        float[] data = new float[(int)(dur * SR)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)SR;
            float freq = Mathf.Lerp(200f, 1000f, t / dur);
            float env = Mathf.Min(1f, t * 20f) * Mathf.Exp(-2f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
        }
        Normalizar(data);
        return CrearClip("escudo", data);
    }

    public static AudioClip EnergiaCargada()
    {
        // Acorde ascendente C5 + E5 + G5 + C6
        float[] data = new float[(int)(0.6f * SR)];
        MezclarSeno(data, 523.25f, 0.25f, 0.00f, 0.6f, 3f);
        MezclarSeno(data, 659.25f, 0.25f, 0.10f, 0.5f, 3f);
        MezclarSeno(data, 783.99f, 0.25f, 0.20f, 0.4f, 3f);
        MezclarSeno(data, 1046.50f,0.25f, 0.30f, 0.3f, 3f);
        Normalizar(data);
        return CrearClip("energia", data);
    }

    public static AudioClip Victoria()
    {
        // Arpegio C-E-G-C ascendente, fanfarria breve
        float[] data = new float[(int)(1.2f * SR)];
        MezclarSeno(data, 523.25f, 0.5f, 0.00f, 0.25f, 4f);
        MezclarSeno(data, 659.25f, 0.5f, 0.20f, 0.25f, 4f);
        MezclarSeno(data, 783.99f, 0.5f, 0.40f, 0.25f, 4f);
        MezclarSeno(data, 1046.50f,0.6f, 0.60f, 0.60f, 2f);
        // pad de armónicos
        MezclarSeno(data, 261.63f, 0.2f, 0.60f, 0.60f, 1.5f);
        Normalizar(data);
        return CrearClip("victoria", data);
    }

    public static AudioClip Derrota()
    {
        // Descendente C-Ab-Eb-C, sensación de caída
        float[] data = new float[(int)(1.2f * SR)];
        MezclarCuadrada(data, 523.25f, 0.3f, 0.00f, 0.25f, 3f);
        MezclarCuadrada(data, 415.30f, 0.3f, 0.20f, 0.25f, 3f);
        MezclarCuadrada(data, 311.13f, 0.3f, 0.40f, 0.30f, 3f);
        MezclarCuadrada(data, 130.81f, 0.4f, 0.60f, 0.60f, 1.5f);
        Normalizar(data);
        return CrearClip("derrota", data);
    }

    public static AudioClip ZonaDestruida()
    {
        // Impacto + sub bajo más grave y largo que el impacto normal
        float[] data = new float[(int)(0.8f * SR)];
        MezclarRuido(data, 0.7f, 0f, 0.6f, 4f);
        MezclarSeno(data, 50f, 0.9f, 0f, 0.55f, 4f);
        MezclarSeno(data, 100f, 0.4f, 0.05f, 0.30f, 6f);
        Normalizar(data);
        return CrearClip("zona_destruida", data);
    }

    public static AudioClip ToastInfo()
    {
        float[] data = new float[(int)(0.12f * SR)];
        MezclarSeno(data, 600f, 0.5f, 0f, 0.10f, 15f);
        Normalizar(data);
        return CrearClip("toast_info", data);
    }

    public static AudioClip ToastExito()
    {
        float[] data = new float[(int)(0.25f * SR)];
        MezclarSeno(data, 700f, 0.5f, 0.00f, 0.10f, 15f);
        MezclarSeno(data, 1000f, 0.5f, 0.08f, 0.12f, 12f);
        Normalizar(data);
        return CrearClip("toast_exito", data);
    }

    public static AudioClip ToastError()
    {
        float[] data = new float[(int)(0.25f * SR)];
        MezclarCuadrada(data, 300f, 0.3f, 0.00f, 0.10f, 12f);
        MezclarCuadrada(data, 200f, 0.3f, 0.08f, 0.12f, 10f);
        Normalizar(data);
        return CrearClip("toast_error", data);
    }

    public static AudioClip ToastAviso()
    {
        // Modulación tipo "warble"
        float dur = 0.18f;
        float[] data = new float[(int)(dur * SR)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)SR;
            float wob = 500f + 100f * Mathf.Sin(2f * Mathf.PI * 30f * t);
            float env = Mathf.Exp(-8f * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * wob * t) * env * 0.4f;
        }
        Normalizar(data);
        return CrearClip("toast_aviso", data);
    }

    public static AudioClip DialogoAbrir()
    {
        float[] data = new float[(int)(0.15f * SR)];
        MezclarSeno(data, 250f, 0.4f, 0f, 0.12f, 8f);
        MezclarSeno(data, 350f, 0.3f, 0.04f, 0.10f, 8f);
        Normalizar(data);
        return CrearClip("dialogo", data);
    }

    // ===================== MÚSICA =====================

    /// <summary>Música menú: pad ambient en Cm. ~30s, loopable.</summary>
    public static AudioClip MusicaMenu()
    {
        return GenerarPadAmbient(duracion: 30f, freqs: new float[] { 130.81f, 155.56f, 196f, 233.08f }, lfoHz: 0.18f, ampPad: 0.18f, conKick: false);
    }

    /// <summary>Música combate: pad Cm + kick pulsado a 100 BPM. ~30s, loopable.</summary>
    public static AudioClip MusicaCombate()
    {
        return GenerarPadAmbient(duracion: 30f, freqs: new float[] { 130.81f, 155.56f, 196f, 233.08f, 392f }, lfoHz: 0.4f, ampPad: 0.16f, conKick: true, kickBPM: 100f);
    }

    private static AudioClip GenerarPadAmbient(float duracion, float[] freqs, float lfoHz, float ampPad, bool conKick, float kickBPM = 100f)
    {
        int samples = (int)(duracion * SR);
        float[] data = new float[samples];

        // Pad: suma de senos con LFO de amplitud (sensación "breathing")
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SR;
            float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * lfoHz * t);
            float val = 0f;
            foreach (var f in freqs)
                val += Mathf.Sin(2f * Mathf.PI * f * t);
            data[i] = val * ampPad * lfo / freqs.Length;
        }

        // Kick periódico
        if (conKick)
        {
            float beatDur = 60f / kickBPM;
            int samplesBeat = (int)(beatDur * SR);
            int kickLen = (int)(0.12f * SR);
            int b = 0;
            while (b * samplesBeat < samples)
            {
                int start = b * samplesBeat;
                for (int i = 0; i < kickLen && start + i < samples; i++)
                {
                    float t = i / (float)SR;
                    float env = Mathf.Exp(-18f * t);
                    // pitch envelope 80 → 45 Hz
                    float freq = 45f + 35f * Mathf.Exp(-25f * t);
                    data[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
                }
                b++;
            }
        }

        Normalizar(data, 0.85f);
        var clip = AudioClip.Create(conKick ? "musica_combate_proc" : "musica_menu_proc", samples, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }
}
