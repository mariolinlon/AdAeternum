using UnityEngine;

/// <summary>
/// Componente pequeño para colgar en panelEspera.
/// Cuando el panel se reactiva, pide a SistemaCombateAlumno que compruebe si
/// hay combate ya en curso y, si lo hay, lo arranca para este alumno.
/// </summary>
public class SensorPanelEspera : MonoBehaviour
{
    [SerializeField] private SistemaCombateAlumno sistemaCombate;

    private void OnEnable()
    {
        if (sistemaCombate == null) sistemaCombate = FindFirstObjectByType<SistemaCombateAlumno>(FindObjectsInactive.Include);
        if (sistemaCombate != null)
        {
            sistemaCombate.SuscribirListenerCombate();
            sistemaCombate.IntentarArrancarSiHayCombateActivo();
        }
    }
}
