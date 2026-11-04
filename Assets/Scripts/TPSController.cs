using UnityEngine;
using UnityEngine.InputSystem;

public class TPSController : MonoBehaviour
{
    private CharacterController _characterController;

    private InputAction _moveAction;
    private Vector2 _moveInput;
    private InputAction _jumpAction;
    private InputAction _aimAction;
    private InputAction _lookAction;
    private Vector2 _lookInput;
    
    [SerializeField] private float _movementSpeed = 10;
    [SerializeField] private float _jumpHeight = 2;

    private float _gravity;
    [SerializeField] private Vector3 _playerGravity;

    [SerializeField] private Transform _sensorTransform;
    [SerializeField] private float _sensorRadius;
    [SerializeField] private LayerMask _groundLayer;

    private Transform _cameraTransform;
    private float _xRotation;
    [SerializeField] private Transform _lookAtCamera;
    [SerializeField] private float _cameraSensitivity = 10;

    [SerializeField] private GameObject _normalCamera;
    [SerializeField] private GameObject _aimCamera;
    

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        _moveAction = InputSystem.actions["Move"];
        _jumpAction = InputSystem.actions["Jump"];
        _aimAction = InputSystem.actions["Aim"];
        _lookAction = InputSystem.actions["Look"];

        _cameraTransform = Camera.main.transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _gravity = Physics.gravity.y;
    }

    // Update is called once per frame
    void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();
        _lookInput = _lookAction.ReadValue<Vector2>();

        Gravity();

        if(_jumpAction.WasPressedThisFrame() && IsGrounded())
        {
            Jump();
        }

        TPSMovement();

        if(_aimAction.WasPressedThisFrame())
        {
            _normalCamera.SetActive(false);
            _aimCamera.SetActive(true);
        }
        else if(_aimAction.WasReleasedThisFrame())
        {
            _normalCamera.SetActive(true);
            _aimCamera.SetActive(false);
        }
    }

    void TPSMovement()
    {
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y);

        float MouseX = _lookInput.x * _cameraSensitivity * Time.deltaTime;
        float MouseY = _lookInput.y * _cameraSensitivity * Time.deltaTime;

        _xRotation -= MouseY;
        _xRotation = Mathf.Clamp(_xRotation, -89, 89);

        transform.Rotate(Vector3.up, MouseX);
        _lookAtCamera.localRotation = Quaternion.Euler(_xRotation, 0, 0);

        if(direction != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            _characterController.Move(moveDirection * _movementSpeed * Time.deltaTime);
        }
    }

    void Gravity()
    {
        if(!IsGrounded())
        {
            _playerGravity.y += _gravity * Time.deltaTime;
        }
        else if(IsGrounded() && _playerGravity.y < 0)
        {
            _playerGravity.y = _gravity;
        }

        _characterController.Move(_playerGravity * Time.deltaTime);
    }

    void Jump()
    {
        _playerGravity.y = Mathf.Sqrt(_jumpHeight * -2 * _gravity);
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(_sensorTransform.position, _sensorRadius, _groundLayer);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_sensorTransform.position, _sensorRadius);
    }
}
