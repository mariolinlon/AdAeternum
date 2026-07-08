using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Arregla el botón "boton/borrar planeta" tras el setup inicial:
///  - Lo reposiciona en la esquina inferior derecha (anchor 1,0; pivot 1,0;
///    offset -20,20).
///  - Limpia los huecos NULL del array botonesDeAccion del PlanetSelectionManager.
///  - Lo deja con activeSelf=false (el ActivarBotonesAccion lo encenderá al
///    seleccionar un planeta).
///
/// Ejecutar: Tools → AdAeternum → Arreglar Boton Borrar Planeta.
/// </summary>
public static class ArreglarBotonBorrar
{
    [MenuItem("Tools/AdAeternum/Arreglar Boton Borrar Planeta")]
    public static void Arreglar()
    {
        var psm = Object.FindFirstObjectByType<PlanetSelectionManager>(FindObjectsInactive.Include);
        if (psm == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró PlanetSelectionManager.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(psm);
        SerializedProperty arr = so.FindProperty("botonesDeAccion");

        // 1. Encontrar el botón borrar.
        GameObject boton = null;
        for (int i = 0; i < arr.arraySize; i++)
        {
            var go = arr.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go != null && go.name.ToLowerInvariant().Contains("borrar"))
            {
                boton = go;
                break;
            }
        }

        if (boton == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No encontré el botón 'borrar' en botonesDeAccion. Ejecuta antes 'Crear Boton Borrar Planeta'.",
                "OK");
            return;
        }

        // 2. Recolocar en la esquina inferior derecha.
        var rt = boton.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Recolocar boton borrar");
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20f, 20f);
            rt.localScale = Vector3.one;
            // Mantenemos el sizeDelta heredado del original (250x80).
            if (rt.sizeDelta.x < 50 || rt.sizeDelta.y < 30)
                rt.sizeDelta = new Vector2(250f, 80f);
        }

        // 3. Limpiar nulls del array botonesDeAccion.
        so.Update();
        int huecosLimpiados = 0;
        for (int i = arr.arraySize - 1; i >= 0; i--)
        {
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                arr.DeleteArrayElementAtIndex(i);
                huecosLimpiados++;
            }
        }
        so.ApplyModifiedProperties();

        // 4. Dejar el botón desactivado (lo activará ActivarBotonesAccion al
        //    seleccionar un planeta).
        boton.SetActive(false);

        // 5. Marcar como dirty para que se guarde.
        EditorUtility.SetDirty(psm);
        EditorUtility.SetDirty(boton);
        EditorSceneManager.MarkSceneDirty(boton.scene);

        Selection.activeGameObject = boton;
        EditorGUIUtility.PingObject(boton);

        Debug.Log($"<color=lime>[ArreglarBotonBorrar] Botón recolocado en esquina inferior derecha. " +
                  $"Limpiados {huecosLimpiados} huecos NULL del array.</color>");

        EditorUtility.DisplayDialog("Éxito",
            $"Botón recolocado en la esquina inferior derecha.\n" +
            $"Limpiados {huecosLimpiados} huecos NULL del array.\n\n" +
            $"Aparecerá automáticamente al seleccionar un planeta.\n" +
            $"No olvides guardar la escena (Ctrl+S).",
            "OK");
    }
}
