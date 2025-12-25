using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

/// <summary>
/// ML-Agents 玩家代理
/// 训练目标：在不被敌人杀死的情况下，收集所有药剂道具
/// </summary>
public class PlayerAgent : Agent
{
    [Header("引用")]
    [SerializeField] private Transform[] medicineTransforms;   // 药剂位置数组
    [SerializeField] private Transform[] enemyTransforms;       // 敌人位置数组
    [SerializeField] private Transform[] spawnPoints;           // 玩家重生点

    [Header("移动设置")]
    [SerializeField] private float moveForce = 10f;            // 移动力度
    [SerializeField] private float rotateSpeed = 200f;         // 旋转速度
    [SerializeField] private float maxVelocity = 8f;           // 最大速度

    [Header("奖励设置")]
    [SerializeField] private float medicineReward = 1.0f;      // 收集药剂奖励
    [SerializeField] private float winReward = 5.0f;           // 胜利大奖励
    [SerializeField] private float killEnemyReward = 0.3f;     // 击杀敌人奖励
    [SerializeField] private float damagePenalty = -0.5f;      // 受伤惩罚
    [SerializeField] private float deathPenalty = -2.0f;       // 死亡惩罚
    [SerializeField] private float approachReward = 0.01f;     // 接近目标奖励
    [SerializeField] private float timePenalty = -0.001f;      // 时间惩罚

    [Header("训练设置")]
    [SerializeField] private bool trainingMode = false;        // 训练模式（默认关闭，仅在训练时开启）

    // 组件引用
    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PlayerShooting playerShooting;
    private CharacterController characterController;

    // 状态变量
    private int collectedMedicines = 0;
    private int totalMedicines = 8;
    private float previousDistanceToMedicine = float.MaxValue;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float episodeStartTime;

    // 缓存
    private Transform nearestMedicine;
    private Transform nearestEnemy;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        playerShooting = GetComponent<PlayerShooting>();
        characterController = GetComponent<CharacterController>();

        // 训练模式下的设置
        if (trainingMode)
        {
            // 禁用 CharacterController 以便使用 Rigidbody
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            // 确保有 Rigidbody 并正确配置
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            // 配置 Rigidbody 防止掉落和翻滚
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                            RigidbodyConstraints.FreezeRotationZ |
                            RigidbodyConstraints.FreezePositionY;  // 锁定Y轴防止掉落
            rb.useGravity = false;  // 关闭重力
            rb.drag = 5f;  // 增加阻力使移动更可控
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;  // 防止穿墙

            // 添加碰撞器（如果没有）用于墙壁碰撞检测
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = new Vector3(0, 1, 0);
            }

            // 禁用第三人称摄像机的 AudioListener（防止重复）
            Transform tpCam = transform.Find("ThirdPersonCamera");
            if (tpCam != null)
            {
                AudioListener listener = tpCam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }
        }

        startPosition = transform.position;
        startRotation = transform.rotation;

        // 订阅事件
        if (GameManager.Instance != null)
        {
            totalMedicines = GameManager.Instance.TotalMedicines;
        }
    }

    public override void OnEpisodeBegin()
    {
        // 重置玩家位置
        ResetPlayer();

        // 重置药剂和敌人
        ResetEnvironment();

        // 重置状态
        collectedMedicines = 0;
        previousDistanceToMedicine = float.MaxValue;
        episodeStartTime = Time.time;

        // 重置 GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeGame();
        }

        // 重置玩家生命
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        Debug.Log("=== 新回合开始 ===");
    }

    /// <summary>
    /// 重置玩家位置
    /// </summary>
    private void ResetPlayer()
    {
        // 选择随机重生点或使用起始位置
        Vector3 spawnPos = startPosition;
        Quaternion spawnRot = startRotation;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            spawnPos = spawnPoints[randomIndex].position;
            spawnRot = spawnPoints[randomIndex].rotation;
        }

        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 重置环境（子类可以重写）
    /// </summary>
    protected virtual void ResetEnvironment()
    {
        // 更新药剂引用
        UpdateMedicineReferences();
        
        // 更新敌人引用
        UpdateEnemyReferences();
    }

    /// <summary>
    /// 更新药剂引用
    /// </summary>
    private void UpdateMedicineReferences()
    {
        GameObject[] medicines = GameObject.FindGameObjectsWithTag("Medicine");
        medicineTransforms = medicines.Select(m => m.transform).ToArray();
    }

    /// <summary>
    /// 更新敌人引用
    /// </summary>
    private void UpdateEnemyReferences()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemyTransforms = enemies.Select(e => e.transform).ToArray();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. 玩家位置 (归一化)
        sensor.AddObservation(transform.localPosition / 50f);  // 3个值

        // 2. 玩家速度 (归一化)
        if (rb != null)
        {
            sensor.AddObservation(rb.velocity / maxVelocity);  // 3个值
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }

        // 3. 玩家朝向
        sensor.AddObservation(transform.forward);  // 3个值

        // 4. 玩家生命值 (归一化)
        float healthNorm = 1f;
        if (GameManager.Instance != null)
        {
            healthNorm = GameManager.Instance.PlayerHealth / GameManager.Instance.MaxPlayerHealth;
        }
        sensor.AddObservation(healthNorm);  // 1个值

        // 5. 当前弹药 (归一化)
        float ammoNorm = 1f;
        if (GameManager.Instance != null)
        {
            ammoNorm = (float)GameManager.Instance.CurrentAmmo / GameManager.Instance.MaxAmmo;
        }
        sensor.AddObservation(ammoNorm);  // 1个值

        // 6. 已收集药剂数 (归一化)
        sensor.AddObservation((float)collectedMedicines / totalMedicines);  // 1个值

        // 7. 找到最近的药剂
        FindNearestMedicine();
        if (nearestMedicine != null)
        {
            Vector3 dirToMedicine = (nearestMedicine.position - transform.position).normalized;
            float distToMedicine = Vector3.Distance(nearestMedicine.position, transform.position) / 50f;
            sensor.AddObservation(dirToMedicine);  // 3个值
            sensor.AddObservation(distToMedicine);  // 1个值
        }
        else
        {
            sensor.AddObservation(Vector3.zero);  // 3个值
            sensor.AddObservation(0f);  // 1个值
        }

        // 8. 找到最近的敌人
        FindNearestEnemy();
        if (nearestEnemy != null)
        {
            Vector3 dirToEnemy = (nearestEnemy.position - transform.position).normalized;
            float distToEnemy = Vector3.Distance(nearestEnemy.position, transform.position) / 50f;
            sensor.AddObservation(dirToEnemy);  // 3个值
            sensor.AddObservation(distToEnemy);  // 1个值
        }
        else
        {
            sensor.AddObservation(Vector3.zero);  // 3个值
            sensor.AddObservation(0f);  // 1个值
        }

        // 总共: 3+3+3+1+1+1+3+1+3+1 = 20 个观察值
    }

    /// <summary>
    /// 找到最近的药剂
    /// </summary>
    private void FindNearestMedicine()
    {
        nearestMedicine = null;
        float minDistance = float.MaxValue;

        // 更新引用
        UpdateMedicineReferences();

        foreach (Transform medicine in medicineTransforms)
        {
            if (medicine != null && medicine.gameObject.activeInHierarchy)
            {
                float distance = Vector3.Distance(transform.position, medicine.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestMedicine = medicine;
                }
            }
        }

        previousDistanceToMedicine = minDistance;
    }

    /// <summary>
    /// 找到最近的敌人
    /// </summary>
    private void FindNearestEnemy()
    {
        nearestEnemy = null;
        float minDistance = float.MaxValue;

        // 更新引用
        UpdateEnemyReferences();

        foreach (Transform enemy in enemyTransforms)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null && !health.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, enemy.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 获取连续动作
        float moveX = actions.ContinuousActions[0];  // 左右移动
        float moveZ = actions.ContinuousActions[1];  // 前后移动
        float rotate = actions.ContinuousActions[2]; // 旋转

        // 获取离散动作
        int shoot = actions.DiscreteActions[0];  // 0=不射击, 1=射击

        // 执行移动
        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        
        if (rb != null)
        {
            rb.AddForce(moveDirection * moveForce, ForceMode.Force);
            
            // 限制最大速度
            if (rb.velocity.magnitude > maxVelocity)
            {
                rb.velocity = rb.velocity.normalized * maxVelocity;
            }
        }

        // 执行旋转
        transform.Rotate(Vector3.up, rotate * rotateSpeed * Time.fixedDeltaTime);

        // 执行射击
        if (shoot == 1 && playerShooting != null)
        {
            playerShooting.Shoot();
        }

        // 计算接近药剂的奖励
        CalculateApproachReward();

        // 时间惩罚
        AddReward(timePenalty);

        // 检查游戏状态
        CheckGameState();
    }

    /// <summary>
    /// 计算接近目标的奖励
    /// </summary>
    private void CalculateApproachReward()
    {
        if (nearestMedicine != null)
        {
            float currentDistance = Vector3.Distance(transform.position, nearestMedicine.position);
            
            if (currentDistance < previousDistanceToMedicine)
            {
                // 接近药剂，给予小奖励
                AddReward(approachReward);
            }
            else if (currentDistance > previousDistanceToMedicine)
            {
                // 远离药剂，给予小惩罚
                AddReward(approachReward * -0.5f);
            }

            previousDistanceToMedicine = currentDistance;
        }
    }

    /// <summary>
    /// 检查游戏状态
    /// </summary>
    private void CheckGameState()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.IsGameOver)
        {
            if (GameManager.Instance.IsGameWon)
            {
                // 胜利
                AddReward(winReward);
                Debug.Log($"AI 胜利! 总奖励: {GetCumulativeReward()}");
            }
            else
            {
                // 失败（死亡）
                AddReward(deathPenalty);
                Debug.Log($"AI 失败! 总奖励: {GetCumulativeReward()}");
            }
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 人类控制模式
        var continuousActions = actionsOut.ContinuousActions;
        var discreteActions = actionsOut.DiscreteActions;

        // 移动
        continuousActions[0] = Input.GetAxis("Horizontal");  // A/D
        continuousActions[1] = Input.GetAxis("Vertical");    // W/S
        continuousActions[2] = Input.GetAxis("Mouse X");     // 鼠标水平

        // 射击
        discreteActions[0] = Input.GetMouseButton(0) ? 1 : 0;  // 鼠标左键
    }

    /// <summary>
    /// 收集药剂时调用
    /// </summary>
    public void OnMedicineCollected()
    {
        collectedMedicines++;
        AddReward(medicineReward);
        Debug.Log($"AI 收集药剂! 进度: {collectedMedicines}/{totalMedicines}, 获得奖励: {medicineReward}");
    }

    /// <summary>
    /// 受到伤害时调用
    /// </summary>
    public void OnDamageTaken(float damage)
    {
        AddReward(damagePenalty);
        Debug.Log($"AI 受到伤害! 惩罚: {damagePenalty}");
    }

    /// <summary>
    /// 击杀敌人时调用
    /// </summary>
    public void OnEnemyKilled()
    {
        AddReward(killEnemyReward);
        Debug.Log($"AI 击杀敌人! 奖励: {killEnemyReward}");
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 收集药剂
        if (other.CompareTag("Medicine"))
        {
            OnMedicineCollected();
        }
    }

    /// <summary>
    /// 掉出地图检测
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("DeathZone"))
        {
            AddReward(deathPenalty);
            EndEpisode();
        }
    }
}
