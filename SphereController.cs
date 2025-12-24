using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SphereController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 50f;     // Мощность двигателя
    public float turnSpeed = 200f;    // Скорость поворота модели
    
    [Header("Реализм")]
    public float speedForMaxTurn = 5f; // На какой скорости (м/с) руль работает на 100%
    public bool enableAirControl = false; // Можно ли рулить в воздухе (для реализма - false)

    [Header("Визуал")]
    public Transform bikeModel;       // Ссылка на модель байка
    public Transform handlebars;      // (Опционально) Ссылка на руль для анимации
    public float heightOffset = 0.5f; // Радиус шара (смещение модели)
    public float alignSpeed = 10f;    // Скорость выравнивания по земле

    [Header("Слои")]
    public LayerMask groundLayer;

    private Rigidbody _rb;
    private GameInput _input;
    private Vector2 _moveInput;
    private float _currentRotateY;
    private bool _isGrounded;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _input = new GameInput();
        
        if (bikeModel != null)
        {
            bikeModel.parent = null; 
            _currentRotateY = bikeModel.eulerAngles.y;
        }
    }

    void OnEnable() => _input.Enable();
    void OnDisable() => _input.Disable();

    void Update()
    {
        _moveInput = _input.Player.Move.ReadValue<Vector2>();

        if (bikeModel == null) return;

        // 1. Проверка земли (Raycast)
        CheckGround();

        // 2. Визуальная привязка к шару
        bikeModel.position = transform.position - new Vector3(0, heightOffset, 0);

        // 3. Расчет коэффициента поворота
        // Если стоим - 0. Если едем быстро - 1.
        float currentSpeed = _rb.linearVelocity.magnitude;
        float turnMultiplier = Mathf.Clamp01(currentSpeed / speedForMaxTurn);

        // Если мы в воздухе и Air Control выключен — множитель = 0
        if (!_isGrounded && !enableAirControl)
        {
            turnMultiplier = 0f;
        }

        // 4. Поворот (меняем угол Y)
        if (_moveInput.x != 0)
        {
            // Умножаем на turnMultiplier: нет скорости = нет поворота
            _currentRotateY += _moveInput.x * turnSpeed * turnMultiplier * Time.deltaTime;
        }

        // 5. Анимация руля (Визуальная)
        // Руль крутится даже если мы стоим (turnMultiplier тут не нужен)
        if (handlebars != null)
        {
            // Поворачиваем руль на 30 градусов влево/вправо
            float targetHandleAngle = _moveInput.x * 30f;
            Vector3 currentEuler = handlebars.localEulerAngles;
            // Используем MoveTowards для плавности
            float newY = Mathf.MoveTowardsAngle(currentEuler.y, targetHandleAngle, 200f * Time.deltaTime);
            handlebars.localRotation = Quaternion.Euler(currentEuler.x, newY, currentEuler.z);
        }

        // 6. Выравнивание модели
        AlignModel();
    }

    void CheckGround()
    {
        // Луч чуть длиннее чем heightOffset
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, heightOffset + 0.2f, groundLayer);
    }

    void AlignModel()
    {
        RaycastHit hit;
        // Для выравнивания стреляем лучом подальше
        if (Physics.Raycast(transform.position, Vector3.down, out hit, heightOffset + 2.0f, groundLayer))
        {
            Vector3 groundNormal = hit.normal;
            Quaternion yRotation = Quaternion.Euler(0, _currentRotateY, 0);
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(yRotation * Vector3.forward, groundNormal).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(forwardOnSlope, groundNormal);
            
            bikeModel.rotation = Quaternion.Slerp(bikeModel.rotation, targetRotation, alignSpeed * Time.deltaTime);
        }
        else
        {
            // В воздухе
            Quaternion targetRotation = Quaternion.Euler(0, _currentRotateY, 0);
            bikeModel.rotation = Quaternion.Slerp(bikeModel.rotation, targetRotation, alignSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // 7. Ускорение (Только если на земле)
        if (_isGrounded && _moveInput.y != 0 && bikeModel != null)
        {
            _rb.AddForce(bikeModel.forward * _moveInput.y * moveSpeed, ForceMode.Acceleration);
        }
        
        // Доп. гравитация (чтобы не летал как пушинка)
        if (!_isGrounded)
        {
            _rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
        }
    }
}