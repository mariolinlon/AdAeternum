using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD exclusivo del atacante:
///  - Barra de energía de ataque
///  - Selector de zona objetivo
///  - Botón "Disparar" (activo solo si energía llena + zona seleccionada y viva)
///  - Overlay "ESCUDO CAÍDO" cuando el escudo de la flota está por debajo del mínimo
/// </summary>
public class HUDCombateAtacante : MonoBehaviour
{
    [Header("Energía")]
    [SerializeField] private Slider sliderEnergia;
    [SerializeField] private TextMeshProUGUI textoEnergia;

    [Header("Selector de zona")]
    [SerializeField] private Transform contenedorBotonesZona;
    [SerializeField] private GameObject prefabBotonZona; // con BotonZonaAtaque

    [Header("Disparo Normal")]
    [SerializeField] private Button botonDispararNormal;
    [SerializeField] private TextMeshProUGUI textoDispararNormal;

    [Header("Disparo Cargado")]
    [SerializeField] private Button botonDispararCargado;
    [SerializeField] private TextMeshProUGUI textoDispararCargado;

    [Header("Overlay escudo caído")]
    [SerializeField] private GameObject overlayEscudoCaido;

    private readonly List<BotonZonaAtaque> botonesZona = new List<BotonZonaAtaque>();
    private int indiceZonaSeleccionada = -1;
    private float energiaActual;
    private float energiaMaxima;
    private float costeNormal;
    private float costeCargado;
    private List<ZonaPlaneta> ultimasZonas;

    /// <summary>
    /// Callback con (índice de zona, esCargado). esCargado=false → ataque normal, true → cargado.
    /// </summary>
    public Action<int, bool> OnDisparar;

    public void Mostrar() { gameObject.SetActive(true); }
    public void Ocultar() { gameObject.SetActive(false); }

    private void Awake()
    {
        if (botonDispararNormal != null)
        {
            botonDispararNormal.onClick.RemoveAllListeners();
            botonDispararNormal.onClick.AddListener(() =>
            {
                if (indiceZonaSeleccionada < 0) return;
                OnDisparar?.Invoke(indiceZonaSeleccionada, false);
            });
        }
        if (botonDispararCargado != null)
        {
            botonDispararCargado.onClick.RemoveAllListeners();
            botonDispararCargado.onClick.AddListener(() =>
            {
                if (indiceZonaSeleccionada < 0) return;
                OnDisparar?.Invoke(indiceZonaSeleccionada, true);
            });
        }
    }

    public void RefrescarEstado(EstadoFlotaCombate estado, string idAlumno, ConfigCombatePlaneta cfg)
    {
        if (estado == null || cfg == null) return;
        energiaMaxima = cfg.energiaAtaqueMaxima;
        costeNormal = cfg.costeAtaqueNormal;
        costeCargado = cfg.costeAtaqueCargado;

        // Energía
        float e = 0f;
        if (estado.energiaAtaquePorAlumno != null && estado.energiaAtaquePorAlumno.TryGetValue(idAlumno, out float val))
            e = val;
        energiaActual = e;

        if (sliderEnergia != null)
        {
            sliderEnergia.maxValue = Mathf.Max(1f, energiaMaxima);
            sliderEnergia.value = Mathf.Clamp(e, 0f, sliderEnergia.maxValue);
        }
        if (textoEnergia != null) textoEnergia.text = $"Energía {Mathf.RoundToInt(e)}/{Mathf.RoundToInt(energiaMaxima)}";

        // Sincronizar botones zona
        ultimasZonas = estado.zonas ?? new List<ZonaPlaneta>();
        SincronizarBotonesZona();

        // Overlay escudo caído
        if (overlayEscudoCaido != null) overlayEscudoCaido.SetActive(estado.EscudoCaido);

        // Actualizar estado de los botones de disparo
        ActualizarBotonesDisparar();
    }

    private void SincronizarBotonesZona()
    {
        if (contenedorBotonesZona == null || prefabBotonZona == null) return;
        if (ultimasZonas == null) ultimasZonas = new List<ZonaPlaneta>();

        while (botonesZona.Count < ultimasZonas.Count)
        {
            GameObject go = Instantiate(prefabBotonZona, contenedorBotonesZona);
            BotonZonaAtaque b = go.GetComponent<BotonZonaAtaque>();
            if (b != null) botonesZona.Add(b);
        }
        while (botonesZona.Count > ultimasZonas.Count)
        {
            int last = botonesZona.Count - 1;
            if (botonesZona[last] != null) Destroy(botonesZona[last].gameObject);
            botonesZona.RemoveAt(last);
        }

        // Si la zona seleccionada se destruyó o ya no existe, deseleccionar
        if (indiceZonaSeleccionada >= 0 &&
            (indiceZonaSeleccionada >= ultimasZonas.Count || ultimasZonas[indiceZonaSeleccionada].EstaDestruida))
        {
            indiceZonaSeleccionada = -1;
        }

        for (int i = 0; i < ultimasZonas.Count; i++)
        {
            var z = ultimasZonas[i];
            botonesZona[i].Configurar(i, z.nombre, z.vidaActual, z.vidaMaxima,
                seleccionado: (i == indiceZonaSeleccionada),
                onSeleccionar: OnClickBotonZona);
        }
    }

    private void OnClickBotonZona(int indice)
    {
        indiceZonaSeleccionada = indice;
        SincronizarBotonesZona(); // refresca resaltado
        ActualizarBotonesDisparar();
    }

    private void ActualizarBotonesDisparar()
    {
        bool zonaValida = indiceZonaSeleccionada >= 0 && ultimasZonas != null
            && indiceZonaSeleccionada < ultimasZonas.Count && !ultimasZonas[indiceZonaSeleccionada].EstaDestruida;
        string nombreZona = zonaValida ? ultimasZonas[indiceZonaSeleccionada].nombre : "";

        // Botón Normal
        if (botonDispararNormal != null)
        {
            bool puedeNormal = energiaActual >= costeNormal - 0.01f && zonaValida;
            botonDispararNormal.interactable = puedeNormal;
            if (textoDispararNormal != null)
            {
                if (!zonaValida) textoDispararNormal.text = $"Normal  ({Mathf.RoundToInt(costeNormal)} ⚡)";
                else if (!puedeNormal) textoDispararNormal.text = $"Normal  ({Mathf.RoundToInt(costeNormal)} ⚡)";
                else textoDispararNormal.text = $"NORMAL → {nombreZona}  ({Mathf.RoundToInt(costeNormal)} ⚡)";
            }
        }

        // Botón Cargado
        if (botonDispararCargado != null)
        {
            bool puedeCargado = energiaActual >= costeCargado - 0.01f && zonaValida;
            botonDispararCargado.interactable = puedeCargado;
            if (textoDispararCargado != null)
            {
                if (!zonaValida) textoDispararCargado.text = $"Cargado  ({Mathf.RoundToInt(costeCargado)} ⚡)";
                else if (!puedeCargado) textoDispararCargado.text = $"Cargado  ({Mathf.RoundToInt(costeCargado)} ⚡)";
                else textoDispararCargado.text = $"CARGADO → {nombreZona}  ({Mathf.RoundToInt(costeCargado)} ⚡)";
            }
        }
    }
}
