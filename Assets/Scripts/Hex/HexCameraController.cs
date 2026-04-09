using UnityEngine;

public class HexCameraController : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 12f;
    [Min(0.1f)] public float rotationSpeed = 90f;
    [Min(0.1f)] public float moveRange = 15f;
    [Min(0.01f)] public float zoomStep = 0.1f;
    [Min(0.1f)] public float minZoomMultiplier = 0.5f;
    [Min(0.1f)] public float maxZoomMultiplier = 1.5f;

    private Camera attachedCamera;
    private Vector3 initialPosition;
    private Vector3 initialPivotPosition;
    private Vector3 initialCameraOffset;
    private float initialYaw;
    private float initialPitch;
    private float initialRoll;
    private float initialFieldOfView;
    private float initialOrthographicSize;
    private Vector3 panOffset;
    private float yawOffset;
    private float zoomMultiplier = 1f;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
        initialPosition = transform.position;
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        initialPitch = eulerAngles.x;
        initialYaw = eulerAngles.y;
        initialRoll = eulerAngles.z;

        if (attachedCamera != null)
        {
            initialFieldOfView = attachedCamera.fieldOfView;
            initialOrthographicSize = attachedCamera.orthographicSize;
        }

        initialPivotPosition = ResolveInitialPivotPosition();
        initialCameraOffset = initialPosition - initialPivotPosition;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        moveRange = Mathf.Max(0.1f, moveRange);
        zoomStep = Mathf.Max(0.01f, zoomStep);
        minZoomMultiplier = Mathf.Max(0.1f, minZoomMultiplier);
        maxZoomMultiplier = Mathf.Max(minZoomMultiplier, maxZoomMultiplier);
    }

    private void Update()
    {
        if (HandleReset())
        {
            return;
        }

        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = GetMoveInput();
        if (moveInput.sqrMagnitude <= 0f)
        {
            return;
        }

        Quaternion yawRotation = Quaternion.Euler(0f, initialYaw + yawOffset, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;

        Vector3 offset = ((right * moveInput.x) + (forward * moveInput.y)) * (moveSpeed * Time.deltaTime);
        Vector3 targetOffset = panOffset + new Vector3(offset.x, 0f, offset.z);

        panOffset = new Vector3(
            Mathf.Clamp(targetOffset.x, -moveRange, moveRange),
            0f,
            Mathf.Clamp(targetOffset.z, -moveRange, moveRange));
    }

    private void HandleRotation()
    {
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            rotationInput -= 1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            rotationInput += 1f;
        }

        if (Mathf.Approximately(rotationInput, 0f))
        {
            return;
        }

        yawOffset += rotationInput * rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(initialPitch, initialYaw + yawOffset, initialRoll);
    }

    private void HandleZoom()
    {
        float scrollInput = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scrollInput, 0f))
        {
            return;
        }

        zoomMultiplier = Mathf.Clamp(
            zoomMultiplier - (scrollInput * zoomStep),
            minZoomMultiplier,
            maxZoomMultiplier);
    }

    private bool HandleReset()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return false;
        }

        panOffset = Vector3.zero;
        yawOffset = 0f;
        zoomMultiplier = 1f;
        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(initialPitch, initialYaw, initialRoll);

        if (attachedCamera != null)
        {
            if (attachedCamera.orthographic)
            {
                attachedCamera.orthographicSize = initialOrthographicSize;
            }
            else
            {
                attachedCamera.fieldOfView = initialFieldOfView;
            }
        }

        return true;
    }

    private void LateUpdate()
    {
        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, yawOffset, 0f);
        Vector3 pivotPosition = initialPivotPosition + panOffset;
        Vector3 cameraOffset = yawRotation * (initialCameraOffset * zoomMultiplier);

        transform.position = pivotPosition + cameraOffset;
        transform.rotation = Quaternion.Euler(initialPitch, initialYaw + yawOffset, initialRoll);
    }

    private Vector3 ResolveInitialPivotPosition()
    {
        Plane groundPlane = new(Vector3.up, Vector3.zero);
        Ray cameraRay = new(initialPosition, transform.forward);
        if (groundPlane.Raycast(cameraRay, out float distance))
        {
            return cameraRay.GetPoint(distance);
        }

        Vector3 fallback = initialPosition + (transform.forward * 10f);
        fallback.y = 0f;
        return fallback;
    }

    private static Vector2 GetMoveInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            vertical += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            vertical -= 1f;
        }

        Vector2 input = new(horizontal, vertical);
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }
}
