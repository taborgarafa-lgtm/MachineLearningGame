using UnityEngine;

// Contrato entre el juego y el sistema de aprendizaje. El juego solo conoce
// esta clase, nunca la implementacion concreta, asi que cambiar de algoritmo
// es cambiar el componente en el Inspector sin tocar el resto del codigo.
//
// Es clase abstracta y no interfaz porque Unity no permite arrastrar
// interfaces a los campos del Inspector.
public abstract class LearningManagerBase : MonoBehaviour
{
    // Decide que color y tamanio prueba la proxima celula.
    public abstract CellGenome SelectGenome();

    // survivalRatio va de 0 a 1: fraccion de la ronda que la celula aguanto viva.
    public abstract void RegisterResult(CellGenome genome, float survivalRatio);

    public abstract Color GetColor(int colorIndex);
    public abstract float GetSize(int sizeIndex);

    public abstract int ColorCount { get; }
    public abstract int SizeCount { get; }

    // La llama el CellSpawner despues de reportar todas las celulas de la ronda.
    // Virtual y no abstract para que DummyLearning no tenga que implementarla.
    public virtual void EndRound() { }

    public abstract void ResetLearning();
}
