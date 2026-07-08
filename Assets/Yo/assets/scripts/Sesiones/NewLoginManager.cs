using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class NewLoginManager : MonoBehaviour
{


[Header("Paneles de Navegación")]
    public GameObject panelloginalumno;
    public GameObject panelRegistroalumno;
    public GameObject panelcodigoaula;
    public GameObject panelloginprofesor;
    public GameObject panelRegistroProfesor;
    public GameObject panelSeleccionAula;
    public GameObject panelinicioprofesor;



 [Header("Inputs fields")]

    //alumno login
    
    public TMP_InputField inputUseralumnoL; 
    public TMP_InputField inputPassAlumnoL;

    //alumno registro

    public TMP_InputField inputUseralumnoR; 
    public TMP_InputField inputPass1AlumnoR;
    public TMP_InputField inputPass2AlumnoR;

    //alumno codigoaula

    public TMP_InputField inputCodigoAula; 

    //profesor login
    public TMP_InputField inputUserProfesorL;
    public TMP_InputField inputPassProfesorL; 
    public TMP_InputField inputCodigoAcasoProfesor; 

    //profesor registro

    public TMP_InputField inputUserProfesorR;
    public TMP_InputField inputPass1ProfesorR; 
    public TMP_InputField inputPass2ProfesorR; 

    //profesor seleccionaula
    
    public TMP_InputField inputNombreNuevaAula; 
    
    
 [Header("UI ScrollView (Listado de Aulas)")]
    public Transform contenedorListaAulas;
    public GameObject prefabBotonAula;
    public TextMeshProUGUI textoerror;

 [Header("HUD Profesor")]
    public TextMeshProUGUI textoCodigoClaseHUD;



// funciones para el login con los botones

public void BotonLoginProfesor()
{
    if (inputUserProfesorL.text.Contains(" ") || inputPassProfesorL.text.Contains(" "))
    {
        textoerror.text = "El usuario y la contraseña no pueden contener espacios.";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    if (inputCodigoAcasoProfesor.text != "PRF")
    {
        textoerror.text ="Código de profesor incorrecto.";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    FirebaseManager.Instance.LoginUsuario(inputUserProfesorL.text, inputPassProfesorL.text, (exito, mensaje) => {
        if (exito)
        {
            panelloginprofesor.SetActive(false);
            panelSeleccionAula.SetActive(true);
            textoerror.text = "";
            ActualizarListaAulasProfesor();
        }
        else
        {
            
            textoerror.text ="Usuario o Contraseña incorrectos.";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        }
    });
}
public void BotonRegistroProfesor()
{
    if (inputUserProfesorR.text.Contains(" ") || inputPass1ProfesorR.text.Contains(" "))
    {
        textoerror.text = "El usuario y la contraseña no pueden contener espacios.";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    if (inputPass1ProfesorR.text != inputPass2ProfesorR.text)
    {
        textoerror.text ="las contraseñas no coinciden";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    FirebaseManager.Instance.RegistrarUsuario(inputUserProfesorR.text, inputPass1ProfesorR.text, (exito, mensaje) => {
        if (exito)
        {
            Debug.Log("<color=green>Profesor registrado correctamente.</color>");
            panelloginprofesor.SetActive(true);
            panelRegistroProfesor.SetActive(false);
            textoerror.text ="";
        }
        else
        {
            textoerror.text ="Usuario ya registrado o no valido";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        }
    });
}
public void BotonLoginAlumno()
{
    if (inputUseralumnoL.text.Contains(" ") || inputPassAlumnoL.text.Contains(" "))
    {
        textoerror.text = "El usuario y la contraseña no pueden contener espacios.";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    FirebaseManager.Instance.LoginUsuario(inputUseralumnoL.text, inputPassAlumnoL.text, (exito, mensaje) => {
        if (exito)
        {
            Debug.Log("<color=cyan>Login de Alumno exitoso.</color>");
            
            panelloginalumno.SetActive(false);
            panelcodigoaula.SetActive(true);
            textoerror.text ="";
        }
        else
        {
            textoerror.text ="Usuario o Contraseña incorrectos.";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        }
    });
}
public void BotonRegistroAlumno()
{
    if (inputUseralumnoR.text.Contains(" ") || inputPass1AlumnoR.text.Contains(" "))
    {
        textoerror.text = "El usuario y la contraseña no pueden contener espacios.";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    if (inputPass1AlumnoR.text != inputPass2AlumnoR.text)
    {
        textoerror.text ="las contraseñas no coinciden";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    FirebaseManager.Instance.RegistrarUsuario(inputUseralumnoR.text, inputPass1AlumnoR.text, (exito, mensaje) => {
        if (exito)
        {
            Debug.Log("<color=green>Alumno registrado correctamente.</color>");
            // 3. Volver al panel de login de alumno tras el registro
            panelloginalumno.SetActive(true);
            panelRegistroalumno.SetActive(false);
            textoerror.text ="";
        }
        else
        {
            textoerror.text ="Usuario ya registrado o no valido";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        }
    });
}

// funciones para crear aula

public void BotonCrearNuevaAula()
{
    // 1. Validar que el campo de nombre no esté vacío
    string nombreAula = inputNombreNuevaAula.text;
    if (string.IsNullOrEmpty(nombreAula))
    {
        textoerror.text ="Nombre de aula vacio";
        AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        return;
    }

    
    string codigoGenerado = GenerarCodigoAula(6);

    
    FirebaseManager.Instance.VerificarYCrearAula(nombreAula, codigoGenerado, (exito, mensaje) => {
        if (exito)
        {
            Debug.Log("<color=green>Aula creada con éxito. Código: " + codigoGenerado + "</color>");
            Toast.Show($"Aula creada. Código: {codigoGenerado}", 5f, Toast.Tipo.Exito);

            // Limpiar el input y podrías refrescar la lista de aulas aquí
            inputNombreNuevaAula.text = "";
            textoerror.text ="";
            
            // Opcional: Llamar a la función de refrescar lista que haremos luego
            // ActualizarListaAulas(); 
        }
        else
        {
            textoerror.text ="ya existe un aula con ese nombre";
            AudioManager.PlaySFX(AudioManager.SFX.ToastError);
        }
    });
}
private string GenerarCodigoAula(int longitud)
{
    const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    char[] resultado = new char[longitud];
    for (int i = 0; i < longitud; i++)
    {
        resultado[i] = caracteres[Random.Range(0, caracteres.Length)];
    }
    return new string(resultado);
}

// funcion para cargar los botones de las aulas del profesor

public void ActualizarListaAulasProfesor()
{
    // 1. Limpiar el contenedor antes de cargar las nuevas aulas
    foreach (Transform child in contenedorListaAulas)
    {
        Destroy(child.gameObject);
    }

    // 2. Pedir al manager que busque las aulas de ESTE profesor
    FirebaseManager.Instance.ObtenerAulasProfesor((listaAulas) => 
    {
        foreach (var datosAula in listaAulas)
        {
            // 3. Instanciar el prefab del botón
            GameObject nuevoBoton = Instantiate(prefabBotonAula, contenedorListaAulas);
            
            // 4. Obtener el nombre del aula desde el diccionario de datos
            string nombre = datosAula["nombreAula"].ToString();
            string codigo = datosAula["codigoClase"].ToString();
            
            // 5. Configurar el texto del botón (asegúrate de que tu prefab tenga un componente TMP_Text)
            nuevoBoton.GetComponentInChildren<TMPro.TMP_Text>().text = nombre;

            // 6. Añadir funcionalidad al botón (opcional, para seleccionar el aula)
            nuevoBoton.GetComponent<Button>().onClick.AddListener(() => {
                AudioManager.PlaySFX(AudioManager.SFX.Login);
                AulaDataManager.Instance.SetCodigoAula(codigo);
                AulaDataManager.Instance.SincronizarEstadoCombate();
                PlanetSpawner.Instance?.CargarPlanetasDesdeNube();
                panelSeleccionAula.SetActive(false);
                panelinicioprofesor.SetActive(true);
                FindFirstObjectByType<ControlCombateProfesor>()?.IniciarEscucha();
                FindFirstObjectByType<HistorialProfesorUI>()?.IniciarHistorial();

                if (textoCodigoClaseHUD != null)
                {
                    textoCodigoClaseHUD.text = "Código: " + codigo;
                    textoCodigoClaseHUD.gameObject.SetActive(true);
                }

                FindFirstObjectByType<ListaAlumnosUI>()?.UpdateList();
                FindFirstObjectByType<PanelControlFlotas>()?.ReiniciarEscucha();
                FindFirstObjectByType<ProgresoGlobalUI>()?.IniciarProgreso();
            });
        }
    });
}




}
