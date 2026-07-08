using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Firebase.Firestore;

public class PanelMensajesAlumno : MonoBehaviour
{
    [Header("Lista")]
    [SerializeField] private Transform contenedorMensajes;
    [SerializeField] private GameObject prefabItemMensaje;

    [Header("Notificación")]
    [SerializeField] private GameObject badgeNuevo;

    private int ultimoConteo = 0;
    private bool escuchandoMensajes = false;

    private void OnEnable()
    {
        if (badgeNuevo != null) badgeNuevo.SetActive(false);

        // Si ya tenemos aula activa y aún no hemos registrado el listener, arrancar
        if (!escuchandoMensajes && AulaDataManager.Instance != null
            && !string.IsNullOrEmpty(AulaDataManager.Instance.GetCodigoAula()))
        {
            IniciarEscucha();
        }
    }

    // Llamar este método desde LoginAlumnoUI tras entrar al aula
    public void IniciarEscucha()
    {
        if (escuchandoMensajes) return;
        escuchandoMensajes = true;
        AulaDataManager.Instance.EscucharMensajes(OnMensajesActualizados);
    }

    private void OnMensajesActualizados(List<Dictionary<string, object>> mensajes)
    {
        if (contenedorMensajes == null || prefabItemMensaje == null)
        {
            Debug.LogWarning("[Mensajes] contenedorMensajes o prefabItemMensaje no asignado en el Inspector.");
            return;
        }

        mensajes.Sort((a, b) =>
        {
            var tA = a.ContainsKey("timestamp") && a["timestamp"] is Timestamp tsA ? tsA.ToDateTime() : System.DateTime.MinValue;
            var tB = b.ContainsKey("timestamp") && b["timestamp"] is Timestamp tsB ? tsB.ToDateTime() : System.DateTime.MinValue;
            return tA.CompareTo(tB);
        });

        foreach (Transform hijo in contenedorMensajes)
            Destroy(hijo.gameObject);

        foreach (var msg in mensajes)
        {
            string texto = msg.ContainsKey("texto") ? msg["texto"].ToString() : "";
            if (string.IsNullOrEmpty(texto)) continue;

            GameObject item = Instantiate(prefabItemMensaje, contenedorMensajes);
            TextMeshProUGUI tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = texto;
        }

        if (mensajes.Count > ultimoConteo && !gameObject.activeSelf)
            if (badgeNuevo != null) badgeNuevo.SetActive(true);

        ultimoConteo = mensajes.Count;
    }
}
