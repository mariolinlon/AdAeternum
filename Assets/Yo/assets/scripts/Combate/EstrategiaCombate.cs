using UnityEngine;

/// <summary>
/// Contrato base para todas las estrategias de combate.
/// Cada TipoCombate (AsaltoPlanetario, ExploracionPura, PvPFlotas) implementa una.
/// SistemaCombateAlumno la instancia y delega la mecánica.
/// </summary>
public abstract class EstrategiaCombate
{
    protected SistemaCombateAlumno sistema;
    protected MotorPreguntas motor;
    protected ConfigCombatePlaneta config;
    protected string idPlaneta;
    protected string nombrePlaneta;
    protected string idAlumno;
    protected string idFlota;
    protected string rolCombate;
    protected string idSesion;

    public bool EstaTerminado { get; protected set; }

    public virtual void Iniciar(
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
        this.sistema = sistema;
        this.motor = motor;
        this.config = config;
        this.idPlaneta = idPlaneta;
        this.nombrePlaneta = nombrePlaneta;
        this.idAlumno = idAlumno;
        this.idFlota = idFlota;
        this.rolCombate = rolCombate;
        this.idSesion = idSesion;
        this.EstaTerminado = false;
    }

    /// <summary>Tick periódico (deltaTime). Lo invoca SistemaCombateAlumno desde Update.</summary>
    public virtual void Tick(float deltaTime) { }

    /// <summary>Llamado por MotorPreguntas cuando una pregunta se ha resuelto.</summary>
    public abstract void OnPreguntaRespondida(Pregunta pregunta, bool correcto, float tiempoRespuesta);

    /// <summary>Llamado por SistemaCombateAlumno cuando termina el combate (por la condición de la estrategia o por el profesor).</summary>
    public virtual void Finalizar() { }
}
