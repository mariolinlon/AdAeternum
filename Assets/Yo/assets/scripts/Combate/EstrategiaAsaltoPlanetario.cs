using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Estrategia del combate Tipo 1 — Asalto Planetario, lado alumno.
///
/// Atacante: cada acierto suma energía. Cuando energía está llena, el alumno elige zona
///   manualmente desde el HUD y pulsa "Disparar". El atacante NO dispara automáticamente.
///
/// Defensor: cada acierto recarga el escudo de la flota. Las preguntas son cíclicas y
///   continuas; no hay "defender ataque" manual. Los ataques entrantes se gestionan
///   automáticamente desde el manager autoritativo del líder: cuando un ataque expira,
///   su daño impacta primero al escudo, y solo si el escudo no es suficiente, el sobrante
///   daña la nave.
///
/// El combate termina cuando todas las zonas están a 0 (completado) o la nave a 0 (eliminado).
/// </summary>
public class EstrategiaAsaltoPlanetario : EstrategiaCombate
{
    private EstadoFlotaCombate estadoActual;
    private bool combatePausado = false;
    private bool primeraSyncRecibida = false;

    // Caché del último snapshot recibido para detectar cambios reales vs eco del local.
    private float _ultSnapshotEscudo = -1f;
    private float _ultSnapshotVidaNave = -1f;

    public override void Iniciar(
        SistemaCombateAlumno sistema,
        MotorPreguntas motor,
        ConfigCombatePlaneta config,
        string idPlaneta,
        string nombrePlaneta,
        string idAlumno,
        string idFlota,
        string rolCombate,
        string idSesion)
    {
        base.Iniciar(sistema, motor, config, idPlaneta, nombrePlaneta, idAlumno, idFlota, rolCombate, idSesion);

        // IMPORTANTE: configurar el motor ANTES de suscribirse al listener de Firebase.
        // Si suscribimos primero, el listener fire inmediatamente (con el estado cacheado del
        // documento) y llama a motor.Reanudar() cuando el poolBase aún está vacío → el motor
        // sirve cola vacía y queda pausado sin que nadie lo reactive.

        var pool = sistema.creadorPreguntas != null
            ? sistema.creadorPreguntas.bibliotecaLocal.Where(p => p.idPlaneta == idPlaneta).ToList()
            : new List<Pregunta>();

        if (pool.Count == 0)
        {
            Debug.LogError("[EstrategiaAsaltoPlanetario] El planeta no tiene preguntas — no se puede iniciar.");
            EstaTerminado = true;
            return;
        }

        motor.Configurar(pool, loop: true, onResultado: OnPreguntaResuelta);

        // Música de combate
        AudioManager.PlayMusic(AudioManager.Music.Combate);

        // Ahora sí, suscribimos el listener. El primer fire ya encontrará el motor configurado.
        AulaDataManager.Instance?.EscucharEstadoFlotaCombate(idSesion, idFlota, OnEstadoFlotaActualizado);

        // HUDs: activar cabecera compartida y el del rol propio
        if (sistema.hudCompartido != null) sistema.hudCompartido.Mostrar();
        if (sistema.hudAtacante != null)
        {
            if (rolCombate == "atacante")
            {
                sistema.hudAtacante.Mostrar();
                sistema.hudAtacante.OnDisparar = OnAtacanteDispara;
            }
            else sistema.hudAtacante.Ocultar();
        }
        if (sistema.hudDefensor != null)
        {
            if (rolCombate == "defensor")
            {
                sistema.hudDefensor.Mostrar();
                // El defensor ya no asume ataques manualmente. La lista de ataques es solo informativa.
                sistema.hudDefensor.OnDefender = null;
            }
            else sistema.hudDefensor.Ocultar();
        }

        if (sistema != null) sistema.MostrarEstadoCombate("Preparando combate...");
    }

    public override void Tick(float deltaTime)
    {
        if (EstaTerminado || estadoActual == null) return;

        // Interpolación local: avanzamos timers en el snapshot actual cada frame para
        // que la UI se mueva fluida entre snapshots de Firestore. El listener corrige
        // estos valores cuando llega un nuevo snapshot.
        if (config != null && estadoActual.escudoActual > 0f)
            estadoActual.escudoActual = Mathf.Max(0f, estadoActual.escudoActual - config.tasaDescargaEscudo * deltaTime);

        if (estadoActual.ataquesEntrantes != null)
        {
            foreach (var a in estadoActual.ataquesEntrantes)
                a.tiempoRestante = Mathf.Max(0f, a.tiempoRestante - deltaTime);
        }

        // Refrescar HUDs cada frame con los valores interpolados
        if (sistema != null)
        {
            sistema.hudCompartido?.RefrescarEstado(estadoActual);
            if (rolCombate == "atacante" && sistema.hudAtacante != null)
                sistema.hudAtacante.RefrescarEstado(estadoActual, idAlumno, config);
            if (rolCombate == "defensor" && sistema.hudDefensor != null)
                sistema.hudDefensor.RefrescarAtaques(estadoActual.ataquesEntrantes, config);
        }

        // Atacante: si escudo cae, pausamos motor; si vuelve a subir, reanudamos
        if (rolCombate == "atacante")
        {
            bool deberiaPausar = estadoActual.EscudoCaido;
            if (deberiaPausar && !combatePausado)
            {
                combatePausado = true;
                motor.CancelarPreguntaActual();
                motor.Pausar();
            }
            else if (!deberiaPausar && combatePausado)
            {
                combatePausado = false;
                motor.Reanudar();
            }
        }

        // Fin del combate (zonas/nave) — detección local, no depende del manager autoritativo
        if (estadoActual.estado == "completado" || estadoActual.estado == "eliminado"
            || estadoActual.TodasLasZonasDestruidas
            || estadoActual.NaveDestruida)
        {
            if (estadoActual.estado != "completado" && estadoActual.estado != "eliminado")
            {
                string estadoFinal = estadoActual.NaveDestruida ? "eliminado" : "completado";
                AulaDataManager.Instance?.MarcarEstadoFlotaCombate(idSesion, idFlota, estadoFinal);
            }
            if (!EstaTerminado)
            {
                // Stinger según resultado + volver a música de menú
                bool victoria = estadoActual.TodasLasZonasDestruidas && !estadoActual.NaveDestruida
                                || estadoActual.estado == "completado";
                AudioManager.PlaySFX(victoria ? AudioManager.SFX.Victoria : AudioManager.SFX.Derrota);
                AudioManager.PlayMusic(AudioManager.Music.Menu);
            }
            EstaTerminado = true;
        }
    }

    private void OnEstadoFlotaActualizado(EstadoFlotaCombate estado)
    {
        if (estado == null) { estadoActual = null; return; }

        // Capturar los valores ORIGINALES del snapshot ANTES de modificar nada (para la caché).
        float snapEscudoOriginal = estado.escudoActual;
        float snapVidaNaveOriginal = estado.vidaNave;

        // Preservar el valor local interpolado si el snapshot trae los MISMOS valores que la
        // última vez (señal de que nadie ha tocado ese campo desde el último snapshot, por
        // ejemplo cuando el manager no está activo y el listener fire por otra acción).
        if (estadoActual != null && _ultSnapshotEscudo >= 0f)
        {
            if (Mathf.Approximately(snapEscudoOriginal, _ultSnapshotEscudo))
                estado.escudoActual = estadoActual.escudoActual; // mantén el local interpolado
            if (Mathf.Approximately(snapVidaNaveOriginal, _ultSnapshotVidaNave))
                estado.vidaNave = estadoActual.vidaNave;
        }

        _ultSnapshotEscudo = snapEscudoOriginal;
        _ultSnapshotVidaNave = snapVidaNaveOriginal;

        estadoActual = estado;

        if (!primeraSyncRecibida)
        {
            primeraSyncRecibida = true;
            if (motor != null) motor.Reanudar();
            else Debug.LogError("[EstrategiaAsalto] motor es null al reanudar.");
            sistema?.MostrarEstadoCombate("");
        }

        // Refresco inmediato del HUD del defensor cuando llegan/desaparecen ataques
        if (rolCombate == "defensor" && sistema != null && sistema.hudDefensor != null)
        {
            sistema.hudDefensor.RefrescarAtaques(estado.ataquesEntrantes, config);
        }
    }

    private void OnPreguntaResuelta(Pregunta pregunta, bool correcto, float tiempoRespuesta)
    {
        sistema.RegistrarRespuestaEstrategia(pregunta, correcto, tiempoRespuesta);

        if (!correcto) return;

        if (rolCombate == "defensor")
        {
            AulaDataManager.Instance?.RecargarEscudo(idSesion, idFlota, config.recargaEscudoPorAcierto);
        }
        else if (rolCombate == "atacante")
        {
            AulaDataManager.Instance?.IncrementarEnergiaAtaque(idSesion, idFlota, idAlumno,
                config.energiaPorAcierto, config.energiaAtaqueMaxima);
        }
    }

    /// <summary>
    /// Callback del HUD atacante: el alumno pulsó disparar a la zona X.
    /// esCargado=false → ataque Normal (coste y daño normales); true → Cargado.
    /// </summary>
    private void OnAtacanteDispara(int indiceZona, bool esCargado)
    {
        if (estadoActual == null) return;
        if (indiceZona < 0 || indiceZona >= estadoActual.zonas.Count) return;
        if (estadoActual.zonas[indiceZona].EstaDestruida) return;

        float daño  = esCargado ? config.dañoAtaqueAtacanteCargado : config.dañoAtaqueAtacante;
        float coste = esCargado ? config.costeAtaqueCargado        : config.costeAtaqueNormal;

        AudioManager.PlaySFX(esCargado ? AudioManager.SFX.DisparoCargado : AudioManager.SFX.Disparo);

        AulaDataManager.Instance?.DispararAtaque(idSesion, idFlota, idAlumno, indiceZona, daño, coste);
    }

    public override void OnPreguntaRespondida(Pregunta pregunta, bool correcto, float tiempoRespuesta) { }

    public override void Finalizar()
    {
        AulaDataManager.Instance?.DetenerListenerEstadoFlota(idSesion, idFlota);
        motor?.DetenerYLimpiar();
        if (sistema != null)
        {
            sistema.hudCompartido?.Ocultar();
            sistema.hudAtacante?.Ocultar();
            sistema.hudDefensor?.Ocultar();
        }
    }
}
