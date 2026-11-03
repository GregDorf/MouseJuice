using UnityEngine;

public class EnemySound : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip walk;
    [SerializeField] private AudioClip shot;
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip push;
    [SerializeField] private AudioClip death;

    [Header("Audio Source")]
    [SerializeField] private AudioSource source;

    [Header("Cooldowns")]
    [SerializeField] private float hitCooldown = 0.15f;

    private bool isWalkingPlaying;
    private float lastHitTime = float.NegativeInfinity;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
    }

    public void PlayWalk()
    {
        if (walk == null || source == null)
            return;

        if (isWalkingPlaying && source.clip == walk && source.isPlaying)
            return;

        source.clip = walk;
        source.loop = true;
        source.Play();
        isWalkingPlaying = true;
    }

    public void StopWalk()
    {
        if (source != null && isWalkingPlaying && source.clip == walk)
        {
            source.Stop();
            source.loop = false;
        }

        isWalkingPlaying = false;
    }

    public void PlayShot()
    {
        if (shot != null && source != null)
            source.PlayOneShot(shot);
    }

    public void PlayHit()
    {
        if (hit == null || source == null)
            return;

        if (Time.time - lastHitTime < hitCooldown)
            return;

        source.PlayOneShot(hit, 2f);
        lastHitTime = Time.time;
    }

    public void PlayPush()
    {
        if (push == null || source == null)
            return;

        if (Time.time - lastHitTime < hitCooldown)
            return;

        source.PlayOneShot(push, 5f);
        lastHitTime = Time.time;
    }

    public void PlayDeath()
    {
        if (death == null)
            return;

        GameObject tempAudioObject = new GameObject("TempDeathSound");
        tempAudioObject.transform.position = transform.position;

        AudioSource tempSource = tempAudioObject.AddComponent<AudioSource>();
        tempSource.clip = death;
        tempSource.spatialBlend = 0f;
        tempSource.volume = 1f;
        tempSource.Play();

        Destroy(tempAudioObject, death.length);
    }
}
