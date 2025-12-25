using UnityEngine;

/// <summary>
/// 敌人远程攻击弹丸
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject hitEffect;

    private void Start()
    {
        // 自动销毁
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// 设置伤害值
    /// </summary>
    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 击中玩家
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            SpawnHitEffect();
            Destroy(gameObject);
        }
        // 击中墙壁或地面
        else if (!other.CompareTag("Enemy") && !other.isTrigger)
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 击中玩家
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
        
        SpawnHitEffect();
        Destroy(gameObject);
    }

    /// <summary>
    /// 生成击中特效
    /// </summary>
    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
    }
}
