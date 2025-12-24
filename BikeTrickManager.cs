using UnityEngine;

public class BikeTrickManager : MonoBehaviour
{
    [Header("🔧 Настройки Разлета (Визуал)")]
    public float expandDistance = 1.2f;   // Насколько далеко разлетаются детали
    public float smoothness = 10f;        // Скорость анимации (чем больше, тем резче)
    
    [Header("🔥 Нитро Система")]
    public float maxNitro = 100f;
    public float nitroBurnRate = 40f;     // Сколько тратится в секунду
    public float trickRewardRate = 30f;   // Сколько даем за трюк в секунду
    
    [Header("💀 Ссылки")]
    public Transform[] trickParts;        // Сюда кидай колеса, руль, тело (все куски)
    public RespawnManager respawnManager; // Ссылка на респаун (если есть)

    // Приватные переменные
    private SledBikeController _controller;
    private Vector3[] _startPositions;    // Запоминаем где детали были
    private float _currentNitro = 0f;
    private float _expansionFactor = 0f;  // 0 = собран, 1 = разобран

    void Start()
    {
        _controller = GetComponent<SledBikeController>();
        
        // Запоминаем исходные позиции деталей
        _startPositions = new Vector3[trickParts.Length];
        for (int i = 0; i < trickParts.Length; i++)
        {
            if (trickParts[i] != null)
                _startPositions[i] = trickParts[i].localPosition;
        }
    }

    void Update()
    {
        HandleNitroLogic();
        HandleTrickLogic();
    }

    void HandleNitroLogic()
    {
        // 1. ТРАТА НИТРО (На земле)
        // Проверяем, активировал ли контроллер буст (через джойстик вверх)
        // (Смотрим приватное поле _isBoosting в контроллере через метод или добавляем свойство IsBoosting)
        // Но пока сделаем проще: если джойстик в зоне Нитро
        
        bool isNitroInput = false;
        if (SmartJoystick.Instance != null) isNitroInput = SmartJoystick.Instance.IsNitro;
        
        // Если есть топливо и мы жмем нитро
        if (isNitroInput && _currentNitro > 0)
        {
            _controller.ActivateBoost(true); // Включаем физику ускорения
            _currentNitro -= nitroBurnRate * Time.deltaTime;
        }
        else
        {
            _controller.ActivateBoost(false); // Выключаем
        }

        // Ограничиваем бак
        _currentNitro = Mathf.Clamp(_currentNitro, 0, maxNitro);
    }

    void HandleTrickLogic()
    {
        // Берем вектор трюка из нашего контроллера (который берет его с джойстика в воздухе)
        Vector2 input = _controller.TrickVector;
        
        // Есть ли ввод трюка? (Если длина вектора > 0.1)
        bool isTricking = input.magnitude > 0.1f;
        
        // Мы в воздухе?
        bool inAir = !_controller.IsGrounded;

        if (inAir && isTricking)
        {
            // --- ДЕЛАЕМ ТРЮК ---
            
            // 1. Плавно увеличиваем фактор разлета
            _expansionFactor = Mathf.Lerp(_expansionFactor, 1f, smoothness * Time.deltaTime);

            // 2. Начисляем нитро
            _currentNitro += trickRewardRate * Time.deltaTime;

            // 3. Двигаем детали
            ApplyExplosion(input);
        }
        else
        {
            // --- СОБИРАЕМСЯ ---
            
            // Плавно уменьшаем фактор к нулю
            _expansionFactor = Mathf.Lerp(_expansionFactor, 0f, smoothness * Time.deltaTime);
            
            // Возвращаем детали (передаем ноль)
            ApplyExplosion(Vector2.zero);
            
            // ПРОВЕРКА НА КРАШ
            // Если мы коснулись земли (inAir == false), но байк еще не собрался (_expansionFactor > 0.3f)
            if (!inAir && _expansionFactor > 0.5f)
            {
                Crash();
            }
        }
    }

    void ApplyExplosion(Vector2 direction)
    {
        // Превращаем 2D вектор джойстика в 3D смещение
        // X -> X, Y -> Y
        Vector3 explosionDir = new Vector3(direction.x, direction.y, 0);

        for (int i = 0; i < trickParts.Length; i++)
        {
            if (trickParts[i] == null) continue;

            // Формула: Старт + (Направление * Дистанцию * ФакторРазлета)
            Vector3 targetPos = _startPositions[i] + (explosionDir * expandDistance * _expansionFactor);
            
            // Можно добавить немного "разнобоя", чтобы детали летели чуть веером, а не линией
            // Например: targetPos += trickParts[i].up * 0.1f; 
            
            trickParts[i].localPosition = Vector3.Lerp(trickParts[i].localPosition, targetPos, smoothness * Time.deltaTime);
        }
    }

    void Crash()
    {
        Debug.Log("WASTED! Разбился при посадке.");
        _expansionFactor = 0f;
        _currentNitro = 0f;
        
        // Возвращаем детали на место мгновенно
        for (int i = 0; i < trickParts.Length; i++)
             if(trickParts[i]) trickParts[i].localPosition = _startPositions[i];

        if (respawnManager != null)
        {
            respawnManager.Respawn();
        }
        else
        {
            // Временный респаун, если нет менеджера
            // transform.position += Vector3.up * 2; 
            // transform.rotation = Quaternion.identity;
        }
    }
}