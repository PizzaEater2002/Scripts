using UnityEngine;

public class CameraAspectAdjuster : MonoBehaviour
{
    public float horizontalFOV = 60f; // Для ПК (широкий экран)
    public float verticalFOV = 95f;   // Для Мобилки (высокий экран) - ставь побольше!

    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        Adjust();
    }

#if UNITY_EDITOR
    void Update() => Adjust(); // В редакторе обновляем постоянно для тестов
#endif

    void Adjust()
    {
        // Если Высота > Ширины (Портретный режим)
        if (Screen.height > Screen.width)
            _cam.fieldOfView = verticalFOV;
        else
            _cam.fieldOfView = horizontalFOV;
    }
}
