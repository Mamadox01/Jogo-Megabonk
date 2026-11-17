using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Estadísticas Base (Escalables por Tomos/Ítems)")]
    public float moveSpeed = 8f;
    public float jumpHeight = 2.2f;
    public int extraJumps = 1;
    public float gravity = -24f;

    [Header("Movimiento y Slide")]
    public float rotationSpeed = 12f;
    public float slideSpeedBoost = 1.45f;
    public float momentumDecay = 4f;

    private CharacterController _controller;
    private Transform _camTransform;

    private Vector3 _velocity;
    private Vector3 _momentum;
    private int _jumpsRemaining;
    private bool _isSliding;

    // Acciones rápidas del nuevo Input System
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _slideAction;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (Camera.main != null) _camTransform = Camera.main.transform;

        _moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        _jumpAction.AddBinding("<Gamepad>/buttonSouth");

        _slideAction = new InputAction("Slide", binding: "<Keyboard>/leftCtrl");
        _slideAction.AddBinding("<Keyboard>/c");
        _slideAction.AddBinding("<Gamepad>/buttonEast");
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        _slideAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        _slideAction.Disable();
    }

    private void Update()
    {
        HandleMovementAndSlide();
        HandleJumpAndGravity();
    }

    private void HandleMovementAndSlide()
    {
        Vector2 input = _moveAction.ReadValue<Vector2>();
        _isSliding = _slideAction.IsPressed();

        // Dirección relativa a la cámara
        Vector3 forward = _camTransform ? _camTransform.forward : Vector3.forward;
        Vector3 right = _camTransform ? _camTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = (forward * input.y + right * input.x).normalized;

        if (moveDir.sqrMagnitude > 0.01f && !_isSliding)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Impulso al deslizarse en pendientes (estilo Megabonk)
        if (_isSliding && _controller.isGrounded && IsOnSlope(out Vector3 slopeNormal))
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
            _momentum += slopeDir * (moveSpeed * slideSpeedBoost * Time.deltaTime * 3f);
        }
        else
        {
            float decay = _controller.isGrounded ? momentumDecay : momentumDecay * 0.25f;
            _momentum = Vector3.Lerp(_momentum, Vector3.zero, decay * Time.deltaTime);
        }

        float currentSpeed = _isSliding ? moveSpeed * 0.6f : moveSpeed;
        Vector3 horizontalMove = (moveDir * currentSpeed) + _momentum;
        _controller.Move(horizontalMove * Time.deltaTime);
    }

    private void HandleJumpAndGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -3f;
            _jumpsRemaining = extraJumps;
        }

        if (_jumpAction.WasPressedThisFrame())
        {
            if (_controller.isGrounded)
            {
                ExecuteJump();
            }
            else if (_jumpsRemaining > 0)
            {
                _jumpsRemaining--;
                ExecuteJump();
            }
        }

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void ExecuteJump()
    {
        _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private bool IsOnSlope(out Vector3 normal)
    {
        normal = Vector3.up;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, (_controller.height * 0.5f) + 0.4f))
        {
            normal = hit.normal;
            return Vector3.Angle(Vector3.up, hit.normal) > 5f;
        }
        return false;
    }
}
