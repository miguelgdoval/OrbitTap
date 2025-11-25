# Sistema de Fondos Dinámicos - Resumen Completo

## ✅ Archivos Creados

### Scripts Principales
1. **`Assets/Scripts/Visual/BackgroundLayer.cs`**
   - Componente para cada capa del fondo (Base, Nebula, Stars, Particles)
   - Gestiona el scroll mediante UV o transform
   - Optimizado para móvil

2. **`Assets/Scripts/Managers/BackgroundManager.cs`**
   - Manager principal del sistema de fondos
   - Controla qué fondo está activo
   - Maneja transiciones suaves (crossfade)
   - Integrado con el sistema de dificultad

3. **`Assets/Scripts/Editor/BackgroundPrefabGenerator.cs`**
   - Herramienta del editor para generar prefabs automáticamente
   - Crea todos los prefabs con sus capas y sprites dummy
   - Accesible desde `Tools > Generate Background Prefabs`

### Documentación
4. **`Assets/Scripts/Managers/BACKGROUND_INTEGRATION.md`**
   - Guía completa de integración
   - Instrucciones paso a paso
   - Ejemplos de código

5. **`Assets/Scripts/Managers/BACKGROUND_SYSTEM_SUMMARY.md`** (este archivo)
   - Resumen completo del sistema

### Modificaciones
6. **`Assets/Scripts/Managers/ObstacleManager.cs`** (modificado)
   - Integrado con BackgroundManager
   - Actualiza automáticamente el fondo cuando cambia la dificultad

## 📁 Estructura de Carpetas Creada

```
Assets/
├── Art/
│   └── Backgrounds/
│       ├── VoidHorizon/
│       ├── NebulaDrift/
│       ├── CosmicSurge/
│       ├── SolarRift/
│       └── EventHorizon/
│
├── Prefabs/
│   └── Backgrounds/
│       ├── VoidHorizon.prefab
│       ├── NebulaDrift.prefab
│       ├── CosmicSurge.prefab
│       ├── SolarRift.prefab
│       └── EventHorizon.prefab
│
└── Scripts/
    ├── Visual/
    │   └── BackgroundLayer.cs
    ├── Managers/
    │   ├── BackgroundManager.cs
    │   ├── ObstacleManager.cs (modificado)
    │   ├── BACKGROUND_INTEGRATION.md
    │   └── BACKGROUND_SYSTEM_SUMMARY.md
    └── Editor/
        └── BackgroundPrefabGenerator.cs
```

## 🎮 Cómo Usar el Sistema

### Paso 1: Generar los Prefabs
1. Abre Unity Editor
2. Ve a `Tools > Generate Background Prefabs`
3. Espera a que se generen los 5 prefabs
4. Los prefabs estarán en `Assets/Prefabs/Backgrounds/`

### Paso 2: Configurar en la Escena
1. Abre la escena `Game.unity`
2. Crea un GameObject vacío llamado "BackgroundManager"
3. Agrega el componente `BackgroundManager`
4. En el Inspector:
   - Arrastra los 5 prefabs al array `Backgrounds` (en orden: VoidHorizon, NebulaDrift, CosmicSurge, SolarRift, EventHorizon)
   - Ajusta `Transition Duration` si lo deseas (default: 0.75s)

### Paso 3: Verificar Integración
El sistema ya está integrado con `ObstacleManager`. Cuando la dificultad cambie, el fondo se actualizará automáticamente.

## 🔄 Mapeo de Dificultad a Fondos

| ObstacleDifficultyLevel | Fondo | Índice | Descripción |
|-------------------------|-------|--------|-------------|
| Easy (0) | VoidHorizon | 0 | Fondo inicial, más simple |
| Medium (1) | NebulaDrift | 1 | Nebulosa suave |
| Hard (2) | CosmicSurge | 2 | Onda cósmica |
| VeryHard (3) | SolarRift | 3 | Grieta solar |
| Extra (4+) | EventHorizon | 4 | Horizonte de eventos (máxima dificultad) |

## 🎨 Estructura de Cada Prefab

Cada prefab de fondo contiene 4 capas:

```
[NombreFondo]
├── BG_Base (Sorting Order: 0)
│   └── BackgroundLayer (scrollSpeed: variable)
├── BG_Nebula (Sorting Order: 1)
│   └── BackgroundLayer (scrollSpeed: variable)
├── BG_Stars (Sorting Order: 2)
│   └── BackgroundLayer (scrollSpeed: variable)
└── BG_Particles (Sorting Order: 3)
    └── BackgroundLayer (scrollSpeed: variable)
```

### Velocidades de Scroll por Fondo

| Fondo | Base | Nebula | Stars | Particles |
|-------|------|--------|-------|-----------|
| VoidHorizon | 0.5 | 0.3 | 0.8 | 1.0 |
| NebulaDrift | 0.7 | 0.4 | 1.0 | 1.2 |
| CosmicSurge | 0.9 | 0.5 | 1.2 | 1.4 |
| SolarRift | 1.1 | 0.6 | 1.4 | 1.6 |
| EventHorizon | 1.3 | 0.7 | 1.6 | 1.8 |

## 💻 Código Completo

### BackgroundLayer.cs
```csharp
// Ver: Assets/Scripts/Visual/BackgroundLayer.cs
// - Gestiona scroll de capas individuales
// - Soporta UV scrolling (eficiente) y transform scrolling (alternativa)
// - Métodos públicos: SetScrollSpeed(), GetScrollSpeed(), SetStatic(), ResetLayer()
```

### BackgroundManager.cs
```csharp
// Ver: Assets/Scripts/Managers/BackgroundManager.cs
// - Singleton pattern
// - Métodos principales:
//   * SetBackground(int index) - Cambio inmediato
//   * SmoothTransition(int index) - Cambio con fade
//   * UpdateDifficulty(int level) - Cambio según dificultad
//   * UpdateDifficulty(ObstacleDifficultyLevel level) - Cambio según enum
```

## 🔌 Integración con ObstacleManager

El sistema ya está integrado. `ObstacleManager` detecta automáticamente cambios en el nivel de dificultad y actualiza el fondo.

**Código agregado:**
```csharp
// En ObstacleManager.cs
private BackgroundManager backgroundManager;
private ObstacleDifficultyLevel lastDifficultyLevel = ObstacleDifficultyLevel.Easy;

// En Start():
backgroundManager = FindObjectOfType<BackgroundManager>();
if (backgroundManager != null)
{
    ObstacleDifficultyLevel initialLevel = GetCurrentDifficultyLevel();
    backgroundManager.UpdateDifficulty(initialLevel);
    lastDifficultyLevel = initialLevel;
}

// En Update() (después de UpdateDifficulty()):
ObstacleDifficultyLevel currentLevel = GetCurrentDifficultyLevel();
if (currentLevel != lastDifficultyLevel && backgroundManager != null)
{
    backgroundManager.UpdateDifficulty(currentLevel);
    lastDifficultyLevel = currentLevel;
}
```

## 🎯 Uso Manual (Opcional)

Si quieres cambiar el fondo manualmente desde otro script:

```csharp
// Cambio inmediato
BackgroundManager.Instance.SetBackground(2); // CosmicSurge

// Cambio con transición suave
BackgroundManager.Instance.SmoothTransition(3); // SolarRift

// Cambio según dificultad
BackgroundManager.Instance.UpdateDifficulty(ObstacleDifficultyLevel.Hard);
```

## 🖼️ Reemplazar Sprites Dummy

Los prefabs generados incluyen sprites dummy temporales. Para usar tus propios sprites:

1. Crea tus sprites en `Assets/Art/Backgrounds/[NombreFondo]/`
2. Abre el prefab correspondiente
3. Arrastra tus sprites a cada capa:
   - BG_Base → sprite base
   - BG_Nebula → sprite de nebulosa
   - BG_Stars → sprite de estrellas
   - BG_Particles → sprite de partículas
4. Ajusta las propiedades del sprite:
   - **Filter Mode**: Bilinear
   - **Wrap Mode**: Repeat (importante para scroll continuo)
   - **Compression**: None o Low (mejor calidad)

## ⚙️ Personalización

### Ajustar Velocidades de Scroll
1. Abre cualquier prefab de fondo
2. Selecciona una capa (ej: BG_Stars)
3. En el componente `BackgroundLayer`, ajusta `Scroll Speed`

### Cambiar Duración de Transiciones
1. Selecciona el GameObject `BackgroundManager` en la escena
2. En el componente `BackgroundManager`, ajusta `Transition Duration`
3. Edita la curva `Fade Curve` para personalizar la animación

### Desactivar Scroll en una Capa
1. Abre el prefab
2. Selecciona la capa
3. En `BackgroundLayer`, marca `Is Static Layer`

## 🐛 Troubleshooting

### Los fondos no aparecen
- ✅ Verifica que los prefabs estén asignados en `BackgroundManager`
- ✅ Verifica que `BackgroundManager` esté en la escena
- ✅ Verifica que los sprites estén asignados en cada capa

### El scroll no funciona
- ✅ Verifica que `Is Static Layer` esté desmarcado
- ✅ Verifica que `Scroll Speed` sea mayor que 0
- ✅ Si usas UV scrolling, verifica que el material tenga la propiedad `_MainTex`

### Las transiciones no funcionan
- ✅ Verifica que los prefabs tengan componentes `SpriteRenderer`
- ✅ Verifica que `Transition Duration` sea mayor que 0
- ✅ Revisa la consola de Unity para errores

### El fondo no cambia con la dificultad
- ✅ Verifica que `ObstacleManager` tenga referencia a `BackgroundManager`
- ✅ Verifica que `useDifficultyProgression` esté activado en `ObstacleManager`
- ✅ Revisa la consola para ver si hay warnings

## 📊 Rendimiento

- ✅ Usa **UV scrolling** por defecto (más eficiente que transform)
- ✅ Los materiales se instancian automáticamente (no afecta materiales compartidos)
- ✅ Optimizado para móvil usando `SpriteRenderer` en lugar de UI
- ✅ Las capas estáticas no consumen recursos de scroll

## 🎉 ¡Listo!

El sistema está completamente implementado e integrado. Solo necesitas:
1. Generar los prefabs (Tools > Generate Background Prefabs)
2. Configurar BackgroundManager en la escena
3. (Opcional) Reemplazar sprites dummy con tus propios sprites

¡Disfruta de tus fondos dinámicos! 🚀

