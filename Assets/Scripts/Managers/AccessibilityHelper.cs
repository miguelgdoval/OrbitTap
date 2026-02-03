using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper estático para aplicar configuraciones de accesibilidad fácilmente
/// </summary>
public static class AccessibilityHelper
{
    /// <summary>
    /// Aplica configuraciones de accesibilidad a un Text recién creado
    /// </summary>
    public static void ApplyAccessibilityToText(Text text)
    {
        if (text == null || AccessibilityManager.Instance == null) return;
        
        if (AccessibilityManager.Instance.IsHighContrastUIEnabled())
        {
            AccessibilityManager.Instance.ApplyHighContrastToText(text);
        }
    }
    
    /// <summary>
    /// Aplica configuraciones de accesibilidad a un ParticleSystem recién creado
    /// </summary>
    public static void ApplyAccessibilityToParticle(ParticleSystem particle)
    {
        if (particle == null || AccessibilityManager.Instance == null) return;
        
        if (AccessibilityManager.Instance.IsReduceAnimationsEnabled())
        {
            AccessibilityManager.Instance.ApplyReduceAnimationsToParticle(particle);
        }
    }
    
    /// <summary>
    /// Aplica configuraciones de accesibilidad a un Animator recién creado
    /// </summary>
    public static void ApplyAccessibilityToAnimator(Animator animator)
    {
        if (animator == null || AccessibilityManager.Instance == null) return;
        
        if (AccessibilityManager.Instance.IsReduceAnimationsEnabled())
        {
            AccessibilityManager.Instance.ApplyReduceAnimationsToAnimator(animator);
        }
    }
    
    /// <summary>
    /// Aplica configuraciones de accesibilidad a un SpriteRenderer recién creado
    /// </summary>
    public static void ApplyAccessibilityToRenderer(SpriteRenderer renderer)
    {
        if (renderer == null || AccessibilityManager.Instance == null) return;
        
        if (AccessibilityManager.Instance.IsColorBlindModeEnabled())
        {
            AccessibilityManager.Instance.ApplyColorBlindToRenderer(renderer);
        }
    }
}
