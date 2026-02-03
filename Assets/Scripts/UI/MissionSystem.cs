using UnityEngine;
using System;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Categorías de misiones
/// </summary>
public enum MissionCategory
{
    Total,      // Misiones permanentes/totales
    Daily,      // Misiones diarias (se resetean cada día)
    Weekly      // Misiones semanales (se resetean cada semana)
}

/// <summary>
/// Tipos de objetivos que puede tener una misión
/// </summary>
public enum MissionObjectiveType
{
    ReachScore,           // Alcanzar una puntuación específica
    SurviveTime,          // Sobrevivir X segundos
    PlayGames,            // Jugar X partidas
    AvoidObstacles,       // Evitar X obstáculos
    ReachHighScore,       // Alcanzar un nuevo récord
    CollectCurrency,      // Recolectar X monedas
    UsePlanet,            // Usar un planeta específico X veces
    DailyChallenge        // Desafío diario
}

/// <summary>
/// Tipos de recompensas
/// </summary>
public enum RewardType
{
    Currency,             // Monedas (Stellar Shards)
    PlanetUnlock,         // Desbloquear un planeta
    Cosmetic              // Cosmético (futuro)
}

/// <summary>
/// Datos de una recompensa
/// </summary>
[System.Serializable]
public class MissionReward
{
    public RewardType type;
    public int amount;              // Para Currency
    public string itemId;          // Para PlanetUnlock o Cosmetic
    public string description;      // Descripción de la recompensa
    
    public MissionReward(RewardType type, int amount = 0, string itemId = "")
    {
        this.type = type;
        this.amount = amount;
        this.itemId = itemId;
        this.description = GenerateDescription();
    }
    
    private string GenerateDescription()
    {
        switch (type)
        {
            case RewardType.Currency:
                return $"{amount} Stellar Shards";
            case RewardType.PlanetUnlock:
                return $"Desbloquear: {itemId}";
            default:
                return "Recompensa";
        }
    }
}

/// <summary>
/// Datos de una misión
/// </summary>
[System.Serializable]
public class MissionData
{
    public string id;                    // ID único de la misión
    public string title;                 // Título de la misión
    public string description;           // Descripción
    public MissionObjectiveType objectiveType;
    public int targetValue;              // Valor objetivo
    public int currentProgress;          // Progreso actual
    public MissionReward reward;        // Recompensa
    public bool isCompleted;            // Si está completada
    public bool isClaimed;              // Si la recompensa fue reclamada
    public MissionCategory category;    // Categoría de la misión
    public DateTime? resetTime;         // Hora de reset (para diarias/semanales)
    public int priority;                // Prioridad de visualización
    
    public MissionData(string id, string title, string description, 
                      MissionObjectiveType objectiveType, int targetValue, 
                      MissionReward reward, MissionCategory category = MissionCategory.Total)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.objectiveType = objectiveType;
        this.targetValue = targetValue;
        this.currentProgress = 0;
        this.reward = reward;
        this.isCompleted = false;
        this.isClaimed = false;
        this.category = category;
        this.priority = 0;
        
        // Configurar reset time según la categoría
        if (category == MissionCategory.Daily)
        {
            this.resetTime = GetNextDailyReset();
        }
        else if (category == MissionCategory.Weekly)
        {
            this.resetTime = GetNextWeeklyReset();
        }
    }
    
    private DateTime GetNextDailyReset()
    {
        DateTime now = DateTime.Now;
        DateTime tomorrow = now.Date.AddDays(1);
        return tomorrow;
    }
    
    private DateTime GetNextWeeklyReset()
    {
        DateTime now = DateTime.Now;
        // Reset cada lunes a las 00:00
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && now.Hour >= 0) daysUntilMonday = 7; // Si ya es lunes, reset el próximo
        return now.Date.AddDays(daysUntilMonday);
    }
    
    public float GetProgressPercentage()
    {
        return Mathf.Clamp01((float)currentProgress / targetValue);
    }
    
    public bool CheckCompletion()
    {
        if (isCompleted) return true;
        isCompleted = currentProgress >= targetValue;
        return isCompleted;
    }
}

/// <summary>
/// Manager del sistema de misiones
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }
    
    private const string MISSIONS_KEY = "MissionsData";
    private const string MISSION_PROGRESS_PREFIX = "MissionProgress_";
    private const string LAST_DAILY_RESET_KEY = "LastDailyReset";
    private const string LAST_WEEKLY_RESET_KEY = "LastWeeklyReset";
    
    private List<MissionData> allMissions = new List<MissionData>();
    private List<MissionData> totalMissions = new List<MissionData>();
    private List<MissionData> dailyMissions = new List<MissionData>();
    private List<MissionData> weeklyMissions = new List<MissionData>();
    private List<MissionData> activeMissions = new List<MissionData>();
    
    public System.Action<MissionData> OnMissionProgress;
    public System.Action<MissionData> OnMissionCompleted;
    
    private void Awake()
    {
        Log("[MissionManager] Awake llamado.");
        if (Instance == null)
        {
            Log("[MissionManager] Creando nueva instancia.");
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 1) Crear TODAS las misiones (sin tocar progreso)
            InitializeMissions();

            // 2) Cargar el progreso guardado de PlayerPrefs
            LoadMissionsProgress();

            // 3) Ahora que las listas y el progreso existen, comprobamos si toca reset diario/semanal
            CheckAndResetTimedMissions();

            // 4) Refrescar lista activa tras posibles resets
            RefreshActiveMissions();

            Log($"[MissionManager] Inicializado. Total misiones: {allMissions.Count}, Activas: {activeMissions.Count}");
        }
        else
        {
            Log("[MissionManager] Instancia ya existe, destruyendo duplicado.");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Inicializa todas las misiones disponibles (solo define los datos base)
    /// </summary>
    private void InitializeMissions()
    {
        Log("[MissionManager] Inicializando misiones (definición de datos)...");

        // Misiones Totales/Permanentes
        InitializeTotalMissions();

        // Misiones Diarias (siempre las mismas para todos)
        InitializeDailyMissions();

        // Misiones Semanales (siempre las mismas para todos)
        InitializeWeeklyMissions();

        RefreshActiveMissions();
        Log($"[MissionManager] Misiones inicializadas. Total: {allMissions.Count}, Totales: {totalMissions.Count}, Diarias: {dailyMissions.Count}, Semanales: {weeklyMissions.Count}");
    }
    
    /// <summary>
    /// Inicializa las misiones totales/permanentes
    /// </summary>
    private void InitializeTotalMissions()
    {
        totalMissions.Add(new MissionData(
            "first_game",
            "Primera Órbita",
            "Juega tu primera partida",
            MissionObjectiveType.PlayGames,
            1,
            new MissionReward(RewardType.Currency, 50),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "score_10",
            "Explorador Novato",
            "Alcanza una puntuación de 10",
            MissionObjectiveType.ReachScore,
            10,
            new MissionReward(RewardType.Currency, 25),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "score_50",
            "Viajero Estelar",
            "Alcanza una puntuación de 50",
            MissionObjectiveType.ReachScore,
            50,
            new MissionReward(RewardType.Currency, 100),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "score_100",
            "Maestro del Espacio",
            "Alcanza una puntuación de 100",
            MissionObjectiveType.ReachScore,
            100,
            new MissionReward(RewardType.Currency, 250),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "score_200",
            "Leyenda Cósmica",
            "Alcanza una puntuación de 200",
            MissionObjectiveType.ReachScore,
            200,
            new MissionReward(RewardType.Currency, 500),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "survive_30",
            "Supervivencia Básica",
            "Sobrevive 30 segundos en una partida",
            MissionObjectiveType.SurviveTime,
            30,
            new MissionReward(RewardType.Currency, 75),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "survive_60",
            "Supervivencia Avanzada",
            "Sobrevive 60 segundos en una partida",
            MissionObjectiveType.SurviveTime,
            60,
            new MissionReward(RewardType.Currency, 150),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "play_5",
            "Veterano",
            "Juega 5 partidas",
            MissionObjectiveType.PlayGames,
            5,
            new MissionReward(RewardType.Currency, 150),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "play_10",
            "Experto",
            "Juega 10 partidas",
            MissionObjectiveType.PlayGames,
            10,
            new MissionReward(RewardType.Currency, 300),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "play_25",
            "Maestro",
            "Juega 25 partidas",
            MissionObjectiveType.PlayGames,
            25,
            new MissionReward(RewardType.Currency, 750),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "high_score_50",
            "Nuevo Récord",
            "Alcanza un nuevo récord de 50 puntos",
            MissionObjectiveType.ReachHighScore,
            50,
            new MissionReward(RewardType.Currency, 200),
            MissionCategory.Total
        ));
        
        totalMissions.Add(new MissionData(
            "high_score_100",
            "Récord Épico",
            "Alcanza un nuevo récord de 100 puntos",
            MissionObjectiveType.ReachHighScore,
            100,
            new MissionReward(RewardType.Currency, 500),
            MissionCategory.Total
        ));
        
        allMissions.AddRange(totalMissions);
    }
    
    /// <summary>
    /// Inicializa las misiones diarias (siempre las mismas para todos)
    /// Ordenadas por tipo y dificultad: primero puntuación (fácil a difícil), luego jugar partidas
    /// </summary>
    private void InitializeDailyMissions()
    {
        // Misiones de puntuación (ordenadas de fácil a difícil)
        dailyMissions.Add(new MissionData(
            "daily_score_20",
            "Puntuación Diaria",
            "Alcanza 20 puntos en una partida",
            MissionObjectiveType.ReachScore,
            20,
            new MissionReward(RewardType.Currency, 40),
            MissionCategory.Daily
        ));
        
        dailyMissions.Add(new MissionData(
            "daily_score_40",
            "Desafío Diario",
            "Alcanza 40 puntos en una partida",
            MissionObjectiveType.ReachScore,
            40,
            new MissionReward(RewardType.Currency, 60),
            MissionCategory.Daily
        ));
        
        // Misiones de jugar partidas (ordenadas de fácil a difícil)
        dailyMissions.Add(new MissionData(
            "daily_play_3",
            "Jugador Diario",
            "Juega 3 partidas hoy",
            MissionObjectiveType.PlayGames,
            3,
            new MissionReward(RewardType.Currency, 50),
            MissionCategory.Daily
        ));
        
        dailyMissions.Add(new MissionData(
            "daily_play_5",
            "Jugador Activo",
            "Juega 5 partidas hoy",
            MissionObjectiveType.PlayGames,
            5,
            new MissionReward(RewardType.Currency, 75),
            MissionCategory.Daily
        ));
        
        allMissions.AddRange(dailyMissions);
    }
    
    /// <summary>
    /// Inicializa las misiones semanales (siempre las mismas para todos)
    /// Ordenadas por tipo y dificultad: primero puntuación (fácil a difícil), luego jugar partidas
    /// </summary>
    private void InitializeWeeklyMissions()
    {
        // Misiones de puntuación (ordenadas de fácil a difícil)
        weeklyMissions.Add(new MissionData(
            "weekly_score_100",
            "Puntuación Semanal",
            "Alcanza 100 puntos en una partida",
            MissionObjectiveType.ReachScore,
            100,
            new MissionReward(RewardType.Currency, 250),
            MissionCategory.Weekly
        ));
        
        weeklyMissions.Add(new MissionData(
            "weekly_score_150",
            "Maestro Semanal",
            "Alcanza 150 puntos en una partida",
            MissionObjectiveType.ReachScore,
            150,
            new MissionReward(RewardType.Currency, 400),
            MissionCategory.Weekly
        ));
        
        // Misiones de jugar partidas (ordenadas de fácil a difícil)
        weeklyMissions.Add(new MissionData(
            "weekly_play_15",
            "Jugador Semanal",
            "Juega 15 partidas esta semana",
            MissionObjectiveType.PlayGames,
            15,
            new MissionReward(RewardType.Currency, 200),
            MissionCategory.Weekly
        ));
        
        weeklyMissions.Add(new MissionData(
            "weekly_play_20",
            "Veterano Semanal",
            "Juega 20 partidas esta semana",
            MissionObjectiveType.PlayGames,
            20,
            new MissionReward(RewardType.Currency, 300),
            MissionCategory.Weekly
        ));
        
        allMissions.AddRange(weeklyMissions);
    }
    
    /// <summary>
    /// Verifica y resetea misiones diarias/semanales si es necesario
    /// </summary>
    private void CheckAndResetTimedMissions()
    {
        DateTime now = DateTime.Now;
        DateTime lastDailyReset = GetLastDailyReset();
        DateTime lastWeeklyReset = GetLastWeeklyReset();
        
        // Verificar si ha pasado un día desde el último reset diario
        if (now.Date > lastDailyReset.Date)
        {
            ResetDailyMissions();
            SaveLastDailyReset(now);
        }
        
        // Verificar si ha pasado una semana desde el último reset semanal
        if (GetWeekNumber(now) > GetWeekNumber(lastWeeklyReset) || 
            (GetWeekNumber(now) == GetWeekNumber(lastWeeklyReset) && now.Year > lastWeeklyReset.Year))
        {
            ResetWeeklyMissions();
            SaveLastWeeklyReset(now);
        }
    }
    
    private int GetWeekNumber(DateTime date)
    {
        System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CurrentCulture;
        return ci.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }
    
    private DateTime GetLastDailyReset()
    {
        string dateStr = PlayerPrefs.GetString(LAST_DAILY_RESET_KEY, "");
        if (string.IsNullOrEmpty(dateStr))
            return DateTime.MinValue;
        
        if (DateTime.TryParse(dateStr, out DateTime date))
            return date;
        
        return DateTime.MinValue;
    }
    
    private DateTime GetLastWeeklyReset()
    {
        string dateStr = PlayerPrefs.GetString(LAST_WEEKLY_RESET_KEY, "");
        if (string.IsNullOrEmpty(dateStr))
            return DateTime.MinValue;
        
        if (DateTime.TryParse(dateStr, out DateTime date))
            return date;
        
        return DateTime.MinValue;
    }
    
    private void SaveLastDailyReset(DateTime date)
    {
        PlayerPrefs.SetString(LAST_DAILY_RESET_KEY, date.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
    }
    
    private void SaveLastWeeklyReset(DateTime date)
    {
        PlayerPrefs.SetString(LAST_WEEKLY_RESET_KEY, date.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Resetea todas las misiones diarias
    /// Si una misión no fue reclamada en el período anterior, se pierde completamente
    /// </summary>
    private void ResetDailyMissions()
    {
        Log("[MissionManager] Reseteando misiones diarias...");
        foreach (var mission in dailyMissions)
        {
            // Si la misión estaba completada pero no reclamada, se pierde
            if (mission.isCompleted && !mission.isClaimed)
            {
                Log($"[MissionManager] Misión diaria '{mission.id}' completada pero no reclamada. Se pierde.");
            }
            
            // Resetear completamente: progreso, estado de completado y reclamado
            mission.currentProgress = 0;
            mission.isCompleted = false;
            mission.isClaimed = false;
            mission.resetTime = GetNextDailyReset();
            SaveMissionProgress(mission);
        }
    }
    
    /// <summary>
    /// Resetea todas las misiones semanales
    /// Si una misión no fue reclamada en el período anterior, se pierde completamente
    /// </summary>
    private void ResetWeeklyMissions()
    {
        Log("[MissionManager] Reseteando misiones semanales...");
        foreach (var mission in weeklyMissions)
        {
            // Si la misión estaba completada pero no reclamada, se pierde
            if (mission.isCompleted && !mission.isClaimed)
            {
                Log($"[MissionManager] Misión semanal '{mission.id}' completada pero no reclamada. Se pierde.");
            }
            
            // Resetear completamente: progreso, estado de completado y reclamado
            mission.currentProgress = 0;
            mission.isCompleted = false;
            mission.isClaimed = false;
            mission.resetTime = GetNextWeeklyReset();
            SaveMissionProgress(mission);
        }
    }
    
    private DateTime GetNextDailyReset()
    {
        DateTime now = DateTime.Now;
        return now.Date.AddDays(1);
    }
    
    private DateTime GetNextWeeklyReset()
    {
        DateTime now = DateTime.Now;
        // Reset cada lunes a las 00:00
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // Si ya es lunes, reset el próximo
        return now.Date.AddDays(daysUntilMonday);
    }
    
    /// <summary>
    /// Actualiza qué misiones están activas
    /// </summary>
    private void RefreshActiveMissions()
    {
        activeMissions.Clear();
        foreach (var mission in allMissions)
        {
            if (!mission.isClaimed)
            {
                activeMissions.Add(mission);
            }
        }
        
        // Ordenar por categoría, luego por prioridad y estado (completadas primero)
        activeMissions.Sort((a, b) => {
            // Primero por categoría: Diarias, Semanales, Totales
            if (a.category != b.category)
            {
                if (a.category == MissionCategory.Daily) return -1;
                if (b.category == MissionCategory.Daily) return 1;
                if (a.category == MissionCategory.Weekly) return -1;
                if (b.category == MissionCategory.Weekly) return 1;
            }
            // Luego por estado (completadas primero)
            if (a.isCompleted != b.isCompleted)
                return a.isCompleted ? -1 : 1;
            // Finalmente por prioridad
            return b.priority.CompareTo(a.priority);
        });
    }
    
    /// <summary>
    /// Reporta progreso en una misión
    /// </summary>
    public void ReportProgress(MissionObjectiveType type, int amount = 1)
    {
        foreach (var mission in activeMissions)
        {
            if (mission.objectiveType == type && !mission.isCompleted)
            {
                int oldProgress = mission.currentProgress;
                mission.currentProgress += amount;
                
                // Para objetivos que solo deben alcanzar un valor máximo (como score)
                if (type == MissionObjectiveType.ReachScore || 
                    type == MissionObjectiveType.SurviveTime ||
                    type == MissionObjectiveType.ReachHighScore)
                {
                    // Solo actualizar si el nuevo valor es mayor
                    if (amount > oldProgress)
                    {
                        mission.currentProgress = Mathf.Max(mission.currentProgress, amount);
                    }
                }
                
                // Verificar si se completó
                bool wasCompleted = mission.isCompleted;
                mission.CheckCompletion();
                
                // Guardar progreso
                SaveMissionProgress(mission);
                
                // Eventos
                OnMissionProgress?.Invoke(mission);
                
                if (mission.isCompleted && !wasCompleted)
                {
                    // Analytics: Registrar misión completada
                    if (AnalyticsManager.Instance != null)
                    {
                        AnalyticsManager.Instance.TrackMissionCompleted(
                            mission.id, 
                            mission.title, 
                            mission.reward.amount
                        );
                    }
                    
                    OnMissionCompleted?.Invoke(mission);
                }
            }
        }
    }
    
    /// <summary>
    /// Reporta un valor específico (útil para score que puede aumentar y disminuir)
    /// </summary>
    public void ReportValue(MissionObjectiveType type, int value)
    {
        foreach (var mission in activeMissions)
        {
            if (mission.objectiveType == type && !mission.isCompleted)
            {
                // Solo actualizar si el valor es mayor que el progreso actual
                if (value > mission.currentProgress)
                {
                    int oldProgress = mission.currentProgress;
                    mission.currentProgress = value;
                    
                    // Verificar si se completó
                    bool wasCompleted = mission.isCompleted;
                    mission.CheckCompletion();
                    
                    // Guardar progreso
                    SaveMissionProgress(mission);
                    
                    // Eventos
                    if (mission.currentProgress != oldProgress)
                    {
                        OnMissionProgress?.Invoke(mission);
                    }
                    
                    if (mission.isCompleted && !wasCompleted)
                    {
                        // Analytics: Registrar misión completada
                        if (AnalyticsManager.Instance != null)
                        {
                            AnalyticsManager.Instance.TrackMissionCompleted(
                                mission.id, 
                                mission.title, 
                                mission.reward.amount
                            );
                        }
                        
                        OnMissionCompleted?.Invoke(mission);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Reclama la recompensa de una misión
    /// </summary>
    public bool ClaimReward(MissionData mission)
    {
        if (!mission.isCompleted || mission.isClaimed)
            return false;
        
        // Aplicar recompensa
        ApplyReward(mission.reward);
        
        mission.isClaimed = true;
        SaveMissionProgress(mission);
        RefreshActiveMissions();
        
        return true;
    }
    
    /// <summary>
    /// Aplica una recompensa al jugador
    /// </summary>
    private void ApplyReward(MissionReward reward)
    {
        switch (reward.type)
        {
            case RewardType.Currency:
                if (CurrencyManager.Instance != null)
                {
                    CurrencyManager.Instance.AddCurrency(reward.amount);
                }
                break;
            case RewardType.PlanetUnlock:
                // Desbloquear planeta (implementar según tu sistema de desbloqueo)
                PlayerPrefs.SetString($"Planet_{reward.itemId}_Unlocked", "true");
                PlayerPrefs.Save();
                break;
        }
    }
    
    /// <summary>
    /// Guarda el progreso de una misión
    /// </summary>
    private void SaveMissionProgress(MissionData mission)
    {
        string key = MISSION_PROGRESS_PREFIX + mission.id;
        string data = JsonUtility.ToJson(new MissionProgressData
        {
            currentProgress = mission.currentProgress,
            isCompleted = mission.isCompleted,
            isClaimed = mission.isClaimed
        });
        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Carga el progreso de todas las misiones
    /// </summary>
    private void LoadMissionsProgress()
    {
        // Cargar progreso de misiones totales
        LoadMissionsProgressForList(totalMissions);
        
        // Cargar progreso de misiones diarias y semanales (solo si no se han reseteado)
        // El reset se hace en CheckAndResetTimedMissions antes de cargar
        LoadMissionsProgressForList(dailyMissions);
        LoadMissionsProgressForList(weeklyMissions);
        
        RefreshActiveMissions();
    }
    
    /// <summary>
    /// Carga el progreso de una lista de misiones con validación
    /// </summary>
    private void LoadMissionsProgressForList(List<MissionData> missions)
    {
        foreach (var mission in missions)
        {
            string key = MISSION_PROGRESS_PREFIX + mission.id;
            if (PlayerPrefs.HasKey(key))
            {
                try
                {
                    string data = PlayerPrefs.GetString(key);
                    if (string.IsNullOrEmpty(data))
                    {
                        LogWarning($"[MissionManager] Datos vacíos para misión {mission.id}, usando valores por defecto");
                        continue;
                    }
                    
                    MissionProgressData progress = JsonUtility.FromJson<MissionProgressData>(data);
                    if (progress != null)
                    {
                        // Validar valores
                        if (progress.currentProgress < 0) 
                        {
                            LogWarning($"[MissionManager] Progreso negativo para misión {mission.id}, reseteando a 0");
                            progress.currentProgress = 0;
                        }
                        
                        if (progress.currentProgress > mission.targetValue) 
                        {
                            LogWarning($"[MissionManager] Progreso excede objetivo para misión {mission.id}, limitando a {mission.targetValue}");
                            progress.currentProgress = mission.targetValue;
                        }
                        
                        mission.currentProgress = progress.currentProgress;
                        mission.isCompleted = progress.isCompleted;
                        mission.isClaimed = progress.isClaimed;
                    }
                    else
                    {
                        LogWarning($"[MissionManager] Error al parsear datos de misión {mission.id}, usando valores por defecto");
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"[MissionManager] Error al cargar misión {mission.id}: {e.Message}. Usando valores por defecto.");
                    // Limpiar datos corruptos
                    PlayerPrefs.DeleteKey(key);
                }
            }
        }
    }
    
    public List<MissionData> GetActiveMissions()
    {
        RefreshActiveMissions();
        Log($"[MissionManager] GetActiveMissions llamado. Retornando {activeMissions.Count} misiones.");
        return activeMissions;
    }
    
    public List<MissionData> GetActiveMissionsByCategory(MissionCategory category)
    {
        RefreshActiveMissions();
        List<MissionData> filtered = new List<MissionData>();
        foreach (var mission in activeMissions)
        {
            if (mission.category == category)
            {
                filtered.Add(mission);
            }
        }
        return filtered;
    }
    
    public List<MissionData> GetAllMissions()
    {
        return allMissions;
    }
    
    public List<MissionData> GetTotalMissions()
    {
        return totalMissions;
    }
    
    public List<MissionData> GetDailyMissions()
    {
        return dailyMissions;
    }
    
    public List<MissionData> GetWeeklyMissions()
    {
        return weeklyMissions;
    }
    
    // Clase auxiliar para serialización
    [System.Serializable]
    private class MissionProgressData
    {
        public int currentProgress;
        public bool isCompleted;
        public bool isClaimed;
    }
}

