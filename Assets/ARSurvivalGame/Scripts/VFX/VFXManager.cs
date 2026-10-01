using UnityEngine;

namespace ARSurvivalGame.VFX
{
    /// <summary>
    /// Gestiona y reproduce efectos visuales (VFX) procedimentales y partículas para impactos, disparos y explosiones.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Custom VFX Prefabs (Opcionales para el Inspector)")]
        [SerializeField] private GameObject customExplosionPrefab;
        [SerializeField] private GameObject customMuzzleFlashPrefab;
        [SerializeField] private GameObject customHitSparksPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        /// <summary>
        /// Genera una explosión con partículas chispeantes y desvanecimiento.
        /// </summary>
        public void PlayExplosion(Vector3 position, Color primaryColor)
        {
            if (customExplosionPrefab != null)
            {
                Instantiate(customExplosionPrefab, position, Quaternion.identity);
                return;
            }

            GameObject expObj = new GameObject("ProceduralExplosion");
            expObj.transform.position = position;

            ParticleSystem ps = expObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = expObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Procedural.ProceduralModelBuilder.GetSafeShader(lit: false));
            psRenderer.material.color = primaryColor;

            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3.5f;
            main.startSize = 0.08f;
            main.startColor = primaryColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.duration = 0.4f;
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(primaryColor, 0.0f), new GradientColorKey(Color.white, 0.2f), new GradientColorKey(Color.yellow, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            ps.Play();
            Destroy(expObj, 1.2f);
        }

        /// <summary>
        /// Genera chispas de impacto cuando un proyectil golpea a un enemigo.
        /// </summary>
        public void PlayHitSparks(Vector3 position, Vector3 normal)
        {
            if (customHitSparksPrefab != null)
            {
                Instantiate(customHitSparksPrefab, position, Quaternion.LookRotation(normal));
                return;
            }

            GameObject hitObj = new GameObject("HitSparks");
            hitObj.transform.position = position;

            ParticleSystem ps = hitObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = hitObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Procedural.ProceduralModelBuilder.GetSafeShader(lit: false));
            psRenderer.material.color = Color.cyan;

            var main = ps.main;
            main.startLifetime = 0.25f;
            main.startSpeed = 2f;
            main.startSize = 0.05f;
            main.startColor = new Color(0.3f, 0.9f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.duration = 0.2f;
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;

            hitObj.transform.rotation = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;

            ps.Play();
            Destroy(hitObj, 0.8f);
        }

        /// <summary>
        /// Destello de disparo en la boca del cañón.
        /// </summary>
        public void PlayMuzzleFlash(Vector3 position, Vector3 direction)
        {
            if (customMuzzleFlashPrefab != null)
            {
                Instantiate(customMuzzleFlashPrefab, position, Quaternion.LookRotation(direction));
                return;
            }

            GameObject flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.position = position;

            ParticleSystem ps = flashObj.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = flashObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = new Material(Procedural.ProceduralModelBuilder.GetSafeShader(lit: false));
            psRenderer.material.color = Color.cyan;

            var main = ps.main;
            main.startLifetime = 0.12f;
            main.startSpeed = 1.5f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.4f, 1f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.duration = 0.1f;
            main.loop = false;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 8) });

            flashObj.transform.rotation = Quaternion.LookRotation(direction);

            ps.Play();
            Destroy(flashObj, 0.5f);
        }
    }
}
