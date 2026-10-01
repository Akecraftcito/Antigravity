using UnityEngine;
using ARSurvivalGame.Enemies;
using ARSurvivalGame.Player;
using ARSurvivalGame.UI;

namespace ARSurvivalGame.Core
{
    public enum GameState
    {
        ScanningPlacement,
        Playing,
        GameOver
    }

    /// <summary>
    /// Núcleo del juego: controla el ciclo de vida (escaneo, partida activa, fin de partida y reinicio).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ARPlacementController placementController;
        [SerializeField] private UIManager uiManager;

        private GameState currentState = GameState.ScanningPlacement;
        private PlayerController activePlayer;
        private Vector3 arenaCenter;
        private float survivalTimer = 0f;
        private int enemiesKilled = 0;
        private int totalScore = 0;

        public GameState CurrentState => currentState;
        public float SurvivalTimer => survivalTimer;
        public int EnemiesKilled => enemiesKilled;
        public int TotalScore => totalScore;
        public Vector3 ArenaCenter => arenaCenter;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (placementController == null) placementController = FindAnyObjectByType<ARPlacementController>();
            if (enemySpawner == null) enemySpawner = FindAnyObjectByType<EnemySpawner>();
            if (uiManager == null) uiManager = FindAnyObjectByType<UIManager>();
        }

        private void Start()
        {
            SetState(GameState.ScanningPlacement);
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                survivalTimer += Time.deltaTime;
                if (uiManager != null)
                {
                    uiManager.UpdateTimer(survivalTimer);
                }
            }
        }

        public void OnPlacementConfirmed(Vector3 placementPosition)
        {
            arenaCenter = placementPosition;
            StartGame(placementPosition);
        }

        public void StartGame(Vector3 position)
        {
            survivalTimer = 0f;
            enemiesKilled = 0;
            totalScore = 0;

            if (uiManager != null)
            {
                uiManager.UpdateKills(0);
                uiManager.UpdateTimer(0f);
            }

            // Instanciar o reubicar al jugador
            if (activePlayer == null)
            {
                if (playerPrefab != null)
                {
                    activePlayer = Instantiate(playerPrefab, position + Vector3.up * 0.05f, Quaternion.identity);
                }
                else
                {
                    // Buscar si ya existe en la escena o crear uno
                    activePlayer = FindAnyObjectByType<PlayerController>();
                    if (activePlayer == null)
                    {
                        GameObject playerObj = new GameObject("AR_Player");
                        playerObj.transform.position = position + Vector3.up * 0.05f;
                        activePlayer = playerObj.AddComponent<PlayerController>();
                    }
                }
            }

            float arenaRadius = placementController != null ? placementController.ArenaRadius : 1.8f;
            activePlayer.SetArenaBounds(arenaCenter, arenaRadius * 0.85f);
            activePlayer.ResetPlayer(position + Vector3.up * 0.05f);

            // Iniciar generador de enemigos
            if (enemySpawner != null)
            {
                enemySpawner.StartSpawning(arenaCenter);
            }

            SetState(GameState.Playing);
        }

        public void OnEnemyKilled(int scoreGained)
        {
            if (currentState != GameState.Playing) return;

            enemiesKilled++;
            totalScore += scoreGained;

            if (uiManager != null)
            {
                uiManager.UpdateKills(enemiesKilled);
            }
        }

        public void OnPlayerDied()
        {
            if (currentState != GameState.Playing) return;

            if (enemySpawner != null)
            {
                enemySpawner.StopSpawning();
            }

            SetState(GameState.GameOver);
        }

        public void RestartGame(bool keepPlacement)
        {
            if (enemySpawner != null)
            {
                enemySpawner.ClearAllEnemies();
            }

            if (keepPlacement)
            {
                StartGame(arenaCenter);
            }
            else
            {
                if (activePlayer != null)
                {
                    activePlayer.gameObject.SetActive(false);
                }

                if (placementController != null)
                {
                    placementController.ResetPlacement();
                }

                SetState(GameState.ScanningPlacement);
            }
        }

        private void SetState(GameState newState)
        {
            currentState = newState;

            switch (currentState)
            {
                case GameState.ScanningPlacement:
                    if (uiManager != null) uiManager.ShowPlacementUI();
                    break;
                case GameState.Playing:
                    if (uiManager != null) uiManager.ShowHUD();
                    break;
                case GameState.GameOver:
                    if (uiManager != null) uiManager.ShowGameOver(survivalTimer, enemiesKilled, totalScore);
                    break;
            }
        }
    }
}
