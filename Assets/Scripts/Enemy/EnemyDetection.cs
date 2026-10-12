using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        //enemy.SetPlayerDetected(true);

        Debug.Log($"{enemy.name}: Player detected.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        //enemy.SetPlayerDetected(false);

        Debug.Log($"{enemy.name}: Player lost.");
    }
}