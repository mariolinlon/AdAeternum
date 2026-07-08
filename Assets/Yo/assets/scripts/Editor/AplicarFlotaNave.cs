using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Aplica el cambio de terminología "flota" → "nave" SOLO en los textos de UI
/// concretos que decidimos (perfil y ranking), y corrige el typo del gestor.
/// El gestor/editor de flotas del profesor conserva "flota".
/// Ejecutar: Tools → AdAeternum → Aplicar flota→nave (textos).
/// </summary>
public static class AplicarFlotaNave
{
    [MenuItem("Tools/AdAeternum/Aplicar flota→nave (textos)")]
    public static void Aplicar()
    {
        // Coincidencia por texto EXACTO (trim) → nuevo texto.
        var map = new Dictionary<string, string>
        {
            { "flota",          "Nave" },           // botón del Perfil del alumno
            { "mejores flotas", "mejores naves" },  // título del ranking
            { "Crear FLota",    "Crear Flota" },    // typo en el gestor (sigue siendo 'flota')
        };

        int n = 0;
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null || EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
            string cur = t.text != null ? t.text.Trim() : null;
            if (cur != null && map.TryGetValue(cur, out string nuevo))
            {
                Undo.RecordObject(t, "flota->nave");
                Debug.Log($"<color=cyan>[FLOTA→NAVE]</color> \"{t.text}\" → \"{nuevo}\"   ::  {Ruta(t.transform)}");
                t.text = nuevo;
                EditorUtility.SetDirty(t);
                n++;
            }
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"<color=lime>[FLOTA→NAVE] {n} textos de escena cambiados. Guarda la escena (Ctrl+S).</color>");
    }

    private static string Ruta(Transform t)
    {
        string r = t.name;
        while (t.parent != null) { t = t.parent; r = t.name + "/" + r; }
        return r;
    }
}
