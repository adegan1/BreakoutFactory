using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraPanController : MonoBehaviour
{
    [Header("Keyboard Pan")]
    [SerializeField] private float keyboardPanSpeed = 14f;

    [Header("Middle Mouse Drag")]
    [SerializeField] private float dragPanMultiplier = 1f;

    [Header("Scroll Zoom")]
    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float minOrthographicSize = 2f;
    [SerializeField] private float maxOrthographicSize = 30f;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool clampToBounds;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);

    private Camera controlledCamera;
    private bool isDragging;
    private Vector2 previousDragScreenPoint;

    private static bool hasSavedState;
    private static Vector3 savedPosition;
    private static float savedOrthographicSize;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();

        if (hasSavedState)
        {
            transform.position = savedPosition;
            if (controlledCamera != null && controlledCamera.orthographic)
            {
                controlledCamera.orthographicSize = Mathf.Clamp(savedOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }
        }
    }

    private void OnDisable()
    {
        SaveState();
    }

    private void SaveState()
    {
        savedPosition = transform.position;
        if (controlledCamera != null && controlledCamera.orthographic)
        {
            savedOrthographicSize = controlledCamera.orthographicSize;
        }
        hasSavedState = true;
    }

    public static void ClearSavedState()
    {
        hasSavedState = false;
        savedPosition = Vector3.zero;
        savedOrthographicSize = 0f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        HandleKeyboardPan(keyboard);
        HandleMiddleMouseDrag(mouse);
        HandleScrollZoom(mouse);
        ApplyBoundsClamp();
    }

    private void HandleKeyboardPan(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return;
        }

        Vector2 movementInput = Vector2.zero;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            movementInput.y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            movementInput.y -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            movementInput.x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            movementInput.x -= 1f;
        }

        if (movementInput.sqrMagnitude <= 0f)
        {
            return;
        }

        movementInput.Normalize();
        Vector3 movement = new Vector3(movementInput.x, movementInput.y, 0f) * keyboardPanSpeed * Time.unscaledDeltaTime;
        transform.position += movement;
    }

    private void HandleMiddleMouseDrag(Mouse mouse)
    {
        if (mouse == null || controlledCamera == null)
        {
            return;
        }

        if (mouse.middleButton.wasPressedThisFrame)
        {
            previousDragScreenPoint = mouse.position.ReadValue();
            isDragging = true;
        }

        if (mouse.middleButton.wasReleasedThisFrame)
        {
            isDragging = false;
            return;
        }

        if (!isDragging || !mouse.middleButton.isPressed)
        {
            return;
        }

        Vector2 currentScreenPoint = mouse.position.ReadValue();
        Vector2 screenDelta = previousDragScreenPoint - currentScreenPoint;

        if (screenDelta.sqrMagnitude < 0.01f)
        {
            previousDragScreenPoint = currentScreenPoint;
            return;
        }

        float worldUnitsPerPixel = controlledCamera.orthographicSize * 2f / Mathf.Max(Screen.height, 1);
        Vector3 worldDelta = new Vector3(screenDelta.x * worldUnitsPerPixel, screenDelta.y * worldUnitsPerPixel, 0f);
        transform.position += worldDelta * dragPanMultiplier;

        previousDragScreenPoint = currentScreenPoint;
    }



    private void HandleScrollZoom(Mouse mouse)
    {
        if (mouse == null || controlledCamera == null || !controlledCamera.orthographic)
        {
            return;
        }

        float scrollDelta = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollDelta) < 0.01f)
        {
            return;
        }

        float orthoSize = controlledCamera.orthographicSize;
        orthoSize -= scrollDelta * zoomSensitivity * 0.1f;
        orthoSize = Mathf.Clamp(orthoSize, minOrthographicSize, maxOrthographicSize);
        controlledCamera.orthographicSize = orthoSize;
    }

    private void ApplyBoundsClamp()
    {
        if (!clampToBounds)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
        transform.position = position;
    }
}
