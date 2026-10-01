using UnityEngine;
using ARSurvivalGame.Core;
using ARSurvivalGame.Player;
using ARSurvivalGame.Procedural;
using ARSurvivalGame.VFX;

namespace ARSurvivalGame.Enemies
{
    /// <summary>
    /// Controlador de enemigo volador/acechador con movimiento procedural, daño al jugador y sistema de salud.
    /// Admite sustitución del modelo 3D en el Inspector.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [Header("Custom 3D Model (Inspector)")]
        [Tooltip("Arrastra aquí tu propio modelo o prefab 3D. Si se deja vacío, se generará el modelo procedural.")]
        [SerializeField] private GameObject customModelPrefab;

        [Header("Stats")]
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private float moveSpeed = 1.3f;
        [SerializeField] private float damage = 15f;
        [SerializeField] private float attackRange = 0.45f;
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private int scoreValue = 100;

        private float currentHealth;
        private Transform playerTarget;
        private float lastAttackTime = 0f;
        private float bobbingOffset;
        private float initialY;
        private bool isDead = false;

        private void Start()
        {
            currentHealth = maxHealth;
            bobbingOffset = Random.Range(0f, 10f);
            initialY = transform.position.y;

            // Instanciar modelo personalizado o generar el procedural
            if (customModelPrefab != null)
            {
                Instantiate(customModelPrefab, transform.position, transform.rotation, transform);
            }
            else
            {
                ProceduralModelBuilder.CreateEnemyModel(transform);
            }

            // Asegurar que tenga un collider y Rigidbody kinematic para recibir impactos de bala
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.radius = 0.32f;
                sc.center = new Vector3(0f, 0.16f, 0f);
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Buscar al jugador si no está asignado
            if (playerTarget == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null) playerTarget = player.transform;
            }
        }

        public void SetTarget(Transform target)
        {
            playerTarget = target;
        }

        public void SetInitialY(float y)
        {
            initialY = y;
        }

        public void ScaleDifficulty(float healthMultiplier, float speedMultiplier)
        {
            maxHealth *= healthMultiplier;
            currentHealth = maxHealth;
            moveSpeed *= speedMultiplier;
        }

        private void Update()
        {
            if (isDead || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            if (playerTarget == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null) playerTarget = player.transform;
                return;
            }

            // Calcular dirección horizontal hacia el jugador
            Vector3 targetPos = playerTarget.position;
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            // Rotar suavemente hacia el jugador
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 6f);
            }

            // Movimiento hacia el jugador
            if (distance > attackRange)
            {
                Vector3 moveDir = toTarget.normalized;
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
            else
            {
                // Dentro del rango de ataque: golpear periódicamente
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    AttackPlayer();
                }
            }

            // Animación procedural de flotación (Bobbing)
            float newY = initialY + Mathf.Sin(Time.time * 5f + bobbingOffset) * 0.035f;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void AttackPlayer()
        {
            if (playerTarget == null) return;
            PlayerController player = playerTarget.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayHitSparks(playerTarget.position + Vector3.up * 0.15f, (transform.position - playerTarget.position).normalized);
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;

            // Breve salto visual al recibir daño
            transform.localScale = Vector3.one * 1.15f;
            Invoke(nameof(ResetScale), 0.08f);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void ResetScale()
        {
            transform.localScale = Vector3.one;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayExplosion(transform.position + Vector3.up * 0.15f, new Color(1f, 0.2f, 0.1f));
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyKilled(scoreValue);
            }

            Destroy(gameObject);
        }
    }
}
