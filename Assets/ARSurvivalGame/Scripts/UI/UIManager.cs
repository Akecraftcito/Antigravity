using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARSurvivalGame.Core;

namespace ARSurvivalGame.UI
{
    /// <summary>
    /// Gestiona toda la interfaz de usuario: Pantalla de escaneo/colocación AR, HUD de combate,
    /// overlay de daño y pantalla de Game Over con opciones de reintento y reposicionamiento.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("HUD Elements")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private CanvasGroup damageVignette;
        [SerializeField] private Button fireButton;

        [Header("Game Over Elements")]
        [SerializeField] private TextMeshProUGUI finalTimeText;
        [SerializeField] private TextMeshProUGUI finalKillsText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button reanchorButton;

        private float targetHealthFill = 1f;
        private float currentHealthFill = 1f;
        private float damageFlashAlpha = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (fireButton != null)
            {
                fireButton.onClick.AddListener(() =>
                {
                    var player = FindAnyObjectByType<ARSurvivalGame.Player.PlayerController>();
                    if (player != null) player.Shoot();
                });
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }
            if (reanchorButton != null)
            {
                reanchorButton.onClick.AddListener(OnReanchorClicked);
            }
        }

        private void Update()
        {
            // Suavizado dinámico de la barra de salud (Lerp)
            if (healthBarFill != null)
            {
                currentHealthFill = Mathf.Lerp(currentHealthFill, targetHealthFill, Time.deltaTime * 8f);
                healthBarFill.fillAmount = currentHealthFill;

                // Transición de color: Verde -> Amarillo -> Rojo
                Color healthColor = Color.Lerp(Color.red, Color.green, currentHealthFill);
                if (currentHealthFill < 0.5f)
                {
                    healthColor = Color.Lerp(Color.red, Color.yellow, currentHealthFill * 2f);
                }
                healthBarFill.color = healthColor;
            }

            // Desvanecimiento suave del efecto de daño (Damage Vignette)
            if (damageVignette != null && damageFlashAlpha > 0f)
            {
                damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, Time.deltaTime * 2.5f);
                damageVignette.alpha = damageFlashAlpha;
            }
        }

        public void ShowPlacementUI()
        {
            if (placementPanel != null) placementPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (damageVignette != null) damageVignette.alpha = 0f;
        }

        public void ShowHUD()
        {
            if (placementPanel != null) placementPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            targetHealthFill = 1f;
            currentHealthFill = 1f;
        }

        public void ShowGameOver(float survivalTime, int kills, int score)
        {
            if (placementPanel != null) placementPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                int minutes = Mathf.FloorToInt(survivalTime / 60f);
                int seconds = Mathf.FloorToInt(survivalTime % 60f);

                if (finalTimeText != null)
                    finalTimeText.text = $"Tiempo Sobrevivido: {minutes:00}:{seconds:00}";

                if (finalKillsText != null)
                    finalKillsText.text = $"Enemigos Eliminados: {kills}";

                if (finalScoreText != null)
                    finalScoreText.text = $"Puntuación Total: {score:N0}";
            }
        }

        public void UpdateHealth(float current, float max)
        {
            targetHealthFill = Mathf.Clamp01(current / max);
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        public void UpdateTimer(float seconds)
        {
            if (timerText != null)
            {
                int mins = Mathf.FloorToInt(seconds / 60f);
                int secs = Mathf.FloorToInt(seconds % 60f);
                timerText.text = $"{mins:00}:{secs:00}";
            }
        }

        public void UpdateKills(int kills)
        {
            if (killsText != null)
            {
                killsText.text = $"Bajas: {kills}";
            }
        }

        public void FlashDamageOverlay()
        {
            damageFlashAlpha = 0.55f;
            if (damageVignette != null)
            {
                damageVignette.alpha = damageFlashAlpha;
            }
        }

        private void OnRetryClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame(keepPlacement: true);
            }
        }

        private void OnReanchorClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame(keepPlacement: false);
            }
        }
    }
}
