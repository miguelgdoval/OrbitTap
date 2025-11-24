using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la navegación entre secciones del menú
/// </summary>
public class MenuNavigation : MonoBehaviour
{
    [Header("Navigation Buttons")]
    public Button homeButton;
    public Button skinsButton;
    public Button shopButton;
    
    [Header("Sections")]
    public GameObject playSection;
    public GameObject skinsSection;
    public GameObject shopSection;
    
    private MenuSection currentSection;
    
    private void Start()
    {
        // Configurar botones de navegación
        if (homeButton != null)
            homeButton.onClick.AddListener(() => NavigateTo(MenuSection.Play));
        if (skinsButton != null)
            skinsButton.onClick.AddListener(() => NavigateTo(MenuSection.Skins));
        if (shopButton != null)
            shopButton.onClick.AddListener(() => NavigateTo(MenuSection.Shop));
        
        // Mostrar sección inicial
        NavigateTo(MenuSection.Play);
    }
    
    public void NavigateTo(MenuSection section)
    {
        // Ocultar todas las secciones
        if (playSection != null) playSection.SetActive(false);
        if (skinsSection != null) skinsSection.SetActive(false);
        if (shopSection != null) shopSection.SetActive(false);
        
        // Mostrar sección seleccionada
        switch (section)
        {
            case MenuSection.Play:
                if (playSection != null) playSection.SetActive(true);
                break;
            case MenuSection.Skins:
                if (skinsSection != null) skinsSection.SetActive(true);
                break;
            case MenuSection.Shop:
                if (shopSection != null) shopSection.SetActive(true);
                break;
        }
        
        currentSection = section;
        
        // Actualizar estado visual de los botones
        UpdateButtonStates(section);
    }
    
    private void UpdateButtonStates(MenuSection activeSection)
    {
        // Aquí puedes cambiar el color/estado de los botones según la sección activa
        // Por ahora solo los activamos/desactivamos visualmente
    }
}

