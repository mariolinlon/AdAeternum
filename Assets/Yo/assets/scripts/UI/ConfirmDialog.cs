using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Diálogo de confirmación modal auto-spawn.
/// Uso: ConfirmDialog.Show("¿Borrar planeta?", () => Borrar());
/// Opcional: ConfirmDialog.Show("...", onConfirm, onCancel, "Sí", "No");
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    private static ConfirmDialog _instance;

    private Canvas canvas;
    private CanvasGroup grupo;
    private Image overlayBg;
    private GameObject panel;
    private TextMeshProUGUI textoPregunta;
    private Button botonSi;
    private Button botonNo;
    private TextMeshProUGUI textoSi;
    private TextMeshProUGUI textoNo;

    private Action callbackConfirm;
    private Action callbackCancel;

    public static void Show(string pregunta, Action onConfirm, Action onCancel = null,
                            string textoConfirmar = "Sí", string textoCancelar = "No")
    {
        GetOrCreate().Mostrar(pregunta, onConfirm, onCancel, textoConfirmar, textoCancelar);
    }

    private static ConfirmDialog GetOrCreate()
    {
        if (_instance != null) return _instance;
        var go = new GameObject("[ConfirmDialog]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ConfirmDialog>();
        _instance.ConstruirUI();
        return _instance;
    }

    private void ConstruirUI()
    {
        // Canvas overlay
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32750;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Overlay (oscurece el fondo)
        var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(transform, false);
        var ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        overlayBg = overlay.GetComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayBg.raycastTarget = true;

        // CanvasGroup en el root para fade
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // Panel centrado
        panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(700f, 280f);

        var img = panel.GetComponent<Image>();
        img.color = new Color32(21, 25, 42, 240);
        img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(34, 211, 238, 255);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        // Texto pregunta
        var preguntaGO = new GameObject("Pregunta", typeof(RectTransform), typeof(CanvasRenderer));
        preguntaGO.transform.SetParent(panel.transform, false);
        var qrt = preguntaGO.GetComponent<RectTransform>();
        qrt.anchorMin = new Vector2(0f, 0.4f);
        qrt.anchorMax = new Vector2(1f, 1f);
        qrt.offsetMin = new Vector2(30f, 0f);
        qrt.offsetMax = new Vector2(-30f, -20f);

        textoPregunta = preguntaGO.AddComponent<TextMeshProUGUI>();
        textoPregunta.fontSize = 32;
        textoPregunta.enableAutoSizing = true;
        textoPregunta.fontSizeMin = 16;
        textoPregunta.fontSizeMax = 36;
        textoPregunta.color = new Color32(241, 245, 249, 255);
        textoPregunta.alignment = TextAlignmentOptions.Center;
        textoPregunta.textWrappingMode = TextWrappingModes.Normal;

        // Botones — fila inferior
        botonSi = CrearBoton(panel.transform, "BotonSi", new Vector2(-150f, 0f), new Color32(34, 211, 238, 255), out textoSi);
        botonNo = CrearBoton(panel.transform, "BotonNo", new Vector2(150f, 0f), new Color32(239, 68, 68, 255), out textoNo);

        botonSi.onClick.AddListener(OnSi);
        botonNo.onClick.AddListener(OnNo);

        // Empezar oculto
        gameObject.SetActive(false);
    }

    private Button CrearBoton(Transform parent, string nombre, Vector2 pos, Color colorBorde, out TextMeshProUGUI label)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(pos.x, 50f);
        rt.sizeDelta = new Vector2(220f, 60f);

        var img = go.GetComponent<Image>();
        img.color = Color.white;
        img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = colorBorde;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        var btn = go.GetComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = new Color32(21, 25, 42, 255);
        cb.highlightedColor = new Color32(35, 42, 70, 255);
        cb.pressedColor = new Color32(10, 12, 22, 255);
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color32(15, 15, 22, 128);
        cb.fadeDuration = 0.12f;
        btn.colors = cb;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(10f, 5f);
        lrt.offsetMax = new Vector2(-10f, -5f);
        label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "OK";
        label.fontSize = 26;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = 30;
        label.color = new Color32(241, 245, 249, 255);
        label.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private void Mostrar(string pregunta, Action onConfirm, Action onCancel, string txtSi, string txtNo)
    {
        textoPregunta.text = pregunta;
        textoSi.text = txtSi;
        textoNo.text = txtNo;
        callbackConfirm = onConfirm;
        callbackCancel = onCancel;

        gameObject.SetActive(true);
        grupo.alpha = 0f;
        grupo.blocksRaycasts = true;
        grupo.interactable = true;
        StopAllCoroutines();
        StartCoroutine(FadeIn());

        AudioManager.PlaySFX(AudioManager.SFX.DialogoAbrir);
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Clamp01(t / 0.15f);
            yield return null;
        }
        grupo.alpha = 1f;
    }

    private void OnSi()
    {
        var cb = callbackConfirm;
        callbackConfirm = null;
        callbackCancel = null;
        Cerrar();
        cb?.Invoke();
    }

    private void OnNo()
    {
        var cb = callbackCancel;
        callbackConfirm = null;
        callbackCancel = null;
        Cerrar();
        cb?.Invoke();
    }

    private void Cerrar()
    {
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        gameObject.SetActive(false);
    }
}
