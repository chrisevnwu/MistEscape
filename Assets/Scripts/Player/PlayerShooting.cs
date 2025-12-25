using UnityEngine;

/// <summary>
/// 玩家射击系统
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;      // 子弹预制件
    [SerializeField] private Transform firePoint;          // 射击点
    [SerializeField] private float bulletSpeed = 50f;      // 子弹速度
    [SerializeField] private float fireRate = 0.2f;        // 射击间隔
    [SerializeField] private float bulletDamage = 25f;     // 子弹伤害

    [Header("音效")]
    [SerializeField] private AudioClip shootSound;         // 射击音效
    [SerializeField] private AudioClip emptySound;         // 空弹匣音效
    [SerializeField] private AudioClip reloadSound;        // 换弹音效

    [Header("视觉效果")]
    [SerializeField] private ParticleSystem muzzleFlash;   // 枪口火焰
    [SerializeField] private Light muzzleLight;            // 枪口光效
    [SerializeField] private float muzzleLightDuration = 0.05f;

    // 组件引用
    private AudioSource audioSource;
    private PlayerController playerController;

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

        // 获取射击方向
        Vector3 shootDirection = GetShootDirection();

        // 创建子弹
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(shootDirection, bulletSpeed, bulletDamage);
            }
            else
            {
                // 如果没有 Bullet 脚本，直接给刚体加速度
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = shootDirection * bulletSpeed;
                }
            }
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
    /// 获取射击方向
    /// </summary>
    private Vector3 GetShootDirection()
    {
        Camera activeCamera = null;

        if (playerController != null)
        {
            activeCamera = playerController.GetActiveCamera();
        }

        if (activeCamera == null)
        {
            activeCamera = Camera.main;
        }

        if (activeCamera != null)
        {
            // 从屏幕中心发射射线
            Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                return (hit.point - firePoint.position).normalized;
            }
            else
            {
                return ray.direction;
            }
        }

        return transform.forward;
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
    /// 获取子弹伤害值（用于AI训练）
    /// </summary>
    public float GetBulletDamage()
    {
        return bulletDamage;
    }
}
