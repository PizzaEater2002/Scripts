using UnityEngine;

public class FPSUnlocker : MonoBehaviour
{
    [Header("Настройки")]
    public int targetFPS = 60; // Ставь 60 для мобилок, 120 для мощных экранов

    void Awake()
    {
        // 1. Отключаем VSync (Вертикальную синхронизацию), 
        // иначе Unity будет игнорировать наши настройки FPS.
        QualitySettings.vSyncCount = 0;

        // 2. Ставим желаемый FPS
        Application.targetFrameRate = targetFPS;
    }
}