using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private bool mouseControls;
    [SerializeField] private float moveSpeed = 10f;

    [Header("Clamp")]
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 7f;

    private Camera mainCamera;

    public bool MouseControls
    {
        get => mouseControls;
        set => mouseControls = value;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mouseControls)
        {
            MoveWithMouse();
            return;
        }

        MoveWithKeyboard();
    }

    private void MoveWithKeyboard()
    {
        float horizontal = 0f;
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        float targetX = transform.position.x + horizontal * moveSpeed * Time.deltaTime;
        SetPositionX(targetX);
    }

    private void MoveWithMouse()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector3 mousePosition = mouse.position.ReadValue();
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        SetPositionX(worldPosition.x);
    }

    private void SetPositionX(float x)
    {
        float clampedX = Mathf.Clamp(x, minX, maxX);
        Vector3 position = transform.position;
        position.x = clampedX;
        transform.position = position;
    }
}
