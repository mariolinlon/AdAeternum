using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conecta sliders de UI a los volúmenes globales del AudioManager.
/// Al activarse, sincroniza los sliders con los valores guardados (PlayerPrefs).
/// Al cambiar un slider, actualiza el AudioManager (que a su vez persiste).
/// </summary>
public class ControlAudioUI : MonoBehaviour
{
    [Header("Sliders 0-1")]
    [SerializeField] private Slider sliderMaster;
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderSFX;

    [Header("Labels de porcentaje (opcionales)")]
    [SerializeField] private TextMeshProUGUI labelMaster;
    [SerializeField] private TextMeshProUGUI labelMusica;
    [SerializeField] private TextMeshProUGUI labelSFX;

    [Header("Preview")]
    [Tooltip("Si está activo, al mover el slider de SFX suena un click para ayudar a calibrar.")]
    [SerializeField] private bool previewAlCambiarSFX = true;

    private float _ultimaPreviewT = 0f;

    private void OnEnable()
    {
        SincronizarDesdeAudioManager();
        Suscribir();
    }

    private void OnDisable()
    {
        Desuscribir();
    }

    private void SincronizarDesdeAudioManager()
    {
        if (sliderMaster != null) sliderMaster.SetValueWithoutNotify(AudioManager.VolumenMaster);
        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(AudioManager.VolumenMusica);
        if (sliderSFX != null)    sliderSFX.SetValueWithoutNotify(AudioManager.VolumenSFX);
        ActualizarLabels();
    }

    private void Suscribir()
    {
        if (sliderMaster != null)
        {
            sliderMaster.onValueChanged.RemoveListener(OnMasterCambio);
            sliderMaster.onValueChanged.AddListener(OnMasterCambio);
        }
        if (sliderMusica != null)
        {
            sliderMusica.onValueChanged.RemoveListener(OnMusicaCambio);
            sliderMusica.onValueChanged.AddListener(OnMusicaCambio);
        }
        if (sliderSFX != null)
        {
            sliderSFX.onValueChanged.RemoveListener(OnSFXCambio);
            sliderSFX.onValueChanged.AddListener(OnSFXCambio);
        }
    }

    private void Desuscribir()
    {
        if (sliderMaster != null) sliderMaster.onValueChanged.RemoveListener(OnMasterCambio);
        if (sliderMusica != null) sliderMusica.onValueChanged.RemoveListener(OnMusicaCambio);
        if (sliderSFX != null)    sliderSFX.onValueChanged.RemoveListener(OnSFXCambio);
    }

    private void OnMasterCambio(float v)
    {
        AudioManager.VolumenMaster = v;
        ActualizarLabels();
    }

    private void OnMusicaCambio(float v)
    {
        AudioManager.VolumenMusica = v;
        ActualizarLabels();
    }

    private void OnSFXCambio(float v)
    {
        AudioManager.VolumenSFX = v;
        ActualizarLabels();
        if (previewAlCambiarSFX) PreviewSFX();
    }

    private void PreviewSFX()
    {
        // Throttle: no spamear si el usuario arrastra el slider rápido
        if (Time.unscaledTime - _ultimaPreviewT < 0.25f) return;
        _ultimaPreviewT = Time.unscaledTime;
        AudioManager.PlaySFX(AudioManager.SFX.ClickBoton);
    }

    private void ActualizarLabels()
    {
        if (labelMaster != null) labelMaster.text = $"{Mathf.RoundToInt(AudioManager.VolumenMaster * 100)}%";
        if (labelMusica != null) labelMusica.text = $"{Mathf.RoundToInt(AudioManager.VolumenMusica * 100)}%";
        if (labelSFX != null)    labelSFX.text    = $"{Mathf.RoundToInt(AudioManager.VolumenSFX * 100)}%";
    }
}
