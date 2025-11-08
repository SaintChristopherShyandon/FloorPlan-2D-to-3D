using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    private AudioSource audioSource;
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null) return;

        Vector3 move = Vector3.zero;

        // WASD movement
        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        if (Keyboard.current.qKey.isPressed) move -= transform.up;   // turun
        if (Keyboard.current.eKey.isPressed) move += transform.up;   // naik

        // Sprint
        float speed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= sprintMultiplier;
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotationY += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        rotationX -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }
}
