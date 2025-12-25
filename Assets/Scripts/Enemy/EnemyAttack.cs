using UnityEngine;

/// <summary>
/// 敌人攻击系统
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("攻击设置")]
    [SerializeField] private float attackDamage = 20f;     // 攻击伤害
    [SerializeField] private float attackCooldown = 1.5f;  // 攻击冷却
    [SerializeField] private float attackRange = 2f;       // 攻击范围

    [Header("攻击方式")]
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [SerializeField] private GameObject projectilePrefab;   // 远程攻击用弹丸
    [SerializeField] private Transform firePoint;           // 发射点
    [SerializeField] private float projectileSpeed = 10f;   // 弹丸速度

    [Header("音效")]
    [SerializeField] private AudioClip attackSound;         // 攻击音效

    public enum AttackType
    {
        Melee,      // 近战攻击
        Ranged      // 远程攻击
    }

    // 组件引用
    private AudioSource audioSource;
    private Transform player;

    // 状态变量
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    // 公共属性
    public float AttackDamage => attackDamage;
    public bool IsAttacking => isAttacking;

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
        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    /// <summary>
    /// 尝试攻击
    /// </summary>
    public void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        if (player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > attackRange)
            return;

        // 执行攻击
        Attack();
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    private void Attack()
    {
        lastAttackTime = Time.time;
        isAttacking = true;

        // 播放攻击音效
        PlaySound(attackSound);

        switch (attackType)
        {
            case AttackType.Melee:
                MeleeAttack();
                break;
            case AttackType.Ranged:
                RangedAttack();
                break;
        }

        // 重置攻击状态
        Invoke(nameof(ResetAttack), 0.5f);

        Debug.Log($"敌人攻击! 类型: {attackType}, 伤害: {attackDamage}");
    }

    /// <summary>
    /// 近战攻击
    /// </summary>
    private void MeleeAttack()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    /// <summary>
    /// 远程攻击
    /// </summary>
    private void RangedAttack()
    {
        if (projectilePrefab == null || player == null)
            return;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 direction = (player.position - spawnPoint.position).normalized;

        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.LookRotation(direction));
        
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * projectileSpeed;
        }

        // 设置弹丸属性
        EnemyProjectile projScript = projectile.GetComponent<EnemyProjectile>();
        if (projScript != null)
        {
            projScript.SetDamage(attackDamage);
        }
    }

    /// <summary>
    /// 重置攻击状态
    /// </summary>
    private void ResetAttack()
    {
        isAttacking = false;
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
    /// 可视化攻击范围（编辑器）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
