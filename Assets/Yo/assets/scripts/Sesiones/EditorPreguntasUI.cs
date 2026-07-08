using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EditorPreguntasUI : MonoBehaviour
{
    [Header("Referencias externas")]
    [SerializeField] private CreadorPreguntas creadorPreguntas;

    [Header("Lista de preguntas")]
    [SerializeField] private Transform contenedorLista;
    [SerializeField] private GameObject prefabItemPregunta;

    [Header("Formulario de edición")]
    [SerializeField] private GameObject panelFormulario;
    [SerializeField] private TMP_InputField inputEnunciado;
    [SerializeField] private TMP_InputField[] inputOpciones;
    [SerializeField] private TMP_Dropdown dropdownCorrecta;
    [SerializeField] private TMP_InputField inputTiempoLimite;
    [SerializeField] private TMP_InputField inputPuntos;

    [Header("Botones")]
    [SerializeField] private Button botonGuardar;
    [SerializeField] private Button botonNueva;
    [SerializeField] private Button botonEliminar;

    [Header("Estado")]
    [SerializeField] private TextMeshProUGUI textoEstado;

    private string idPreguntaEditando = null;

    private void OnEnable()
    {
        RefrescarLista();
        MostrarFormulario(false);
    }

    public void RefrescarLista()
    {
        if (contenedorLista == null || prefabItemPregunta == null) return;

        foreach (Transform hijo in contenedorLista)
            Destroy(hijo.gameObject);

        PlanetSelectable planeta = PlanetSelectionManager.Instance?.ObtenerPlanetaActual();
        if (planeta == null)
        {
            SetEstado("Selecciona un planeta para ver sus preguntas.");
            return;
        }

        var preguntas = creadorPreguntas.bibliotecaLocal
            .Where(p => p.idPlaneta == planeta.IdUnico).ToList();

        SetEstado(preguntas.Count == 0
            ? $"No hay preguntas para '{planeta.NombrePlaneta}'."
            : $"{preguntas.Count} pregunta(s) en '{planeta.NombrePlaneta}'.");

        foreach (Pregunta preg in preguntas)
        {
            GameObject item = Instantiate(prefabItemPregunta, contenedorLista);
            TextMeshProUGUI texto = item.GetComponentInChildren<TextMeshProUGUI>();
            if (texto != null)
                texto.text = preg.enunciado.Length > 60
                    ? preg.enunciado[..60] + "..."
                    : preg.enunciado;

            Button btn = item.GetComponentInChildren<Button>();
            if (btn != null)
            {
                string idCapturado = preg.id;
                btn.onClick.AddListener(() => SeleccionarPregunta(idCapturado));
            }
        }
    }

    private void SeleccionarPregunta(string id)
    {
        Pregunta preg = creadorPreguntas.bibliotecaLocal.FirstOrDefault(p => p.id == id);
        if (preg == null) return;

        idPreguntaEditando = id;

        if (inputEnunciado != null) inputEnunciado.text = preg.enunciado;
        for (int i = 0; i < inputOpciones.Length; i++)
            if (inputOpciones[i] != null)
                inputOpciones[i].text = i < preg.opciones.Length ? preg.opciones[i] : "";

        if (dropdownCorrecta != null) dropdownCorrecta.value = preg.respuestaCorrecta;
        if (inputTiempoLimite != null) inputTiempoLimite.text = preg.tiempoLimite.ToString("F0");
        if (inputPuntos != null) inputPuntos.text = preg.puntosPorAcierto.ToString();

        if (botonEliminar != null) botonEliminar.interactable = true;
        MostrarFormulario(true);
        SetEstado("Editando pregunta.");
    }

    public void ClickNuevaPregunta()
    {
        idPreguntaEditando = null;

        if (inputEnunciado != null) inputEnunciado.text = "";
        foreach (var campo in inputOpciones)
            if (campo != null) campo.text = "";
        if (dropdownCorrecta != null) dropdownCorrecta.value = 0;
        if (inputTiempoLimite != null) inputTiempoLimite.text = "30";
        if (inputPuntos != null) inputPuntos.text = "10";
        if (botonEliminar != null) botonEliminar.interactable = false;

        MostrarFormulario(true);
        SetEstado("Nueva pregunta.");
    }

    public void ClickGuardar()
    {
        PlanetSelectable planeta = PlanetSelectionManager.Instance?.ObtenerPlanetaActual();
        if (planeta == null) { SetEstado("Error: no hay planeta seleccionado."); return; }
        if (inputEnunciado == null || string.IsNullOrWhiteSpace(inputEnunciado.text))
        {
            SetEstado("El enunciado no puede estar vacío.");
            return;
        }

        string[] opciones = new string[inputOpciones.Length];
        for (int i = 0; i < inputOpciones.Length; i++)
            opciones[i] = inputOpciones[i] != null ? inputOpciones[i].text : "";

        int correcta = dropdownCorrecta != null ? dropdownCorrecta.value : 0;

        float tiempo = 30f;
        if (inputTiempoLimite != null && float.TryParse(inputTiempoLimite.text, out float t) && t > 0)
            tiempo = t;

        int puntos = 10;
        if (inputPuntos != null && int.TryParse(inputPuntos.text, out int pts) && pts > 0)
            puntos = pts;

        if (idPreguntaEditando != null)
        {
            Pregunta existente = creadorPreguntas.bibliotecaLocal
                .FirstOrDefault(p => p.id == idPreguntaEditando);

            if (existente != null)
            {
                existente.enunciado = inputEnunciado.text;
                existente.opciones = opciones;
                existente.respuestaCorrecta = correcta;
                existente.tiempoLimite = tiempo;
                existente.puntosPorAcierto = puntos;
                AulaDataManager.Instance.GuardarPreguntaEnFirebase(existente);
                SetEstado("Pregunta guardada.");
            }
        }
        else
        {
            Pregunta nueva = new Pregunta(
                System.Guid.NewGuid().ToString(),
                planeta.IdUnico,
                inputEnunciado.text,
                opciones,
                correcta,
                tiempo,
                puntos
            );
            creadorPreguntas.bibliotecaLocal.Add(nueva);
            AulaDataManager.Instance.GuardarPreguntaEnFirebase(nueva);
            idPreguntaEditando = nueva.id;
            if (botonEliminar != null) botonEliminar.interactable = true;
            SetEstado("Pregunta creada.");
        }

        RefrescarLista();
    }

    public void ClickEliminar()
    {
        if (idPreguntaEditando == null) return;

        Pregunta preg = creadorPreguntas.bibliotecaLocal
            .FirstOrDefault(p => p.id == idPreguntaEditando);
        if (preg == null) return;

        string textoConfirm = string.IsNullOrEmpty(preg.enunciado)
            ? "¿Eliminar esta pregunta? Esta acción no se puede deshacer."
            : $"¿Eliminar la pregunta?\n\n\"{(preg.enunciado.Length > 80 ? preg.enunciado.Substring(0, 80) + "…" : preg.enunciado)}\"";

        ConfirmDialog.Show(textoConfirm, () =>
        {
            creadorPreguntas.bibliotecaLocal.Remove(preg);
            AulaDataManager.Instance.BorrarPreguntaDeFirebase(preg);
            idPreguntaEditando = null;
            MostrarFormulario(false);
            RefrescarLista();
            SetEstado("Pregunta eliminada.");
            Toast.Show("Pregunta eliminada.", 2f, Toast.Tipo.Exito);
        }, textoConfirmar: "Eliminar", textoCancelar: "Cancelar");
    }

    private void MostrarFormulario(bool mostrar)
    {
        if (panelFormulario != null) panelFormulario.SetActive(mostrar);
    }

    private void SetEstado(string msg)
    {
        if (textoEstado != null) textoEstado.text = msg;
    }
}
