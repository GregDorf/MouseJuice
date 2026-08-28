using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damager : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    public int TakeDamage()
    {
        return damage;
    }
}

/* TODO:
 */