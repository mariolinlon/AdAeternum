using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Toast/notificación auto-spawn: muestra un mensaje en pantalla durante N segundos.
/// Uso: Toast.Show("texto"); o Toast.Show("texto", 5f, Toast.Tipo.Error);
/// El primer Show() crea automáticamente el Canvas y los GameObjects necesarios.
/// </summary>
public class Toast : MonoBehaviour
{
    public enum Tipo { Info, Exito, Error, Aviso }

    private static Toast _instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private Image fondo;
    [SerializeField] private TextMeshProUGUI texto;

    private Coroutine rutinaActual;

    /// <summary>Muestra un toast con tipo Info.</summary>
    public static void Show(string mensaje, float duracion = 3f)
        => GetOrCreate().Mostrar(mensaje, duracion, Tipo.Info);

    /// <summary>Muestra un toast con tipo específico (Exito/Error/Aviso/Info).</summary>
    public static void Show(string mensaje, float duracion, Tipo tipo)
        => GetOrCreate().Mostrar(mensaje, duracion, tipo);

    private static Toast GetOrCreate()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("[Toast]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<Toast>();
        _instance.ConstruirUI();
        return _instance;
    }

    private void ConstruirUI()
    {
        // Canvas overlay
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760; // por encima de todo
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Panel del toast: anclado abajo-centro, con margen
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(transform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.sizeDelta = new Vector2(700f, 80f);

        fondo = panel.GetComponent<Image>();
        fondo.color = new Color32(21, 25, 42, 230); // dark
        // Bordes redondeados con el UISprite por defecto
        fondo.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        fondo.type = Image.Type.Sliced;

        // Outline cyan
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(34, 211, 238, 255);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        grupo = panel.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // Texto
        var textoGO = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer));
        textoGO.transform.SetParent(panel.transform, false);
        var trt = textoGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 10f);
        trt.offsetMax = new Vector2(-20f, -10f);

        texto = textoGO.AddComponent<TextMeshProUGUI>();
        texto.text = "";
        texto.fontSize = 28;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 14;
        texto.fontSizeMax = 32;
        texto.color = new Color32(241, 245, 249, 255);
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;
    }

    private void Mostrar(string mensaje, float duracion, Tipo tipo)
    {
        if (texto == null) return;

        texto.text = mensaje;

        // Colores y SFX según tipo
        switch (tipo)
        {
            case Tipo.Exito:
                fondo.color = new Color32(21, 25, 42, 230);
                ApplyOutline(new Color32(34, 197, 94, 255));    // verde
                AudioManager.PlaySFX(AudioManager.SFX.ToastExito);
                break;
            case Tipo.Error:
                fondo.color = new Color32(35, 18, 22, 235);
                ApplyOutline(new Color32(239, 68, 68, 255));    // rojo
                AudioManager.PlaySFX(AudioManager.SFX.ToastError);
                break;
            case Tipo.Aviso:
                fondo.color = new Color32(35, 30, 18, 235);
                ApplyOutline(new Color32(245, 158, 11, 255));   // ámbar
                AudioManager.PlaySFX(AudioManager.SFX.ToastAviso);
                break;
            case Tipo.Info:
            default:
                fondo.color = new Color32(21, 25, 42, 230);
                ApplyOutline(new Color32(34, 211, 238, 255));   // cian
                AudioManager.PlaySFX(AudioManager.SFX.ToastInfo);
                break;
        }

        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(RutinaToast(duracion));
    }

    private void ApplyOutline(Color c)
    {
        if (fondo == null) return;
        var outline = fondo.GetComponent<Outline>();
        if (outline != null) outline.effectColor = c;
    }

    private IEnumerator RutinaToast(float duracion)
    {
        // Fade in
        float t = 0f;
        while (t < 0.20f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Clamp01(t / 0.20f);
            yield return null;
        }
        grupo.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(duracion);

        // Fade out
        t = 0f;
        while (t < 0.30f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = 1f - Mathf.Clamp01(t / 0.30f);
            yield return null;
        }
        grupo.alpha = 0f;
        rutinaActual = null;
    }
}
