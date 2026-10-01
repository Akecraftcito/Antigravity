using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ARSurvivalGame.Input
{
    /// <summary>
    /// Joystick virtual en pantalla optimizado para dispositivos móviles táctiles y emulador en Unity Editor (mouse y teclado).
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static VirtualJoystick Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform handleRect;

        [Header("Settings")]
        [SerializeField] private float handleRange = 60f;
        [SerializeField] private bool allowKeyboardFallback = true;

        private Canvas parentCanvas;
        private Camera targetCamera;
        private Vector2 inputVector = Vector2.zero;
        private bool isPointerDragging = false;

        public Vector2 Direction => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                targetCamera = parentCanvas.worldCamera;
            }

            if (backgroundRect == null) backgroundRect = GetComponent<RectTransform>();
            if (handleRect == null && transform.childCount > 0)
            {
                handleRect = transform.GetChild(0).GetComponent<RectTransform>();
            }
        }

        private void Update()
        {
            // Si el usuario no está arrastrando con toque/mouse, comprobar fallback de teclado en Editor
            if (!isPointerDragging && allowKeyboardFallback)
            {
                float h = 0f;
                float v = 0f;

#if ENABLE_INPUT_SYSTEM
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
                }
#else
                h = UnityEngine.Input.GetAxisRaw("Horizontal");
                v = UnityEngine.Input.GetAxisRaw("Vertical");
#endif

                Vector2 keyInput = new Vector2(h, v);
                if (keyInput.sqrMagnitude > 1f) keyInput.Normalize();

                inputVector = keyInput;

                if (handleRect != null)
                {
                    handleRect.anchoredPosition = inputVector * handleRange;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDragging = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (backgroundRect == null || handleRect == null) return;

            Vector2 position;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    backgroundRect,
                    eventData.position,
                    targetCamera,
                    out position))
            {
                // Limitar al radio del joystick
                position = Vector2.ClampMagnitude(position, handleRange);
                handleRect.anchoredPosition = position;

                // Normalizar la dirección de entrada
                inputVector = position / handleRange;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDragging = false;
            inputVector = Vector2.zero;
            if (handleRect != null)
            {
                handleRect.anchoredPosition = Vector2.zero;
            }
        }

        public void ResetJoystick()
        {
            isPointerDragging = false;
            inputVector = Vector2.zero;
            if (handleRect != null)
            {
                handleRect.anchoredPosition = Vector2.zero;
            }
        }
    }
}
