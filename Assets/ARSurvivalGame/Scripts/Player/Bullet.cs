using UnityEngine;
using ARSurvivalGame.Enemies;
using ARSurvivalGame.VFX;

namespace ARSurvivalGame.Player
{
    /// <summary>
    /// Proyectil de plasma disparado por el jugador hacia los enemigos.
    /// Utiliza barrido continuo (SphereCast) y detección de trigger con Rigidbody para garantizar impactos al 100% sin atravesar enemigos.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 14f;
        [SerializeField] private float damage = 35f;
        [SerializeField] private float lifeTime = 2.5f;
        [SerializeField] private float hitRadius = 0.18f;

        private float timer = 0f;
        private bool hasHit = false;

        private void Awake()
        {
            // Garantizar Rigidbody kinematic para compatibilidad de física y triggers de Unity
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        public void Initialize(float customDamage, float customSpeed)
        {
            damage = customDamage;
            speed = customSpeed;
        }

        private void Update()
        {
            if (hasHit) return;

            float moveDistance = speed * Time.deltaTime;
            Vector3 moveDirection = transform.forward;
            Vector3 nextPosition = transform.position + moveDirection * moveDistance;

            // Barrido continuo con SphereCast para evitar que la bala atraviese al enemigo a altas velocidades
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, hitRadius, moveDirection, moveDistance);
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].collider.GetComponent<EnemyController>();
                if (enemy == null)
                {
                    enemy = hits[i].collider.GetComponentInParent<EnemyController>();
                }

                if (enemy != null)
                {
                    Vector3 contactPoint = hits[i].point != Vector3.zero ? hits[i].point : hits[i].collider.transform.position;
                    HitEnemy(enemy, contactPoint);
                    return;
                }
            }

            transform.position = nextPosition;

            timer += Time.deltaTime;
            if (timer >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy == null)
            {
                enemy = other.GetComponentInParent<EnemyController>();
            }

            if (enemy != null)
            {
                HitEnemy(enemy, transform.position);
            }
        }

        private void HitEnemy(EnemyController enemy, Vector3 hitPosition)
        {
            if (hasHit) return;
            hasHit = true;

            enemy.TakeDamage(damage);

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayHitSparks(hitPosition, -transform.forward);
            }

            Destroy(gameObject);
        }
    }
}
