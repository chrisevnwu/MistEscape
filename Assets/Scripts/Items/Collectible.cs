using UnityEngine;

/// <summary>
/// 可收集道具（药剂）
/// </summary>
public class Collectible : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private CollectibleType itemType = CollectibleType.Medicine;
    [SerializeField] private int value = 1;                // 收集价值

    [Header("动画")]
    [SerializeField] private float rotateSpeed = 90f;      // 旋转速度
    [SerializeField] private float bobSpeed = 2f;          // 上下浮动速度
    [SerializeField] private float bobHeight = 0.3f;       // 浮动高度

    [Header("音效")]
    [SerializeField] private AudioClip pickupSound;        // 拾取音效

    [Header("粒子效果")]
    [SerializeField] private ParticleSystem glowEffect;    // 发光效果
    [SerializeField] private ParticleSystem pickupEffect;  // 拾取特效

    public enum CollectibleType
    {
        Medicine,   // 药剂
        Health,     // 血包
        Ammo        // 弹药
    }

    // 初始位置
    private Vector3 startPosition;
    private bool isCollected = false;

    // 公共属性
    public CollectibleType ItemType => itemType;

    private void Start()
    {
        startPosition = transform.position;

        // 🔥 自动调整 Collider 大小（训练模式专用）
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.radius = 1.5f;  // 增加到1.5米（原来可能是0.5-0.8米）
            col.isTrigger = true;
        }
        // 如果是 BoxCollider 或其他类型
        else
        {
            Collider[] cols = GetComponents<Collider>();
            foreach (Collider c in cols)
            {
                c.isTrigger = true;
                // 尝试扩大范围
                if (c is BoxCollider)
                {
                    BoxCollider box = c as BoxCollider;
                    box.size = box.size * 2.0f;  // 扩大2倍
                }
            }
        }

        // 启动发光效果
        if (glowEffect != null)
        {
            glowEffect.Play();
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // 旋转动画
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // 上下浮动动画
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // 🔥 磁吸效果：吸引附近的玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < 3f && dist > 0.5f)  // 3米内开始吸引
            {
                Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
                transform.position += dirToPlayer * 2f * Time.deltaTime;  // 2m/s 拉近速度
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
        }
    }

    /// <summary>
    /// 收集道具
    /// </summary>
    private void Collect(GameObject collector)
    {
        isCollected = true;

        // 根据类型执行不同的效果
        switch (itemType)
        {
            case CollectibleType.Medicine:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectMedicine();
                }
                break;
            case CollectibleType.Health:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Heal(25f * value);  // 恢复生命值，value作为倍数
                }
                break;
            case CollectibleType.Ammo:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddAmmo(10 * value);  // 增加弹药，value作为倍数
                }
                break;
        }

        // 播放音效
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // 播放拾取特效
        if (pickupEffect != null)
        {
            pickupEffect.transform.SetParent(null);
            pickupEffect.Play();
            Destroy(pickupEffect.gameObject, pickupEffect.main.duration);
        }

        // 停止发光效果
        if (glowEffect != null)
        {
            glowEffect.Stop();
        }

        Debug.Log($"收集了 {itemType}!");

        // 销毁物体
        Destroy(gameObject);
    }

    /// <summary>
    /// 可视化收集范围（编辑器）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
