using UnityEngine;
using System;

/// <summary>
/// Define un preset de fondo con todas sus configuraciones
/// </summary>
[CreateAssetMenu(fileName = "NewBackgroundPreset", menuName = "Background System/Preset")]
public class BackgroundPreset : ScriptableObject
{
    [Header("Preset Info")]
    public string presetName = "New Preset";
    public string description = "";
    
    [Header("Layer 0: Base")]
    public bool enableBase = true;
    public Color baseColor = Color.black;
    public Sprite baseSprite;
    public float baseOpacity = 1f;
    
    [Header("Layer 1: Nebulas")]
    public bool enableNebulas = true;
    public Sprite nebulaSprite;
    public float nebulaScrollSpeed = 0.2f;
    public float nebulaOpacity = 0.6f;
    public Color nebulaTint = Color.white;
    public BackgroundLayer.ScrollDirection nebulaDirection = BackgroundLayer.ScrollDirection.Down;
    
    [Header("Layer 2: Stars Far")]
    public bool enableStarsFar = true;
    public Sprite starsFarSprite;
    public float starsFarScrollSpeed = 0.5f;
    public float starsFarOpacity = 0.8f;
    public float starsFarParallax = 0.5f; // Más lento = más lejano
    public int starsFarDensity = 2;
    
    [Header("Layer 3: Stars Near")]
    public bool enableStarsNear = true;
    public Sprite starsNearSprite;
    public float starsNearScrollSpeed = 1f;
    public float starsNearOpacity = 1f;
    public float starsNearParallax = 1.2f; // Más rápido = más cercano
    public int starsNearDensity = 2;
    
    [Header("Layer 4: Particles")]
    public bool enableParticles = true;
    public Sprite particleSprite;
    public float particleScrollSpeed = 1.5f;
    public float particleOpacity = 0.7f;
    public bool particlePulsing = false;
    public float particlePulseSpeed = 2f;
    public int particleDensity = 3;
    
    [Header("Global Settings")]
    public BackgroundLayer.ScrollDirection globalDirection = BackgroundLayer.ScrollDirection.Down;
    public Color ambientColor = Color.white;
    
    [Header("Prefab Mode (Alternative)")]
    [Tooltip("Si se asigna un prefab, se usará en lugar de crear capas dinámicamente")]
    public GameObject backgroundPrefab;
}

