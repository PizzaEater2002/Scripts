using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUDManager : MonoBehaviour
{
    [Header("Ссылки на Физику")]
    public SledBikeController bikeScript;
    public Rigidbody bikeRb;

    [Header("UI Группы")]
    public GameObject mobileControlsRoot; // Ссылка на объект Mobile_Controls
    public GameObject debugPanel;         // Ссылка на Debug_Panel

    [Header("UI Статистика")]
    public TextMeshProUGUI statsText;

    [Header("UI Настройки (Debug)")]
    public Slider speedSlider;
    public TextMeshProUGUI speedValueText;
    public Slider jumpSlider;
    public TextMeshProUGUI jumpValueText;

    [Header("ТЕСТЫ")]
    public bool debugMobileOnPC = true; // <--- НОВАЯ ГАЛОЧКА!

    // Переменные для расчета FPS
    private float _deltaTime = 0.0f;

    void Start()
    {

        // Показываем кнопки, если это телефон ИЛИ если мы включили галочку теста
        if (Application.isMobilePlatform || debugMobileOnPC)
        {
            mobileControlsRoot.SetActive(true);
        }
        else
        {
            mobileControlsRoot.SetActive(false);
        }
        // 1. АВТО-ОПРЕДЕЛЕНИЕ УСТРОЙСТВА
        // Если мы на телефоне -> Показываем джойстики
        // Если на ПК -> Скрываем их
        if (Application.isMobilePlatform)
        {
            mobileControlsRoot.SetActive(true);
        }
        else
        {
            mobileControlsRoot.SetActive(false);
            // Если хочешь тестировать джойстик в редакторе Unity, 
            // закомментируй строку выше или поставь true временно.
        }

        // 2. Инициализация слайдеров значениями из байка
        if (bikeScript != null)
        {
            SetupSlider(speedSlider, speedValueText, bikeScript.maxSpeed, 10f, 100f, val => bikeScript.maxSpeed = val);
            SetupSlider(jumpSlider, jumpValueText, bikeScript.maxJumpForce, 500f, 2000f, val => bikeScript.maxJumpForce = val);
        }

        // Скрываем меню настроек при старте
        debugPanel.SetActive(false);
    }

    void Update()
    {
        // 1. ПРОВЕРКА УПРАВЛЕНИЯ (Теперь работает в реальном времени!)
        if (mobileControlsRoot != null)
        {
            // Показывать, если это телефон ИЛИ если стоит галочка
            bool shouldShow = Application.isMobilePlatform || debugMobileOnPC;
            
            // Чтобы не спамить SetActive каждый кадр, проверяем, изменилось ли состояние
            if (mobileControlsRoot.activeSelf != shouldShow)
            {
                mobileControlsRoot.SetActive(shouldShow);
            }
        }

        // 2. --- FPS MONITOR (Остальной код) ---
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        float fps = 1.0f / _deltaTime;

        if (bikeRb != null)
        {
            // Для Unity 6 используй linearVelocity, для старых - velocity
            float speedKmh = bikeRb.linearVelocity.magnitude * 3.6f;
            
            if(statsText != null) // Проверка на всякий случай
            {
                statsText.text = $"FPS: {fps:0}\nSPD: {speedKmh:0} km/h";
                statsText.color = (fps < 30f) ? Color.red : Color.green;
            }
        }
    }

    // Метод для кнопки Шестеренки
    public void ToggleDebugMenu()
    {
        debugPanel.SetActive(!debugPanel.activeSelf);
    }

    // Хелпер для быстрой настройки слайдеров
    void SetupSlider(Slider s, TextMeshProUGUI t, float startVal, float min, float max, UnityEngine.Events.UnityAction<float> action)
    {
        // 1. ПРОВЕРКА: Если слайдера нет, пишем ошибку в консоль и выходим, чтобы не крашить игру
        if (s == null)
        {
            Debug.LogWarning("Внимание! В GameHUDManager не назначен какой-то Слайдер. Проверь Инспектор.");
            return; 
        }

        s.minValue = min;
        s.maxValue = max;
        s.value = startVal;

        // 2. ПРОВЕРКА ТЕКСТА: Если текста нет, мы просто не обновляем цифры, но слайдер будет работать!
        if (t != null)
        {
            t.text = startVal.ToString("F0");
            s.onValueChanged.AddListener(val => t.text = val.ToString("F0"));
        }
        else
        {
            // Debug.Log("У слайдера " + s.name + " нет текста для цифр, но это не страшно.");
        }
        
        // Подписываем действие (изменение скорости/прыжка)
        s.onValueChanged.AddListener(action);
    }
}