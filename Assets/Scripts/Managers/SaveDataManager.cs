using UnityEngine;
using System;
using System.Collections;
using System.IO;
using static LogHelper;

/// <summary>
/// Manager centralizado para guardar y cargar datos del juego
/// Incluye sistema de backup automático, validación y recuperación de datos corruptos
/// </summary>
public class SaveDataManager : MonoBehaviour
{
    public static SaveDataManager Instance { get; private set; }
    
    [Header("Save Settings")]
    [Tooltip("Frecuencia de guardado automático (en segundos). 0 = deshabilitado")]
    [SerializeField] private float autoSaveInterval = 30f;
    
    [Tooltip("Número de backups a mantener")]
    [SerializeField] private int maxBackups = 5;
    
    [Tooltip("Habilitar guardado automático")]
    [SerializeField] private bool enableAutoSave = true;
    
    private const string SAVE_DATA_KEY = "GameSaveData";
    private const string BACKUP_PREFIX = "GameSaveData_Backup_";
    private const int CURRENT_SAVE_VERSION = 1;
    
    private SaveData currentSaveData;
    private float timeSinceLastSave = 0f;
    private bool isSaving = false;
    private bool isDirty = false; // Flag para indicar que hay cambios sin guardar
    
    // Eventos
    public System.Action<SaveData> OnSaveDataLoaded;
    public System.Action OnSaveDataSaved;
    public System.Action<string> OnSaveError;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Guardado automático periódico
        if (enableAutoSave && autoSaveInterval > 0 && isDirty)
        {
            timeSinceLastSave += Time.deltaTime;
            if (timeSinceLastSave >= autoSaveInterval)
            {
                SaveDataAsync();
                timeSinceLastSave = 0f;
            }
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isDirty)
        {
            // Guardar cuando la app va a background
            SaveDataSync();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isDirty)
        {
            // Guardar cuando la app pierde el foco
            SaveDataSync();
        }
    }
    
    private void OnDestroy()
    {
        // Guardar al destruir el objeto
        if (isDirty)
        {
            SaveDataSync();
        }
    }
    
    /// <summary>
    /// Inicializa el sistema de guardado
    /// </summary>
    private void InitializeSaveSystem()
    {
        Log("[SaveDataManager] Inicializando sistema de guardado...");
        
        // Asegurar que Time.timeScale esté en 1 (por si acaso)
        if (Time.timeScale == 0f)
        {
            LogWarning("[SaveDataManager] Time.timeScale estaba en 0, restaurando a 1");
            Time.timeScale = 1f;
        }
        
        // Intentar cargar datos existentes
        if (!LoadSaveData())
        {
            // Si no hay datos, crear datos nuevos
            Log("[SaveDataManager] No se encontraron datos guardados, creando datos nuevos");
            currentSaveData = CreateNewSaveData();
            // Guardar de forma asíncrona para no bloquear el inicio
            StartCoroutine(SaveDataAfterFrame());
        }
        
        // Migrar datos antiguos de PlayerPrefs si es necesario (también asíncrono)
        StartCoroutine(MigrateFromPlayerPrefsDelayed());
        
        // Retroalimentar estadísticas desde datos existentes si están vacías
        RetroPopulateStatistics();
        
        Log("[SaveDataManager] Sistema de guardado inicializado");
    }
    
    /// <summary>
    /// Guarda los datos después de un frame (para no bloquear la inicialización)
    /// </summary>
    private IEnumerator SaveDataAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        SaveDataAsync();
    }
    
    /// <summary>
    /// Migra datos de PlayerPrefs después de un frame
    /// </summary>
    private IEnumerator MigrateFromPlayerPrefsDelayed()
    {
        yield return new WaitForEndOfFrame();
        MigrateFromPlayerPrefs();
    }
    
    /// <summary>
    /// Crea datos de guardado nuevos con valores por defecto
    /// </summary>
    private SaveData CreateNewSaveData()
    {
        SaveData newData = new SaveData
        {
            saveVersion = CURRENT_SAVE_VERSION,
            saveDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            highScore = 0,
            lastScore = 0,
            playerName = "Jugador",
            stellarShards = 0,
            cosmicCrystals = 0,
            soundEnabled = true,
            vibrationEnabled = true,
            language = "ES",
            graphicsQuality = 1,
            masterVolume = 1f,
            musicVolume = 1f,
            sfxVolume = 1f,
            colorBlindMode = false,
            highContrastUI = false,
            reduceAnimations = false,
            tutorialEnabled = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1, // Leer de PlayerPrefs si existe, sino true por defecto
            gamesSinceLastAd = 0,
            lastAdTimestamp = 0,
            removeAdsPurchased = false,
            statistics = new PlayerStatistics() // Inicializar estadísticas
        };
        
        Log($"[SaveDataManager] Nuevo SaveData creado con estadísticas inicializadas (tutorialEnabled: {newData.tutorialEnabled})");
        return newData;
    }
    
    /// <summary>
    /// Carga los datos guardados
    /// </summary>
    public bool LoadSaveData()
    {
        try
        {
            // Asegurar que Time.timeScale esté en 1 durante la carga
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
            
            // Intentar cargar datos principales
            if (PlayerPrefs.HasKey(SAVE_DATA_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_DATA_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    currentSaveData = JsonUtility.FromJson<SaveData>(json);
                    
                    if (currentSaveData != null)
                    {
                        // Asegurar que statistics esté inicializado
                        if (currentSaveData.statistics == null)
                        {
                            currentSaveData.statistics = new PlayerStatistics();
                            isDirty = true;
                        }
                        
                        // Sincronizar tutorialEnabled con PlayerPrefs si existe (PlayerPrefs tiene prioridad)
                        // Esto asegura que si el usuario cambió el valor en Settings, se preserve
                        if (PlayerPrefs.HasKey("TutorialEnabled"))
                        {
                            bool playerPrefsValue = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1;
                            if (currentSaveData.tutorialEnabled != playerPrefsValue)
                            {
                                Log($"[SaveDataManager] Sincronizando tutorialEnabled desde PlayerPrefs: {playerPrefsValue} (SaveData tenía: {currentSaveData.tutorialEnabled})");
                                currentSaveData.tutorialEnabled = playerPrefsValue;
                                isDirty = true;
                            }
                        }
                        
                        // Validar y corregir datos
                        bool wasFixed = currentSaveData.ValidateAndFix();
                        if (wasFixed)
                        {
                            LogWarning("[SaveDataManager] Se corrigieron datos inválidos durante la carga");
                            isDirty = true;
                        }
                        
                        // Migrar si es necesario
                        if (currentSaveData.saveVersion < CURRENT_SAVE_VERSION)
                        {
                            MigrateSaveData(currentSaveData);
                        }
                        
                        OnSaveDataLoaded?.Invoke(currentSaveData);
                        Log("[SaveDataManager] Datos cargados exitosamente");
                        return true;
                    }
                }
            }
            
            // Si falla, intentar cargar desde backup
            LogWarning("[SaveDataManager] No se pudieron cargar los datos principales, intentando backup...");
            return LoadFromBackup();
        }
        catch (Exception e)
        {
            LogError($"[SaveDataManager] Error al cargar datos: {e.Message}");
            OnSaveError?.Invoke($"Error al cargar: {e.Message}");
            
            // Intentar cargar desde backup
            return LoadFromBackup();
        }
    }
    
    /// <summary>
    /// Intenta cargar desde un backup
    /// </summary>
    private bool LoadFromBackup()
    {
        for (int i = maxBackups; i >= 1; i--)
        {
            string backupKey = $"{BACKUP_PREFIX}{i}";
            if (PlayerPrefs.HasKey(backupKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(backupKey);
                    if (!string.IsNullOrEmpty(json))
                    {
                        currentSaveData = JsonUtility.FromJson<SaveData>(json);
                        if (currentSaveData != null && currentSaveData.ValidateAndFix())
                        {
                            Log($"[SaveDataManager] Datos restaurados desde backup {i}");
                            isDirty = true; // Marcar como sucio para guardar los datos restaurados
                            OnSaveDataLoaded?.Invoke(currentSaveData);
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogWarning($"[SaveDataManager] Error al cargar backup {i}: {e.Message}");
                }
            }
        }
        
        LogError("[SaveDataManager] No se pudieron cargar datos ni backups, creando datos nuevos");
        return false;
    }
    
    /// <summary>
    /// Guarda los datos de forma síncrona (bloquea el hilo principal)
    /// </summary>
    public void SaveDataSync()
    {
        if (isSaving) return;
        if (currentSaveData == null) return;
        
        isSaving = true;
        
        try
        {
            // Actualizar fecha de guardado
            currentSaveData.saveDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            currentSaveData.saveVersion = CURRENT_SAVE_VERSION;
            
            // Validar antes de guardar
            currentSaveData.ValidateAndFix();
            
            // Crear backup antes de guardar
            CreateBackup();
            
            // Guardar datos principales
            string json = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString(SAVE_DATA_KEY, json);
            PlayerPrefs.Save();
            
            isDirty = false;
            timeSinceLastSave = 0f;
            
            OnSaveDataSaved?.Invoke();
            Log("[SaveDataManager] Datos guardados exitosamente");
        }
        catch (Exception e)
        {
            LogError($"[SaveDataManager] Error al guardar datos: {e.Message}");
            OnSaveError?.Invoke($"Error al guardar: {e.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
    
    /// <summary>
    /// Guarda los datos de forma asíncrona (no bloquea el hilo principal)
    /// </summary>
    public void SaveDataAsync()
    {
        if (isSaving || currentSaveData == null) return;
        
        StartCoroutine(SaveDataCoroutine());
    }
    
    private IEnumerator SaveDataCoroutine()
    {
        isSaving = true;
        
        // Esperar al final del frame para no bloquear
        yield return new WaitForEndOfFrame();
        
        try
        {
            // Actualizar fecha de guardado
            currentSaveData.saveDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            currentSaveData.saveVersion = CURRENT_SAVE_VERSION;
            
            // Validar antes de guardar
            currentSaveData.ValidateAndFix();
            
            // Crear backup antes de guardar
            CreateBackup();
            
            // Guardar datos principales
            string json = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString(SAVE_DATA_KEY, json);
            PlayerPrefs.Save();
            
            isDirty = false;
            timeSinceLastSave = 0f;
            
            OnSaveDataSaved?.Invoke();
            Log("[SaveDataManager] Datos guardados asíncronamente");
        }
        catch (Exception e)
        {
            LogError($"[SaveDataManager] Error al guardar datos asíncronamente: {e.Message}");
            OnSaveError?.Invoke($"Error al guardar: {e.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
    
    /// <summary>
    /// Crea un backup de los datos actuales
    /// </summary>
    private void CreateBackup()
    {
        try
        {
            // Rotar backups (el más antiguo se elimina)
            for (int i = maxBackups; i > 1; i--)
            {
                string oldKey = $"{BACKUP_PREFIX}{i - 1}";
                string newKey = $"{BACKUP_PREFIX}{i}";
                
                if (PlayerPrefs.HasKey(oldKey))
                {
                    string backupData = PlayerPrefs.GetString(oldKey);
                    PlayerPrefs.SetString(newKey, backupData);
                }
            }
            
            // Crear nuevo backup con los datos actuales
            string currentJson = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString($"{BACKUP_PREFIX}1", currentJson);
            
            Log($"[SaveDataManager] Backup creado (manteniendo {maxBackups} backups)");
        }
        catch (Exception e)
        {
            LogWarning($"[SaveDataManager] Error al crear backup: {e.Message}");
        }
    }
    
    /// <summary>
    /// Migra datos de versiones antiguas a la versión actual
    /// </summary>
    private void MigrateSaveData(SaveData data)
    {
        if (data.saveVersion >= CURRENT_SAVE_VERSION)
        {
            return; // Ya está en la versión actual
        }
        
        Log($"[SaveDataManager] Migrando datos de versión {data.saveVersion} a {CURRENT_SAVE_VERSION}");
        
        // Aquí se pueden agregar migraciones específicas por versión
        // Por ejemplo:
        // if (data.saveVersion < 2)
        // {
        //     // Migrar datos de versión 1 a 2
        // }
        
        data.saveVersion = CURRENT_SAVE_VERSION;
        isDirty = true;
    }
    
    /// <summary>
    /// Retroalimenta las estadísticas desde datos existentes del SaveData
    /// cuando las estadísticas están vacías pero hay evidencia de partidas jugadas.
    /// Esto ocurre cuando el sistema de estadísticas se añade a un juego ya existente.
    /// </summary>
    private void RetroPopulateStatistics()
    {
        if (currentSaveData == null) return;
        
        if (currentSaveData.statistics == null)
        {
            currentSaveData.statistics = new PlayerStatistics();
        }
        
        PlayerStatistics stats = currentSaveData.statistics;
        
        // Si ya hay partidas registradas en estadísticas, no hacer nada
        if (stats.totalGamesPlayed > 0) return;
        
        bool populated = false;
        
        // Sincronizar bestScore desde highScore global
        if (currentSaveData.highScore > stats.bestScore)
        {
            stats.bestScore = currentSaveData.highScore;
            populated = true;
        }
        
        // Estimar partidas jugadas desde el leaderboard local
        if (currentSaveData.leaderboardEntries != null && currentSaveData.leaderboardEntries.Count > 0)
        {
            int leaderboardCount = currentSaveData.leaderboardEntries.Count;
            stats.totalGamesPlayed = leaderboardCount;
            
            // Calcular puntuación total y llenar recentScores
            int totalScore = 0;
            stats.recentScores = new System.Collections.Generic.List<int>();
            stats.recentPlayTimes = new System.Collections.Generic.List<float>();
            
            foreach (var entry in currentSaveData.leaderboardEntries)
            {
                totalScore += entry.score;
                stats.recentScores.Add(entry.score);
                stats.recentPlayTimes.Add(0f); // No tenemos datos de tiempo
                
                if (entry.score > stats.bestScore)
                {
                    stats.bestScore = entry.score;
                }
            }
            
            stats.totalScore = totalScore;
            if (stats.totalGamesPlayed > 0)
            {
                stats.averageScore = totalScore / stats.totalGamesPlayed;
            }
            
            populated = true;
        }
        else if (currentSaveData.highScore > 0)
        {
            // No hay leaderboard pero sí hay highScore → al menos 1 partida
            stats.totalGamesPlayed = 1;
            stats.totalScore = currentSaveData.highScore;
            stats.averageScore = currentSaveData.highScore;
            stats.recentScores = new System.Collections.Generic.List<int> { currentSaveData.highScore };
            stats.recentPlayTimes = new System.Collections.Generic.List<float> { 0f };
            populated = true;
        }
        
        // También chequear PlayerPrefs antiguos como fallback
        if (!populated && PlayerPrefs.HasKey("HighScore"))
        {
            int oldHighScore = PlayerPrefs.GetInt("HighScore", 0);
            if (oldHighScore > 0)
            {
                stats.bestScore = oldHighScore;
                stats.totalGamesPlayed = 1;
                stats.totalScore = oldHighScore;
                stats.averageScore = oldHighScore;
                stats.recentScores = new System.Collections.Generic.List<int> { oldHighScore };
                stats.recentPlayTimes = new System.Collections.Generic.List<float> { 0f };
                populated = true;
            }
        }
        
        if (populated)
        {
            Log($"[SaveDataManager] Estadísticas retroalimentadas: {stats.totalGamesPlayed} partidas, bestScore={stats.bestScore}");
            isDirty = true;
        }
    }
    
    /// <summary>
    /// Migra datos antiguos de PlayerPrefs al nuevo sistema
    /// </summary>
    private void MigrateFromPlayerPrefs()
    {
        bool migrated = false;
        
        // Migrar HighScore
        if (PlayerPrefs.HasKey("HighScore") && currentSaveData.highScore == 0)
        {
            int oldHighScore = PlayerPrefs.GetInt("HighScore", 0);
            if (oldHighScore > 0)
            {
                currentSaveData.highScore = oldHighScore;
                migrated = true;
            }
        }
        
        // Migrar LastScore
        if (PlayerPrefs.HasKey("LastScore") && currentSaveData.lastScore == 0)
        {
            int oldLastScore = PlayerPrefs.GetInt("LastScore", 0);
            if (oldLastScore > 0)
            {
                currentSaveData.lastScore = oldLastScore;
                migrated = true;
            }
        }
        
        // Migrar PlayerName
        if (PlayerPrefs.HasKey("PlayerName") && currentSaveData.playerName == "Jugador")
        {
            string oldName = PlayerPrefs.GetString("PlayerName", "Jugador");
            if (oldName != "Jugador")
            {
                currentSaveData.playerName = oldName;
                migrated = true;
            }
        }
        
        // Migrar monedas
        if (PlayerPrefs.HasKey("StellarShards") && currentSaveData.stellarShards == 0)
        {
            int oldShards = PlayerPrefs.GetInt("StellarShards", 0);
            if (oldShards > 0)
            {
                currentSaveData.stellarShards = oldShards;
                migrated = true;
            }
        }
        
        if (PlayerPrefs.HasKey("CosmicCrystals") && currentSaveData.cosmicCrystals == 0)
        {
            int oldCrystals = PlayerPrefs.GetInt("CosmicCrystals", 0);
            if (oldCrystals > 0)
            {
                currentSaveData.cosmicCrystals = oldCrystals;
                migrated = true;
            }
        }
        
        // Migrar settings
        if (PlayerPrefs.HasKey("SoundEnabled"))
        {
            currentSaveData.soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("VibrationEnabled"))
        {
            currentSaveData.vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("Language"))
        {
            currentSaveData.language = PlayerPrefs.GetString("Language", "ES");
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("GraphicsQuality"))
        {
            currentSaveData.graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 1);
            migrated = true;
        }
        
        // Migrar audio volumes
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            currentSaveData.masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            currentSaveData.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            currentSaveData.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            migrated = true;
        }
        
        // Migrar accesibilidad
        if (PlayerPrefs.HasKey("ColorBlindMode"))
        {
            currentSaveData.colorBlindMode = PlayerPrefs.GetInt("ColorBlindMode", 0) == 1;
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("HighContrastUI"))
        {
            currentSaveData.highContrastUI = PlayerPrefs.GetInt("HighContrastUI", 0) == 1;
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("ReduceAnimations"))
        {
            currentSaveData.reduceAnimations = PlayerPrefs.GetInt("ReduceAnimations", 0) == 1;
            migrated = true;
        }
        
        // Migrar tutorial enabled
        if (PlayerPrefs.HasKey("TutorialEnabled"))
        {
            currentSaveData.tutorialEnabled = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1;
            migrated = true;
        }
        
        // Migrar ad tracking
        if (PlayerPrefs.HasKey("GamesSinceLastAd"))
        {
            currentSaveData.gamesSinceLastAd = PlayerPrefs.GetInt("GamesSinceLastAd", 0);
            migrated = true;
        }
        
        if (PlayerPrefs.HasKey("LastAdTimestamp"))
        {
            string timestampStr = PlayerPrefs.GetString("LastAdTimestamp", "0");
            // Intentar parsear como double primero (puede tener decimales) y luego convertir a long
            if (double.TryParse(timestampStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double timestampDouble))
            {
                currentSaveData.lastAdTimestamp = (long)timestampDouble;
                migrated = true;
            }
            else if (long.TryParse(timestampStr, out long timestamp))
            {
                currentSaveData.lastAdTimestamp = timestamp;
                migrated = true;
            }
            else
            {
                LogWarning($"[SaveDataManager] No se pudo parsear LastAdTimestamp: '{timestampStr}', usando 0");
                currentSaveData.lastAdTimestamp = 0;
                migrated = true;
            }
        }
        
        if (PlayerPrefs.HasKey("RemoveAdsPurchased"))
        {
            currentSaveData.removeAdsPurchased = PlayerPrefs.GetInt("RemoveAdsPurchased", 0) == 1;
            migrated = true;
        }
        
        if (migrated)
        {
            Log("[SaveDataManager] Datos migrados desde PlayerPrefs antiguos");
            isDirty = true;
            SaveDataSync();
        }
    }
    
    /// <summary>
    /// Obtiene los datos de guardado actuales
    /// </summary>
    public SaveData GetSaveData()
    {
        // Asegurar que currentSaveData esté inicializado
        if (currentSaveData == null)
        {
            currentSaveData = CreateNewSaveData();
        }
        
        // Asegurar que statistics esté inicializado
        if (currentSaveData.statistics == null)
        {
            currentSaveData.statistics = new PlayerStatistics();
            isDirty = true;
        }
        
        // Sincronizar tutorialEnabled con PlayerPrefs si existe (PlayerPrefs tiene prioridad)
        // Esto asegura que los cambios recientes en Settings se reflejen inmediatamente
        if (PlayerPrefs.HasKey("TutorialEnabled"))
        {
            bool playerPrefsValue = PlayerPrefs.GetInt("TutorialEnabled", 1) == 1;
            if (currentSaveData.tutorialEnabled != playerPrefsValue)
            {
                Log($"[SaveDataManager] Sincronizando tutorialEnabled desde PlayerPrefs en GetSaveData: {playerPrefsValue}");
                currentSaveData.tutorialEnabled = playerPrefsValue;
                isDirty = true;
            }
        }
        
        return currentSaveData;
    }
    
    /// <summary>
    /// Marca los datos como modificados (para guardado automático)
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
    }
    
    /// <summary>
    /// Fuerza un guardado inmediato
    /// </summary>
    public void ForceSave()
    {
        SaveDataSync();
    }
    
    /// <summary>
    /// Restaura desde un backup específico
    /// </summary>
    public bool RestoreFromBackup(int backupIndex)
    {
        if (backupIndex < 1 || backupIndex > maxBackups)
        {
            LogError($"[SaveDataManager] Índice de backup inválido: {backupIndex}");
            return false;
        }
        
        string backupKey = $"{BACKUP_PREFIX}{backupIndex}";
        if (!PlayerPrefs.HasKey(backupKey))
        {
            LogError($"[SaveDataManager] Backup {backupIndex} no existe");
            return false;
        }
        
        try
        {
            string json = PlayerPrefs.GetString(backupKey);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            if (currentSaveData != null && currentSaveData.ValidateAndFix())
            {
                isDirty = true;
                SaveDataSync();
                Log($"[SaveDataManager] Datos restaurados desde backup {backupIndex}");
                return true;
            }
        }
        catch (Exception e)
        {
            LogError($"[SaveDataManager] Error al restaurar backup {backupIndex}: {e.Message}");
        }
        
        return false;
    }
    
    /// <summary>
    /// Elimina todos los datos guardados (útil para testing)
    /// </summary>
    public void ClearAllSaveData()
    {
        // Eliminar datos principales
        if (PlayerPrefs.HasKey(SAVE_DATA_KEY))
        {
            PlayerPrefs.DeleteKey(SAVE_DATA_KEY);
        }
        
        // Eliminar backups
        for (int i = 1; i <= maxBackups; i++)
        {
            string backupKey = $"{BACKUP_PREFIX}{i}";
            if (PlayerPrefs.HasKey(backupKey))
            {
                PlayerPrefs.DeleteKey(backupKey);
            }
        }
        
        PlayerPrefs.Save();
        currentSaveData = CreateNewSaveData();
        isDirty = true;
        
        Log("[SaveDataManager] Todos los datos guardados han sido eliminados");
    }
}
