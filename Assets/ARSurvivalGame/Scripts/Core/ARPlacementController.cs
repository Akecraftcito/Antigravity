using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARSurvivalGame.Procedural;

namespace ARSurvivalGame.Core
{
    /// <summary>
    /// Gestiona la detección de superficies AR (ARPlane) mediante ARRaycastManager y el posicionamiento de la arena.
    /// Incluye soporte y fallback automático para pruebas directas en el Unity Editor.
    /// </summary>
    public class ARPlacementController : MonoBehaviour
    {
        public static ARPlacementController Instance { get; private set; }

        [Header("AR References")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Reticle & Arena Settings")]
        [SerializeField] private GameObject customReticlePrefab;
        [SerializeField] private float arenaRadius = 1.8f;

        private GameObject reticleInstance;
        private GameObject arenaBoundaryInstance;
        private Pose currentPose;
        private bool hasPlacementPose = false;
        private bool isPlacementLocked = false;
        private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

        public bool IsPlacementLocked => isPlacementLocked;
        public Pose CurrentPose => currentPose;
        public float ArenaRadius => arenaRadius;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (raycastManager == null) raycastManager = FindAnyObjectByType<ARRaycastManager>();
            if (planeManager == null) planeManager = FindAnyObjectByType<ARPlaneManager>();
        }

        private void Start()
        {
            // Instanciar o crear retículo holográfico
            if (customReticlePrefab != null)
            {
                reticleInstance = Instantiate(customReticlePrefab);
            }
            else
            {
                reticleInstance = ProceduralModelBuilder.CreateReticleObject();
            }

            reticleInstance.SetActive(false);
        }

        private void Update()
        {
            if (isPlacementLocked) return;

            UpdatePlacementPose();
            UpdateReticleVisual();

            // Detectar interacción para fijar el juego en la superficie
            CheckPlacementInput();
        }

        private void UpdatePlacementPose()
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            hasPlacementPose = false;

            // 1. Intentar Raycast ARFoundation contra ARPlanes
            if (raycastManager != null && raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds))
            {
                currentPose = hits[0].pose;
                hasPlacementPose = true;
                return;
            }

            // 2. Fallback para Unity Editor: Proyectar contra plano virtual en Y = 0
            if (Application.isEditor)
            {
                Ray ray = Camera.main != null ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) : new Ray(Vector3.up * 1.5f, Vector3.down);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

                if (groundPlane.Raycast(ray, out float enter))
                {
                    currentPose.position = ray.GetPoint(enter);
                    currentPose.rotation = Quaternion.identity;
                    hasPlacementPose = true;
                }
            }
        }

        private void UpdateReticleVisual()
        {
            if (reticleInstance == null) return;

            if (hasPlacementPose)
            {
                reticleInstance.SetActive(true);
                reticleInstance.transform.position = currentPose.position;
                reticleInstance.transform.rotation = currentPose.rotation;

                // Animación de pulso holográfico
                float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.06f;
                reticleInstance.transform.localScale = new Vector3(pulse, 1f, pulse);
            }
            else
            {
                reticleInstance.SetActive(false);
            }
        }

        private void CheckPlacementInput()
        {
            if (!hasPlacementPose) return;

            bool triggered = false;

            // Detección en móvil (toque en pantalla que no esté sobre la UI)
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        triggered = true;
                    }
                }
            }
            // Detección en Unity Editor (clic del ratón)
            else if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    triggered = true;
                }
            }

            if (triggered)
            {
                ConfirmPlacement();
            }
        }

        public void ConfirmPlacement()
        {
            isPlacementLocked = true;
            if (reticleInstance != null) reticleInstance.SetActive(false);

            // Generar o posicionar el anillo de la arena
            if (arenaBoundaryInstance == null)
            {
                arenaBoundaryInstance = ProceduralModelBuilder.CreateArenaBoundaryObject(arenaRadius);
            }
            arenaBoundaryInstance.transform.position = currentPose.position;
            arenaBoundaryInstance.SetActive(true);

            // Ocultar planos detectados para una experiencia visual limpia
            SetPlanesActive(false);

            // Notificar al GameManager para arrancar la partida
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlacementConfirmed(currentPose.position);
            }
        }

        public void ResetPlacement()
        {
            isPlacementLocked = false;
            hasPlacementPose = false;

            if (arenaBoundaryInstance != null) arenaBoundaryInstance.SetActive(false);
            if (reticleInstance != null) reticleInstance.SetActive(false);

            // Reactivar visualización de planos AR
            SetPlanesActive(true);
        }

        private void SetPlanesActive(bool active)
        {
            if (planeManager != null)
            {
                foreach (var plane in planeManager.trackables)
                {
                    plane.gameObject.SetActive(active);
                }
                planeManager.enabled = active;
            }
        }
    }
}
