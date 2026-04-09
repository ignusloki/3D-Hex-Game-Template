using UnityEngine;

public class HexCameraController : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 12f;
    [Min(0.1f)] public float rotationSpeed = 90f;
    [Min(0.1f)] public float moveRange = 15f;

    private Vector3 initialPosition;
    private float initialYaw;
    private float initialPitch;
    private float initialRoll;
    private float yawOffset;

    private void Awake()
    {
        initialPosition = transform.position;
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        initialPitch = eulerAngles.x;
        initialYaw = eulerAngles.y;
        initialRoll = eulerAngles.z;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        moveRange = Mathf.Max(0.1f, moveRange);
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = GetMoveInput();
        if (moveInput.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;

        Vector3 right = transform.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0f ? right.normalized : Vector3.right;

        Vector3 offset = ((right * moveInput.x) + (forward * moveInput.y)) * (moveSpeed * Time.deltaTime);
        Vector3 targetPosition = transform.position + offset;

        float clampedX = Mathf.Clamp(targetPosition.x, initialPosition.x - moveRange, initialPosition.x + moveRange);
        float clampedZ = Mathf.Clamp(targetPosition.z, initialPosition.z - moveRange, initialPosition.z + moveRange);

        transform.position = new Vector3(clampedX, initialPosition.y, clampedZ);
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
