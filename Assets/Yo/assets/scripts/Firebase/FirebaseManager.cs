using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    [Header("Referencias Firebase")]
    public FirebaseAuth auth;
    public FirebaseFirestore db;
    public FirebaseUser usuarioActual;

    private static FirebaseApp _appInstance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InicializarFirebase();
        }
        else { Destroy(gameObject); }
    }

    void InicializarFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.Result != DependencyStatus.Available)
            {
                Debug.LogError($"Firebase Error: {(task.IsFaulted ? task.Exception?.Message : task.Result.ToString())}");
                return;
            }

            // Unique app name per OS process prevents credential-file conflicts
            // when two builds run simultaneously on the same machine.
            if (_appInstance == null)
            {
                string appName = "ada_" + System.Diagnostics.Process.GetCurrentProcess().Id;
                try
                {
                    _appInstance = FirebaseApp.Create(FirebaseApp.DefaultInstance.Options, appName);
                }
                catch
                {
                    _appInstance = FirebaseApp.DefaultInstance;
                }
            }

            auth = FirebaseAuth.GetAuth(_appInstance);
            db   = FirebaseFirestore.GetInstance(_appInstance);
            Debug.Log("<color=green>Firebase: Conexión establecida correctamente.</color>");
        });
    }

    // --- AUTENTICACIÓN ---

    public void RegistrarUsuario(string nombre, string password, System.Action<bool, string> callback)
    {
        if (auth == null) { callback(false, "Conectando con el servidor… espera unos segundos e inténtalo de nuevo."); return; }
        string email = nombre.Trim().Replace(" ", "").ToLower() + "@adaeternum.com";
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("[Registro] Falló CreateUser: " + task.Exception);
                callback(false, TraducirErrorAuth(task.Exception));
            }
            else callback(true, "");
        });
    }

    public void LoginUsuario(string nombre, string password, System.Action<bool, string> callback)
    {
        if (auth == null) { callback(false, "Conectando con el servidor… espera unos segundos e inténtalo de nuevo."); return; }
        string email = nombre.Trim().Replace(" ", "").ToLower() + "@adaeternum.com";
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                // OJO: antes esto mostraba "usuario/contraseña incorrectos" para CUALQUIER
                // fallo (red, reloj del sistema, etc.), ocultando la causa real.
                Debug.LogError("[Login] Falló SignIn: " + task.Exception);
                callback(false, TraducirErrorAuth(task.Exception));
            }
            else if (task.IsCanceled)
            {
                callback(false, "Inicio de sesión cancelado.");
            }
            else
            {
                usuarioActual = task.Result.User;
                callback(true, "");
            }
        });
    }

    /// <summary>Convierte una excepción de Firebase Auth en un mensaje claro para el
    /// usuario, distinguiendo credenciales incorrectas de problemas de red/servidor.</summary>
    private string TraducirErrorAuth(System.AggregateException agg)
    {
        if (agg != null)
        {
            foreach (var e in agg.Flatten().InnerExceptions)
            {
                if (e is FirebaseException fbEx)
                {
                    switch ((AuthError)fbEx.ErrorCode)
                    {
                        case AuthError.WrongPassword:
                        case AuthError.InvalidEmail:
                        case AuthError.UserNotFound:
                        case AuthError.MissingPassword:
                            return "Usuario o contraseña incorrectos.";
                        case AuthError.NetworkRequestFailed:
                            return "Sin conexión con el servidor. Revisa la conexión a Internet y la fecha/hora del equipo.";
                        case AuthError.UserDisabled:
                            return "Esta cuenta está deshabilitada.";
                        case AuthError.TooManyRequests:
                            return "Demasiados intentos. Espera un momento e inténtalo de nuevo.";
                        case AuthError.EmailAlreadyInUse:
                            return "Ese usuario ya está registrado.";
                        case AuthError.WeakPassword:
                            return "La contraseña es demasiado débil (mínimo 6 caracteres).";
                        default:
                            return "Error de autenticación [" + ((AuthError)fbEx.ErrorCode) + "]: " + fbEx.Message;
                    }
                }
            }
            return "Error: " + agg.Flatten().InnerExceptions[0].Message;
        }
        return "No se pudo completar la operación.";
    }

    // --- GESTIÓN DE AULAS (FIRESTORE) ---

    public void VerificarYCrearAula(string nombreAula, string codigo, System.Action<bool, string> callback)
    {
        if (usuarioActual == null) { callback(false, "No hay sesión activa."); return; }

        db.Collection("artifacts").Document("adaeternum").Collection("public").Document("data").Collection("Aulas")
          .WhereEqualTo("profesorId", usuarioActual.UserId)
          .WhereEqualTo("nombreAula", nombreAula)
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted) { callback(false, "Error al verificar aula."); return; }
              if (task.Result.Count > 0)
                  callback(false, "Ya tienes un aula con ese nombre.");
              else
                  GuardarAulaEnNube(nombreAula, codigo, callback);
          });
    }

    private void GuardarAulaEnNube(string nombreAula, string codigo, System.Action<bool, string> callback)
    {
        DocumentReference docRef = db.Collection("artifacts").Document("adaeternum")
                                     .Collection("public").Document("data")
                                     .Collection("Aulas").Document(codigo);

        Dictionary<string, object> datos = new Dictionary<string, object>
        {
            { "nombreAula", nombreAula },
            { "codigoClase", codigo },
            { "profesorId", usuarioActual.UserId },
            { "fechaCreacion", Timestamp.GetCurrentTimestamp() }
        };

        docRef.SetAsync(datos).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) callback(false, "Error al guardar en Firestore.");
            else callback(true, "");
        });
    }

    public void ObtenerAulasProfesor(System.Action<List<Dictionary<string, object>>> callback)
    {
        if (usuarioActual == null) { callback(new List<Dictionary<string, object>>()); return; }

        db.Collection("artifacts").Document("adaeternum").Collection("public").Document("data").Collection("Aulas")
          .WhereEqualTo("profesorId", usuarioActual.UserId)
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              var listaAulas = new List<Dictionary<string, object>>();
              if (!task.IsFaulted && task.IsCompleted)
              {
                  foreach (DocumentSnapshot doc in task.Result.Documents)
                      listaAulas.Add(doc.ToDictionary());
              }
              callback(listaAulas);
          });
    }

    public void CerrarSesion()
    {
        auth?.SignOut();
        usuarioActual = null;
    }
}
