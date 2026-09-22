using UnityEngine;

/// <summary>
/// CONTRATO entre el juego y el sistema de aprendizaje.
///
/// El resto del juego solo conoce esta clase, nunca la implementacion
/// concreta. Eso permite que Aaron construya todo el juego usando
/// DummyLearning (valores al azar) mientras Grover programa el
/// LearningManager real. Al final solo se cambia el componente que
/// esta arrastrado en el Inspector: ni una linea de codigo del juego.
///
/// NOTA: es una clase abstracta y no una interfaz porque Unity no
/// permite arrastrar interfaces a los campos del Inspector.
/// </summary>
public abstract class LearningManagerBase : MonoBehaviour
{
    /// <summary>
    /// Decide que color y que tamanio va a probar la proxima celula.
    /// Aqui es donde vive la politica de exploracion vs explotacion.
    /// </summary>
    public abstract CellGenome SelectGenome();

    /// <summary>
    /// Informa el resultado de una celula al terminar la ronda.
    /// survivalRatio va de 0 a 1: es la fraccion de la ronda que aguanto viva.
    /// 1.0 = nunca la eliminaron. 0.3 = la mataron al 30% de la ronda.
    /// </summary>
    public abstract void RegisterResult(CellGenome genome, float survivalRatio);

    /// <summary>Traduce un indice de color al Color real.</summary>
    public abstract Color GetColor(int colorIndex);

    /// <summary>Traduce un indice de tamanio a la escala real.</summary>
    public abstract float GetSize(int sizeIndex);

    /// <summary>Cuantas opciones de color hay disponibles.</summary>
    public abstract int ColorCount { get; }

    /// <summary>Cuantas opciones de tamanio hay disponibles.</summary>
    public abstract int SizeCount { get; }

    /// <summary>
    /// Cierra la ronda. La llama el CellSpawner DESPUES de haber reportado
    /// el resultado de todas las celulas. Aqui el sistema real hace decaer
    /// epsilon y guarda las estadisticas de la ronda.
    ///
    /// Es virtual y no abstract para que DummyLearning no tenga que
    /// implementarla: por defecto no hace nada.
    /// </summary>
    public virtual void EndRound() { }

    /// <summary>Borra todo lo aprendido. Se llama al empezar una partida nueva.</summary>
    public abstract void ResetLearning();
}
