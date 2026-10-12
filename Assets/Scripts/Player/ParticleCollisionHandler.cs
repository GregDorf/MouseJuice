using System.Collections.Generic;
using UnityEngine;

public class ParticleCollisionHandler : MonoBehaviour
{
    [SerializeField] private ParticleSystem partSystem;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collisionSound;

    private readonly List<ParticleCollisionEvent> collisionEvents = new();
    private readonly List<Vector3> collidedPositions = new();

    private void Start()
    {
        if (partSystem == null)
            partSystem = GetComponent<ParticleSystem>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnParticleCollision(GameObject other)
    {
        int eventCount = partSystem.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < eventCount; i++)
        {
            Vector3 collisionPosition = collisionEvents[i].intersection;

            bool alreadyPlayed = false;

            foreach (Vector3 position in collidedPositions)
            {
                if (Vector3.Distance(position, collisionPosition) < 0.1f)
                {
                    alreadyPlayed = true;
                    break;
                }
            }

            if (alreadyPlayed)
                continue;

            collidedPositions.Add(collisionPosition);

            if (audioSource != null && collisionSound != null)
            {
                audioSource.PlayOneShot(collisionSound);
            }
        }
    }
}