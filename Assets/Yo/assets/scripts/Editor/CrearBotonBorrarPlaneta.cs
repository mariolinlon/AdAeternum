using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using System.Linq;
using System.Reflection;

/// <summary>
/// Script de Editor que crea el botón "Borrar planeta" en la esquina inferior
/// derecha del Canvas y lo enlaza al PlanetSelectionManager. Se ejecuta UNA
/// SOLA VEZ desde el menú: Tools → Ad Aeternum → Crear Botón Borrar Planeta.
///
/// Después de ejecutarlo y verificar que el botón aparece correctamente, este
/// script se puede borrar — solo es una utilidad de setup, no runtime.
/// </summary>
public static class CrearBotonBorrarPlaneta
{
    [MenuItem("Tools/AdAeternum/Crear Boton Borrar Planeta")]
    public static void Crear()
    {
        // 1. Encontrar el PlanetSelectionManager en la escena activa.
        var psm = Object.FindFirstObjectByType<PlanetSelectionManager>(FindObjectsInactive.Include);
        if (psm == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró ningún PlanetSelectionManager en la escena activa.",
                "OK");
            return;
        }

        // 2. Localizar el array botonesDeAccion via reflection (es private SerializeField).
        SerializedObject soPsm = new SerializedObject(psm);
        SerializedProperty arrBotones = soPsm.FindProperty("botonesDeAccion");
        if (arrBotones == null || !arrBotones.isArray)
        {
            EditorUtility.DisplayDialog("Error",
                "PlanetSelectionManager no tiene un array 'botonesDeAccion' accesible.",
                "OK");
            return;
        }

        // 3. Tomar como REFERENCIA el primer botón del array (para clonarlo y
        //    heredar estilo visual, tamaño, fuentes, etc.).
        GameObject referencia = null;
        for (int i = 0; i < arrBotones.arraySize; i++)
        {
            var el = arrBotones.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (el != null) { referencia = el; break; }
        }

        if (referencia == null)
        {
            EditorUtility.DisplayDialog("Error",
                "El array 'botonesDeAccion' está vacío. Necesitamos al menos un botón " +
                "existente para usarlo como plantilla. Añade alguno y vuelve a ejecutar.",
                "OK");
            return;
        }

        // 4. Duplicar la referencia.
        GameObject nuevo = Object.Instantiate(referencia, referencia.transform.parent);
        nuevo.name = "boton/borrar planeta";
        nuevo.SetActive(true);

        // 5. Reposicionar a la esquina inferior derecha.
        RectTransform rt = nuevo.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            // Margen de 20px desde los bordes inferior y derecho.
            rt.anchoredPosition = new Vector2(-20f, 20f);
            // El tamaño se hereda del original (suele ser 250x80).
        }

        // 6. Cambiar el texto a "Borrar planeta".
        //    Buscamos tanto TMP_Text como Text por si el original usa uno u otro.
        var tmp = nuevo.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = "Borrar planeta";
        var txtLegacy = nuevo.GetComponentInChildren<Text>(true);
        if (txtLegacy != null) txtLegacy.text = "Borrar planeta";

        // 7. Asignar el OnClick → PlanetSelectionManager.ClickBorrarPlaneta.
        Button btn = nuevo.GetComponent<Button>();
        if (btn != null)
        {
            // Limpiar OnClicks heredados del botón clonado.
            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(btn.onClick, i);

            // Añadir el nuevo persistent listener.
            UnityAction call = psm.ClickBorrarPlaneta;
            UnityEventTools.AddPersistentListener(btn.onClick, call);
        }
        else
        {
            Debug.LogWarning("[CrearBotonBorrarPlaneta] El botón duplicado no tiene componente Button.");
        }

        // 8. Añadirlo al array botonesDeAccion para que aparezca/desaparezca
        //    con la selección de planeta (controlado por el PlanetSelectionManager).
        soPsm.Update();
        int idx = arrBotones.arraySize;
        arrBotones.InsertArrayElementAtIndex(idx);
        arrBotones.GetArrayElementAtIndex(idx).objectReferenceValue = nuevo;
        soPsm.ApplyModifiedProperties();

        // 9. Como el botón por defecto está desactivado (igual que los demás
        //    botones de acción, que solo aparecen al seleccionar planeta),
        //    también lo desactivamos hasta que el usuario seleccione uno.
        nuevo.SetActive(false);

        // 10. Marcar la escena como dirty para que Unity guarde los cambios.
        EditorUtility.SetDirty(psm);
        EditorSceneManager.MarkSceneDirty(nuevo.scene);

        // 11. Seleccionar el nuevo botón en la jerarquía para que el usuario lo vea.
        Selection.activeGameObject = nuevo;
        EditorGUIUtility.PingObject(nuevo);

        Debug.Log($"<color=lime>[CrearBotonBorrarPlaneta] Botón creado y enlazado correctamente.</color>\n" +
                  $"Posición: esquina inferior derecha (anchor 1,0; offset -20,20).\n" +
                  $"Padre: {referencia.transform.parent.name}\n" +
                  $"Añadido al array botonesDeAccion[{idx}].\n" +
                  $"Recuerda: guarda la escena (Ctrl+S) para que los cambios persistan.");

        EditorUtility.DisplayDialog("Éxito",
            "Botón \"Borrar planeta\" creado y enlazado correctamente.\n\n" +
            "Posicionado en la esquina inferior derecha.\n" +
            "Aparecerá automáticamente al seleccionar un planeta.\n\n" +
            "No olvides guardar la escena (Ctrl+S).",
            "OK");
    }
}
