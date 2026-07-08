using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlCombateProfesor : MonoBehaviour
{
    [Header("UI")]
    public Button botonIniciarCombate;
    public Button botonDetenerCombate;
    public TextMeshProUGUI textoEstado;
    public TextMeshProUGUI textoPlaneta;

    [Header("Combate Tipo 1")]
    public CombateAsaltoManager combateAsaltoManager;

    [Header("Validación")]
    public CreadorPreguntas creadorPreguntas;

    private void OnEnable()
    {
        ActualizarPlaneta();
    }

    public void IniciarEscucha()
    {
        // El profesor solo dispara el combate y muestra estado en UI. El tick autoritativo
        // lo ejecuta el líder de cada flota desde su propio cliente (CombateAsaltoManager).
        AulaDataManager.Instance?.EscucharEstadoCombate((estado, idPlaneta, idSesion) =>
        {
            bool enCombate = estado == "enCombate";
            if (textoEstado != null)
                textoEstado.text = enCombate ? "Combate: Abierto" : "Combate: Cerrado";
            if (botonIniciarCombate != null) botonIniciarCombate.interactable = !enCombate;
            if (botonDetenerCombate != null) botonDetenerCombate.interactable = enCombate;
        });
    }

    private void Update()
    {
        ActualizarPlaneta();
    }

    private void ActualizarPlaneta()
    {
        if (textoPlaneta == null) return;
        PlanetSelectable planeta = PlanetSelectionManager.Instance?.ObtenerPlanetaActual();
        textoPlaneta.text = planeta != null
            ? $"Planeta: {planeta.NombrePlaneta}"
            : "Sin planeta seleccionado";
    }

    public void ClickIniciarCombate()
    {
        PlanetSelectable planeta = PlanetSelectionManager.Instance?.ObtenerPlanetaActual();

        if (planeta == null)
            planeta = FindFirstObjectByType<PlanetSelectable>();

        if (planeta == null)
        {
            if (textoEstado != null) textoEstado.text = "No hay planetas disponibles.";
            return;
        }

        // Validación previa: el planeta debe tener al menos una pregunta
        int numPreguntas = 0;
        if (creadorPreguntas != null && creadorPreguntas.bibliotecaLocal != null)
        {
            foreach (var p in creadorPreguntas.bibliotecaLocal)
                if (p.idPlaneta == planeta.IdUnico) numPreguntas++;
        }
        if (numPreguntas == 0)
        {
            if (textoEstado != null) textoEstado.text = "El planeta '" + planeta.NombrePlaneta + "' no tiene preguntas.";
            Debug.LogWarning("[ControlCombateProfesor] Bloqueado inicio: planeta sin preguntas.");
            return;
        }

        AulaDataManager.Instance.IniciarCombateEnAula(planeta.IdUnico);
        // El listener de IniciarEscucha detectará el cambio de estado y arrancará el manager Tipo 1
    }

    public void ClickDetenerCombate()
    {
        AulaDataManager.Instance.DetenerCombateEnAula();
        // El listener de IniciarEscucha detectará el cambio de estado y parará el manager
    }
}
