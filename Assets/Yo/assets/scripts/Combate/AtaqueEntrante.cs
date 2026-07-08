using System;
using System.Collections.Generic;

/// <summary>
/// Tipo de ataque entrante automático del planeta.
/// </summary>
public enum TipoAtaqueEntrante
{
    Normal   = 0,
    Agravado = 1
}

/// <summary>
/// Ataque entrante (NPC) lanzado por el planeta sobre la flota.
/// Aparece en los avisos de los defensores. Uno lo asume en exclusiva.
/// Si no se resuelve en tiempo o se falla la pregunta, hace daño.
/// </summary>
[Serializable]
public class AtaqueEntrante
{
    public string id;
    public TipoAtaqueEntrante tipo;
    public float tiempoLimite;
    public float tiempoRestante;
    public string idAsumidoPor; // idAlumno que lo está defendiendo, vacío si nadie

    public AtaqueEntrante() { }

    public AtaqueEntrante(string id, TipoAtaqueEntrante tipo, float tiempoLimite)
    {
        this.id = id;
        this.tipo = tipo;
        this.tiempoLimite = tiempoLimite;
        this.tiempoRestante = tiempoLimite;
        this.idAsumidoPor = "";
    }

    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>
        {
            { "id",             id ?? "" },
            { "tipo",           (int)tipo },
            { "tiempoLimite",   tiempoLimite },
            { "tiempoRestante", tiempoRestante },
            { "idAsumidoPor",   idAsumidoPor ?? "" }
        };
    }

    public static AtaqueEntrante FromDict(Dictionary<string, object> d)
    {
        if (d == null) return null;
        AtaqueEntrante a = new AtaqueEntrante();
        a.id              = d.ContainsKey("id")             ? d["id"].ToString()                         : "";
        a.tipo            = d.ContainsKey("tipo")           ? (TipoAtaqueEntrante)Convert.ToInt32(d["tipo"]) : TipoAtaqueEntrante.Normal;
        a.tiempoLimite    = d.ContainsKey("tiempoLimite")   ? Convert.ToSingle(d["tiempoLimite"])        : 10f;
        a.tiempoRestante  = d.ContainsKey("tiempoRestante") ? Convert.ToSingle(d["tiempoRestante"])      : a.tiempoLimite;
        a.idAsumidoPor    = d.ContainsKey("idAsumidoPor")   ? d["idAsumidoPor"].ToString()               : "";
        return a;
    }
}
