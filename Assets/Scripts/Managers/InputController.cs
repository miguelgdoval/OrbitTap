using UnityEngine;
using UnityEngine.EventSystems;
using static LogHelper;

public class InputController : MonoBehaviour
{
    private PlayerOrbit player;

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        // No procesar input si el juego está pausado (ej: revive UI)
        if (Time.timeScale == 0f) return;
        
        // Si no tenemos referencia al player, intentar encontrarlo (puede pasar tras revive)
        if (player == null)
        {
            FindPlayer();
            if (player == null) return; // Aún no hay player
        }
        
        // No procesar input si el toque está sobre UI
        if (IsPointerOverUI()) return;
        
        // Detect touch input (mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleInput();
            }
        }
        // Detect mouse click (for testing in editor)
        else if (Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }
    }
    
    /// <summary>
    /// Busca el PlayerOrbit en la escena
    /// </summary>
    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerOrbit>();
    }

    private void HandleInput()
    {
        if (player != null)
        {
            player.ToggleDirection();
        }
    }
    
    /// <summary>
    /// Comprueba si el toque/click está sobre un elemento de UI
    /// </summary>
    private bool IsPointerOverUI()
    {
        // Comprobar touches en móvil
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current != null)
            {
                return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            }
        }
        
        // Comprobar mouse en editor
        if (EventSystem.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
        
        return false;
    }
}

