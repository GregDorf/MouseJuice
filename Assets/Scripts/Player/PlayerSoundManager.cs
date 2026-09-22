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

    [Header("Player Slide")]
    [SerializeField] private AudioClip player_slide;

    [Header("Player Damage")]
    [SerializeField] AudioClip hit;
    [SerializeField] AudioClip death;

    [Header("Shotgun")]
    [SerializeField] private AudioClip shotgun_fire;
    [SerializeField] private AudioClip shotgun_reload;

    private AudioSource audioSource;
    private int prev_step;
    private int prev_jump;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        prev_step = Random.Range(0, player_steps.Count);
        prev_jump = Random.Range(0, player_jumps.Count);
    }

    public void PlaySteps()
    {
        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
            return;

        int num_step;
        do num_step = Random.Range(0, player_steps.Count); while (num_step == prev_step);
        audioSource.PlayOneShot(player_steps[num_step]);
        prev_step = num_step;

        // Запускаем таймер следующего шага
        stepTimer = stepInterval;
    }

    public void PlayJump()
    {
        int num_jump;
        do num_jump = Random.Range(0, player_jumps.Count); while (num_jump == prev_jump);
        audioSource.PlayOneShot(player_jumps[num_jump]);
        prev_jump = num_jump;
    }

    public void PlayLanding()
    {
        audioSource.PlayOneShot(player_landing);
    }

    public void PlaySlide()
    {
        audioSource.PlayOneShot(player_slide);
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
