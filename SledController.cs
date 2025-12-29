using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SledBikeController : MonoBehaviour
{
    [Header("🎮 Настройки Управления")]
    [Range(0.1f, 1.0f)]
    public float stickThreshold = 0.85f; // Насколько сильно нужно тянуть джойстик

    [Header("🤸 Трюки (Безопасность)")]
    public float minTrickHeight = 2.5f;   // Минимальная высота для начала трюка
    public bool requireStickReset = true; // Нужно ли вернуть палец в центр перед трюком

    [Header("Настройки Движения")]
    public float acceleration = 60f;      
    public float turnSpeed = 80f;         
    public float maxSpeed = 40f;          

    [Header("Прыжок Pure-Style")]
    public float minJumpForce = 300f;     
    public float maxJumpForce = 1000f;    
    public float chargeTime = 0.8f;       
    public float squashAmount = 0.2f;     // Насколько приседает байк
    
    [Header("Воздух")]
    public float airPitchSpeed = 3f;      
    public float extraGravity = 20f;      

    [Header("Визуал")]
    public float leanAngle = 35f;         
    public float leanSpeed = 5f;          
    public Transform bikeModel;           // Весь корпус для наклона
    public Transform bikeMeshRoot;        // Для приседания при прыжке

    [Header("Слои")]
    public LayerMask groundLayer;         

    [Header("Нитро")]
    public float boostMultiplier = 2.0f; 
    private bool _isBoosting = false;

    // --- Внутренние переменные ---
    private Rigidbody _rb;
    private GameInput _input;  
    private Vector2 _controlInput; 
    
    // Этот вектор читает BikeTrickManager
    public Vector2 TrickVector { get; private set; } 

    private float _jumpCharge = 0f;       
    private bool _isCharging = false;
    private Vector3 _originalMeshPos;     
    private bool _trickInputLocked = false; // Блокировка трюков

    public bool IsGrounded { get; private set; }
    public float DistanceToGround { get; private set; } 

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _input = new GameInput();
        _rb.centerOfMass = new Vector3(0, -0.5f, 0);

        if (bikeMeshRoot != null)
            _originalMeshPos = bikeMeshRoot.localPosition;
    }

    void OnEnable() => _input.Enable();
    void OnDisable() => _input.Disable();

    void Update()
    {
        // Передаем настройку чувствительности в джойстик
        if (SmartJoystick.Instance != null)
            SmartJoystick.Instance.actionThreshold = stickThreshold;

        CheckGroundStatus(); 
        HandleInput();
        HandlePureJump();
        HandleVisuals();
    }

    void CheckGroundStatus()
    {
        RaycastHit hit;
        // Проверяем реальную высоту до земли
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f, groundLayer))
        {
            DistanceToGround = hit.distance;
            IsGrounded = DistanceToGround < 0.8f; 
        }
        else
        {
            DistanceToGround = 100f; 
            IsGrounded = false;
        }
    }

    void HandleInput()
    {
        TrickVector = Vector2.zero;

        // --- МОБИЛЬНОЕ УПРАВЛЕНИЕ ---
        if (SmartJoystick.Instance != null && SmartJoystick.Instance.joystickBackground.gameObject.activeSelf)
        {
            Vector2 rawJoystick = SmartJoystick.Instance.InputVector;

            if (IsGrounded)
            {
                // НА ЗЕМЛЕ: Едем
                _trickInputLocked = true; // Включаем защиту
                float steer = SmartJoystick.Instance.Horizontal;
                float gas = 1f; // Всегда газ
                
                // Если тянем вниз (зарядка), газ отключаем
                if (SmartJoystick.Instance.IsCharging) gas = 0.8f;
                
                ActivateBoost(SmartJoystick.Instance.IsNitro);

                _controlInput = new Vector2(steer, gas);
            }
            else
            {
                // В ВОЗДУХЕ: Трюки
                _controlInput = Vector2.zero;
                
                bool highEnough = DistanceToGround > minTrickHeight;

                if (highEnough)
                {
                    // Если включена защита - ждем, пока игрок отпустит джойстик
                    if (_trickInputLocked && requireStickReset)
                    {
                        if (rawJoystick.magnitude < 0.1f) _trickInputLocked = false; 
                    }
                    else
                    {
                        TrickVector = rawJoystick;
                    }
                }
                else
                {
                    _trickInputLocked = true; // Слишком низко для трюков
                }
            }
        }
        // --- ПК УПРАВЛЕНИЕ ---
        else
        {
            Vector2 keyboard = _input.Player.Move.ReadValue<Vector2>();
            if (IsGrounded) {
                 _controlInput = keyboard;
                 _trickInputLocked = true;
            }
            else {
                 _controlInput = Vector2.zero;
                 if (DistanceToGround > minTrickHeight) TrickVector = keyboard; 
                 else TrickVector = Vector2.zero;
            }
        }
    }

    void FixedUpdate()
    {
        if (IsGrounded)
        {
            // Поворот
            if (_controlInput.x != 0)
            {
                float turn = _controlInput.x * turnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                _rb.MoveRotation(_rb.rotation * turnRotation);
            }

            // Газ и тормоз
            if (_controlInput.y != 0)
            {
                float speedLimit = maxSpeed * (_isBoosting ? 1.5f : 1f);
                if (_rb.linearVelocity.magnitude < speedLimit)
                {
                    float currentAccel = acceleration;
                    if (_isBoosting && _controlInput.y > 0) currentAccel *= boostMultiplier; 
                    float force = _controlInput.y * currentAccel;
                    if (_controlInput.y < 0) force *= 0.5f; // Тормоз слабее газа
                    _rb.AddForce(transform.forward * force, ForceMode.Acceleration);
                }
            }
        }
        else
        {
            // Гравитация в воздухе
            _rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    void HandlePureJump()
    {
        bool pcJumpKey = _input.Player.Accelerate.IsPressed(); 
        bool mobileCharge = (SmartJoystick.Instance != null && SmartJoystick.Instance.IsCharging);
        bool shouldCharge = pcJumpKey || mobileCharge;

        if (!IsGrounded && !_isCharging) shouldCharge = false;

        if (shouldCharge)
        {
            if (IsGrounded || _isCharging)
            {
                _isCharging = true;
                _jumpCharge += Time.deltaTime / chargeTime;
                _jumpCharge = Mathf.Clamp01(_jumpCharge);

                // Визуальное приседание
                if (bikeMeshRoot != null)
                {
                    float squashY = Mathf.Lerp(0, -squashAmount, _jumpCharge);
                    bikeMeshRoot.localPosition = _originalMeshPos + new Vector3(0, squashY, 0);
                }
            }
        }
        else
        {
            // Прыжок!
            if (_isCharging)
            {
                if (IsGrounded)
                {
                    float finalForce = Mathf.Lerp(minJumpForce, maxJumpForce, _jumpCharge);
                    Vector3 jumpVector = (Vector3.up * 0.9f + transform.forward * 0.1f).normalized;
                    _rb.AddForce(jumpVector * finalForce, ForceMode.Impulse);
                }
            }
            _isCharging = false;
            _jumpCharge = 0f;
            if (bikeMeshRoot != null)
                bikeMeshRoot.localPosition = Vector3.Lerp(bikeMeshRoot.localPosition, _originalMeshPos, 10f * Time.deltaTime);
        }
    }

    void HandleVisuals()
    {
        if (bikeModel != null)
        {
            float targetZ = (IsGrounded) ? -_controlInput.x * leanAngle : 0f;
            Vector3 currentEuler = bikeModel.localEulerAngles;
            float newZ = Mathf.LerpAngle(currentEuler.z, targetZ, leanSpeed * Time.deltaTime);
            bikeModel.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, newZ);
        }
    }

    public void ActivateBoost(bool active)
    {
        _isBoosting = active;
    }
}