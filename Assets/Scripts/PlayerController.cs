using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController _characterController;
    private Animator _animator;

    private InputAction _moveAction;
    private Vector2 _moveInput;
    private InputAction _jumpAction;
    private InputAction _aimAction;
    private InputAction _interactAction;
    private InputAction _attackAction;
    
    [SerializeField] private float _movementSpeed = 10;
    [SerializeField] private float _jumpHeight = 2;
    [SerializeField] private float _pushForce = 5;
    [SerializeField] private float _throwForce = 20;

    private float _turnSmoothVelocity;
    [SerializeField] float _smoothTime = 1;

    private float _gravity;
    [SerializeField] private Vector3 _playerGravity;

    [SerializeField] private Transform _sensorTransform;
    [SerializeField] private float _sensorRadius;
    [SerializeField] private LayerMask _groundLayer;

    private Transform _cameraTransform;

    private GameObject _grabbedObject;
    [SerializeField] private Transform _handsTransform;
    [SerializeField] private float _handsRadius = 1;

    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        _moveAction = InputSystem.actions["Move"];
        _jumpAction = InputSystem.actions["Jump"];
        _aimAction = InputSystem.actions["Aim"];
        _interactAction = InputSystem.actions["Interact"];
        _attackAction = InputSystem.actions["Attack"];

        _cameraTransform = Camera.main.transform;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _gravity = Physics.gravity.y;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        _moveInput = _moveAction.ReadValue<Vector2>();

        Gravity();

        if(_jumpAction.WasPressedThisFrame() && IsGrounded())
        {
            Jump();
        }

        _animator.SetBool("IsJumping", !IsGrounded());

        if(_aimAction.IsPressed())
        {
            AimMovement();
        }
        else
        {
            TPMovement();
        }    

        if(_interactAction.WasPressedThisFrame())
        {
            GrabObject();
        }    

        if(_attackAction.WasPressedThisFrame())
        {
            ThrowObject();
        }
    }

    void TopDownMovement()
    {
        Vector3 moveDirection = new Vector3(_moveInput.x, 0, _moveInput.y);

        if(moveDirection != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _smoothTime);

            transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

            _characterController.Move(moveDirection * _movementSpeed * Time.deltaTime);
        }
    }

    void TPMovement()
    {
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y);

        _animator.SetFloat("Vertical", direction.magnitude, 0.2f, Time.deltaTime);
        _animator.SetFloat("Horizontal", 0);

        if(direction != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _smoothTime);

            transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            _characterController.Move(moveDirection * _movementSpeed * Time.deltaTime);
        }
    }

    void AimMovement()
    {
        Vector3 direction = new Vector3(_moveInput.x, 0, _moveInput.y);

        _animator.SetFloat("Horizontal", _moveInput.x, 0.2f, Time.deltaTime);
        _animator.SetFloat("Vertical", _moveInput.y, 0.2f, Time.deltaTime);

        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, _cameraTransform.eulerAngles.y, ref _turnSmoothVelocity, _smoothTime);

        transform.rotation = Quaternion.Euler(0, smoothAngle, 0);

        if(direction != Vector3.zero)
        {
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

    void GrabObject()
    {
        if(_grabbedObject == null)
        {
            Collider[] objectsToGrab = Physics.OverlapSphere(_handsTransform.position, _handsRadius);

            foreach(Collider item in objectsToGrab)
            {
                if(item.CompareTag("Empujable"))
                {
                    _grabbedObject = item.gameObject;
                    _grabbedObject.transform.SetParent(_handsTransform);
                    _grabbedObject.transform.position = _handsTransform.position;
                    _grabbedObject.transform.rotation = _handsTransform.rotation;
                    Rigidbody objectRigidbody = _grabbedObject.GetComponent<Rigidbody>();
                    objectRigidbody.isKinematic = true;
                    objectRigidbody.constraints = RigidbodyConstraints.None;

                    return;
                }
            }
        }
        else
        {
            Rigidbody objectRigidbody = _grabbedObject.GetComponent<Rigidbody>();
            objectRigidbody.isKinematic = false;
            _grabbedObject.transform.SetParent(null);
            _grabbedObject = null;
        }
    }

    void ThrowObject()
    {
        if(_grabbedObject != null)
        {
            Rigidbody objectRigidbody = _grabbedObject.GetComponent<Rigidbody>();
            objectRigidbody.isKinematic = false;
            _grabbedObject.transform.SetParent(null);
            objectRigidbody.AddForce(_cameraTransform.forward * _throwForce, ForceMode.Impulse);
            _grabbedObject = null;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.gameObject.CompareTag("Empujable"))
        {
            Rigidbody _rbody = hit.gameObject.GetComponent<Rigidbody>();

            Vector3 pushDirection =  hit.moveDirection;
            pushDirection.y = 0;

            Debug.Log(pushDirection);

            //_rbody.AddForce(pushDirection * _pushForce, ForceMode.Force);
            _rbody.linearVelocity = pushDirection * _pushForce / _rbody.mass;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_sensorTransform.position, _sensorRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_handsTransform.position, _handsRadius);
    }
}