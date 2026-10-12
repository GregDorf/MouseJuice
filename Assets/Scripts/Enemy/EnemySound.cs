using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySound : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip walk;
    [SerializeField] private AudioClip shot;
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip push;
    [SerializeField] private AudioClip death;

    [Header("SoundManager")]
    [SerializeField] private AudioSource source;

    private bool isWalkingPlaying = false;

    // Защита от наложения звуков (кулдауны)
    private float lastHitTime;
    private float hitCooldown = 0.15f; // Минимальное время между звуками ударов

    void Start()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
    }

    public void PlayWalk()
    {
        if (walk == null) return;

        if (!isWalkingPlaying || source.clip != walk || !source.isPlaying)
        {
            source.clip = walk;
            source.loop = true;
            source.Play();
            isWalkingPlaying = true;
        }
    }

    public void StopWalk()
    {
        if (isWalkingPlaying && source.clip == walk)
        {
            source.Stop();
            source.loop = false;
            isWalkingPlaying = false;
        }
    }

    public void PlayShot()
    {
        if (shot != null)
        {
            source.PlayOneShot(shot);
        }
    }

    public void PlayHit()
    {
        if (hit != null)
        {
            // Защита от спама: если удар был только что, пропускаем
            if (Time.time - lastHitTime < hitCooldown) return;

            source.PlayOneShot(hit, 2f);
            lastHitTime = Time.time;
        }
    }

    public void PlayPush()
    {
        if (push != null)
        {
            if (Time.time - lastHitTime < hitCooldown) return;

            source.PlayOneShot(push, 5f);
            lastHitTime = Time.time;
        }
    }

    public void PlayDeath()
    {
        if (death != null)
        {
            GameObject tempAudioObj = new GameObject("TempDeathSound");
            tempAudioObj.transform.position = transform.position;

            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = death;
            tempSource.spatialBlend = 0f;
            tempSource.volume = 3.0f; // Безопасное усиление (не 15.0, чтобы не было перегрузки)
            tempSource.Play();

            Destroy(tempAudioObj, death.length);
        }
    }
}