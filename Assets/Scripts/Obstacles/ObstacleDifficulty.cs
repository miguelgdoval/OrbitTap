using UnityEngine;

/// <summary>
/// Define los niveles de dificultad de los obstáculos
/// </summary>
public enum ObstacleDifficultyLevel
{
    Easy = 0,      // Obstáculos simples (DoorFixed, StaticArc)
    Medium = 1,    // Obstáculos con movimiento básico (DoorRandom, OscillatingBarrier, RotatingArc)
    Hard = 2,      // Obstáculos complejos (PulsatingRing, SpiralFragment, ZigzagBarrier)
    VeryHard = 3   // Obstáculos muy complejos (futuros: PhasingObstacle, OrbitingFragments, etc.)
}

/// <summary>
/// Atributo para marcar la dificultad de un obstáculo
/// </summary>
public class ObstacleDifficultyAttribute : System.Attribute
{
    public ObstacleDifficultyLevel Difficulty { get; private set; }
    
    public ObstacleDifficultyAttribute(ObstacleDifficultyLevel difficulty)
    {
        Difficulty = difficulty;
    }
}

/// <summary>
/// Interfaz para que los obstáculos reporten su dificultad
/// </summary>
public interface IObstacleDifficulty
{
    ObstacleDifficultyLevel GetDifficulty();
}

