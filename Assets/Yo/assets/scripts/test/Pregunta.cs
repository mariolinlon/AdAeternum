using System;

[Serializable]
public class Pregunta
{
    public string id;              
    public string idPlaneta;       // Ahora usamos el ID único del planeta, no su nombre
    public string enunciado;       
    public string[] opciones;      
    public int respuestaCorrecta;  
    
    
    public float tiempoLimite = 30f;
    public int puntosPorAcierto = 10;

    public Pregunta(string _id, string _idPlaneta, string _enunciado, string[] _opciones, int _respuestaCorrecta, float _tiempoLimite = 30f, int _puntosPorAcierto = 10)
    {
        this.id = _id;
        this.idPlaneta = _idPlaneta;
        this.enunciado = _enunciado;
        this.opciones = _opciones;
        this.respuestaCorrecta = _respuestaCorrecta;
        this.tiempoLimite = _tiempoLimite;
        this.puntosPorAcierto = _puntosPorAcierto;
    }
}