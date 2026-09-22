using System;

/// <summary>
/// El "genoma" de una celula: describe como se ve.
///
/// Guardamos indices (numeros enteros) en lugar del Color y el tamanio
/// reales porque el sistema de aprendizaje no trabaja con colores libres,
/// sino con una lista corta de opciones posibles. Aprender sobre 8 opciones
/// es realista; aprender sobre 16 millones de colores RGB no lo es.
/// </summary>
[Serializable]
public struct CellGenome
{
    /// <summary>Posicion dentro del array de colores (0 a 7).</summary>
    public int colorIndex;

    /// <summary>Posicion dentro del array de tamanios (0 a 3).</summary>
    public int sizeIndex;

    public CellGenome(int colorIndex, int sizeIndex)
    {
        this.colorIndex = colorIndex;
        this.sizeIndex = sizeIndex;
    }

    public override string ToString()
    {
        return "color " + colorIndex + ", tamanio " + sizeIndex;
    }
}
