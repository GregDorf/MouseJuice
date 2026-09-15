using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private ParticleSystem smoke;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        smoke = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;

        Vector2 direction = mouse - transform.position;

        if (direction.x > 0)
        {
            spriteRenderer.flipY = false;
        }
        else
        {
            spriteRenderer.flipY = true;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void StartSmoke()
    {
        smoke.Play();
    }

    public void HideShotgun()
    {
        spriteRenderer.enabled = false;
    }

    public void ShowShotgun()
    {
        spriteRenderer.enabled=true;
    }
}
