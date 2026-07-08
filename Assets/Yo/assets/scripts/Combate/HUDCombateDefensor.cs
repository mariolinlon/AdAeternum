using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HUD exclusivo del defensor:
///   - Lista vertical de tarjetas de ataques entrantes (cada una con "Defender")
/// </summary>
public class HUDCombateDefensor : MonoBehaviour
{
    [SerializeField] private Transform contenedorAtaques;
    [SerializeField] private GameObject prefabTarjetaAtaque; // con TarjetaAtaqueEntrante

    private readonly Dictionary<string, TarjetaAtaqueEntrante> tarjetasPorId = new Dictionary<string, TarjetaAtaqueEntrante>();

    public Action<string> OnDefender; // callback con idAtaque

    public void Mostrar() { gameObject.SetActive(true); }
    public void Ocultar() { gameObject.SetActive(false); }

    public void RefrescarAtaques(List<AtaqueEntrante> ataques, ConfigCombatePlaneta cfg = null)
    {
        if (ataques == null) ataques = new List<AtaqueEntrante>();
        if (contenedorAtaques == null || prefabTarjetaAtaque == null) return;

        var idsActuales = new HashSet<string>();
        foreach (var a in ataques) if (a != null && !string.IsNullOrEmpty(a.id)) idsActuales.Add(a.id);

        // Borrar tarjetas que ya no existen
        var aBorrar = new List<string>();
        foreach (var kv in tarjetasPorId) if (!idsActuales.Contains(kv.Key)) aBorrar.Add(kv.Key);
        foreach (var id in aBorrar)
        {
            if (tarjetasPorId[id] != null) Destroy(tarjetasPorId[id].gameObject);
            tarjetasPorId.Remove(id);
        }

        // Crear/actualizar tarjetas
        foreach (var a in ataques)
        {
            if (a == null || string.IsNullOrEmpty(a.id)) continue;
            float daño = cfg != null
                ? (a.tipo == TipoAtaqueEntrante.Agravado ? cfg.dañoAtaqueAgravado : cfg.dañoAtaqueNormal)
                : 0f;

            if (!tarjetasPorId.ContainsKey(a.id))
            {
                GameObject go = Instantiate(prefabTarjetaAtaque, contenedorAtaques);
                TarjetaAtaqueEntrante t = go.GetComponent<TarjetaAtaqueEntrante>();
                if (t != null)
                {
                    tarjetasPorId[a.id] = t;
                    t.Configurar(a, daño, OnDefender);
                    // Alarma cuando aparece un nuevo ataque entrante
                    AudioManager.PlaySFX(AudioManager.SFX.AlarmaAtaque);
                }
            }
            else
            {
                tarjetasPorId[a.id].Configurar(a, daño, OnDefender);
            }
        }
    }
}
