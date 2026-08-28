using UnityEngine;

public class SkullAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHP playerHP;

    [Header("HP Thresholds (0 - 100)")]
    [Range(0, 100)][SerializeField] private float normalStateLimit = 75f;
    [Range(0, 100)][SerializeField] private float lowStateLimit = 40f;
    [Range(0, 100)][SerializeField] private float criticalStateLimit = 15f;

    private float maxHP;
    private int currentHPState = -1; // -1, чтобы при запуске сработала смена состояния

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerHP == null) playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();

        maxHP = playerHP.hit_points;
    }

    void Update()
    {
        float hpPercent = (playerHP.current_hp / maxHP) * 100f;
        int nextState = CalculateState(hpPercent);

        // Если состояние изменилось
        if (nextState != currentHPState)
        {
            currentHPState = nextState;

            // 1. Устанавливаем новое состояние в основном слое
            animator.SetInteger("HP_State", currentHPState);

            // 2. Активируем помехи в слое эффектов
            animator.SetTrigger("Play_Noise");
        }
    }

    private int CalculateState(float percent)
    {
        if (percent <= 0f) return 3;           // Death
        if (percent <= criticalStateLimit) return 2; // Critical
        if (percent <= lowStateLimit) return 1;      // Low
        return 0;                                    // Normal
    }
}