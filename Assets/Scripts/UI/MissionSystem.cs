using UnityEngine;
using System;
using System.Collections.Generic;

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
    public bool isDaily;                // Si es una misión diaria
    public DateTime? dailyResetTime;    // Hora de reset (para misiones diarias)
    public int priority;                // Prioridad de visualización
    
    public MissionData(string id, string title, string description, 
                      MissionObjectiveType objectiveType, int targetValue, 
                      MissionReward reward, bool isDaily = false)
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
        this.isDaily = isDaily;
        this.priority = 0;
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
    
    private List<MissionData> allMissions = new List<MissionData>();
    private List<MissionData> activeMissions = new List<MissionData>();
    
    public System.Action<MissionData> OnMissionProgress;
    public System.Action<MissionData> OnMissionCompleted;
    
    private void Awake()
    {
        Debug.Log("[MissionManager] Awake llamado.");
        if (Instance == null)
        {
            Debug.Log("[MissionManager] Creando nueva instancia.");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMissions();
            LoadMissionsProgress();
            Debug.Log($"[MissionManager] Inicializado. Total misiones: {allMissions.Count}, Activas: {activeMissions.Count}");
        }
        else
        {
            Debug.Log("[MissionManager] Instancia ya existe, destruyendo duplicado.");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Inicializa todas las misiones disponibles
    /// </summary>
    private void InitializeMissions()
    {
        Debug.Log("[MissionManager] Inicializando misiones...");
        // Misiones permanentes
        allMissions.Add(new MissionData(
            "first_game",
            "Primera Órbita",
            "Juega tu primera partida",
            MissionObjectiveType.PlayGames,
            1,
            new MissionReward(RewardType.Currency, 50)
        ));
        
        allMissions.Add(new MissionData(
            "score_10",
            "Explorador Novato",
            "Alcanza una puntuación de 10",
            MissionObjectiveType.ReachScore,
            10,
            new MissionReward(RewardType.Currency, 25)
        ));
        
        allMissions.Add(new MissionData(
            "score_50",
            "Viajero Estelar",
            "Alcanza una puntuación de 50",
            MissionObjectiveType.ReachScore,
            50,
            new MissionReward(RewardType.Currency, 100)
        ));
        
        allMissions.Add(new MissionData(
            "score_100",
            "Maestro del Espacio",
            "Alcanza una puntuación de 100",
            MissionObjectiveType.ReachScore,
            100,
            new MissionReward(RewardType.Currency, 250)
        ));
        
        allMissions.Add(new MissionData(
            "score_200",
            "Leyenda Cósmica",
            "Alcanza una puntuación de 200",
            MissionObjectiveType.ReachScore,
            200,
            new MissionReward(RewardType.Currency, 500)
        ));
        
        allMissions.Add(new MissionData(
            "survive_30",
            "Supervivencia Básica",
            "Sobrevive 30 segundos en una partida",
            MissionObjectiveType.SurviveTime,
            30,
            new MissionReward(RewardType.Currency, 75)
        ));
        
        allMissions.Add(new MissionData(
            "survive_60",
            "Supervivencia Avanzada",
            "Sobrevive 60 segundos en una partida",
            MissionObjectiveType.SurviveTime,
            60,
            new MissionReward(RewardType.Currency, 150)
        ));
        
        allMissions.Add(new MissionData(
            "play_5",
            "Veterano",
            "Juega 5 partidas",
            MissionObjectiveType.PlayGames,
            5,
            new MissionReward(RewardType.Currency, 150)
        ));
        
        allMissions.Add(new MissionData(
            "play_10",
            "Experto",
            "Juega 10 partidas",
            MissionObjectiveType.PlayGames,
            10,
            new MissionReward(RewardType.Currency, 300)
        ));
        
        allMissions.Add(new MissionData(
            "play_25",
            "Maestro",
            "Juega 25 partidas",
            MissionObjectiveType.PlayGames,
            25,
            new MissionReward(RewardType.Currency, 750)
        ));
        
        allMissions.Add(new MissionData(
            "high_score_50",
            "Nuevo Récord",
            "Alcanza un nuevo récord de 50 puntos",
            MissionObjectiveType.ReachHighScore,
            50,
            new MissionReward(RewardType.Currency, 200)
        ));
        
        allMissions.Add(new MissionData(
            "high_score_100",
            "Récord Épico",
            "Alcanza un nuevo récord de 100 puntos",
            MissionObjectiveType.ReachHighScore,
            100,
            new MissionReward(RewardType.Currency, 500)
        ));
        
        RefreshActiveMissions();
        Debug.Log($"[MissionManager] Misiones inicializadas. Total: {allMissions.Count}, Activas: {activeMissions.Count}");
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
                // Si es diaria, verificar si necesita reset
                if (mission.isDaily && mission.dailyResetTime.HasValue)
                {
                    if (DateTime.Now >= mission.dailyResetTime.Value)
                    {
                        ResetDailyMission(mission);
                    }
                }
                
                activeMissions.Add(mission);
            }
        }
        
        // Ordenar por prioridad y estado (completadas primero)
        activeMissions.Sort((a, b) => {
            if (a.isCompleted != b.isCompleted)
                return a.isCompleted ? -1 : 1;
            return b.priority.CompareTo(a.priority);
        });
    }
    
    /// <summary>
    /// Resetea una misión diaria
    /// </summary>
    private void ResetDailyMission(MissionData mission)
    {
        mission.currentProgress = 0;
        mission.isCompleted = false;
        mission.isClaimed = false;
        mission.dailyResetTime = DateTime.Now.AddDays(1);
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
        foreach (var mission in allMissions)
        {
            string key = MISSION_PROGRESS_PREFIX + mission.id;
            if (PlayerPrefs.HasKey(key))
            {
                string data = PlayerPrefs.GetString(key);
                MissionProgressData progress = JsonUtility.FromJson<MissionProgressData>(data);
                mission.currentProgress = progress.currentProgress;
                mission.isCompleted = progress.isCompleted;
                mission.isClaimed = progress.isClaimed;
            }
        }
        RefreshActiveMissions();
    }
    
    public List<MissionData> GetActiveMissions()
    {
        RefreshActiveMissions();
        Debug.Log($"[MissionManager] GetActiveMissions llamado. Retornando {activeMissions.Count} misiones.");
        return activeMissions;
    }
    
    public List<MissionData> GetAllMissions()
    {
        return allMissions;
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

