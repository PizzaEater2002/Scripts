using UnityEngine;

public class ArcadeCamera : MonoBehaviour
{
    [Header("Цель слежения")]
    public Transform target;       // Ссылка на Bike Model (Визуал)
    public Rigidbody targetRb;     // Ссылка на Шар (Физика)

    [Header("Настройки")]
    public Vector3 offset = new Vector3(0f, 2.5f, -6f);
    public float followSpeed = 10f;
    public float rotationSpeed = 5f;

    [Header("Эффекты")]
    public bool enableSpeedEffect = true;
    public float minFov = 60f;
    public float maxFov = 85f;
    public float speedForMaxFov = 50f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // ВАЖНО: LateUpdate вызывается в самом конце кадра, когда байк уже подвинулся (в Update).
    // Это гарантирует отсутствие дрожания (Jitter).
    void LateUpdate()
    {
        if (target == null) return;

        // 1. Позиция
        // Следим только за поворотом по Y, игнорируем наклоны вверх/вниз
        Quaternion flatRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Vector3 desiredPosition = target.position + (flatRotation * offset);

        // Используем deltaTime вместо fixedDeltaTime, так как мы в LateUpdate (экранное время)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // 2. Поворот
        Vector3 lookTarget = target.position + (Vector3.up * 1.5f);
        Vector3 direction = lookTarget - transform.position;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. FOV (Эффект скорости)
        if (enableSpeedEffect && cam != null && targetRb != null)
        {
            float speed = targetRb.linearVelocity.magnitude;
            float targetFov = Mathf.Lerp(minFov, maxFov, speed / speedForMaxFov);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 2f);
        }
    }
}