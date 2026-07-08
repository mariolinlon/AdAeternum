using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlotaMemberRow : MonoBehaviour
{
    [Header("Texto")]
    [SerializeField] private TextMeshProUGUI textNombre;

    [Header("Botones (visibles solo si quien mira es el lider)")]
    [SerializeField] private Button botonAtaque;
    [SerializeField] private Button botonDefensa;

    private string idAlumno;
    private string rolCombateActual;

    public void Configurar(string idAlumno, string nombreMostrado, string rolCombate, bool somosLider)
    {
        this.idAlumno = idAlumno;
        this.rolCombateActual = rolCombate;

        if (textNombre != null) textNombre.text = nombreMostrado;

        // Mostrar / ocultar botones según si el observador es el lider
        if (botonAtaque != null)  botonAtaque.gameObject.SetActive(somosLider);
        if (botonDefensa != null) botonDefensa.gameObject.SetActive(somosLider);

        if (somosLider)
        {
            if (botonAtaque != null)
            {
                botonAtaque.onClick.RemoveAllListeners();
                botonAtaque.onClick.AddListener(OnClickAtaque);
                ResaltarBoton(botonAtaque, rolCombate == "atacante");
            }
            if (botonDefensa != null)
            {
                botonDefensa.onClick.RemoveAllListeners();
                botonDefensa.onClick.AddListener(OnClickDefensa);
                ResaltarBoton(botonDefensa, rolCombate == "defensor");
            }
        }
    }

    private void OnClickAtaque()
    {
        if (AulaDataManager.Instance == null) return;
        AulaDataManager.Instance.SetRolCombateAlumno(idAlumno, "atacante");
    }

    private void OnClickDefensa()
    {
        if (AulaDataManager.Instance == null) return;
        AulaDataManager.Instance.SetRolCombateAlumno(idAlumno, "defensor");
    }

    private void ResaltarBoton(Button b, bool seleccionado)
    {
        // Resalta el botón con color si su rol está activo
        var img = b.GetComponent<Image>();
        if (img == null) return;
        img.color = seleccionado ? new Color(0.95f, 0.85f, 0.30f) : Color.white;
    }
}
