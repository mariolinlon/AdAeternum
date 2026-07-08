using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Diagnostica por qué el botón "boton/borrar planeta" no aparece al seleccionar
/// un planeta. Mira el array botonesDeAccion del PlanetSelectionManager y reporta:
///  - Si el botón está en el array.
///  - Si está dentro de una jerarquía activa (padres todos activos).
///  - Si tiene RectTransform con tamaño razonable y posición en pantalla.
///  - Si tiene Canvas padre y está habilitado.
///  - Si tiene Image (para que sea visible y clicable).
///
/// Ejecutar: Tools → AdAeternum → Diagnosticar Boton Borrar Planeta.
/// </summary>
public static class DiagnosticarBotonBorrar
{
    [MenuItem("Tools/AdAeternum/Diagnosticar Boton Borrar Planeta")]
    public static void Diagnosticar()
    {
        var psm = Object.FindFirstObjectByType<PlanetSelectionManager>(FindObjectsInactive.Include);
        if (psm == null)
        {
            Debug.LogError("[Diagnóstico] No se encontró PlanetSelectionManager.");
            return;
        }

        SerializedObject so = new SerializedObject(psm);
        SerializedProperty arr = so.FindProperty("botonesDeAccion");

        GameObject boton = null;
        int idx = -1;
        for (int i = 0; i < arr.arraySize; i++)
        {
            var go = arr.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go != null && go.name.ToLowerInvariant().Contains("borrar"))
            {
                boton = go;
                idx = i;
                break;
            }
        }

        if (boton == null)
        {
            Debug.LogError("[Diagnóstico] El array botonesDeAccion NO contiene ningún botón cuyo nombre contenga 'borrar'.");
            Debug.LogError("Contenido actual del array:");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var go = arr.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                Debug.LogError($"  [{i}] {(go != null ? go.name : "NULL")}");
            }
            return;
        }

        Debug.Log($"<color=cyan>═══ Botón encontrado en botonesDeAccion[{idx}]: '{boton.name}' ═══</color>");

        // Reportar el estado actual.
        Debug.Log($"  activeSelf: {boton.activeSelf}");
        Debug.Log($"  activeInHierarchy: {boton.activeInHierarchy}");
        Debug.Log($"  layer: {boton.layer} ({LayerMask.LayerToName(boton.layer)})");

        // Jerarquía de padres y su estado.
        Debug.Log("  Cadena de padres (de hijo a raíz):");
        Transform t = boton.transform;
        int depth = 0;
        while (t != null)
        {
            string marca = t.gameObject.activeSelf ? "✓" : "✗";
            Debug.Log($"    {marca} [{depth}] {t.name} (active={t.gameObject.activeSelf}, inHierarchy={t.gameObject.activeInHierarchy})");
            t = t.parent;
            depth++;
        }

        // RectTransform.
        var rt = boton.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"  RectTransform:");
            Debug.Log($"    anchorMin: {rt.anchorMin}  anchorMax: {rt.anchorMax}");
            Debug.Log($"    anchoredPosition: {rt.anchoredPosition}");
            Debug.Log($"    sizeDelta: {rt.sizeDelta}");
            Debug.Log($"    pivot: {rt.pivot}");
            Debug.Log($"    localScale: {rt.localScale}");
        }
        else
        {
            Debug.LogWarning("  ✗ No tiene RectTransform.");
        }

        // Canvas padre.
        var canvas = boton.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            Debug.Log($"  Canvas padre: '{canvas.name}' (enabled={canvas.enabled}, renderMode={canvas.renderMode})");
        }
        else
        {
            Debug.LogWarning("  ✗ NO está dentro de ningún Canvas → será invisible siempre.");
        }

        // Image.
        var img = boton.GetComponent<Image>();
        if (img != null)
        {
            Debug.Log($"  Image: enabled={img.enabled}, color={img.color}, alpha={img.color.a}");
        }

        // Button.
        var btn = boton.GetComponent<Button>();
        if (btn != null)
        {
            Debug.Log($"  Button: interactable={btn.interactable}, persistentEventCount={btn.onClick.GetPersistentEventCount()}");
            for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
            {
                Debug.Log($"    OnClick[{i}]: target={btn.onClick.GetPersistentTarget(i)}, method={btn.onClick.GetPersistentMethodName(i)}");
            }
        }

        // CanvasGroup en cadena.
        Transform tg = boton.transform;
        while (tg != null)
        {
            var cg = tg.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"  CanvasGroup en '{tg.name}': alpha={cg.alpha}, interactable={cg.interactable}, blocksRaycasts={cg.blocksRaycasts}");
            }
            tg = tg.parent;
        }

        // Conclusión.
        Debug.Log("<color=lime>═══ Fin del diagnóstico ═══</color>");
        Debug.Log("Si activeInHierarchy=False → algún padre está desactivado.");
        Debug.Log("Si anchorMin/Max no son (1,0) o sizeDelta es (0,0) → no se ve aunque esté activo.");
        Debug.Log("Si no hay Canvas padre → la UI nunca se renderiza.");
        Debug.Log("Si CanvasGroup.alpha=0 → invisible aunque activeInHierarchy=True.");

        Selection.activeGameObject = boton;
        EditorGUIUtility.PingObject(boton);
    }
}
