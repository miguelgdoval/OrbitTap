# Integración del Sistema de Fondos Dinámicos

## Resumen
Este documento explica cómo integrar el sistema de fondos dinámicos con el GameManager y el sistema de dificultad existente.

## Estructura del Sistema

### Archivos Creados
- `Assets/Scripts/Visual/BackgroundLayer.cs` - Componente para cada capa del fondo
- `Assets/Scripts/Managers/BackgroundManager.cs` - Manager principal del sistema
- `Assets/Scripts/Editor/BackgroundPrefabGenerator.cs` - Herramienta para generar prefabs

### Carpetas Creadas
- `Assets/Art/Backgrounds/` - Contiene subcarpetas para cada tipo de fondo
- `Assets/Prefabs/Backgrounds/` - Contiene los prefabs de cada fondo

## Mapeo de Dificultad a Fondos

| Nivel de Dificultad | Fondo | Índice |
|---------------------|-------|--------|
| Easy (0) | VoidHorizon | 0 |
| Medium (1) | NebulaDrift | 1 |
| Hard (2) | CosmicSurge | 2 |
| VeryHard (3) | SolarRift | 3 |
| Extra (4+) | EventHorizon | 4 |

## Pasos de Integración

### 1. Generar los Prefabs
1. Abre Unity Editor
2. Ve a `Tools > Generate Background Prefabs`
3. Esto creará automáticamente todos los prefabs en `Assets/Prefabs/Backgrounds/`

### 2. Configurar BackgroundManager en la Escena
1. Crea un GameObject vacío llamado "BackgroundManager" en la escena Game
2. Agrega el componente `BackgroundManager`
3. En el Inspector, arrastra los 5 prefabs de backgrounds al array `Backgrounds`:
   - VoidHorizon
   - NebulaDrift
   - CosmicSurge
   - SolarRift
   - EventHorizon
4. Ajusta `Transition Duration` si lo deseas (default: 0.75s)

### 3. Integrar con ObstacleManager

Abre `Assets/Scripts/Managers/ObstacleManager.cs` y agrega:

```csharp
private BackgroundManager backgroundManager;

private void Start()
{
    // ... código existente ...
    
    // Buscar BackgroundManager
    backgroundManager = FindObjectOfType<BackgroundManager>();
    if (backgroundManager == null)
    {
        Debug.LogWarning("ObstacleManager: BackgroundManager not found!");
    }
}

private ObstacleDifficultyLevel GetCurrentDifficultyLevel()
{
    // ... código existente del método ...
    
    ObstacleDifficultyLevel level = /* ... tu lógica actual ... */;
    
    // Actualizar fondo según dificultad
    if (backgroundManager != null)
    {
        backgroundManager.UpdateDifficulty(level);
    }
    
    return level;
}
```

### 4. Integrar con GameManager (Opcional)

Si quieres que el fondo cambie también cuando aumenta la velocidad general:

```csharp
// En GameManager.cs
private BackgroundManager backgroundManager;

private void Start()
{
    // ... código existente ...
    backgroundManager = FindObjectOfType<BackgroundManager>();
}

private void IncreaseDifficulty()
{
    // ... código existente ...
    
    // Calcular nivel de dificultad basado en velocidad
    // (ajusta según tu lógica)
    if (backgroundManager != null && player != null)
    {
        float speedRatio = player.angularSpeed / initialSpeed;
        int difficultyLevel = Mathf.FloorToInt(speedRatio);
        backgroundManager.UpdateDifficulty(difficultyLevel);
    }
}
```

### 5. Integrar con ScoreManager (Opcional)

Si quieres que el fondo cambie según el score:

```csharp
// En ScoreManager.cs o donde manejes el score
private BackgroundManager backgroundManager;

private void Start()
{
    backgroundManager = FindObjectOfType<BackgroundManager>();
}

// Cuando el score cambie significativamente
private void OnScoreChanged(float newScore)
{
    if (backgroundManager != null)
    {
        // Mapear score a nivel de dificultad
        int level = Mathf.FloorToInt(newScore / 20f); // Ajusta según tu juego
        backgroundManager.UpdateDifficulty(level);
    }
}
```

## Uso Manual

### Cambiar Fondo Inmediatamente
```csharp
BackgroundManager.Instance.SetBackground(2); // Cambia a CosmicSurge
```

### Cambiar Fondo con Transición Suave
```csharp
BackgroundManager.Instance.SmoothTransition(3); // Cambia a SolarRift con fade
```

### Cambiar Según Dificultad
```csharp
BackgroundManager.Instance.UpdateDifficulty(ObstacleDifficultyLevel.Hard);
// o
BackgroundManager.Instance.UpdateDifficulty(2);
```

## Personalización

### Velocidades de Scroll por Capa
Cada capa en los prefabs tiene su propia velocidad configurable:
- **BG_Base**: Velocidad base (más lenta)
- **BG_Nebula**: Velocidad media
- **BG_Stars**: Velocidad rápida
- **BG_Particles**: Velocidad muy rápida

Puedes ajustar estas velocidades en el Inspector de cada prefab.

### Crear Sprites Personalizados
1. Crea tus sprites en `Assets/Art/Backgrounds/[NombreFondo]/`
2. Arrastra los sprites a las capas correspondientes en el prefab
3. Asegúrate de que los sprites tengan:
   - Formato: RGBA32
   - Compression: None o Low (para mejor calidad)
   - Filter Mode: Bilinear
   - Wrap Mode: Repeat (importante para el scroll)

### Ajustar Transiciones
En `BackgroundManager`:
- `Transition Duration`: Duración de la transición (0.5-1.0s recomendado)
- `Fade Curve`: Curva de animación del fade (puedes editarla en el Inspector)

## Notas de Rendimiento

- El sistema usa **UV scrolling** por defecto, que es más eficiente que mover transforms
- Si tienes problemas de rendimiento, puedes desactivar capas estáticas con `isStaticLayer = true`
- Los materiales se instancian automáticamente para no afectar materiales compartidos
- El sistema está optimizado para móvil usando SpriteRenderer en lugar de UI Image

## Troubleshooting

### Los fondos no se ven
- Verifica que los prefabs tengan sprites asignados
- Verifica que las capas tengan el componente `BackgroundLayer`
- Verifica que los Sorting Orders estén correctos (Base: 0, Nebula: 1, Stars: 2, Particles: 3)

### El scroll no funciona
- Verifica que `isStaticLayer` esté en `false`
- Verifica que `scrollSpeed` sea mayor que 0
- Si usas UV scrolling, asegúrate de que el material tenga la propiedad `_MainTex`

### Las transiciones no funcionan
- Verifica que `BackgroundManager` esté en la escena
- Verifica que los prefabs estén asignados en el array `backgrounds`
- Verifica que los prefabs tengan componentes `SpriteRenderer` o `CanvasGroup`

