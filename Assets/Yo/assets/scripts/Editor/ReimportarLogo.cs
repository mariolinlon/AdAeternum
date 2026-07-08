using UnityEngine;
using UnityEditor;

/// <summary>Fuerza la reimportación de la textura del logo (por si Unity no
/// detecta el cambio del archivo sobrescrito). Tools → AdAeternum → Reimportar logo.</summary>
public static class ReimportarLogo
{
    [MenuItem("Tools/AdAeternum/Reimportar logo")]
    public static void Reimportar()
    {
        string p = "Assets/Yo/assets/mios/logo 500 px.png";
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh();
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        string dim = tex != null ? (tex.width + "x" + tex.height) : "NULL";
        Debug.Log("[ReimportarLogo] Reimportado '" + p + "'. Textura: " + dim);
    }
}
