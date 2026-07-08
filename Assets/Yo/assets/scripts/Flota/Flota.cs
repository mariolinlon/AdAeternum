using System;
using System.Collections.Generic;

[Serializable]
public class Flota
{
    public string id;
    public string nombre;
    public int maxAlumnos;
    public string liderID;         // ID del alumno capitán
    public List<string> alumnos;   // Lista de IDs de los alumnos miembros

    // --- ADN de la Nave Modular ---
    public int idCabina;           // ID del modelo de cabina
    public int idAlas;             // ID del modelo de alas
    public int idMotor;            // ID del modelo de motor
    public string colorHex;        // Color de la nave en formato Hexadecimal

    // Constructor para crear la flota
    public Flota(string _id, string _nombre, int _max)
    {
        this.id = _id;
        this.nombre = _nombre;
        this.maxAlumnos = _max;
        this.liderID = "";
        this.alumnos = new List<string>();

        // Valores iniciales por defecto (Nave básica)
        this.idCabina = 0;
        this.idAlas = 0;
        this.idMotor = 0;
        this.colorHex = "#FFFFFF"; // Blanco por defecto
    }
}