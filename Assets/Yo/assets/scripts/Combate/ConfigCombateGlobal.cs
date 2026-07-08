using UnityEngine;

/// <summary>
/// Parámetros comunes a TODOS los combates Tipo 1 (Asalto planetario).
/// Editables desde el Inspector, no se guardan por planeta.
/// Va en el GameObject quizmanager (singleton vía Instance).
/// </summary>
public class ConfigCombateGlobal : MonoBehaviour
{
    public static ConfigCombateGlobal Instance { get; private set; }

    [Header("Ataques entrantes del planeta (NPC)")]
    public float cadenciaAtaquesEntrantes = 15f;
    public float dañoAtaqueNormal         = 10f;
    public float dañoAtaqueAgravado       = 25f;

    [Header("Atacante — energía")]
    public float energiaAtaqueMaxima      = 200f;
    public float energiaPorAcierto        = 25f;

    [Header("Atacante — disparo Normal")]
    public float costeAtaqueNormal        = 50f;
    public float dañoAtaqueAtacante       = 20f; // daño del ataque Normal

    [Header("Atacante — disparo Cargado")]
    public float costeAtaqueCargado       = 150f;
    public float dañoAtaqueAtacanteCargado = 60f;

    [Header("Defensor / Escudo")]
    public float escudoMaximo             = 100f;
    public float escudoMinimo             = 20f;
    public float tasaDescargaEscudo       = 1f;
    public float recargaEscudoPorAcierto  = 8f;

    [Header("Nave")]
    public float vidaNaveMaxima           = 100f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    /// <summary>Inyecta los valores globales en una ConfigCombatePlaneta (mezcla per-planet + global).</summary>
    public void AplicarA(ConfigCombatePlaneta cfg)
    {
        if (cfg == null) return;
        cfg.cadenciaAtaquesEntrantes = cadenciaAtaquesEntrantes;
        cfg.dañoAtaqueNormal         = dañoAtaqueNormal;
        cfg.dañoAtaqueAgravado       = dañoAtaqueAgravado;
        cfg.energiaAtaqueMaxima      = energiaAtaqueMaxima;
        cfg.energiaPorAcierto        = energiaPorAcierto;
        cfg.costeAtaqueNormal        = costeAtaqueNormal;
        cfg.dañoAtaqueAtacante       = dañoAtaqueAtacante;
        cfg.costeAtaqueCargado       = costeAtaqueCargado;
        cfg.dañoAtaqueAtacanteCargado = dañoAtaqueAtacanteCargado;
        cfg.escudoMaximo             = escudoMaximo;
        cfg.escudoMinimo             = escudoMinimo;
        cfg.tasaDescargaEscudo       = tasaDescargaEscudo;
        cfg.recargaEscudoPorAcierto  = recargaEscudoPorAcierto;
        cfg.vidaNaveMaxima           = vidaNaveMaxima;
    }
}
