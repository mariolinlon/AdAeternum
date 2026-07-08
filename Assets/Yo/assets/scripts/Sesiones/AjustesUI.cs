using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AjustesUI : MonoBehaviour
{
    public void ClickCerrarSesion()
    {
        // Damos margen al SFX de click (que se reproduce en el AudioSource del
        // AudioManagerScene de esta escena) para que suene entero antes de
        // recargar la escena, ya que LoadScene destruye ese AudioSource y
        // cortaría el sonido a medias.
        StartCoroutine(CerrarSesionConDelay());
    }

    private IEnumerator CerrarSesionConDelay()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        FirebaseManager.Instance.CerrarSesion();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ClickSalirJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
