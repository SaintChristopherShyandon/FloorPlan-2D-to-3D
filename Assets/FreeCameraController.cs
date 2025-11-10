using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    public float movementSpeed = 5f;
    public float rotationSpeed = 2f;
    public float zoomSpeed = 5f;
    public float minZoomFOV = 10f;
    public float maxZoomFOV = 60f;

    private float currentZoomFOV;

    void Start()
    {
        currentZoomFOV = Camera.main.fieldOfView; // Initialize with current FOV
    }

    void Update()
    {
        // Movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        float upDownInput = 0;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb.spaceKey.isPressed) upDownInput += 1;
        if (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) upDownInput -= 1;

        Vector3 moveDirection = transform.right * horizontalInput +
                                transform.forward * verticalInput +
                                transform.up * upDownInput;

        transform.position += moveDirection * movementSpeed * Time.deltaTime;

        // Rotation
        if (Input.GetMouseButton(1)) // Right-click to rotate
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            transform.Rotate(Vector3.up, mouseX * rotationSpeed, Space.World);
            transform.Rotate(Vector3.left, mouseY * rotationSpeed, Space.Self);
        }

        // Zoom (Field of View)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            currentZoomFOV -= scrollInput * zoomSpeed;
            currentZoomFOV = Mathf.Clamp(currentZoomFOV, minZoomFOV, maxZoomFOV);
            Camera.main.fieldOfView = currentZoomFOV;
        }
    }
}