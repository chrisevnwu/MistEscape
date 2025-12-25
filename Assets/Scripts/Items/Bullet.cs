using UnityEngine;

/// <summary>
/// 子弹脚本
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float damage = 25f;           // 子弹伤害
    [SerializeField] private float lifeTime = 3f;          // 生存时间
    [SerializeField] private float speed = 50f;            // 移动速度
    
    [Header("视觉效果")]
    [SerializeField] private GameObject hitEffect;          // 击中特效
    [SerializeField] private TrailRenderer trail;           // 弹道轨迹

    // 组件引用
    private Rigidbody rb;
    private bool isInitialized = false;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 配置刚体
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 自动查找或创建轨迹渲染器
        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }
        
        // 配置轨迹以增加可见性
        if (trail != null)
        {
            trail.time = 0.3f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.02f;
            trail.startColor = Color.yellow;
            trail.endColor = new Color(1, 1, 0, 0);
        }
    }

    private void Start()
    {
        // 自动销毁
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// 初始化子弹
    /// </summary>
    public void Initialize(Vector3 direction, float bulletSpeed, float bulletDamage)
    {
        moveDirection = direction.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        
        if (rb != null)
        {
            rb.velocity = moveDirection * speed;
        }

        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized && rb != null && rb.velocity.magnitude < 0.1f)
        {
            // 如果没有初始化且没有速度，使用默认方向
            rb.velocity = transform.forward * speed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject, collision.contacts[0].point);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 忽略玩家（不能打到自己）
        if (other.CompareTag("Player"))
            return;

        HandleHit(other.gameObject, transform.position);
    }

    /// <summary>
    /// 处理击中
    /// </summary>
    private void HandleHit(GameObject hitObject, Vector3 hitPoint)
    {
        // 击中敌人
        if (hitObject.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = hitObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
            Debug.Log($"子弹击中敌人，造成 {damage} 点伤害");
        }

        // 生成击中特效
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hitPoint, Quaternion.identity);
        }

        // 分离轨迹渲染器
        if (trail != null)
        {
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, trail.time);
        }

        // 销毁子弹
        Destroy(gameObject);
    }

    /// <summary>
    /// 获取伤害值
    /// </summary>
    public float GetDamage()
    {
        return damage;
    }
}
