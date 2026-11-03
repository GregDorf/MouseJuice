using UnityEngine;

// Диагностический компонент триггера.
// Сам по себе не переключает PlayerDetected: видимость проверяет Enemy через дистанцию и Raycast.
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

        if (enemy != null)
            Debug.Log($"{enemy.name}: Player entered detection trigger.", enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (enemy != null)
            Debug.Log($"{enemy.name}: Player exited detection trigger.", enemy);
    }
}
