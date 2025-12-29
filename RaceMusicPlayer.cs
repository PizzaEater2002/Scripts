using UnityEngine;
using System.Collections.Generic;

public class RaceMusicPlayer : MonoBehaviour
{
    [Header("🎵 Твои треки")]
    public List<AudioClip> playlist; // Сюда перетащишь файлы

    [Header("⚙️ Настройки")]
    [Range(0f, 1f)] public float volume = 0.4f; // Громкость (0.4 = 40%)
    public bool shuffle = true; // Перемешивать?

    private AudioSource _audioSource;
    private int _currentTrackIndex = -1;

    void Start()
    {
        // Добавляем источник звука автоматически
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.volume = volume;
        _audioSource.loop = false; // Не зацикливаем одну песню
        _audioSource.playOnAwake = false;

        // Запускаем первый трек
        PlayNextTrack();
    }

    void Update()
    {
        // Если песня закончилась сама — включаем следующую
        if (!_audioSource.isPlaying && playlist.Count > 0)
        {
            // Небольшая задержка или сразу (тут сразу)
            // Проверка, чтобы не переключало, если игра на паузе (Time.timeScale == 0)
            if (Time.timeScale > 0) 
            {
                PlayNextTrack();
            }
        }
        
        // (Опция) Обновляем громкость, если покрутишь ползунок во время игры
        _audioSource.volume = volume;
    }

    // Внутренняя логика выбора песни
    private void PlayNextTrack()
    {
        if (playlist.Count == 0) return;

        if (shuffle)
        {
            // Выбираем случайную, стараясь не повторять прошлую
            int newIndex = Random.Range(0, playlist.Count);
            if (playlist.Count > 1 && newIndex == _currentTrackIndex)
            {
                newIndex = (newIndex + 1) % playlist.Count;
            }
            _currentTrackIndex = newIndex;
        }
        else
        {
            // По порядку
            _currentTrackIndex = (_currentTrackIndex + 1) % playlist.Count;
        }

        _audioSource.clip = playlist[_currentTrackIndex];
        _audioSource.Play();
    }

    // --- ЭТОТ МЕТОД МЫ ПОВЕСИМ НА КНОПКУ ---
    public void SkipSong()
    {
        PlayNextTrack();
    }
}