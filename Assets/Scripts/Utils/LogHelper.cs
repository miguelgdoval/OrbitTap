using UnityEngine;

/// <summary>
/// Helper centralizado para logging que automáticamente desactiva logs en builds de producción
/// El código de debug NO se compila en producción, reduciendo el tamaño del build
/// </summary>
public static class LogHelper
{
    // Cambiar a false para desactivar TODOS los logs (incluso en editor)
    private static bool enableLogs = true;
    
    /// <summary>
    /// Log de debug (solo se compila en editor, NO en producción)
    /// </summary>
#if UNITY_EDITOR
    public static void Log(object message)
    {
        if (enableLogs)
        {
            Debug.Log(message);
        }
    }
    
    /// <summary>
    /// Log de debug con contexto (solo se compila en editor, NO en producción)
    /// </summary>
    public static void Log(object message, Object context)
    {
        if (enableLogs)
        {
            Debug.Log(message, context);
        }
    }
#else
    // En producción, estos métodos están vacíos y el compilador los elimina completamente
    [System.Diagnostics.Conditional("FALSE")]
    public static void Log(object message) { }
    
    [System.Diagnostics.Conditional("FALSE")]
    public static void Log(object message, Object context) { }
#endif
    
    /// <summary>
    /// Log de warning (siempre activo, útil para problemas que no son críticos)
    /// </summary>
    public static void LogWarning(object message)
    {
        if (enableLogs)
        {
            Debug.LogWarning(message);
        }
    }
    
    /// <summary>
    /// Log de warning con contexto
    /// </summary>
    public static void LogWarning(object message, Object context)
    {
        if (enableLogs)
        {
            Debug.LogWarning(message, context);
        }
    }
    
    /// <summary>
    /// Log de error (siempre activo, crítico)
    /// </summary>
    public static void LogError(object message)
    {
        if (enableLogs)
        {
            Debug.LogError(message);
        }
    }
    
    /// <summary>
    /// Log de error con contexto
    /// </summary>
    public static void LogError(object message, Object context)
    {
        if (enableLogs)
        {
            Debug.LogError(message, context);
        }
    }
    
    /// <summary>
    /// Desactiva todos los logs (útil para testing de rendimiento)
    /// </summary>
    public static void DisableLogs()
    {
        enableLogs = false;
    }
    
    /// <summary>
    /// Activa todos los logs
    /// </summary>
    public static void EnableLogs()
    {
        enableLogs = true;
    }
}

