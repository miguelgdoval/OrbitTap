using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manager para controlar el audio del juego (Master, Music, SFX)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    // Valores por defecto
    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAudioSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        ApplyAudioSettings();
    }
    
    /// <summary>
    /// Establece el volumen master (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.masterVolume = masterVolume;
                SaveDataManager.Instance.MarkDirty();
            }
        }
        else
        {
            // Fallback a PlayerPrefs
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.Save();
        }
        
        ApplyMasterVolume();
    }
    
    /// <summary>
    /// Establece el volumen de música (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.musicVolume = musicVolume;
                SaveDataManager.Instance.MarkDirty();
            }
        }
        else
        {
            // Fallback a PlayerPrefs
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.Save();
        }
        
        ApplyMusicVolume();
    }
    
    /// <summary>
    /// Establece el volumen de SFX (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.sfxVolume = sfxVolume;
                SaveDataManager.Instance.MarkDirty();
            }
        }
        else
        {
            // Fallback a PlayerPrefs
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.Save();
        }
        
        ApplySFXVolume();
    }
    
    /// <summary>
    /// Obtiene el volumen master
    /// </summary>
    public float GetMasterVolume()
    {
        return masterVolume;
    }
    
    /// <summary>
    /// Obtiene el volumen de música
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }
    
    /// <summary>
    /// Obtiene el volumen de SFX
    /// </summary>
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
    
    private void ApplyMasterVolume()
    {
        // Si hay AudioMixer, usar decibeles (-80 a 0)
        if (masterMixerGroup != null && masterMixerGroup.audioMixer != null)
        {
            float db = masterVolume > 0 ? Mathf.Log10(masterVolume) * 20f : -80f;
            masterMixerGroup.audioMixer.SetFloat("MasterVolume", db);
        }
        else
        {
            // Fallback: usar AudioListener
            AudioListener.volume = masterVolume;
        }
    }
    
    private void ApplyMusicVolume()
    {
        if (musicMixerGroup != null && musicMixerGroup.audioMixer != null)
        {
            float db = musicVolume > 0 ? Mathf.Log10(musicVolume) * 20f : -80f;
            musicMixerGroup.audioMixer.SetFloat("MusicVolume", db);
        }
    }
    
    private void ApplySFXVolume()
    {
        if (sfxMixerGroup != null && sfxMixerGroup.audioMixer != null)
        {
            float db = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20f : -80f;
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", db);
        }
    }
    
    private void ApplyAudioSettings()
    {
        ApplyMasterVolume();
        ApplyMusicVolume();
        ApplySFXVolume();
    }
    
    private void LoadAudioSettings()
    {
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                masterVolume = saveData.masterVolume;
                musicVolume = saveData.musicVolume;
                sfxVolume = saveData.sfxVolume;
                return;
            }
        }
        
        // Fallback a PlayerPrefs
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }
}

