using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Sustituye el término «monitoreo» por «seguimiento» en la escena activa:
///   - Textos de UI (TextMeshPro y UI.Text), incluidos objetos inactivos.
///   - Nombres de GameObject (p. ej. "boton/monitoreo").
/// Respeta las mayúsculas (Monitoreo→Seguimiento, monitoreo→seguimiento).
/// No usa diálogos modales para poder ejecutarse desde el MCP sin bloquear.
///
/// Ejecutar: Tools → AdAeternum → Reemplazar monitoreo por seguimiento.
/// </summary>
public static class ReemplazarMonitoreoSeguimiento
{
    [MenuItem("Tools/AdAeternum/Reemplazar monitoreo por seguimiento")]
    public static void Reemplazar()
    {
        int textos = 0, nombres = 0;
        var cambios = new List<string>();

        // 1) Textos TextMeshPro (incluye inactivos)
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null || EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
            string nuevo = Sustituir(t.text);
            if (nuevo != t.text)
            {
                Undo.RecordObject(t, "Reemplazar monitoreo");
                cambios.Add($"texto TMP: \"{t.text}\" → \"{nuevo}\"   [{Ruta(t.transform)}]");
                t.text = nuevo;
                EditorUtility.SetDirty(t);
                textos++;
            }
        }

        // 2) Textos UI.Text legacy (por si acaso)
        foreach (var t in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (t == null || EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
            string nuevo = Sustituir(t.text);
            if (nuevo != t.text)
            {
                Undo.RecordObject(t, "Reemplazar monitoreo");
                cambios.Add($"texto UI: \"{t.text}\" → \"{nuevo}\"");
                t.text = nuevo;
                EditorUtility.SetDirty(t);
                textos++;
            }
        }

        // 3) Nombres de GameObject (incluye inactivos)
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || EditorUtility.IsPersistent(go) || !go.scene.IsValid()) continue;
            string nuevo = Sustituir(go.name);
            if (nuevo != go.name)
            {
                Undo.RecordObject(go, "Renombrar monitoreo");
                cambios.Add($"nombre: '{go.name}' → '{nuevo}'");
                go.name = nuevo;
                EditorUtility.SetDirty(go);
                nombres++;
            }
        }

        if (textos + nombres > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        foreach (var c in cambios) Debug.Log("<color=cyan>[Monitoreo→Seguimiento]</color> " + c);
        Debug.Log($"<color=lime>[Monitoreo→Seguimiento] HECHO — {textos} texto(s) y {nombres} nombre(s) cambiados. Guarda la escena (Ctrl+S).</color>");
    }

    private static string Sustituir(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("MONITOREO", "SEGUIMIENTO")
                .Replace("Monitoreo", "Seguimiento")
                .Replace("monitoreo", "seguimiento")
                // Erratas detectadas
                .Replace("Tiemopo", "Tiempo")
                .Replace("tiemopo", "tiempo");
    }

    private static string Ruta(Transform t)
    {
        string r = t.name;
        while (t.parent != null) { t = t.parent; r = t.name + "/" + r; }
        return r;
    }
}
