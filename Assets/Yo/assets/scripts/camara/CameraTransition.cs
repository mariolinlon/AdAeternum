using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CameraViewTransition : MonoBehaviour
{
    public static CameraViewTransition Instance;

    [Header("Puntos de vista")]
    [SerializeField] private List<Transform> puntosDeVista = new List<Transform>();

    [Header("Configuración de transición")]
    [SerializeField] private float duracion = 1.5f;
    [SerializeField] private AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Estado inicial")]
    [SerializeField] private int puntoInicial = 0;
    [SerializeField] private bool colocarEnPuntoInicialAlEmpezar = true;

    [Header("Movimiento lateral mantenido")]
    [SerializeField] private float velocidadMovimientoLateral = 10f;

    private Coroutine transicionActual;
    private int indiceActual = -1;

    private bool moverIzquierda;
    private bool moverDerecha;

    private float limiteIzquierda = float.MinValue;
    private float limiteDerecha   = float.MaxValue;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (colocarEnPuntoInicialAlEmpezar)
            IrInstantaneamenteAPunto(puntoInicial);
    }

    private void Update()
    {
        if (moverDerecha && transform.position.x < limiteDerecha)
            transform.position += Vector3.right * velocidadMovimientoLateral * Time.deltaTime;

        if (moverIzquierda && transform.position.x > limiteIzquierda)
            transform.position += Vector3.left * velocidadMovimientoLateral * Time.deltaTime;
    }

    public void RecalcularLimites()
    {
        var planetas = FindObjectsByType<PlanetSelectable>(FindObjectsSortMode.None);
        if (planetas.Length == 0) return;
        limiteIzquierda = planetas.Min(p => p.transform.position.x);
        limiteDerecha   = planetas.Max(p => p.transform.position.x);
    }

    public void IrAPunto(int indice)
    {
        if (!IndiceValido(indice))
        {
            Debug.LogWarning($"CameraViewTransition: el índice {indice} no es válido.");
            return;
        }

        if (transicionActual != null)
        {
            StopCoroutine(transicionActual);
        }
        
        transicionActual = StartCoroutine(TransicionarAPunto(puntosDeVista[indice], indice));
    }

    public void IrInstantaneamenteAPunto(int indice)
    {
        if (!IndiceValido(indice))
        {
            Debug.LogWarning($"CameraViewTransition: el índice {indice} no es válido.");
            return;
        }

        Transform destino = puntosDeVista[indice];

        transform.position = destino.position;
        transform.rotation = destino.rotation;

        indiceActual = indice;
    }

    public void EmpezarMoverDerecha()
    {
        moverDerecha = true;
    }

    public void PararMoverDerecha()
    {
        moverDerecha = false;
    }

    public void EmpezarMoverIzquierda()
    {
        moverIzquierda = true;
    }

    public void PararMoverIzquierda()
    {
        moverIzquierda = false;
    }

    public void PararTodoMovimientoLateral()
    {
        moverDerecha = false;
        moverIzquierda = false;
    }

    public void CopiarTransformEnPunto(int indice, Transform referencia)
    {
        if (!IndiceValido(indice))
        {
            Debug.LogWarning($"CameraViewTransition: el índice {indice} no es válido.");
            return;
        }

        if (referencia == null)
        {
            Debug.LogWarning("CameraViewTransition: la referencia es null.");
            return;
        }

        puntosDeVista[indice].position = referencia.position;
        puntosDeVista[indice].rotation = referencia.rotation;
    }

    public int ObtenerIndiceActual()
    {
        return indiceActual;
    }

    private IEnumerator TransicionarAPunto(Transform destino, int indiceDestino)
    {
        Vector3 posicionInicial = transform.position;
        Quaternion rotacionInicial = transform.rotation;

        Vector3 posicionFinal = destino.position;
        Quaternion rotacionFinal = destino.rotation;

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(tiempo / duracion);
            float tCurva = curva.Evaluate(t);

            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, tCurva);
            transform.rotation = Quaternion.Slerp(rotacionInicial, rotacionFinal, tCurva);

            yield return null;
        }

        transform.position = posicionFinal;
        transform.rotation = rotacionFinal;

        indiceActual = indiceDestino;
        transicionActual = null;
    }

    private bool IndiceValido(int indice)
    {
        return indice >= 0 && indice < puntosDeVista.Count && puntosDeVista[indice] != null;
    }
}