using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    private bool _isActivated = false; // Чтобы не сохранять один и тот же чекпоинт 100 раз

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что в нас въехал именно Игрок (или его часть)
        // Ищем скрипт RespawnManager на том, кто въехал, или на его родителях
        RespawnManager playerRespawn = other.GetComponentInParent<RespawnManager>();

        if (playerRespawn != null && !_isActivated)
        {
            // Сохраняем позицию ЭТОГО ТРИГГЕРА, а не игрока.
            // Это важно: игрок может въехать боком, а возродиться он должен ровно.
            playerRespawn.SetCheckpoint(transform.position, transform.rotation);
            
            _isActivated = true;
            Debug.Log("Checkpoint Activated: " + gameObject.name);
        }
    }
}