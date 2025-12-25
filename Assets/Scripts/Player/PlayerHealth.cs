using UnityEngine;

/// <summary>
/// 玩家生命值系统
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float invincibilityDuration = 0.5f;  // 受伤后无敌时间
    
    [Header("视觉效果")]
    [SerializeField] private GameObject damageEffect;   // 受伤特效
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.3f);  // 受伤屏幕颜色

    [Header("音效")]
    [SerializeField] private AudioClip hurtSound;       // 受伤音效
    [SerializeField] private AudioClip deathSound;      // 死亡音效

    // 组件引用
    private AudioSource audioSource;

    // 状态变量
    private bool isInvincible = false;
    private float lastDamageTime = 0f;
    private bool isDead = false;

    // 事件
    public delegate void PlayerHurtHandler(float damage);
    public event PlayerHurtHandler OnPlayerHurt;

    public delegate void PlayerDeathHandler();
    public event PlayerDeathHandler OnPlayerDeath;

    public bool IsDead => isDead;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // 订阅 GameManager 事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHealthChanged -= OnHealthChanged;
        }
    }

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible)
            return;

        // 通知 GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(damage);
        }

        // 触发受伤事件
        OnPlayerHurt?.Invoke(damage);

        // 播放受伤音效
        PlaySound(hurtSound);

        // 显示受伤特效
        ShowDamageEffect();

        // 启用无敌时间
        StartInvincibility();

        Debug.Log($"玩家受到 {damage} 点伤害");
    }

    /// <summary>
    /// 健康值变化回调
    /// </summary>
    private void OnHealthChanged(float current, float max)
    {
        if (current <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// 玩家死亡
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("玩家死亡!");

        // 播放死亡音效
        PlaySound(deathSound);

        // 触发死亡事件
        OnPlayerDeath?.Invoke();

        // 禁用玩家控制
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.enabled = false;
        }
    }

    /// <summary>
    /// 启用无敌时间
    /// </summary>
    private void StartInvincibility()
    {
        isInvincible = true;
        lastDamageTime = Time.time;
        Invoke(nameof(EndInvincibility), invincibilityDuration);
    }

    /// <summary>
    /// 结束无敌时间
    /// </summary>
    private void EndInvincibility()
    {
        isInvincible = false;
    }

    /// <summary>
    /// 显示受伤特效
    /// </summary>
    private void ShowDamageEffect()
    {
        if (damageEffect != null)
        {
            damageEffect.SetActive(true);
            Invoke(nameof(HideDamageEffect), 0.2f);
        }
    }

    /// <summary>
    /// 隐藏受伤特效
    /// </summary>
    private void HideDamageEffect()
    {
        if (damageEffect != null)
        {
            damageEffect.SetActive(false);
        }
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
    /// 重置状态（用于训练）
    /// </summary>
    public void ResetHealth()
    {
        isDead = false;
        isInvincible = false;
        
        // 重新启用组件
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
        }

        PlayerShooting shooting = GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            shooting.enabled = true;
        }
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 检测敌人攻击
        if (other.CompareTag("EnemyAttack"))
        {
            float damage = 10f;  // 默认伤害
            EnemyAttack attack = other.GetComponent<EnemyAttack>();
            if (attack != null)
            {
                damage = attack.AttackDamage;
            }
            TakeDamage(damage);
        }
    }
}
