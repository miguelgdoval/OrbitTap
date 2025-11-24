using UnityEngine;

/// <summary>
/// Enum para las diferentes secciones del menú
/// </summary>
public enum MenuSection
{
    Play,
    Skins,
    Shop
}

/// <summary>
/// Clase base para las secciones del menú
/// </summary>
public abstract class BaseMenuSection : MonoBehaviour
{
    public abstract MenuSection SectionType { get; }
    
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}

