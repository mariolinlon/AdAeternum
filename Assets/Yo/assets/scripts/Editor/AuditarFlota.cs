using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Solo LISTA (no cambia nada) todos los textos de UI de la escena que contienen
/// la palabra "flota", con su ruta de pantalla, para decidir cuáles pasan a "nave"
/// y cuáles se quedan (editor/gestor de flotas). Ejecutar:
/// Tools → AdAeternum → Auditar 'flota' (listar).
/// </summary>
public static class AuditarFlota
{
    [MenuItem("Tools/AdAeternum/Auditar 'flota' (listar)")]
    public static void Auditar()
    {
        int n = 0;
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null || EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
            if (!string.IsNullOrEmpty(t.text) && t.text.ToLowerInvariant().Contains("flota"))
            {
                Debug.Log($"[AUDIT-FLOTA] \"{t.text}\"  ::  {Ruta(t.transform)}");
                n++;
            }
        }
        foreach (var t in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (t == null || EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
            if (!string.IsNullOrEmpty(t.text) && t.text.ToLowerInvariant().Contains("flota"))
            {
                Debug.Log($"[AUDIT-FLOTA] (UI) \"{t.text}\"  ::  {Ruta(t.transform)}");
                n++;
            }
        }
        Debug.Log($"<color=lime>[AUDIT-FLOTA] Total: {n} textos de UI con 'flota'.</color>");
    }

    private static string Ruta(Transform t)
    {
        string r = t.name;
        while (t.parent != null) { t = t.parent; r = t.name + "/" + r; }
        return r;
    }
}
