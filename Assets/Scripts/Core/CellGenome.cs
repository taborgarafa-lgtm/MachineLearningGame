using System;

// Describe como se ve una celula. Guardamos indices y no el Color y el
// tamanio reales porque el aprendizaje trabaja sobre una lista corta de
// opciones: 8 colores posibles se pueden aprender, 16 millones no.
[Serializable]
public struct CellGenome
{
    public int colorIndex;
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
