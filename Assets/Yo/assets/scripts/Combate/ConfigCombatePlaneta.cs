using System;
using System.Collections.Generic;

/// <summary>
/// Configuración de combate de un planeta. Se guarda junto al planeta en Firestore.
/// Estos son los valores que el profesor edita en el editor de combate del planeta.
/// El estado runtime (vida actual de zonas, escudo, etc.) vive en EstadoFlotaCombate.
/// </summary>
[Serializable]
public class ConfigCombatePlaneta
{
    // Tipo de mecánica
    public TipoCombate tipo = TipoCombate.AsaltoPlanetario;

    // Zonas del planeta (configuración: nombre + vida máxima)
    public List<ZonaPlaneta> zonas = new List<ZonaPlaneta>();

    // Ataques entrantes del planeta (NPC)
    public float cadenciaAtaquesEntrantes = 15f; // segundos entre ataques
    public float dañoAtaqueNormal         = 10f;
    public float dañoAtaqueAgravado       = 25f;

    // Daño que hace el atacante a una zona al disparar (modo Normal)
    public float dañoAtaqueAtacante       = 20f;
    // Daño del modo Cargado
    public float dañoAtaqueAtacanteCargado = 60f;

    // Escudo de la flota
    public float escudoMaximo             = 100f;
    public float escudoMinimo             = 20f;   // umbral por debajo del cual los atacantes no pueden actuar
    public float tasaDescargaEscudo       = 1f;    // unidades de escudo perdidas por segundo
    public float recargaEscudoPorAcierto  = 8f;    // unidades que sube el escudo por cada acierto del defensor

    // Vida de la nave
    public float vidaNaveMaxima           = 100f;

    // Energía de ataque del atacante
    public float energiaAtaqueMaxima      = 200f;
    public float energiaPorAcierto        = 25f;
    public float costeAtaqueNormal        = 50f;
    public float costeAtaqueCargado       = 150f;

    /// <summary>Serializa SOLO tipo y zonas. El resto son globales (ConfigCombateGlobal).</summary>
    public Dictionary<string, object> ToDict()
    {
        var dictZonas = new List<object>();
        foreach (var z in zonas) dictZonas.Add(z.ToDict());

        return new Dictionary<string, object>
        {
            { "tipo",  (int)tipo },
            { "zonas", dictZonas }
        };
    }

    /// <summary>Deserializa tipo+zonas y rellena el resto con ConfigCombateGlobal.Instance.</summary>
    public static ConfigCombatePlaneta FromDict(Dictionary<string, object> d)
    {
        ConfigCombatePlaneta c = new ConfigCombatePlaneta();
        if (d == null) return ConfigDefault();

        c.tipo = d.ContainsKey("tipo") ? (TipoCombate)Convert.ToInt32(d["tipo"]) : TipoCombate.AsaltoPlanetario;

        c.zonas = new List<ZonaPlaneta>();
        if (d.ContainsKey("zonas") && d["zonas"] is List<object> rawList)
        {
            foreach (var item in rawList)
            {
                if (item is Dictionary<string, object> z)
                    c.zonas.Add(ZonaPlaneta.FromDict(z));
            }
        }

        // Si no hay zonas, garantizamos al menos una para no romper el combate
        if (c.zonas.Count == 0)
            c.zonas.Add(new ZonaPlaneta("Núcleo", 100f));

        // Aplicar globales si el manager existe
        ConfigCombateGlobal.Instance?.AplicarA(c);

        return c;
    }

    /// <summary>Config por defecto. Aplica globales si están disponibles.</summary>
    public static ConfigCombatePlaneta ConfigDefault()
    {
        ConfigCombatePlaneta c = new ConfigCombatePlaneta();
        c.zonas.Add(new ZonaPlaneta("Núcleo", 100f));
        ConfigCombateGlobal.Instance?.AplicarA(c);
        return c;
    }
}
