using UnityEngine;

/// <summary>
/// 玩家射击系统
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;      // 子弹预制件（用于视觉效果）
    [SerializeField] private Transform firePoint;          // 射击点
    [SerializeField] private float fireRate = 0.2f;        // 射击间隔
    [SerializeField] private float bulletDamage = 25f;     // 子弹伤害
    
    [Header("射程设置")]
    [SerializeField] private float effectiveRange = 35f;   // 有效射程（米）
    [SerializeField] private float maxRange = 50f;         // 最大射程（米）
    [SerializeField] private bool useDamageFalloff = true; // 启用距离衰减
    [SerializeField] private float aimAssistAngle = 10f;   // 辅助瞄准角度（度）
    [SerializeField] private float sphereCastRadius = 0.3f;// 射线检测半径（米）

    [Header("音效")]
    [SerializeField] private AudioClip shootSound;         // 射击音效
    [SerializeField] private AudioClip emptySound;         // 空弹匣音效
    [SerializeField] private AudioClip reloadSound;        // 换弹音效

    [Header("视觉效果")]
    [SerializeField] private ParticleSystem muzzleFlash;   // 枪口火焰
    [SerializeField] private Light muzzleLight;            // 枪口光效
    [SerializeField] private float muzzleLightDuration = 0.05f;
    [SerializeField] private GameObject bulletTrailPrefab; // 弹道轨迹预制件
    [SerializeField] private GameObject hitEffectPrefab;   // 击中特效

    // 组件引用
    private AudioSource audioSource;
    private PlayerController playerController;
    private PlayerAgent playerAgent;

    // 状态变量
    private float nextFireTime = 0f;
    private bool canShoot = true;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        playerController = GetComponent<PlayerController>();
        playerAgent = GetComponent<PlayerAgent>();

        // 自动查找射击点（如果未在 Inspector 中赋值）
        if (firePoint == null)
        {
            // 尝试在第一人称摄像机下查找
            Transform fpCam = transform.Find("FirstPersonCamera");
            if (fpCam != null)
            {
                firePoint = fpCam.Find("FirePoint");
            }
            
            // 如果还是没找到，创建一个默认的射击点
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(fpCam != null ? fpCam : transform);
                fp.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                firePoint = fp.transform;
                Debug.Log("已自动创建射击点 FirePoint");
            }
        }

        // 初始化枪口光效
        if (muzzleLight != null)
        {
            muzzleLight.enabled = false;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused))
            return;

        // 训练模式下不处理手动输入（完全由 Agent 控制）
        if (playerAgent != null && playerAgent.IsTrainingMode)
            return;

        HandleShooting();
    }

    /// <summary>
    /// 处理射击输入
    /// </summary>
    private void HandleShooting()
    {
        // 鼠标左键射击
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime && canShoot)
        {
            Shoot();
        }
    }

    /// <summary>
    /// 执行射击
    /// </summary>
    public void Shoot()
    {
        // 检查射击间隔（防止多次调用导致连发）
        if (Time.time < nextFireTime || !canShoot)
        {
            return;
        }

        // 检查弹药
        if (GameManager.Instance != null && !GameManager.Instance.UseAmmo())
        {
            // 没有弹药
            PlaySound(emptySound);
            return;
        }

        nextFireTime = Time.time + fireRate;

        // 获取活动相机
        Camera activeCamera = null;
        if (playerController != null)
        {
            activeCamera = playerController.GetActiveCamera();
        }
        if (activeCamera == null)
        {
            activeCamera = Camera.main;
        }

        // 从屏幕中心发射射线进行射击检测
        if (activeCamera != null && firePoint != null)
        {
            Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            Vector3 targetPoint;
            
            GameObject targetEnemy = null;
            float hitDistance = 0f;

            // 1. 使用球形射线检测（更宽容的命中判定）
            bool hitSomething = Physics.SphereCast(ray, sphereCastRadius, out hit, maxRange);

            if (hitSomething && hit.collider.CompareTag("Enemy"))
            {
                // 直接命中敌人
                targetEnemy = hit.collider.gameObject;
                hitDistance = hit.distance;
                targetPoint = hit.point;
            }
            else
            {
                // 2. 辅助瞄准：查找准星附近的敌人
                targetEnemy = FindNearbyEnemy(activeCamera, effectiveRange, aimAssistAngle);
                
                if (targetEnemy != null)
                {
                    hitDistance = Vector3.Distance(firePoint.position, targetEnemy.transform.position);
                    targetPoint = targetEnemy.transform.position;
                    Debug.Log($"辅助瞄准锁定敌人，距离: {hitDistance:F1}m");
                }
                else if (hitSomething)
                {
                    // 命中其他物体
                    targetPoint = hit.point;
                }
                else
                {
                    // 未命中任何物体
                    targetPoint = ray.GetPoint(maxRange);
                }
            }

            // 3. 处理命中敌人
            if (targetEnemy != null)
            {
                // 检查是否在有效射程内
                if (hitDistance > effectiveRange)
                {
                    Debug.Log($"<color=yellow>目标距离 {hitDistance:F1}m，超出有效射程 {effectiveRange}m，未造成有效伤害</color>");
                    // 播放音效提示玩家
                    PlaySound(emptySound); // 可以用空弹音效作为提示
                    
                    // 仍然显示弹道特效，但不造成伤害
                    CreateBulletTrail(firePoint.position, targetPoint);
                    return; // 不造成伤害
                }
                
                // 计算距离衰减伤害
                float actualDamage = CalculateDamageWithFalloff(bulletDamage, hitDistance);
                
                EnemyHealth enemyHealth = targetEnemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    enemyHealth.TakeDamage(actualDamage);
                    Debug.Log($"<color=green>命中敌人! 距离: {hitDistance:F1}m, 伤害: {actualDamage:F1}/{bulletDamage}</color>");
                }

                // 生成击中特效
                if (hitEffectPrefab != null)
                {
                    GameObject hitEffect = Instantiate(hitEffectPrefab, targetPoint, Quaternion.LookRotation(activeCamera.transform.forward));
                    Destroy(hitEffect, 2f);
                }
            }
            else if (hitSomething)
            {
                // 命中其他物体（墙壁等）
                targetPoint = hit.point;
                
                // 生成击中特效
                if (hitEffectPrefab != null)
                {
                    GameObject hitEffect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(hitEffect, 2f);
                }
            }

            // 创建弹道轨迹
            CreateBulletTrail(firePoint.position, targetPoint);
        }

        // 播放射击音效
        PlaySound(shootSound);

        // 播放枪口火焰
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // 枪口光效
        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            Invoke(nameof(DisableMuzzleLight), muzzleLightDuration);
        }

        Debug.Log("射击!");
    }

    /// <summary>
    /// 禁用枪口光效
    /// </summary>
    private void DisableMuzzleLight()
    {
        if (muzzleLight != null)
        {
            muzzleLight.enabled = false;
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
    /// 设置是否可以射击
    /// </summary>
    public void SetCanShoot(bool value)
    {
        canShoot = value;
    }

    /// <summary>
    /// 创建弹道轨迹
    /// </summary>
    private void CreateBulletTrail(Vector3 startPoint, Vector3 endPoint)
    {
        // 如果有弹道轨迹预制件，创建轨迹
        if (bulletTrailPrefab != null)
        {
            GameObject trail = Instantiate(bulletTrailPrefab, startPoint, Quaternion.identity);
            LineRenderer lineRenderer = trail.GetComponent<LineRenderer>();
            
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, endPoint);
            }
            
            // 销毁轨迹对象
            Destroy(trail, 0.5f);
        }
        else if (bulletPrefab != null)
        {
            // 如果没有专门的轨迹预制件，使用原有的子弹预制件作为视觉效果
            GameObject bullet = Instantiate(bulletPrefab, startPoint, Quaternion.LookRotation(endPoint - startPoint));
            
            // 移除物理组件，只保留视觉效果
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            Collider col = bullet.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null) Destroy(bulletScript);
            
            // 让子弹快速移动到终点（纯视觉效果）
            StartCoroutine(MoveBulletToTarget(bullet, startPoint, endPoint));
        }
    }

    /// <summary>
    /// 移动子弹到目标点（视觉效果）
    /// </summary>
    private System.Collections.IEnumerator MoveBulletToTarget(GameObject bullet, Vector3 start, Vector3 end)
    {
        float duration = 0.1f; // 子弹飞行时间
        float elapsed = 0f;

        while (elapsed < duration && bullet != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bullet.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (bullet != null)
        {
            Destroy(bullet);
        }
    }

    /// <summary>
    /// 查找准星附近的敌人（辅助瞄准）
    /// </summary>
    private GameObject FindNearbyEnemy(Camera cam, float maxDistance, float maxAngle)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float minDistance = maxDistance;
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy) continue;
            
            // 检查敌人是否已死亡
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null && health.IsDead) continue;
            
            Vector3 dirToEnemy = enemy.transform.position - cam.transform.position;
            float distance = dirToEnemy.magnitude;
            
            // 超出最大距离，跳过
            if (distance > maxDistance) continue;
            
            // 检查角度（准星偏离角度）
            float angle = Vector3.Angle(cam.transform.forward, dirToEnemy);
            if (angle < maxAngle && distance < minDistance)
            {
                nearestEnemy = enemy;
                minDistance = distance;
            }
        }
        
        if (nearestEnemy != null)
        {
            Debug.Log($"辅助瞄准找到敌人，角度偏差: {Vector3.Angle(cam.transform.forward, nearestEnemy.transform.position - cam.transform.position):F1}度");
        }
        
        return nearestEnemy;
    }

    /// <summary>
    /// 计算距离衰减伤害
    /// </summary>
    private float CalculateDamageWithFalloff(float baseDamage, float distance)
    {
        if (!useDamageFalloff)
        {
            return baseDamage;
        }
        
        // 距离衰减曲线
        if (distance < 15f)
        {
            return baseDamage;  // 100% 伤害（近距离）
        }
        else if (distance < 25f)
        {
            return baseDamage * 0.8f;  // 80% 伤害（中距离）
        }
        else if (distance < effectiveRange)
        {
            return baseDamage * 0.6f;  // 60% 伤害（远距离）
        }
        else
        {
            return 0f;  // 超出有效射程，无伤害
        }
    }

    /// <summary>
    /// 获取子弹伤害值（用于AI训练）
    /// </summary>
    public float GetBulletDamage()
    {
        return bulletDamage;
    }
}
