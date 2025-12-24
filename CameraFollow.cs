using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Цель")]
    public Transform target; // Лучше перетащи сюда Bike_Physics (Корневой объект), а не визуал!

    [Header("Настройки")]
    public float smoothSpeed = 0.125f; 
    public Vector3 offset = new Vector3(0, 3, -6); // Стандартная позиция

    void LateUpdate()
    {
        if (target == null) return;

        // 1. ПОЗИЦИЯ
        // Берем позицию цели
        Vector3 targetPos = target.position;

        // САМОЕ ВАЖНОЕ: Мы создаем "искусственный" поворот.
        // Мы берем у байка ТОЛЬКО угол Y (куда он едет по компасу).
        // Углы X (сальто) и Z (крен) мы принудительно ставим в 0.
        Quaternion rotationOnlyY = Quaternion.Euler(0, target.eulerAngles.y, 0);

        // Теперь умножаем offset на этот "плоский" поворот
        Vector3 desiredPosition = targetPos + (rotationOnlyY * offset);

        // Плавно летим
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 2. ВЗГЛЯД
        // Смотрим на байк. Тут тоже можно схитрить:
        // Смотрим не просто на центр байка, а чуть выше, 
        // чтобы камера не "клевала носом", если байк упадет в яму.
        transform.LookAt(targetPos + Vector3.up * 1.5f);
    }
}