using UnityEngine;
using System.Runtime.InteropServices;
using static LogHelper;

/// <summary>
/// Manager para compartir puntuaciones en redes sociales
/// </summary>
public class SocialShareManager : MonoBehaviour
{
    public static SocialShareManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Comparte la puntuación actual en redes sociales
    /// </summary>
    public void ShareScore(int score, int highScore, bool isNewRecord = false)
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Jugador");
        
        string message = isNewRecord
            ? $"¡Nuevo récord! 🚀 Conseguí {score} puntos en Starbound Orbit! ¿Puedes superarme? #StarboundOrbit"
            : $"Conseguí {score} puntos en Starbound Orbit! Mi mejor puntuación es {highScore}. ¿Puedes superarme? #StarboundOrbit";
        
        ShareText(message);
    }
    
    /// <summary>
    /// Comparte texto genérico
    /// </summary>
    public void ShareText(string text)
    {
        #if UNITY_ANDROID
        ShareAndroid(text);
        #elif UNITY_IOS
        ShareIOS(text);
        #else
        // Fallback para editor/otras plataformas
        GUIUtility.systemCopyBuffer = text; // Copiar al portapapeles
        Log($"[SocialShareManager] Texto copiado al portapapeles (Editor/Desktop). En Android/iOS se abrirá el selector de compartir.");
        Log($"[SocialShareManager] Texto copiado: {text}");
        #endif
    }
    
    #if UNITY_ANDROID
    private void ShareAndroid(string text)
    {
        try
        {
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
            
            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intentObject.Call<AndroidJavaObject>("setType", "text/plain");
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
            
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Compartir puntuación");
            currentActivity.Call("startActivity", chooser);
            
            Log($"[SocialShareManager] Compartiendo en Android: {text}");
        }
        catch (System.Exception e)
        {
            LogError($"[SocialShareManager] Error al compartir en Android: {e.Message}");
            // Fallback: copiar al portapapeles
            GUIUtility.systemCopyBuffer = text;
        }
    }
    #endif
    
    #if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void _ShareText(string text);
    
    private void ShareIOS(string text)
    {
        try
        {
            _ShareText(text);
            Log($"[SocialShareManager] Compartiendo en iOS: {text}");
        }
        catch (System.Exception e)
        {
            LogError($"[SocialShareManager] Error al compartir en iOS: {e.Message}");
            // Fallback: copiar al portapapeles
            GUIUtility.systemCopyBuffer = text;
        }
    }
    #endif
}

