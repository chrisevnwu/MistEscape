using UnityEngine;

/// <summary>
/// 敌人生命值系统
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("生命值设置")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("视觉效果")]
    [SerializeField] private GameObject deathEffect;       // 死亡特效
    [SerializeField] private float destroyDelay = 2f;      // 死亡后销毁延迟

    [Header("音效")]
    [SerializeField] private AudioClip hurtSound;          // 受伤音效
    [SerializeField] private AudioClip deathSound;         // 死亡音效

    // 组件引用
    private AudioSource audioSource;
    private EnemyAI enemyAI;
    private UnityEngine.AI.NavMeshAgent agent;
    private Collider enemyCollider;

    // 状态变量
    private bool isDead = false;

    // 公共属性
    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    // 事件
    public delegate void EnemyDeathHandler(EnemyHealth enemy);
    public static event EnemyDeathHandler OnEnemyDeath;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        enemyCollider = GetComponent<Collider>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"敌人受到 {damage} 点伤害，剩余生命值: {currentHealth}/{maxHealth}");

        // 播放受伤音效
        PlaySound(hurtSound);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // 被攻击后进入追击状态
            if (enemyAI != null && enemyAI.CurrentState == EnemyAI.EnemyState.Patrol)
            {
                enemyAI.SetState(EnemyAI.EnemyState.Alert);
            }
        }
    }

    /// <summary>
    /// 敌人死亡
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("敌人死亡!");

        // 播放死亡音效
        PlaySound(deathSound);

        // 生成死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 禁用组件
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        if (agent != null)
        {
            agent.enabled = false;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // 通知 GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyDeath(transform.position);
        }

        // 触发事件
        OnEnemyDeath?.Invoke(this);

        // 延迟销毁
        Destroy(gameObject, destroyDelay);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 重置生命值（用于训练）
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        
        if (enemyAI != null)
        {
            enemyAI.enabled = true;
        }

        if (agent != null)
        {
            agent.enabled = true;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }
    }
}
