/// <summary>
/// Tipo de combate del planeta. Por ahora solo se implementa AsaltoPlanetario.
/// La arquitectura admite futuros tipos sin reescribir el flujo de inicio.
/// </summary>
public enum TipoCombate
{
    AsaltoPlanetario = 1,
    ExploracionPura  = 2,
    PvPFlotas        = 3
}
