using System;
using System.Collections.Generic;

/// <summary>
/// Estado de una flota concreta durante un combate Tipo 1.
/// Vive en Firestore en: Aulas/{cod}/combateActivo/{idSesion}/flotas/{idFlota}
/// El profesor (autoritativo) lo escribe; los alumnos de esa flota lo leen via listener.
/// </summary>
[Serializable]
public class EstadoFlotaCombate
{
    public string idFlota;
    public string idPlaneta;
    public string idSesion;

    public float vidaNave;
    public float vidaNaveMaxima;

    public float escudoActual;
    public float escudoMaximo;
    public float escudoMinimo;

    public List<ZonaPlaneta> zonas = new List<ZonaPlaneta>();

    public List<AtaqueEntrante> ataquesEntrantes = new List<AtaqueEntrante>();

    /// <summary>Energía de ataque acumulada por cada atacante de la flota.</summary>
    public Dictionary<string, float> energiaAtaquePorAlumno = new Dictionary<string, float>();

    /// <summary>"activo" | "completado" | "eliminado".</summary>
    public string estado = "activo";

    public float cadenciaActual;
    public long timestampInicioMs;

    // ── Failover del manager autoritativo ─────────────────────────────────────
    // El "líder oficial" de la flota está en Flota.liderID (asignado por el
    // profesor). Pero si ese alumno se desconecta a mitad de combate, su flota
    // se quedaría sin tick. Para evitarlo, cualquier miembro puede ASUMIR el
    // rol de manager si detecta que el actual lleva varios segundos sin
    // heartbeat. El epoch garantiza que un ex-líder reconectándose no siga
    // escribiendo y pisando al nuevo líder.
    public string liderActivoId = "";     // quién actúa como manager ahora
    public long   liderHeartbeatMs = 0L;   // timestamp Unix ms del último latido
    public int    liderEpoch = 0;          // se incrementa con cada reclamación

    public bool EscudoCaido => escudoActual < escudoMinimo;

    public bool TodasLasZonasDestruidas
    {
        get
        {
            if (zonas == null || zonas.Count == 0) return false;
            foreach (var z in zonas) if (!z.EstaDestruida) return false;
            return true;
        }
    }

    public bool NaveDestruida => vidaNave <= 0f;

    public Dictionary<string, object> ToDict()
    {
        var listaZonas = new List<object>();
        foreach (var z in zonas) listaZonas.Add(z.ToDict());

        var listaAtaques = new List<object>();
        foreach (var a in ataquesEntrantes) listaAtaques.Add(a.ToDict());

        var energias = new Dictionary<string, object>();
        foreach (var kv in energiaAtaquePorAlumno) energias[kv.Key] = kv.Value;

        return new Dictionary<string, object>
        {
            { "idFlota",                idFlota ?? "" },
            { "idPlaneta",              idPlaneta ?? "" },
            { "idSesion",               idSesion ?? "" },
            { "vidaNave",               vidaNave },
            { "vidaNaveMaxima",         vidaNaveMaxima },
            { "escudoActual",           escudoActual },
            { "escudoMaximo",           escudoMaximo },
            { "escudoMinimo",           escudoMinimo },
            { "zonas",                  listaZonas },
            { "ataquesEntrantes",       listaAtaques },
            { "energiaAtaquePorAlumno", energias },
            { "estado",                 estado ?? "activo" },
            { "cadenciaActual",         cadenciaActual },
            { "timestampInicioMs",      timestampInicioMs },
            { "liderActivoId",          liderActivoId ?? "" },
            { "liderHeartbeatMs",       liderHeartbeatMs },
            { "liderEpoch",             liderEpoch }
        };
    }

    public static EstadoFlotaCombate FromDict(Dictionary<string, object> d)
    {
        if (d == null) return null;
        EstadoFlotaCombate e = new EstadoFlotaCombate();

        e.idFlota         = d.ContainsKey("idFlota")          ? d["idFlota"].ToString()                : "";
        e.idPlaneta       = d.ContainsKey("idPlaneta")        ? d["idPlaneta"].ToString()              : "";
        e.idSesion        = d.ContainsKey("idSesion")         ? d["idSesion"].ToString()               : "";
        e.vidaNave        = d.ContainsKey("vidaNave")         ? Convert.ToSingle(d["vidaNave"])        : 100f;
        e.vidaNaveMaxima  = d.ContainsKey("vidaNaveMaxima")   ? Convert.ToSingle(d["vidaNaveMaxima"])  : 100f;
        e.escudoActual    = d.ContainsKey("escudoActual")     ? Convert.ToSingle(d["escudoActual"])    : 100f;
        e.escudoMaximo    = d.ContainsKey("escudoMaximo")     ? Convert.ToSingle(d["escudoMaximo"])    : 100f;
        e.escudoMinimo    = d.ContainsKey("escudoMinimo")     ? Convert.ToSingle(d["escudoMinimo"])    : 20f;
        e.estado          = d.ContainsKey("estado")           ? d["estado"].ToString()                 : "activo";
        e.cadenciaActual  = d.ContainsKey("cadenciaActual")   ? Convert.ToSingle(d["cadenciaActual"])  : 15f;
        e.timestampInicioMs = d.ContainsKey("timestampInicioMs") ? Convert.ToInt64(d["timestampInicioMs"]) : 0L;
        e.liderActivoId    = d.ContainsKey("liderActivoId")    ? d["liderActivoId"].ToString()           : "";
        e.liderHeartbeatMs = d.ContainsKey("liderHeartbeatMs") ? Convert.ToInt64(d["liderHeartbeatMs"])  : 0L;
        e.liderEpoch       = d.ContainsKey("liderEpoch")       ? Convert.ToInt32(d["liderEpoch"])        : 0;

        e.zonas = new List<ZonaPlaneta>();
        if (d.ContainsKey("zonas") && d["zonas"] is List<object> zList)
        {
            foreach (var item in zList)
                if (item is Dictionary<string, object> zd) e.zonas.Add(ZonaPlaneta.FromDict(zd));
        }

        e.ataquesEntrantes = new List<AtaqueEntrante>();
        if (d.ContainsKey("ataquesEntrantes") && d["ataquesEntrantes"] is List<object> aList)
        {
            foreach (var item in aList)
                if (item is Dictionary<string, object> ad) e.ataquesEntrantes.Add(AtaqueEntrante.FromDict(ad));
        }

        e.energiaAtaquePorAlumno = new Dictionary<string, float>();
        if (d.ContainsKey("energiaAtaquePorAlumno") && d["energiaAtaquePorAlumno"] is Dictionary<string, object> em)
        {
            foreach (var kv in em)
                e.energiaAtaquePorAlumno[kv.Key] = Convert.ToSingle(kv.Value);
        }

        return e;
    }

    /// <summary>Construye el estado inicial de una flota a partir de la config del planeta.</summary>
    public static EstadoFlotaCombate Crear(string idSesion, string idFlota, string idPlaneta, ConfigCombatePlaneta cfg)
    {
        EstadoFlotaCombate e = new EstadoFlotaCombate();
        e.idFlota         = idFlota;
        e.idPlaneta       = idPlaneta;
        e.idSesion        = idSesion;
        e.vidaNaveMaxima  = cfg.vidaNaveMaxima;
        e.vidaNave        = cfg.vidaNaveMaxima;
        e.escudoMaximo    = cfg.escudoMaximo;
        e.escudoMinimo    = cfg.escudoMinimo;
        e.escudoActual    = cfg.escudoMaximo;
        e.cadenciaActual  = cfg.cadenciaAtaquesEntrantes;
        e.estado          = "activo";
        e.timestampInicioMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Clonar zonas de la config (cada flota tiene su instancia con su vida actual)
        e.zonas = new List<ZonaPlaneta>();
        foreach (var zCfg in cfg.zonas)
            e.zonas.Add(new ZonaPlaneta(zCfg.nombre, zCfg.vidaMaxima));

        // Ataque inicial para feedback inmediato al defensor.
        // El líder que reclama por primera vez creará este estado en Firebase
        // dentro de la transacción de reclamación. Si soy un líder de failover,
        // el doc ya existe con su propia lista de ataques actual.
        e.ataquesEntrantes.Add(new AtaqueEntrante(
            Guid.NewGuid().ToString(),
            TipoAtaqueEntrante.Normal,
            12f
        ));

        return e;
    }
}
