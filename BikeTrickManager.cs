using UnityEngine;

public class BikeTrickManager : MonoBehaviour
{
    [Header("Связи")]
    public SledBikeController bikeController; // Ссылка на контроллер

    [Header("Настройки Трюков")]
    public float trickSpeed = 5f; // Как быстро детали разлетаются
    public float returnSpeed = 10f; // Как быстро возвращаются

    [Header("Детали для трюков")]
    // Сюда перетащи: Руль, Колеса, Кузов (по отдельности)
    public Transform[] trickParts; 

    // Запоминаем начальные позиции деталей
    private Vector3[] _originalPositions;
    private Quaternion[] _originalRotations;

    private bool _isTricking = false;

    void Start()
    {
        // Если забыли привязать контроллер руками, ищем его сами
        if (bikeController == null)
            bikeController = GetComponent<SledBikeController>();

        // Сохраняем, где стояли детали при старте
        if (trickParts != null && trickParts.Length > 0)
        {
            _originalPositions = new Vector3[trickParts.Length];
            _originalRotations = new Quaternion[trickParts.Length];

            for (int i = 0; i < trickParts.Length; i++)
            {
                if (trickParts[i] != null)
                {
                    _originalPositions[i] = trickParts[i].localPosition;
                    _originalRotations[i] = trickParts[i].localRotation;
                }
            }
        }
    }

    void Update()
    {
        if (bikeController == null) return;

        // 1. Читаем вектор трюка из контроллера
        // (Контроллер сам решает, можно ли делать трюк по высоте и вводу)
        Vector2 input = bikeController.TrickVector;

        // 2. Если вектор есть — делаем трюк
        if (input.magnitude > 0.1f)
        {
            _isTricking = true;
            PerformTrick(input);
        }
        else
        {
            _isTricking = false;
            ResetParts();
        }
        
        // 3. Проверка на смерть (Крэш)
        // Если мы приземлились (IsGrounded), но детали все еще вразброс (_isTricking)
        if (bikeController.IsGrounded && _isTricking)
        {
             // Тут можно вызвать эффект взрыва или рестарт
             Debug.Log("CRASH! Приземлился во время трюка!");
             // Пока просто сбросим детали мгновенно
             ResetPartsImmediate();
        }
    }

    void PerformTrick(Vector2 direction)
    {
        // Просто растаскиваем детали в стороны в зависимости от направления джойстика
        // direction.x - влево/вправо
        // direction.y - вверх/вниз
        
        Vector3 moveOffset = new Vector3(direction.x, direction.y, 0) * 0.5f; // 0.5f - амплитуда разлета

        for (int i = 0; i < trickParts.Length; i++)
        {
            if (trickParts[i] != null)
            {
                // У каждой детали может быть своя логика, но пока сделаем общий "взрыв"
                // Можно добавить Random, чтобы они летели хаотично
                Vector3 individualOffset = moveOffset * (i % 2 == 0 ? 1 : -1); 
                
                Vector3 targetPos = _originalPositions[i] + individualOffset;
                
                // Плавное движение к позиции трюка
                trickParts[i].localPosition = Vector3.Lerp(trickParts[i].localPosition, targetPos, trickSpeed * Time.deltaTime);
                
                // Вращение (для красоты)
                trickParts[i].Rotate(Vector3.up * direction.x * 100f * Time.deltaTime);
            }
        }
    }

    void ResetParts()
    {
        // Плавно возвращаем всё на место
        for (int i = 0; i < trickParts.Length; i++)
        {
            if (trickParts[i] != null)
            {
                trickParts[i].localPosition = Vector3.Lerp(trickParts[i].localPosition, _originalPositions[i], returnSpeed * Time.deltaTime);
                trickParts[i].localRotation = Quaternion.Lerp(trickParts[i].localRotation, _originalRotations[i], returnSpeed * Time.deltaTime);
            }
        }
    }
    
    void ResetPartsImmediate()
    {
        for (int i = 0; i < trickParts.Length; i++)
        {
            if (trickParts[i] != null)
            {
                trickParts[i].localPosition = _originalPositions[i];
                trickParts[i].localRotation = _originalRotations[i];
            }
        }
    }
}