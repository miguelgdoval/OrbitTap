# 🌌 Background System - Sistema Completo de Fondos Dinámicos

## 📋 Descripción

Sistema completo de fondos dinámicos para Unity 2D, optimizado para móvil (resolución vertical 9:16). Incluye parallax, scroll infinito, transiciones suaves y 5 presets predefinidos.

## 🏗️ Arquitectura

```
BackgroundManager (controlador principal)
  ├── Layer 0: Base (gradiente o color sólido, sin movimiento)
  ├── Layer 1: Nebulas (sprites suaves, movimiento LENTO)
  ├── Layer 2: StarsFar (estrellas lejanas, parallax)
  ├── Layer 3: StarsNear (estrellas cercanas, movimiento más rápido)
  └── Layer 4: Particles (star dust, partículas dinámicas)
```

## 📁 Estructura de Carpetas

```
Assets/BackgroundSystem/
├── Scripts/
│   ├── BackgroundLayer.cs          # Componente para cada capa
│   ├── BackgroundManager.cs        # Manager principal
│   ├── BackgroundPreset.cs         # ScriptableObject para presets
│   ├── BackgroundSystemAPI.cs      # API simple para uso externo
│   └── Editor/
│       └── BackgroundSystemGenerator.cs  # Generador automático
├── Layers/                          # (Para sprites de capas)
├── Prefabs/                         # Prefabs generados
├── Materials/                       # Materiales optimizados
├── Textures/                        # Texturas
└── Presets/                         # ScriptableObjects de presets
```

## 🚀 Inicio Rápido

### 1. Generar el Sistema Completo

1. Abre Unity Editor
2. Ve a **`Tools > Background System > Generate Complete System`**
3. Esto generará:
   - ✅ 5 Presets (VoidSpace, BlueDrift, NebulaStorm, CosmicWinds, SupernovaEcho)
   - ✅ Materiales optimizados
   - ✅ Prefabs
   - ✅ Escena de demo

### 2. Usar en tu Escena

1. Arrastra el prefab `BackgroundManager` a tu escena
2. En el Inspector, asigna los 5 presets al array `Presets`
3. Configura `Default Preset Index` (0-4)
4. ¡Listo! El fondo se cargará automáticamente al iniciar

### 3. Cambiar Fondos desde Código

```csharp
// Cambio simple
BackgroundSystemAPI.SetPreset("BlueDrift");

// Cambio con transición personalizada
BackgroundSystemAPI.SetPreset("NebulaStorm", transitionDuration: 0.8f);

// Obtener preset actual
string current = BackgroundSystemAPI.GetCurrentPreset();

// Activar/desactivar capas
BackgroundSystemAPI.SetLayerEnabled(4, false); // Desactivar partículas
```

## 🎨 Presets Disponibles

### 1. Void Space
- Fondo casi negro
- Pocas estrellas
- Nebulosa mínima
- **Uso**: Inicio del juego, dificultad baja

### 2. Blue Drift
- Gradiente azul
- Nebulosa azul suave
- Estrellas más vivas
- **Uso**: Nivel medio, ambiente tranquilo

### 3. Nebula Storm
- Nebulosa densa
- Colores vibrantes
- Movimiento más rápido
- **Uso**: Nivel difícil, acción intensa

### 4. Cosmic Winds
- Partículas más rápidas
- Estrellas diagonales
- Movimiento dinámico
- **Uso**: Nivel muy difícil

### 5. Supernova Echo
- Luz pulsante
- Nebulosas rojizas
- Efecto dramático
- **Uso**: Boss final, momento épico

## 🔧 Características Técnicas

### BackgroundLayer.cs

- ✅ Movimiento horizontal/vertical configurable
- ✅ Parallax (multiplicador configurable)
- ✅ Scroll infinito automático (duplicando sprites)
- ✅ Pulsing (cambiar escala suavemente)
- ✅ Configuración de opacidad
- ✅ Auto-scaling para pantalla móvil (9:16)
- ✅ Random offsets para evitar patrones repetidos
- ✅ Optimizado para móvil (UV scrolling)

### BackgroundManager.cs

- ✅ Sistema de presets (ScriptableObjects)
- ✅ Transiciones suaves entre presets
- ✅ Activación/desactivación de capas
- ✅ API simple y limpia
- ✅ Singleton pattern
- ✅ Auto-inicialización

## 📝 Crear tu Propio Preset

1. Click derecho en Project → `Create > Background System > Preset`
2. Configura todas las capas:
   - Base: Color y opacidad
   - Nebulas: Sprite, velocidad, opacidad, color
   - StarsFar: Sprite, velocidad, parallax, densidad
   - StarsNear: Sprite, velocidad, parallax, densidad
   - Particles: Sprite, velocidad, pulsing, densidad
3. Asigna el preset al BackgroundManager

## 🎮 Integración con el Juego

### Cambiar Fondo según Dificultad

```csharp
public class GameManager : MonoBehaviour
{
    void OnDifficultyChanged(int level)
    {
        switch(level)
        {
            case 0: BackgroundSystemAPI.SetPreset("VoidSpace"); break;
            case 1: BackgroundSystemAPI.SetPreset("BlueDrift"); break;
            case 2: BackgroundSystemAPI.SetPreset("NebulaStorm"); break;
            case 3: BackgroundSystemAPI.SetPreset("CosmicWinds"); break;
            case 4: BackgroundSystemAPI.SetPreset("SupernovaEcho"); break;
        }
    }
}
```

### Cambiar Fondo según Score

```csharp
public class ScoreManager : MonoBehaviour
{
    void OnScoreChanged(float score)
    {
        if (score > 1000)
            BackgroundSystemAPI.SetPreset("SupernovaEcho", 1.5f);
        else if (score > 500)
            BackgroundSystemAPI.SetPreset("CosmicWinds", 1f);
        // etc...
    }
}
```

## 🎨 Personalización

### Agregar tus Propios Sprites

1. Coloca tus sprites en `Assets/BackgroundSystem/Layers/`
2. Abre el preset que quieres modificar
3. Arrastra los sprites a las capas correspondientes:
   - `Nebula Sprite` → Para nebulosas
   - `Stars Far Sprite` → Para estrellas lejanas
   - `Stars Near Sprite` → Para estrellas cercanas
   - `Particle Sprite` → Para partículas

### Configurar Materiales

Los materiales están en `Assets/BackgroundSystem/Materials/`:
- `NebulaMaterial` - Para nebulosas (soft additive)
- `StarMaterial` - Para estrellas (unlit)
- `ParticleMaterial` - Para partículas (unlit)

Puedes modificar estos materiales o crear los tuyos propios.

## ⚙️ Configuración Avanzada

### Ajustar Velocidades de Scroll

En cada preset, puedes ajustar:
- `Nebula Scroll Speed` - Velocidad de nebulosas (0.1-0.5 recomendado)
- `Stars Far Scroll Speed` - Velocidad estrellas lejanas (0.3-0.7)
- `Stars Near Scroll Speed` - Velocidad estrellas cercanas (0.8-1.5)
- `Particle Scroll Speed` - Velocidad partículas (1.0-2.0)

### Ajustar Parallax

En cada preset:
- `Stars Far Parallax` - <1.0 = más lento (más lejano)
- `Stars Near Parallax` - >1.0 = más rápido (más cercano)

### Activar Pulsing

En el preset, marca `Particle Pulsing` y ajusta:
- `Particle Pulse Speed` - Velocidad del pulso (1-3 recomendado)

## 🐛 Troubleshooting

### Los fondos no se ven
- ✅ Verifica que los presets estén asignados al BackgroundManager
- ✅ Verifica que la cámara esté configurada (Orthographic)
- ✅ Verifica Sorting Orders (deben ser negativos: -10 a -6)

### El scroll no funciona
- ✅ Verifica que `Scroll Speed` > 0 en el preset
- ✅ Verifica que `Infinite Scroll` esté habilitado

### Las transiciones no funcionan
- ✅ Verifica que `Transition Duration` > 0
- ✅ Verifica que los presets estén correctamente asignados

### Rendimiento en móvil
- ✅ Usa `UV Scrolling` (habilitado por defecto)
- ✅ Reduce `Sprite Density` si hay lag
- ✅ Desactiva capas que no uses

## 📊 Optimizaciones para Móvil

- ✅ UV Scrolling (más eficiente que Transform)
- ✅ Materiales unlit (menos cálculos)
- ✅ SpriteRenderer simple (sin shaders complejos)
- ✅ Auto-scaling para evitar sprites muy grandes
- ✅ Instancias reutilizadas para scroll infinito

## 🎯 Próximos Pasos

1. Genera el sistema completo con `Tools > Background System > Generate Complete System`
2. Abre la escena de demo `BackgroundDemoScene`
3. Prueba los diferentes presets
4. Integra con tu sistema de dificultad
5. Personaliza con tus propios sprites

## 📞 Soporte

Si tienes problemas:
1. Revisa la consola de Unity para errores
2. Verifica que todos los presets estén asignados
3. Asegúrate de que la cámara esté configurada correctamente
4. Prueba la escena de demo primero

¡Disfruta de tus fondos dinámicos! 🚀

