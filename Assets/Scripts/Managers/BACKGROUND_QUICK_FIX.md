# 🔧 Solución Rápida: Fondos No Se Ven

## ✅ Pasos para Solucionar

### 1. Asignar Sprites a los Prefabs
1. Abre Unity Editor
2. Ve a **`Tools > Assign Background Sprites`**
3. Haz clic en **"Asignar Sprites a Todos los Prefabs"**
4. Esto asignará automáticamente los sprites de `Assets/Art/Backgrounds/` a las capas de los prefabs

### 2. Verificar que los Sprites Estén Asignados
1. Ve a **`Tools > Assign Background Sprites`**
2. Haz clic en **"Verificar Configuración de Prefabs"**
3. Revisa el reporte en la consola

### 3. Verificar el BackgroundManager
1. Selecciona el GameObject `BackgroundManager` en la escena
2. En el Inspector, verifica que:
   - El array `Backgrounds` tenga 5 prefabs asignados
   - `Transition Duration` sea mayor que 0
   - `Current Background Index` cambie cuando juegas (debería ser 0 al inicio)

### 4. Verificar Sorting Order
Los fondos deben tener Sorting Order negativo para estar detrás de todo:
- BG_Base: -10
- BG_Nebula: -9
- BG_Stars: -8
- BG_Particles: -7

**Para verificar:**
1. Abre cualquier prefab de fondo (ej: `Assets/Prefabs/Backgrounds/VoidHorizon.prefab`)
2. Selecciona cada capa (BG_Base, BG_Nebula, etc.)
3. En el `SpriteRenderer`, verifica que `Sorting Order` sea el correcto

### 5. Verificar Posición y Escala
Los fondos deben:
- Estar en posición Z positiva (más lejos de la cámara, ej: Z = 10)
- Tener escala suficiente para cubrir toda la pantalla

**Solución rápida:**
1. Abre cada prefab de fondo
2. Agrega el componente `BackgroundSetupHelper` al GameObject raíz
3. Marca `Setup On Start` y `Scale To Fit Camera`
4. Ajusta `Z Position` a 10 (o el valor que prefieras)

### 6. Verificar que los Sprites Tengan Configuración Correcta
En el Project window, selecciona cada sprite y verifica:
- **Texture Type**: Sprite (2D and UI)
- **Filter Mode**: Bilinear
- **Wrap Mode**: Repeat (importante para scroll continuo)
- **Pixels Per Unit**: 100 (o el valor que prefieras)

### 7. Verificar en Play Mode
1. Entra en Play Mode
2. Abre la consola (Ctrl+Shift+C / Cmd+Shift+C)
3. Busca mensajes de `BackgroundManager`
4. Deberías ver: `"BackgroundManager: Background changed to index 0"`

### 8. Forzar Activación Manual (Debug)
Si aún no se ven, prueba esto en la consola durante Play Mode:
```csharp
// En la consola de Unity, ejecuta:
BackgroundManager.Instance.SetBackground(0);
```

O crea un script temporal:
```csharp
using UnityEngine;

public class BackgroundDebug : MonoBehaviour
{
    void Start()
    {
        if (BackgroundManager.Instance != null)
        {
            BackgroundManager.Instance.SetBackground(0);
            Debug.Log("Fondo forzado a índice 0");
        }
    }
}
```

## 🔍 Checklist de Verificación

- [ ] Los sprites están asignados a las capas de los prefabs
- [ ] Los prefabs están asignados al BackgroundManager
- [ ] BackgroundManager está en la escena y activo
- [ ] Los Sorting Orders son negativos (BG_Base: -10, etc.)
- [ ] Los sprites tienen Wrap Mode: Repeat
- [ ] Los fondos tienen posición Z positiva
- [ ] Los fondos tienen escala suficiente para cubrir la pantalla
- [ ] No hay errores en la consola de Unity

## 🐛 Problemas Comunes

### "Los fondos no aparecen"
- **Causa**: Sprites no asignados o Sorting Order incorrecto
- **Solución**: Usa `Tools > Assign Background Sprites`

### "Los fondos se ven muy pequeños"
- **Causa**: Escala incorrecta
- **Solución**: Agrega `BackgroundSetupHelper` al prefab raíz

### "Los fondos están delante de otros objetos"
- **Causa**: Sorting Order muy alto
- **Solución**: Asegúrate de que Sorting Order sea negativo (ej: -10)

### "El scroll no funciona"
- **Causa**: `Is Static Layer` está marcado o `Scroll Speed` es 0
- **Solución**: Desmarca `Is Static Layer` y ajusta `Scroll Speed` > 0

### "Las transiciones no funcionan"
- **Causa**: Los prefabs no tienen SpriteRenderers o están mal configurados
- **Solución**: Verifica con `Tools > Assign Background Sprites > Verificar`

## 📞 Si Nada Funciona

1. Verifica la consola de Unity para errores
2. Asegúrate de que los prefabs estén correctamente instanciados
3. Verifica que la cámara esté configurada correctamente (Orthographic)
4. Prueba desactivar y reactivar el BackgroundManager en la escena

