using UnityEngine;
using static LogHelper;

public class InputController : MonoBehaviour
{
    private PlayerOrbit player;

    private void Start()
    {
        player = FindObjectOfType<PlayerOrbit>();
        if (player == null)
        {
            LogError("InputController: PlayerOrbit not found!");
        }
    }

    private void Update()
    {
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

    private void HandleInput()
    {
        if (player != null)
        {
            player.ToggleDirection();
        }
    }
}

