using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelControlFlotas : MonoBehaviour
{
    [Header("Configuración de Prefabs")]
    public GameObject prefabTarjetaFlota; // Tu prefab que tiene el script FlotaUI
    public Transform contenedorTarjetas; // El objeto con el Vertical/Grid Layout Group

    [Header("Creación de Flota")]
    public TMP_InputField inputNombreFlota;
    public TMP_InputField inputMaxAlumnos;

    [Header("Estado")]
    public string idAlumnoSeleccionado;

    public static PanelControlFlotas Instance { get; private set; }
    private bool escuchando = false;

    // Límites del aula: máximo de flotas simultáneas y máximo de alumnos por flota.
    public const int MAX_FLOTAS = 6;
    public const int MAX_ALUMNOS_POR_FLOTA = 10;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        IniciarEscuchaFlotas();
    }

    public void IniciarEscuchaFlotas()
    {
        if (escuchando) return;
        if (AulaDataManager.Instance == null) return;
        escuchando = true;
        AulaDataManager.Instance.EscucharFlotas(ActualizarInterfazFlotas);
    }

    public void ReiniciarEscucha()
    {
        escuchando = false;
        IniciarEscuchaFlotas();
    }

    void ActualizarInterfazFlotas()
    {
        for (int i = contenedorTarjetas.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedorTarjetas.GetChild(i);
            hijo.SetParent(null);
            Destroy(hijo.gameObject);
        }

        // 3. Crear una tarjeta por cada flota que haya en el AulaDataManager
        // Nota: He visto en tu AulaDataManager que guardas las flotas en una lista de objetos 'Flota'
        foreach (Flota f in AulaDataManager.Instance.flotasActivas)
        {
            GameObject nuevaTarjeta = Instantiate(prefabTarjetaFlota, contenedorTarjetas);
            FlotaUI scriptUI = nuevaTarjeta.GetComponent<FlotaUI>();

            // 4. Configuramos la tarjeta con los datos de Firebase
            scriptUI.Configurar(f.id, f.nombre, f.maxAlumnos, f.alumnos, f.liderID);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contenedorTarjetas as RectTransform);
    }

    public void ClickCrearFlota()
    {
        string nombre = inputNombreFlota != null ? inputNombreFlota.text.Trim() : "";
        string maxStr = inputMaxAlumnos != null ? inputMaxAlumnos.text.Trim() : "";

        // Validación: tope de flotas simultáneas en el aula
        if (AulaDataManager.Instance != null &&
            AulaDataManager.Instance.flotasActivas.Count >= MAX_FLOTAS)
        {
            Toast.Show($"Máximo {MAX_FLOTAS} flotas permitidas. Borra una para crear otra.", 3f, Toast.Tipo.Error);
            return;
        }

        // Validación: nombre obligatorio
        if (string.IsNullOrEmpty(nombre))
        {
            Toast.Show("Pon un nombre a la flota.", 3f, Toast.Tipo.Error);
            return;
        }

        // Validación: longitud razonable
        if (nombre.Length < 2)
        {
            Toast.Show("El nombre de flota debe tener al menos 2 caracteres.", 3f, Toast.Tipo.Error);
            return;
        }

        // Validación: max alumnos numérico
        if (!int.TryParse(maxStr, out int max))
        {
            Toast.Show("El máximo de alumnos debe ser un número.", 3f, Toast.Tipo.Error);
            return;
        }

        // Validación: max alumnos en rango razonable
        if (max < 1 || max > MAX_ALUMNOS_POR_FLOTA)
        {
            Toast.Show($"El máximo de alumnos debe estar entre 1 y {MAX_ALUMNOS_POR_FLOTA}.", 3f, Toast.Tipo.Error);
            return;
        }

        // Todo correcto — crear flota
        AulaDataManager.Instance.CrearNuevaFlotaEnNube(nombre, max);

        inputNombreFlota.text = "";
        inputMaxAlumnos.text = "4"; // Valor por defecto
    }
}