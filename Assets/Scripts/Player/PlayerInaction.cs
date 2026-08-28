using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInaction : MonoBehaviour
{
    [SerializeField] private float pause_time_1;
    [SerializeField] private float pause_time_2;
    [SerializeField] private float pause_time_3;

    private Animator animator;
    private float current_time = 0;

    private bool anim_1_played = false;
    private bool anim_2_played = false;
    private bool anim_3_played = false;

    void Start()
    {
        anim_1_played = false;
        anim_2_played = false;
        anim_3_played = false;
        current_time = 0f;
        animator = GetComponent<Animator>();
    }


    void Update()
    {
        current_time += Time.deltaTime;

        if (current_time > pause_time_3 && !anim_3_played)
        {
            animator.SetTrigger("timeWait3Trigger");
            anim_3_played = true;
            return;
        }
        else if (current_time > pause_time_2 && !anim_2_played)
        {
            animator.SetTrigger("timeWait2Trigger");
            anim_2_played = true;
            return;
        }
        else if (current_time > pause_time_1 && !anim_1_played)
        {
            animator.SetTrigger("timeWait1Trigger");
            anim_1_played = true;
            return;
        }
    }

    public void PlayerFirstActivity()
    {
        anim_1_played = false;
        anim_2_played = false;
        anim_3_played = false;
        current_time = 0f;

        animator.SetTrigger("playerActive");
    }
}

/* TODO:
 */