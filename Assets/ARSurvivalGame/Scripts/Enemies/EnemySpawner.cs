using System.Collections.Generic;
using UnityEngine;
using ARSurvivalGame.Core;
using ARSurvivalGame.Procedural;
using ARSurvivalGame.VFX;

namespace ARSurvivalGame.Enemies
{
    /// <summary>
    /// Generador de oleadas de enemigos alrededor de la arena de juego con dificultad progresiva.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefab (Inspector)")]
        [Tooltip("Prefab del enemigo para instanciar. Si está vacío, se generará el objeto con EnemyController y modelo procedural.")]
        [SerializeField] private GameObject customEnemyPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadiusMin = 1.2f;
        [SerializeField] private float spawnRadiusMax = 1.8f;
        [SerializeField] private float initialSpawnInterval = 2.5f;
        [SerializeField] private float minSpawnInterval = 0.85f;
        [SerializeField] private float difficultyRampTime = 60f; // Tiempo para alcanzar dificultad máxima

        private Vector3 arenaCenter;
        private bool isSpawning = false;
        private float spawnTimer = 0f;
        private float gameTime = 0f;
        private List<EnemyController> activeEnemies = new List<EnemyController>();

        public void StartSpawning(Vector3 center)
        {
            arenaCenter = center;
            isSpawning = true;
            spawnTimer = 0.5f; // Primer enemigo aparece rápido
            gameTime = 0f;
            ClearAllEnemies();
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        private void Update()
        {
            if (!isSpawning || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            gameTime += Time.deltaTime;

            // Calcular intervalo de aparición dinámico según el tiempo transcurrido
            float progress = Mathf.Clamp01(gameTime / difficultyRampTime);
            float currentInterval = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, progress);

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = currentInterval;
                SpawnEnemy(progress);
            }
        }

        private void SpawnEnemy(float difficultyProgress)
        {
            // Posición aleatoria en el perímetro de la arena
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomRadius = Random.Range(spawnRadiusMin, spawnRadiusMax);

            Vector3 spawnPos = arenaCenter + new Vector3(
                Mathf.Cos(randomAngle) * randomRadius,
                0f,
                Mathf.Sin(randomAngle) * randomRadius
            );

            // Efecto visual de teletransporte / portal
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayExplosion(spawnPos + Vector3.up * 0.15f, new Color(1f, 0.4f, 0.1f));
            }

            GameObject enemyObj;
            if (customEnemyPrefab != null)
            {
                enemyObj = Instantiate(customEnemyPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                enemyObj = new GameObject("AR_Enemy");
                enemyObj.transform.position = spawnPos;
                enemyObj.AddComponent<EnemyController>();
            }

            EnemyController enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.SetInitialY(spawnPos.y);
                float hpMult = 1f + difficultyProgress * 0.8f;
                float spdMult = 1f + difficultyProgress * 0.4f;
                enemy.ScaleDifficulty(hpMult, spdMult);
                activeEnemies.Add(enemy);
            }
        }

        public void ClearAllEnemies()
        {
            activeEnemies.RemoveAll(e => e == null);
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i] != null)
                {
                    Destroy(activeEnemies[i].gameObject);
                }
            }
            activeEnemies.Clear();

            // Limpieza exhaustiva de cualquier enemigo residual en la escena
            EnemyController[] residualEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            for (int i = 0; i < residualEnemies.Length; i++)
            {
                if (residualEnemies[i] != null)
                {
                    Destroy(residualEnemies[i].gameObject);
                }
            }
        }
    }
}
