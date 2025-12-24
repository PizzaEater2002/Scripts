using UnityEngine;
using UnityEngine.EventSystems;

public class SmartJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static SmartJoystick Instance;

    [Header("Визуал")]
    public RectTransform joystickBackground; 
    public RectTransform joystickHandle;     

    [Header("Настройки (Базовые)")]
    public float handleRange = 100f; 
    public float deadZone = 0.1f;    
    public float sectorAngle = 45f;  
    
    // Эту переменную мы теперь будем менять из скрипта байка!
    [HideInInspector] // Скрыли в инспекторе джойстика, чтобы не путаться
    public float actionThreshold = 0.8f; 

    // Свойства
    public float Horizontal { get; private set; }
    public bool IsNitro { get; private set; }
    public bool IsCharging { get; private set; }
    public Vector2 InputVector { get { return _inputVector; } } // Сырой вектор

    private Vector2 _inputVector;
    private bool _isLocked; 

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if(joystickBackground != null) joystickBackground.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        joystickBackground.gameObject.SetActive(true);
        // Установка позиции джойстика
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground.parent as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint))
        {
            joystickBackground.anchoredPosition = localPoint;
        }

        joystickHandle.anchoredPosition = Vector2.zero;
        _inputVector = Vector2.zero;
        _isLocked = false;
        ResetInput();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, eventData.position, eventData.pressEventCamera, out pos))
        {
            Vector2 size = joystickBackground.sizeDelta;
            pos.x = (pos.x / size.x) * 2;
            pos.y = (pos.y / size.y) * 2;

            _inputVector = pos;
            if (_inputVector.magnitude > 1.0f) _inputVector.Normalize();

            float radius = (size.x / 2f) * (handleRange / 100f);
            joystickHandle.anchoredPosition = _inputVector * radius;

            ProcessLogic();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        joystickBackground.gameObject.SetActive(false);
        _inputVector = Vector2.zero;
        _isLocked = false;
        ResetInput();
    }

    private void ResetInput()
    {
        Horizontal = 0;
        IsNitro = false;
        IsCharging = false;
    }

    private void ProcessLogic()
    {
        float magnitude = _inputVector.magnitude;

        if (magnitude < deadZone)
        {
            _isLocked = false; 
            ResetInput();
            return;
        }

        if (_isLocked) return; 

        float angle = Vector2.Angle(Vector2.up, _inputVector);
        
        // ВЕРХ (Нитро) + Проверка порога
        if (angle < sectorAngle && magnitude > actionThreshold) 
        {
            IsNitro = true;
            IsCharging = false;
            Horizontal = 0;
            _isLocked = true;
        }
        // НИЗ (Зарядка) + Проверка порога
        else if (angle > (180 - sectorAngle) && magnitude > actionThreshold)
        {
            IsNitro = false;
            IsCharging = true;
            Horizontal = 0;
            _isLocked = true;
        }
        // БОКА (Руление)
        else
        {
            IsNitro = false;
            IsCharging = false;
            Horizontal = _inputVector.x;
        }
    }
}