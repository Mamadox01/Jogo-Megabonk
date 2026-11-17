using UnityEngine;
using UnityEngine.InputSystem;

public class CamaraTerceraPersona : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Órbita y Distancia")]
    public float distance = 6.5f;
    public float minPitch = -35f;
    public float maxPitch = 70f;
    public float mouseSensitivity = 0.15f;
    public float gamepadSensitivity = 140f;

    [Header("Colisión de Cámara")]
    public float cameraRadius = 0.25f;
    public LayerMask collisionLayers = ~0; // Ignora la capa del Player en el Inspector

    private float _yaw;
    private float _pitch = 15f;
    private InputAction _lookAction;

    private void Awake()
    {
        _lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        _lookAction.AddBinding("<Gamepad>/rightStick");
    }

    private void OnEnable() => _lookAction.Enable();
    private void OnDisable() => _lookAction.Disable();

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
            _yaw = target.eulerAngles.y;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Liberar cursor con Escape para pruebas en el Editor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }

        Vector2 lookInput = _lookAction.ReadValue<Vector2>();
        bool isGamepad = _lookAction.activeControl?.device is Gamepad;
        float sensitivity = isGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        _yaw += lookInput.x * sensitivity;
        _pitch -= lookInput.y * sensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivotPosition = target.position + targetOffset;
        Vector3 desiredDirection = rotation * Vector3.back;

        // Evita que la cámara se meta dentro del suelo o rampas
        float adjustedDistance = distance;
        if (Physics.SphereCast(pivotPosition, cameraRadius, desiredDirection, out RaycastHit hit, distance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            adjustedDistance = Mathf.Max(0.5f, hit.distance - 0.1f);
        }

        transform.position = pivotPosition + desiredDirection * adjustedDistance;
        transform.rotation = rotation;
    }
}
