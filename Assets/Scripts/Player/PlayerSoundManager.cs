using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    [Header("Player Steps")]
    [SerializeField] private List<AudioClip> player_steps = new List<AudioClip>();
    [SerializeField] private float stepInterval = 0.3f;

    private float stepTimer;

    [Header("Player Jump")]
    [SerializeField] private List<AudioClip> player_jumps = new List<AudioClip>();
    [SerializeField] private AudioClip player_landing;

    [Header("Player Damage")]
    [SerializeField] AudioClip hit;
    [SerializeField] AudioClip death;

    [Header("Shotgun")]
    [SerializeField] private AudioClip shotgun_fire;
    [SerializeField] private AudioClip shotgun_reload;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    [System.Obsolete]
    public void PlaySteps()
    {
        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
            return;

        int num_step = Random.Range(0, player_steps.Count);
        audioSource.PlayOneShot(player_steps[num_step]);

        // Запускаем таймер следующего шага
        stepTimer = stepInterval;
    }

    [System.Obsolete]
    public void PlayJump()
    {
        int num_jump = Random.RandomRange(0, player_jumps.Count);
        audioSource.PlayOneShot(player_jumps[num_jump]);
    }

    public void PlayLanding()
    {
        audioSource.PlayOneShot(player_landing);
    }

    public void PlayHit()
    {
        audioSource.PlayOneShot(hit);
    }

    public void PlayDeath()
    {
        audioSource.PlayOneShot(death);
    }

    public void PlayFire()
    {
        audioSource.PlayOneShot(shotgun_fire);
    }

    public void PlayReload()
    {
        audioSource.PlayOneShot(shotgun_reload);
    }
}
