using UnityEngine;

public class SimpleBattleCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float rotationSpeed = 90f;

    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;

    [SerializeField] private Vector2 boundsX = new Vector2(-50, 50);
    [SerializeField] private Vector2 boundsZ = new Vector2(-50, 50);

    private Vector3 lastMousePosition;

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 movement = direction * moveSpeed * Time.deltaTime;

        Vector3 newPosition = transform.position + movement;
        newPosition.x = Mathf.Clamp(newPosition.x, boundsX.x, boundsX.y);
        newPosition.z = Mathf.Clamp(newPosition.z, boundsZ.x, boundsZ.y);

        transform.position = newPosition;
    }

    private void HandleRotation()
    {
        float rotation = 0f;

        if (Input.GetKey(KeyCode.Q)) rotation = -1f;
        if (Input.GetKey(KeyCode.E)) rotation = 1f;

        transform.Rotate(0, rotation * rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Vector3 zoom = transform.forward * scroll * zoomSpeed;

        Vector3 newPosition = transform.position + zoom;
        float currentZoom = Vector3.Distance(newPosition, transform.position);

        if (currentZoom >= minZoom && currentZoom <= maxZoom)
        {
            transform.position = newPosition;
        }
    }
}
