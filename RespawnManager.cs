using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RespawnManager : MonoBehaviour
{
    [Header("Настройки")]
    public float fallThreshold = -20f;
    
    // Как часто сохраняться (раз в 1 сек)
    public float autoSaveInterval = 1.0f; 

    private Vector3 _lastCheckpointPos;
    private Quaternion _lastCheckpointRot;
    private Rigidbody _rb;
    private GameInput _input;
    
    // Ссылка на контроллер, чтобы проверять землю
    private SledBikeController _bikeController;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _bikeController = GetComponent<SledBikeController>();
        
        _input = new GameInput();
        _input.Player.Respawn.performed += ctx => Respawn();

        // Старт
        SetCheckpoint(transform.position, transform.rotation);
        
        // Запускаем таймер автосохранения
        StartCoroutine(AutoSaveRoutine());
    }

    void OnEnable() => _input.Enable();
    void OnDisable() => _input.Disable();

    // Эта штука работает параллельно игре
    IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            // Сохраняемся ТОЛЬКО если:
            // 1. Мы на земле (IsGrounded берем из твоего контроллера - сделай это поле public в SledBikeController)
            // 2. Мы стоим нормально (не вверх ногами)
            // 3. Мы не падаем быстро вниз
            
            bool isUpright = Vector3.Dot(transform.up, Vector3.up) > 0.5f; // Угол наклона нормальный

            // Примечание: В SledBikeController нужно сделать поле _isGrounded публичным (public bool IsGrounded)
            if (_bikeController.IsGrounded && isUpright)
            {
                SetCheckpoint(transform.position, transform.rotation);
                // Debug.Log("Auto Saved!"); // Можно раскомментить для проверки
            }
        }
    }

    void Update()
    {
        if (transform.position.y < fallThreshold) Respawn();
    }

  public void Respawn()
    {
        // 1. Полная остановка физики
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.Sleep(); // Усыпляем физику на долю секунды, чтобы сбросить коллизии

        // 2. Телепортация ПОВЫШЕ
        // Добавляем +2 метра высоты, чтобы точно не застрять в полу
        transform.position = _lastCheckpointPos + Vector3.up * 2.0f; 
        
        // 3. Выравниваем вращение
        transform.rotation = _lastCheckpointRot;

        // 4. Будим физику обратно
        _rb.WakeUp();
        
        Debug.Log("Respawned High!");
    }

    public void SetCheckpoint(Vector3 newPos, Quaternion newRot)
    {
        _lastCheckpointPos = newPos;
        _lastCheckpointRot = newRot;
    }
}