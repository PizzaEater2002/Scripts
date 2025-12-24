using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SledBikeController : MonoBehaviour
{
    [Header("🎮 Настройки Управления")]
    [Range(0.1f, 1.0f)]
    public float stickThreshold = 0.85f; // Порог для Нитро/Зарядки

    [Header("🤸 Трюки (Ограничения)")]
    public float minTrickHeight = 2.5f; // Минимальная высота для начала трюка (чтобы не срабатывало на кочках)
    public bool requireStickReset = true; // Нужно ли вернуть джойстик в центр перед трюком

    [Header("Настройки Движения")]
    public float acceleration = 60f;      
    public float turnSpeed = 80f;         
    public float maxSpeed = 40f;          

    [Header("Прыжок Pure-Style")]
    public float minJumpForce = 300f;     
    public float maxJumpForce = 1000f;    
    public float chargeTime = 0.8f;       
    public float squashAmount = 0.2f;     
    
    [Header("Воздух")]
    public float airPitchSpeed = 3f;      
    public float extraGravity = 20f;      

    [Header("Визуал")]
    public float leanAngle = 35f;         
    public float leanSpeed = 5f;          
    public Transform bikeModel; 
    public Transform bikeMeshRoot; 

    [Header("Слои")]
    public LayerMask groundLayer;         

    [Header("Нитро")]
    public float boostMultiplier = 2.0f; 
    private bool _isBoosting = false;

    // --- Внутренние переменные ---
    private Rigidbody _rb;
    private GameInput _input;  
    private Vector2 _controlInput; 
    
    // Вектор трюка для менеджера
    public Vector2 TrickVector { get; private set; } 

    private float _jumpCharge = 0f;       
    private bool _isCharging = false;
    private Vector3 _originalMeshPos;     
    private bool _trickInputLocked = false; // Флаг блокировки трюка

    public bool IsGrounded { get; private set; }
    public float DistanceToGround { get; private set; } // Текущая высота

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
        // Передаем порог в джойстик
        if (SmartJoystick.Instance != null)
            SmartJoystick.Instance.actionThreshold = stickThreshold;

        CheckGroundStatus(); // Обновленная проверка земли и высоты
        HandleInput();
        HandlePureJump();
        HandleVisuals();
    }

    // Новый метод для умной проверки высоты
    void CheckGroundStatus()
    {
        RaycastHit hit;
        // Пускаем луч вниз, чтобы узнать точную высоту
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f, groundLayer))
        {
            DistanceToGround = hit.distance;
            IsGrounded = DistanceToGround < 0.8f; // Считаем землей, если ближе 0.8м
        }
        else
        {
            DistanceToGround = 100f; // Мы высоко в небе
            IsGrounded = false;
        }
    }

    void HandleInput()
    {
        // По умолчанию трюков нет
        TrickVector = Vector2.zero;

        // --- МОБИЛКА ---
        if (SmartJoystick.Instance != null && SmartJoystick.Instance.joystickBackground.gameObject.activeSelf)
        {
            Vector2 rawJoystick = SmartJoystick.Instance.InputVector;

            if (IsGrounded)
            {
                // НА ЗЕМЛЕ
                // Сбрасываем блокировку трюков, чтобы в следующем прыжке снова требовался сброс
                _trickInputLocked = true; 

                float steer = SmartJoystick.Instance.Horizontal;
                float gas = 1f;
                if (SmartJoystick.Instance.IsCharging) gas = 0f;
                
                ActivateBoost(SmartJoystick.Instance.IsNitro);

                _controlInput = new Vector2(steer, gas);
            }
            else
            {
                // В ВОЗДУХЕ
                _controlInput = Vector2.zero;

                // === ЛОГИКА ТРЮКОВ "КАК В PURE" ===
                
                // 1. Проверка Высоты: Достаточно ли мы высоко?
                bool highEnough = DistanceToGround > minTrickHeight;

                if (highEnough)
                {
                    // 2. Проверка Сброса Джойстика (Input Reset)
                    // Если флаг Locked стоит - мы ждем, пока игрок отпустит джойстик
                    if (_trickInputLocked && requireStickReset)
                    {
                        // Если джойстик вернулся в центр (magnitude < 0.1)
                        if (rawJoystick.magnitude < 0.1f)
                        {
                            _trickInputLocked = false; // Разблокируем! Можно делать трюк
                        }
                    }
                    else
                    {
                        // Если разблокировано - передаем управление в Трюки
                        TrickVector = rawJoystick;
                    }
                }
                else
                {
                    // Если мы слишком низко - трюки запрещены, и мы держим блокировку
                    _trickInputLocked = true; 
                }
            }
        }
        // --- ПК ---
        else
        {
            Vector2 keyboard = _input.Player.Move.ReadValue<Vector2>();
            if (IsGrounded) {
                 _controlInput = keyboard;
                 _trickInputLocked = true;
            }
            else {
                 _controlInput = Vector2.zero;
                 
                 // Для ПК логика высоты такая же
                 if (DistanceToGround > minTrickHeight)
                     TrickVector = keyboard; 
                 else 
                     TrickVector = Vector2.zero;
            }
        }
    }

    void FixedUpdate()
    {
        if (IsGrounded)
        {
            // ПОВОРОТ
            if (_controlInput.x != 0)
            {
                float turn = _controlInput.x * turnSpeed * Time.fixedDeltaTime;
                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
                _rb.MoveRotation(_rb.rotation * turnRotation);
            }

            // ГАЗ
            if (_controlInput.y != 0)
            {
                float speedLimit = maxSpeed * (_isBoosting ? 1.5f : 1f);
                if (_rb.linearVelocity.magnitude < speedLimit)
                {
                    float currentAccel = acceleration;
                    if (_isBoosting && _controlInput.y > 0) currentAccel *= boostMultiplier; 
                    float force = _controlInput.y * currentAccel;
                    if (_controlInput.y < 0) force *= 0.5f; 
                    _rb.AddForce(transform.forward * force, ForceMode.Acceleration);
                }
            }
        }
        else
        {
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

                if (bikeMeshRoot != null)
                {
                    float squashY = Mathf.Lerp(0, -squashAmount, _jumpCharge);
                    bikeMeshRoot.localPosition = _originalMeshPos + new Vector3(0, squashY, 0);
                }
            }
        }
        else
        {
            if (_isCharging)
            {
                if (IsGrounded) PerformJump();
            }
            _isCharging = false;
            _jumpCharge = 0f;
            if (bikeMeshRoot != null)
                bikeMeshRoot.localPosition = Vector3.Lerp(bikeMeshRoot.localPosition, _originalMeshPos, 10f * Time.deltaTime);
        }
    }

    void PerformJump()
    {
        if (IsGrounded)
        {
            float finalForce = Mathf.Lerp(minJumpForce, maxJumpForce, _jumpCharge);
            Vector3 jumpVector = (Vector3.up * 0.9f + transform.forward * 0.1f).normalized;
            _rb.AddForce(jumpVector * finalForce, ForceMode.Impulse);
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