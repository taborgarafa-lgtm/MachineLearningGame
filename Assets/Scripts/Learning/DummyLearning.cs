using UnityEngine;

// Implementacion provisional: elige todo al azar y no aprende nada.
// Se uso durante el desarrollo para construir y probar el juego completo
// mientras el sistema real todavia no estaba listo. Se conserva porque
// demuestra que el juego no depende de ninguna implementacion concreta:
// basta con cambiar el componente en el Inspector.
public class DummyLearning : LearningManagerBase
{
    public Color[] colors = new Color[]
    {
        new Color(0.85f, 0.35f, 0.35f),
        new Color(0.85f, 0.60f, 0.30f),
        new Color(0.85f, 0.85f, 0.35f),
        new Color(0.40f, 0.75f, 0.40f),
        new Color(0.30f, 0.70f, 0.65f),
        new Color(0.35f, 0.50f, 0.85f),
        new Color(0.60f, 0.40f, 0.85f),
        new Color(0.85f, 0.40f, 0.65f)
    };

    public float[] sizes = new float[] { 0.5f, 0.75f, 1.0f, 1.25f };

    public override int ColorCount => colors.Length;
    public override int SizeCount => sizes.Length;

    public override CellGenome SelectGenome()
    {
        return new CellGenome(
            Random.Range(0, colors.Length),
            Random.Range(0, sizes.Length));
    }

    public override void RegisterResult(CellGenome genome, float survivalRatio) { }

    public override Color GetColor(int colorIndex)
    {
        return colors[Mathf.Clamp(colorIndex, 0, colors.Length - 1)];
    }

    public override float GetSize(int sizeIndex)
    {
        return sizes[Mathf.Clamp(sizeIndex, 0, sizes.Length - 1)];
    }

    public override void ResetLearning() { }
}
