using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CreadorPreguntas : MonoBehaviour
{
    [Header("Configuración de UI")]
    public TMP_InputField inputEnunciado;
    public TMP_InputField[] camposOpciones;
    public TMP_Dropdown dropdownCorrecta;
    public TMP_InputField inputTiempoLimite;
    public TextMeshProUGUI textoContador;
    public TextMeshProUGUI textoPlanetaActual;

    [Header("Base de Datos Temporal")]
    public List<Pregunta> bibliotecaLocal = new List<Pregunta>();

    private void Update()
    {
        if (PlanetSelectionManager.Instance != null && PlanetSelectionManager.Instance.ObtenerPlanetaActual() != null)
        {
            if (textoPlanetaActual != null)
                textoPlanetaActual.text = "Preguntas para: " + PlanetSelectionManager.Instance.ObtenerPlanetaActual().NombrePlaneta;
        }
    }

    public void GuardarPreguntaEnPlaneta()
    {
        PlanetSelectable planetaActual = PlanetSelectionManager.Instance.ObtenerPlanetaActual();
        
        if (planetaActual == null)
        {
            Toast.Show("Debes seleccionar un planeta primero.", 3f, Toast.Tipo.Aviso);
            return;
        }

        if (string.IsNullOrEmpty(inputEnunciado.text))
        {
            Toast.Show("El enunciado de la pregunta no puede estar vacío.", 3f, Toast.Tipo.Aviso);
            return;
        }

        string nuevoIdPregunta = System.Guid.NewGuid().ToString();
        string[] ops = new string[camposOpciones.Length];
        for (int i = 0; i < camposOpciones.Length; i++)
        {
            ops[i] = camposOpciones[i].text;
        }

        // GUARDAMOS EL ID DEL PLANETA, NO EL NOMBRE
        float tiempo = 30f;
        if (inputTiempoLimite != null && float.TryParse(inputTiempoLimite.text, out float t) && t > 0)
            tiempo = t;

        Pregunta nuevaP = new Pregunta(
            nuevoIdPregunta,
            planetaActual.IdUnico,
            inputEnunciado.text,
            ops,
            dropdownCorrecta.value,
            tiempo
        );

        bibliotecaLocal.Add(nuevaP);
        AulaDataManager.Instance.GuardarPreguntaEnFirebase(nuevaP);
        ActualizarContador();
        LimpiarFormulario();
    }

    void LimpiarFormulario()
    {
        inputEnunciado.text = "";
        foreach (var campo in camposOpciones) campo.text = "";
        dropdownCorrecta.value = 0;
        if (inputTiempoLimite != null) inputTiempoLimite.text = "30";
    }

    void ActualizarContador()
    {
        if (textoContador != null)
            textoContador.text = "Preguntas totales: " + bibliotecaLocal.Count;
    }
}