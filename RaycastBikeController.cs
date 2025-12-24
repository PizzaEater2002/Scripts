using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RaycastBikeController : MonoBehaviour
{
    [Header("Колеса и Визуал")]
    public Transform frontWheelPoint;
    public Transform rearWheelPoint;
    public Transform frontWheelMesh;
    public Transform rearWheelMesh;
    [Tooltip("Радиус колеса. Увеличьте, если колеса проваливаются в землю.")]
    public float wheelRadius = 0.4f;

    [Header("Подвеска")]
    public float restLength = 0.5f;
    public float springStrength = 500f; // Увеличил для массы 80
    public float springDamper = 50f;

    [Header("Движение")]
    [Tooltip("Сила ускорения. Для массы 80кг ставьте 2000-5000.")]
    public float accelerationForce = 3000f; 
    public float brakingForce = 5000f;
    public float maxSpeed = 60f;

    [Header("Управление и Стабилизация")]
    public float steerAngle = 30f;
    public float steerSpeed = 5f;
    public float gripFactor = 0.5f; // Чуть уменьшил сцепление
    [Tooltip("Сила, которая держит байк вертикально. Поставьте 0, если хотите, чтобы он падал.")]
    public float uprightStiffness = 10f; // Уменьшил с 50 до 10, чтобы байк был "живее"

    [Header("Системные")]
    public LayerMask drivableLayers;

    // --- Внутренние переменные ---
    private Rigidbody rb;
    private GameInput _input;
    
    // Данные ввода
    private float currentSteerAngle;
    private float currentGasInput;   // 0 или 1 (Пробел)
    private float currentBrakeInput; // 0 или 1 (Кнопка S)

    // Данные подвески
    private float frontLastCompression;
    private float rearLastCompression;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f); // Центр масс ниже, чтобы меньше переворачивался сам

        _input = new GameInput();
    }

    void OnEnable() => _input.Enable();
    void OnDisable() => _input.Disable();

    void Update()
    {
        // 1. Читаем вектор движения (WASD)
        Vector2 moveInput = _input.Player.Move.ReadValue<Vector2>();

        // РУЛЬ: Берем X (A/D)
        float targetSteer = moveInput.x * steerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, steerSpeed * Time.deltaTime);

        // ТОРМОЗ: Берем Y, но только если он меньше 0 (нажата S)
        currentBrakeInput = (moveInput.y < -0.01f) ? Mathf.Abs(moveInput.y) : 0f;

        // ГАЗ: Читаем кнопку Accelerate (Пробел)
        currentGasInput = _input.Player.Accelerate.IsPressed() ? 1f : 0f;

        // --- Debug: Показываем реальную силу ---
        if (currentGasInput > 0) 
        {
            float totalForce = currentGasInput * accelerationForce;
            // Debug.Log($"ГАЗ НАЖАТ! Ввод: {currentGasInput} | Применяемая сила: {totalForce}");
        }
    }

    void FixedUpdate()
    {
        ProcessWheel(frontWheelPoint, frontWheelMesh, true, ref frontLastCompression);
        ProcessWheel(rearWheelPoint, rearWheelMesh, false, ref rearLastCompression);
        ApplyUprightForce();
    }

    void ProcessWheel(Transform suspensionPoint, Transform wheelMesh, bool isFront, ref float lastCompression)
    {
        RaycastHit hit;
        // Стреляем лучом вниз
        bool hasHit = Physics.Raycast(suspensionPoint.position, -suspensionPoint.up, out hit, restLength, drivableLayers);

        if (hasHit)
        {
            // 1. ПОДВЕСКА
            Vector3 springDir = suspensionPoint.up; 
            float distance = hit.distance;
            
            // Сжатие пружины (0 = полностью разжата, 1 = полностью сжата)
            float compression = 1.0f - (distance / restLength);
            
            // Скорость сжатия (для амортизатора)
            float compressionRate = (compression - lastCompression) / Time.fixedDeltaTime;
            lastCompression = compression;

            float springForce = (compression * springStrength) + (compressionRate * springDamper);
            
            // Применяем силу пружины
            rb.AddForceAtPosition(springDir * springForce, hit.point);

            // 2. ВЫЧИСЛЕНИЕ НАПРАВЛЕНИЙ
            Vector3 steerDir = suspensionPoint.forward;
            Vector3 rightDir = suspensionPoint.right;

            if (isFront) // Поворот переднего колеса
            {
                Quaternion steerRot = Quaternion.AngleAxis(currentSteerAngle, suspensionPoint.up);
                steerDir = steerRot * suspensionPoint.forward;
                rightDir = steerRot * suspensionPoint.right;
            }

            Vector3 worldVel = rb.GetPointVelocity(hit.point);
            float steeringVel = Vector3.Dot(worldVel, rightDir); // Скорость бокового скольжения
            float forwardVel = Vector3.Dot(worldVel, steerDir);  // Скорость движения вперед

            // 3. БОКОВОЕ ТРЕНИЕ (Чтобы не скользил как на льду)
            float gripForce = -steeringVel * gripFactor * (springForce / 2f); 
            rb.AddForceAtPosition(rightDir * gripForce, hit.point);

            // 4. УСКОРЕНИЕ (Только заднее колесо)
            if (!isFront) 
            {
                if (currentGasInput > 0 && forwardVel < maxSpeed)
                {
                    // Важно: Умножаем на accelerationForce (которая теперь 3000, а не 20)
                    rb.AddForceAtPosition(steerDir * currentGasInput * accelerationForce, hit.point);
                }
            }

            // 5. ТОРМОЗ
            if (currentBrakeInput > 0) 
            {
                rb.AddForceAtPosition(steerDir * -currentBrakeInput * brakingForce, hit.point);
            }

            // 6. ВИЗУАЛ КОЛЕС
            if (wheelMesh != null)
            {
                // Ставим колесо в точку удара луча + радиус колеса (чтобы оно стояло НА земле)
                wheelMesh.position = hit.point + (suspensionPoint.up * wheelRadius); 
                
                // Вращение визуального колеса (руль)
                if (isFront)
                {
                    wheelMesh.localRotation = Quaternion.Euler(wheelMesh.localEulerAngles.x, currentSteerAngle, 0f);
                }
            }
        }
        else
        {
            // Если колесо в воздухе -> опускаем его на максимум (Rest Length)
            lastCompression = 0f;
            if (wheelMesh != null)
            {
                wheelMesh.position = suspensionPoint.position - (suspensionPoint.up * restLength);
            }
        }
    }

    void ApplyUprightForce()
    {
        // Стабилизатор (не дает упасть)
        if (uprightStiffness <= 0.1f) return;

        Quaternion currentRot = rb.rotation;
        // Целевое вращение - выравнивание "вверх" по Y
        Quaternion goalRot = Quaternion.FromToRotation(transform.up, Vector3.up) * currentRot;

        float angle; 
        Vector3 axis;
        Quaternion deltaRot = goalRot * Quaternion.Inverse(currentRot);
        deltaRot.ToAngleAxis(out angle, out axis);

        if (angle > 180f) angle -= 360f;

        if (angle != 0)
        {
            // Применяем крутящий момент, чтобы вернуть байк в вертикальное положение
            rb.AddTorque(axis * angle * uprightStiffness * Time.fixedDeltaTime, ForceMode.Acceleration); 
        }
    }

    private void OnDrawGizmos()
    {
        // Рисуем лучи подвески в редакторе
        Gizmos.color = Color.yellow;
        if (frontWheelPoint) Gizmos.DrawRay(frontWheelPoint.position, -frontWheelPoint.up * restLength);
        if (rearWheelPoint) Gizmos.DrawRay(rearWheelPoint.position, -rearWheelPoint.up * restLength);
    }
}