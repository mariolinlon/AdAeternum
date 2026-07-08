using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Arregla los ScrollRect de la escena que tienen el campo Content a NULO.
/// Un ScrollRect sin Content, con la visibilidad de scrollbars en
/// "AutoHideAndExpandViewport", lanza un NullReferenceException dentro de
/// ScrollRect.SetLayoutHorizontal() -> LayoutRebuilder cada vez que se
/// reconstruye el layout. Esa excepción ABORTA la reconstrucción de todo el
/// canvas, por lo que en la build los paneles se ven con los elementos
/// solapados (en el editor a veces no se reproduce).
///
/// La herramienta:
///   1) Asigna Content = primer hijo del Viewport (estructura estándar de
///      Scroll View) o un descendiente llamado Content/Contenido/Contenedor.
///   2) Si no encuentra candidato, pone la visibilidad de scrollbars en
///      "Permanent" para que SetLayoutHorizontal no llame al rebuild nulo.
///
/// Ejecutar: Tools → AdAeternum → Arreglar ScrollRects rotos.
/// </summary>
public static class ArreglarScrollRectsRotos
{
    [MenuItem("Tools/AdAeternum/Arreglar ScrollRects rotos")]
    public static void Arreglar()
    {
        var scrolls = Resources.FindObjectsOfTypeAll<ScrollRect>();
        int revisados = 0, asignados = 0, ajustados = 0;

        foreach (var sr in scrolls)
        {
            if (sr == null) continue;
            if (EditorUtility.IsPersistent(sr)) continue;          // ignora prefabs en disco
            if (!sr.gameObject.scene.IsValid()) continue;          // solo objetos de escena
            revisados++;

            if (sr.content != null) continue;                      // ya está bien

            RectTransform candidato = null;

            // 1) Hijo del Viewport (estructura estándar Scroll View → Viewport → Content)
            if (sr.viewport != null && sr.viewport.childCount > 0)
                candidato = sr.viewport.GetChild(0) as RectTransform;

            // 2) Descendiente cuyo nombre sugiera que es el contenido
            if (candidato == null)
            {
                foreach (var rt in sr.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt == sr.transform) continue;
                    string n = rt.name.ToLowerInvariant();
                    if (n.Contains("content") || n.Contains("contenido") || n.Contains("contenedor"))
                    {
                        candidato = rt;
                        break;
                    }
                }
            }

            Undo.RecordObject(sr, "Arreglar ScrollRect");
            if (candidato != null)
            {
                sr.content = candidato;
                asignados++;
                Debug.Log($"<color=lime>[ScrollFix]</color> '{Ruta(sr.transform)}' → Content = '{candidato.name}'");
            }
            else
            {
                sr.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                sr.verticalScrollbarVisibility   = ScrollRect.ScrollbarVisibility.Permanent;
                ajustados++;
                Debug.LogWarning($"[ScrollFix] '{Ruta(sr.transform)}' sin candidato de Content; visibilidad de scrollbars puesta a Permanent para evitar el NullReference.");
            }
            EditorUtility.SetDirty(sr);
        }

        if (revisados > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Arreglar ScrollRects",
            $"ScrollRects revisados: {revisados}\n" +
            $"Content asignado: {asignados}\n" +
            $"Sin candidato (scrollbars a Permanent): {ajustados}\n\n" +
            $"Guarda la escena (Ctrl+S) y vuelve a generar la build.",
            "OK");
    }

    private static string Ruta(Transform t)
    {
        string r = t.name;
        while (t.parent != null) { t = t.parent; r = t.name + "/" + r; }
        return r;
    }
}
