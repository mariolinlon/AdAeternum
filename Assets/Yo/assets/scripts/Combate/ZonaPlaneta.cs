using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una zona del planeta a defender/atacar en el combate Tipo 1 (Asalto planetario).
/// Lleva nombre, vida máxima (config) y vida actual (estado runtime).
/// </summary>
[Serializable]
public class ZonaPlaneta
{
    public string nombre;
    public float vidaMaxima;
    public float vidaActual;

    public ZonaPlaneta() { }

    public ZonaPlaneta(string nombre, float vidaMaxima)
    {
        this.nombre = nombre;
        this.vidaMaxima = vidaMaxima;
        this.vidaActual = vidaMaxima;
    }

    public bool EstaDestruida => vidaActual <= 0f;

    public float Porcentaje => vidaMaxima > 0f ? Mathf.Clamp01(vidaActual / vidaMaxima) : 0f;

    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>
        {
            { "nombre",     nombre ?? "" },
            { "vidaMaxima", vidaMaxima },
            { "vidaActual", vidaActual }
        };
    }

    public static ZonaPlaneta FromDict(Dictionary<string, object> d)
    {
        if (d == null) return new ZonaPlaneta("Núcleo", 100f);
        ZonaPlaneta z = new ZonaPlaneta();
        z.nombre     = d.ContainsKey("nombre")     ? d["nombre"].ToString()                 : "Zona";
        z.vidaMaxima = d.ContainsKey("vidaMaxima") ? Convert.ToSingle(d["vidaMaxima"])      : 100f;
        z.vidaActual = d.ContainsKey("vidaActual") ? Convert.ToSingle(d["vidaActual"])      : z.vidaMaxima;
        return z;
    }
}
