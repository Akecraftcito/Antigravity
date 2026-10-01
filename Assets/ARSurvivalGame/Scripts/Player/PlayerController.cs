using UnityEngine;
using ARSurvivalGame.Core;
using ARSurvivalGame.Enemies;
using ARSurvivalGame.Input;
using ARSurvivalGame.Procedural;
using ARSurvivalGame.UI;
using ARSurvivalGame.VFX;

namespace ARSurvivalGame.Player
{
    /// <summary>
    /// Controlador del personaje jugador. Gestiona el movimiento mediante VirtualJoystick y teclado,
    /// auto-apuntado y disparo de proyectiles, y sistema de vida.
    /// Soporta sustitución del modelo 3D desde el Inspector.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Custom 3D Model (Inspector)")]
        [Tooltip("Arrastra aquí tu propio modelo o prefab 3D del personaje. Si está vacío, se generará el modelo procedural.")]
        [SerializeField] private GameObject customModelPrefab;

        [Header("Custom Bullet Prefab (Opcional)")]
        [SerializeField] private GameObject customBulletPrefab;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Combat Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float fireRate = 3.2f; // Balas por segundo
        [Tooltip("Radio del área de disparo y auto-apuntado del jugador")]
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private bool autoShoot = true;
        [SerializeField] private bool showRangeIndicator = true;

        [Header("Arena Bounds")]
        [SerializeField] private float maxArenaRadius = 1.8f;

        private float currentHealth;
        private float nextFireTime = 0f;
        private Vector3 arenaCenter;
        private bool isDead = false;
        private Camera arCamera;
        private EnemyController currentTarget;
        private GameObject rangeIndicator;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Start()
        {
            arCamera = Camera.main;
            arenaCenter = transform.position;
            currentHealth = maxHealth;

            // Instanciar modelo personalizado o generar el modelo procedural
            if (customModelPrefab != null)
            {
                Instantiate(customModelPrefab, transform.position, transform.rotation, transform);
            }
            else
            {
                ProceduralModelBuilder.CreatePlayerModel(transform);
            }

            // Crear indicador visual del área de disparo
            CreateRangeIndicatorVisual();

            // Asegurar que tenga collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 0.25f;
                sc.center = new Vector3(0f, 0.15f, 0f);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            }
        }

        private void CreateRangeIndicatorVisual()
        {
            if (!showRangeIndicator) return;

            rangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeIndicator.name = "ShootingRangeIndicator";
            rangeIndicator.transform.SetParent(transform, false);
            rangeIndicator.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            rangeIndicator.transform.localScale = new Vector3(attackRange * 2f, 0.001f, attackRange * 2f);
            rangeIndicator.GetComponent<MeshRenderer>().sharedMaterial = ProceduralModelBuilder.GetReticleMaterial();
            Destroy(rangeIndicator.GetComponent<Collider>());
        }

        public void SetArenaBounds(Vector3 center, float radius)
        {
            arenaCenter = center;
            maxArenaRadius = radius;
        }

        private void Update()
        {
            if (rangeIndicator != null)
            {
                rangeIndicator.transform.rotation = Quaternion.identity;
                rangeIndicator.transform.localScale = new Vector3(attackRange * 2f, 0.001f, attackRange * 2f);
            }

            if (isDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            HandleMovement();
            HandleCombat();

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Shoot();
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                Shoot();
            }
#endif
        }

        private void HandleMovement()
        {
            Vector2 inputDir = Vector2.zero;
            if (VirtualJoystick.Instance != null)
            {
                inputDir = VirtualJoystick.Instance.Direction;
            }

            // Calcular dirección en el plano XZ relativa a la vista de la cámara AR
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;

            if (arCamera != null)
            {
                camForward = arCamera.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                camRight = arCamera.transform.right;
                camRight.y = 0f;
                camRight.Normalize();
            }

            Vector3 moveDirection = (camRight * inputDir.x + camForward * inputDir.y);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

                // Limitar al radio de la arena
                Vector3 offsetFromCenter = newPosition - arenaCenter;
                offsetFromCenter.y = 0f;
                if (offsetFromCenter.magnitude > maxArenaRadius)
                {
                    offsetFromCenter = offsetFromCenter.normalized * maxArenaRadius;
                    newPosition = new Vector3(arenaCenter.x + offsetFromCenter.x, newPosition.y, arenaCenter.z + offsetFromCenter.z);
                }

                transform.position = newPosition;

                // Si no hay enemigo en rango, rotar hacia la dirección del movimiento
                if (currentTarget == null)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDirection.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
            }
        }

        private void HandleCombat()
        {
            // Buscar el enemigo más cercano
            currentTarget = FindClosestEnemy();

            if (currentTarget != null)
            {
                // Rotar suavemente hacia el enemigo objetivo
                Vector3 toEnemy = currentTarget.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(toEnemy.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed * 1.5f);
                }

                // Disparo automático si está activado
                if (autoShoot && Time.time >= nextFireTime)
                {
                    Shoot();
                }
            }
        }

        public void Shoot()
        {
            if (isDead) return;

            nextFireTime = Time.time + (1f / fireRate);

            Vector3 spawnPos = transform.position + transform.forward * 0.22f + Vector3.up * 0.15f;
            Vector3 shootDirection = transform.forward;

            if (currentTarget != null)
            {
                Vector3 targetCenter = currentTarget.transform.position + Vector3.up * 0.16f;
                Vector3 toTarget = targetCenter - spawnPos;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    shootDirection = toTarget.normalized;
                }
            }

            Quaternion spawnRot = Quaternion.LookRotation(shootDirection);

            GameObject bulletObj;
            if (customBulletPrefab != null)
            {
                bulletObj = Instantiate(customBulletPrefab, spawnPos, spawnRot);
            }
            else
            {
                bulletObj = ProceduralModelBuilder.CreateBulletObject();
                bulletObj.transform.position = spawnPos;
                bulletObj.transform.rotation = spawnRot;
                bulletObj.AddComponent<Bullet>();
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayMuzzleFlash(spawnPos, shootDirection);
            }
        }

        private EnemyController FindClosestEnemy()
        {
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            EnemyController closest = null;
            float minSqrDist = attackRange * attackRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                float sqrDist = (enemies[i].transform.position - transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closest = enemies[i];
                }
            }

            return closest;
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            if (currentHealth < 0f) currentHealth = 0f;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
                UIManager.Instance.FlashDamageOverlay();
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayExplosion(transform.position + Vector3.up * 0.2f, new Color(0.2f, 0.7f, 1f));
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied();
            }

            gameObject.SetActive(false);
        }

        public void ResetPlayer(Vector3 spawnPosition)
        {
            gameObject.SetActive(true);
            isDead = false;
            currentHealth = maxHealth;
            transform.position = spawnPosition;
            arenaCenter = spawnPosition;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
