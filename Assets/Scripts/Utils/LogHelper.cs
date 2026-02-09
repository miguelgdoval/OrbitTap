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
    
    /// <summary>
    /// Sanitiza información sensible en logs (para producción)
    /// Oculta parcialmente Game IDs, API keys, etc.
    /// </summary>
    public static string SanitizeLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        
        // En producción, ocultar información sensible
#if !UNITY_EDITOR
        // Ocultar Game IDs completos (mostrar solo últimos 3 dígitos)
        message = System.Text.RegularExpressions.Regex.Replace(
            message, 
            @"(\d{4,})", 
            m => m.Value.Length > 6 ? "***" + m.Value.Substring(m.Value.Length - 3) : "***"
        );
        
        // Ocultar posibles API keys (patrones comunes)
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"AIza[0-9A-Za-z_-]{35}",
            "AIza***"
        );
        
        // Ocultar posibles tokens
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"[A-Za-z0-9]{32,}",
            m => m.Value.Length > 40 ? "***" + m.Value.Substring(m.Value.Length - 4) : "***"
        );
#endif
        
        return message;
    }
    
    /// <summary>
    /// Log seguro que sanitiza información sensible automáticamente
    /// </summary>
    public static void LogSafe(object message)
    {
        string sanitized = SanitizeLogMessage(message?.ToString() ?? "");
        Log(sanitized);
    }
    
    /// <summary>
    /// Log de error seguro que sanitiza información sensible
    /// </summary>
    public static void LogErrorSafe(object message)
    {
        string sanitized = SanitizeLogMessage(message?.ToString() ?? "");
        LogError(sanitized);
    }
}

