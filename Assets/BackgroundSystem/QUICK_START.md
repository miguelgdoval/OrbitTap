# 🚀 Quick Start - Background System

## ⚡ Inicio Rápido (3 Pasos)

### Paso 1: Generar el Sistema
1. Abre Unity Editor
2. Ve a **`Tools > Background System > Generate Complete System`**
3. Espera a que se generen todos los archivos

### Paso 2: Configurar en tu Escena
1. Abre tu escena de juego (ej: `Game.unity`)
2. Arrastra el prefab `Assets/BackgroundSystem/Prefabs/BackgroundManager.prefab` a la escena
3. En el Inspector del BackgroundManager:
   - Arrastra los 5 presets del folder `Assets/BackgroundSystem/Presets/` al array `Presets`
   - Configura `Default Preset Index` (0 = VoidSpace, 1 = BlueDrift, etc.)

### Paso 3: ¡Jugar!
El fondo se cargará automáticamente al iniciar la escena.

## 🎮 Cambiar Fondos desde Código

```csharp
// Método 1: Usando la API simple
BackgroundSystemAPI.SetPreset("BlueDrift");
BackgroundSystemAPI.SetPreset("NebulaStorm", transitionDuration: 0.8f);

// Método 2: Directamente
BackgroundManager.Instance.SetPreset("CosmicWinds", 1f);
```

## 📋 Presets Disponibles

| Preset | Nombre | Descripción |
|--------|--------|-------------|
| 0 | VoidSpace | Fondo oscuro, pocas estrellas |
| 1 | BlueDrift | Gradiente azul, ambiente tranquilo |
| 2 | NebulaStorm | Nebulosa densa, colores vibrantes |
| 3 | CosmicWinds | Partículas rápidas, movimiento diagonal |
| 4 | SupernovaEcho | Luz pulsante, efecto dramático |

## 🔗 Integración con Dificultad

```csharp
// En tu ObstacleManager o GameManager
void OnDifficultyChanged(int level)
{
    BackgroundSystemAPI.SetPreset(GetPresetName(level), 1f);
}

string GetPresetName(int level)
{
    switch(level)
    {
        case 0: return "VoidSpace";
        case 1: return "BlueDrift";
        case 2: return "NebulaStorm";
        case 3: return "CosmicWinds";
        case 4: return "SupernovaEcho";
        default: return "BlueDrift";
    }
}
```

## ✅ Checklist de Verificación

- [ ] Sistema generado con `Tools > Background System > Generate Complete System`
- [ ] BackgroundManager en la escena
- [ ] 5 presets asignados al array `Presets`
- [ ] `Default Preset Index` configurado
- [ ] Cámara configurada (Orthographic recomendado)
- [ ] Probar en Play Mode

## 🎨 Escena de Demo

Abre `Assets/BackgroundSystem/BackgroundDemoScene.unity` para ver todos los presets en acción.

## 📚 Documentación Completa

Ver `Assets/BackgroundSystem/README.md` para documentación detallada.

