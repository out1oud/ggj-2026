using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Controls the main menu screen shown at game start.
    /// Pauses the game (timeScale = 0) until the player clicks Start.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] GameObject menuPanel;
        [SerializeField] Button startButton;

        void Awake()
        {
            // Pause the game when menu is shown
            Time.timeScale = 0f;
            
            // Ensure menu is visible
            if (menuPanel != null)
                menuPanel.SetActive(true);
            
            // Setup button listener
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
        }

        void OnStartClicked()
        {
            // Resume game time
            Time.timeScale = 1f;
            
            // Hide the menu
            if (menuPanel != null)
                menuPanel.SetActive(false);
            
            Debug.Log("[MainMenuController] Game started, menu hidden");
        }

        void OnDestroy()
        {
            // Clean up listener
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
        }
    }
}
