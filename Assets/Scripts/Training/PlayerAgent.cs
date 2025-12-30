using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

/// <summary>
/// ML-Agents 玩家代理
/// 训练目标：在不被敌人杀死的情况下，收集所有药剂道具
/// 
/// 设计原则：
/// - Training Mode OFF: PlayerController 完全控制，PlayerAgent 不干涉
/// - Training Mode ON: PlayerAgent 接管控制，使用与 PlayerController 相同的物理系统
/// </summary>
public class PlayerAgent : Agent
{
    [Header("引用")]
    [SerializeField] private Transform[] medicineTransforms;   // 药剂位置数组
    [SerializeField] private Transform[] enemyTransforms;       // 敌人位置数组
    [SerializeField] private Transform[] spawnPoints;           // 玩家重生点

    [Header("移动设置 - 仅训练模式使用")]
    [SerializeField] private float moveSpeed = 5f;              // 移动速度（与PlayerController保持一致）
    [SerializeField] private float rotateSpeed = 200f;          // 旋转速度
    [SerializeField] private float gravity = -9.81f;            // 重力

    [Header("动作响应性设置")]
    [SerializeField] private float moveActionScale = 30.0f;     // 移动动作放大倍数（从25提升到30）
    [SerializeField] private float rotateActionScale = 12.0f;   // 旋转动作放大倍数（从10提升到12）

    [Header("Episode 设置")]
    [SerializeField] private float maxEpisodeTime = 300f;       // Episode 超时时间（秒，大地图需要更多时间）

    [Header("奖励设置")]
    [SerializeField] private float medicineReward = 1.0f;      // 收集药剂奖励
    [SerializeField] private float winReward = 5.0f;           // 胜利大奖励
    [SerializeField] private float killEnemyReward = 0.5f;     // 击杀敌人奖励（提升）
    [SerializeField] private float damagePenalty = -0.2f;      // 受伤惩罚（降低）
    [SerializeField] private float deathPenalty = -1.0f;       // 死亡惩罚（降低）
    [SerializeField] private float timePenalty = 0f;     // 时间惩罚（暂时移除，避免负奖励累积）
    
    [Header("行为塑形奖励")]
    [SerializeField] private float movementPenalty = 0f;  // 移动过慢惩罚（暂时移除）

    [Header("训练设置")]
    [SerializeField] private bool trainingMode = false;        // 训练模式（默认关闭，仅在训练时开启）

    // 公共属性
    public bool IsTrainingMode => trainingMode;

    // 组件引用
    private CharacterController characterController;
    private PlayerHealth playerHealth;
    private PlayerShooting playerShooting;
    private PlayerController playerController;

    // 状态变量
    private int collectedMedicines = 0;
    private int totalMedicines = 8;
    private float previousDistanceToMedicine = float.MaxValue;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float episodeStartTime;
    
    // 训练模式专用变量
    private Vector3 trainingVelocity;
    private float trainingYRotation = 0f;

    // 缓存
    private Transform nearestMedicine;
    private Transform nearestEnemy;
    
    // 射击状态跟踪
    private bool wantedToShootLastFrame = false;
    
    // 引导奖励跟踪（方案B）
    private float lastDistanceToMedicine = float.MaxValue;
    private Vector3 lastPosition;
    private float lastMoveCheckTime = 0f;  // 上次检查移动的时间
    private Vector3 lastMoveCheckPosition;  // 上次检查时的位置
    
    // 方向一致性跟踪
    private Vector3 lastMoveDirection = Vector3.zero;  // 上一帧的移动方向
    private int consistentMoveFrames = 0;  // 持续同方向移动的帧数
    
    // 朝向跟踪
    private float lastAlignmentToMedicine = 0f;  // 上一帧朝向药剂的对齐度

    public override void Initialize()
    {
        characterController = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
        playerShooting = GetComponent<PlayerShooting>();
        playerController = GetComponent<PlayerController>();

        startPosition = transform.position;
        startRotation = transform.rotation;
        trainingYRotation = transform.eulerAngles.y;

        // 训练模式下的设置
        if (trainingMode)
        {
            // 禁用 PlayerController（由 Agent 接管控制）
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // 确保 CharacterController 启用（Agent 使用它移动）
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // 禁用所有AudioListener，只保留第一人称摄像机的
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
            Transform fpCam = transform.Find("FirstPersonCamera");
            
            foreach (AudioListener listener in allListeners)
            {
                // 只保留第一人称摄像机上的AudioListener
                if (fpCam != null && listener.transform == fpCam)
                {
                    listener.enabled = true;
                }
                else
                {
                    listener.enabled = false;
                }
            }
            
            // 解锁并显示光标（防止鼠标影响AI视角）
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log($"PlayerAgent: 训练模式已启用，已禁用{allListeners.Length - 1}个多余的AudioListener");
        }
        else
        {
            // 非训练模式：确保 PlayerController 启用
            if (playerController != null)
            {
                playerController.enabled = true;
            }
            
            Debug.Log("PlayerAgent: 训练模式未启用，PlayerController 正常工作");
        }

        // 订阅事件
        if (GameManager.Instance != null)
        {
            totalMedicines = GameManager.Instance.TotalMedicines;
        }
        else
        {
            // 如果没有 GameManager，动态检测地图上的药剂数量
            GameObject[] medicines = GameObject.FindGameObjectsWithTag("Medicine");
            if (medicines.Length > 0)
            {
                totalMedicines = medicines.Length;
            }
        }
        Debug.Log($"PlayerAgent: 需要收集的药剂总数 = {totalMedicines}");
    }

    public override void OnEpisodeBegin()
    {
        // 只有在训练模式下才重置
        if (!trainingMode) return;

        // 重置玩家位置
        ResetPlayer();

        // 重置环境
        ResetEnvironment();

        // 重置状态
        collectedMedicines = 0;
        previousDistanceToMedicine = float.MaxValue;
        episodeStartTime = Time.time;
        trainingVelocity = Vector3.zero;
        wantedToShootLastFrame = false;
        
        // 重置移动检查
        lastPosition = transform.position;
        lastMoveCheckPosition = transform.position;
        lastMoveCheckTime = Time.time;
        lastAlignmentToMedicine = 0f;

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

        // 禁用 CharacterController 以便直接设置位置
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        trainingVelocity = Vector3.zero;
        trainingYRotation = spawnRot.eulerAngles.y;
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
        Vector3 currentVelocity = Vector3.zero;
        if (characterController != null)
        {
            currentVelocity = characterController.velocity;
        }
        sensor.AddObservation(currentVelocity / 8f);  // 3个值

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
            
            // 🔥 关键：添加有符号角度，让AI知道目标在左边还是右边
            float signedAngle = Vector3.SignedAngle(transform.forward, dirToMedicine, Vector3.up);
            float normalizedAngle = signedAngle / 180f;  // 归一化到 [-1, 1]
            // -1 = 目标在正后方左侧
            //  0 = 目标在正前方
            // +1 = 目标在正后方右侧
            
            sensor.AddObservation(dirToMedicine);  // 3个值
            sensor.AddObservation(distToMedicine);  // 1个值
            sensor.AddObservation(normalizedAngle);  // 🔥 1个值 - 告诉AI应该左转还是右转！
        }
        else
        {
            sensor.AddObservation(Vector3.zero);  // 3个值
            sensor.AddObservation(0f);  // 1个值
            sensor.AddObservation(0f);  // 🔥 1个值 - 角度
        }

        // 8. 找到最近的敌人
        FindNearestEnemy();
        if (nearestEnemy != null)
        {
            Vector3 dirToEnemy = (nearestEnemy.position - transform.position).normalized;
            float distToEnemy = Vector3.Distance(nearestEnemy.position, transform.position) / 50f;
            
            // 🔥 同样添加敌人的有符号角度
            float signedAngleEnemy = Vector3.SignedAngle(transform.forward, dirToEnemy, Vector3.up);
            float normalizedAngleEnemy = signedAngleEnemy / 180f;
            
            sensor.AddObservation(dirToEnemy);  // 3个值
            sensor.AddObservation(distToEnemy);  // 1个值
            sensor.AddObservation(normalizedAngleEnemy);  // 🔥 1个值
            
            // 新增：敌人相对速度
            Vector3 enemyVelocity = Vector3.zero;
            UnityEngine.AI.NavMeshAgent navAgent = nearestEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                enemyVelocity = navAgent.velocity;
            }
            sensor.AddObservation(enemyVelocity / 8f);  // 3个值（归一化）
        }
        else
        {
            sensor.AddObservation(Vector3.zero);  // 3个值
            sensor.AddObservation(0f);  // 1个值
            sensor.AddObservation(0f);  // 🔥 1个值 - 角度
            sensor.AddObservation(Vector3.zero);  // 3个值（敌人速度）
        }
        
        // 9. 视野内目标计数
        int visibleMedicines = CountVisibleObjects("Medicine", 30f);
        int visibleEnemies = CountVisibleObjects("Enemy", 30f);
        sensor.AddObservation((float)visibleMedicines / 8f);  // 1个值
        sensor.AddObservation((float)visibleEnemies / 8f);   // 1个值
        
        // 10. 玩家朝向与最近敌人的对齐度（用于射击判断）
        float enemyAlignment = 0f;
        if (nearestEnemy != null)
        {
            Vector3 dirToEnemy = (nearestEnemy.position - transform.position).normalized;
            enemyAlignment = Vector3.Dot(transform.forward, dirToEnemy);  // -1到1之间
        }
        sensor.AddObservation(enemyAlignment);  // 1个值
        
        // 11. 前进速度（速度在朝向上的投影）
        float forwardSpeed = 0f;
        if (characterController != null && characterController.velocity.magnitude > 0.1f)
        {
            forwardSpeed = Vector3.Dot(characterController.velocity, transform.forward) / moveSpeed;
        }
        sensor.AddObservation(forwardSpeed);  // 1个值（归一化到[-1, 1]，负值=倒退）

        // 总共: 3+3+3+1+1+1+3+1+1 + 3+1+1+3 + 1+1+1+1 = 29 个观察值
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
        // 非训练模式下不处理动作
        if (!trainingMode) return;
        
        if (characterController == null || !characterController.enabled) return;

        // 获取连续动作并应用缩放
        float moveX = actions.ContinuousActions[0] * moveActionScale;
        float moveZ = actions.ContinuousActions[1] * moveActionScale;
        float rotate = actions.ContinuousActions[2] * rotateActionScale;

        // 获取离散动作
        int shoot = actions.DiscreteActions[0];  // 0=不射击, 1=射击

        // 执行旋转（更快的响应）
        trainingYRotation += rotate * rotateSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0f, trainingYRotation, 0f);

        // 计算移动方向并归一化（防止对角线移动过快）
        Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ);
        if (moveDirection.magnitude > moveSpeed)
        {
            moveDirection = moveDirection.normalized * moveSpeed;
        }
        
        // 使用 CharacterController 移动
        Vector3 horizontalMove = moveDirection * Time.deltaTime;

        // 处理重力和地面检测
        bool isGrounded = characterController.isGrounded;
        if (isGrounded && trainingVelocity.y < 0)
        {
            trainingVelocity.y = -2f;
        }

        // 应用重力
        trainingVelocity.y += gravity * Time.deltaTime;

        // 移动角色
        characterController.Move(horizontalMove + trainingVelocity * Time.deltaTime);

        // 执行射击 - 简化逻辑
        bool wantsToShoot = (shoot == 1);
        
        if (wantsToShoot && !wantedToShootLastFrame && playerShooting != null)
        {
            TryIntelligentShoot();
        }
        
        wantedToShootLastFrame = wantsToShoot;

        // 核心奖励系统
        CalculateGuidedRewards();
        
        // 🔥 新增：动作一致性奖励（修复倒退问题）
        RewardMovementConsistency();

        // 移动惩罚：基于实际移动距离（每2秒检查一次）
        if (Time.time - lastMoveCheckTime > 2f)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastMoveCheckPosition);
            
            // 🔧 修改：只在极端静止时惩罚（从5米降低到2米）
            if (distanceMoved < 2f)  // 2秒内移动不足2米 → 惩罚
            {
                AddReward(movementPenalty);  // 基础惩罚
                Debug.Log($"AI 移动不足：{distanceMoved:F1}米/2秒，惩罚 {movementPenalty}");
            }
            else
            {
                // 🔥 新增：移动充分时给予小奖励
                AddReward(0.002f);
            }
            
            // 更新检查点
            lastMoveCheckPosition = transform.position;
            lastMoveCheckTime = Time.time;
        }

        // 时间惩罚（轻微）
        AddReward(timePenalty);

        // 检查游戏状态
        CheckGameState();
    }

    /// <summary>
    /// 智能射击：只在合理情况下射击
    /// </summary>
    private bool TryIntelligentShoot()
    {
        if (playerShooting == null) return false;
        
        // 检查是否有足够弹药
        if (GameManager.Instance != null && GameManager.Instance.CurrentAmmo <= 0)
        {
            return false;
        }
        
        // 检查是否有敌人在射程内
        if (nearestEnemy == null)
        {
            return false;
        }
        
        float distToEnemy = Vector3.Distance(transform.position, nearestEnemy.position);
        
        // 有效射程设置为35米（与PlayerShooting保持一致）
        float effectiveRange = 35f;
        
        // 敌人超出有效射程 - 不射击
        if (distToEnemy > effectiveRange)
        {
            return false;
        }
        
        // 检查是否瞄准敌人
        Camera activeCamera = GetActiveCamera();
        if (activeCamera != null)
        {
            Ray ray = activeCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            
            // 使用球形射线检测（与PlayerShooting一致，半径0.3米）
            if (Physics.SphereCast(ray, 0.3f, out hit, effectiveRange))
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    // 瞄准敌人且在有效射程内，射击
                    playerShooting.Shoot();
                    Debug.Log($"AI 射击敌人，距离: {distToEnemy:F1}m");
                    return true;
                }
            }
        }
        
        // 敌人很近（<8米）即使没完美瞄准也射击（应急措施）
        if (distToEnemy < 8f)
        {
            playerShooting.Shoot();
            Debug.Log($"AI 近距离射击，距离: {distToEnemy:F1}m");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 方案C：聚焦于持续方向移动的奖励系统
    /// </summary>
    private void CalculateGuidedRewards()
    {
        if (nearestMedicine == null)
        {
            FindNearestMedicine();
            if (nearestMedicine == null) return;
        }
        
        float currentDistance = Vector3.Distance(transform.position, nearestMedicine.position);
        Vector3 directionToMedicine = (nearestMedicine.position - transform.position).normalized;
        
        // === 核心奖励 0：朝向目标（大幅降低，避免原地调整视角） ===
        float facingAlignment = Vector3.Dot(transform.forward, directionToMedicine);
        
        // 🔧 移除持续的朝向奖励，改为只在接近时才奖励朝向
        // 这样AI不会因为"一直对准"而停在原地
        
        if (facingAlignment < -0.3f)  // 背对目标！（更严重才惩罚）
        {
            AddReward(-0.02f);  // 轻度惩罚
            Debug.Log($"❌ AI 背对药剂，惩罚 -0.02");
        }
        
        // 🔧 完全移除"改善朝向"奖励 - 这是导致视角乱晃的主因！
        // AI为了获得这个奖励会不停微调视角
        
        lastAlignmentToMedicine = facingAlignment;
        
        // === 核心奖励 1：大幅奖励“移动后更近” ===
        if (lastDistanceToMedicine != float.MaxValue)
        {
            float distanceChange = lastDistanceToMedicine - currentDistance;
            
            if (distanceChange > 0.1f)  // 更近了！
            {
                // 💎 最终平衡版：降低倍数避免奖励过大波动
                float proximityBonus = distanceChange * 2.0f;  // 降低（从3.0到2.0）
                
                // 距离越近，奖励越高
                if (currentDistance < 10f)
                {
                    proximityBonus *= 3f;  // 降低（从5x到3x）
                }
                else if (currentDistance < 20f)
                {
                    proximityBonus *= 2f;  // 降低（从3x到2x）
                }
                
                // 如果朝向也正确，额外奖励
                if (facingAlignment > 0.5f)  // 降低要求（从0.7到0.5）
                {
                    proximityBonus *= 1.5f;  // 降低（从2.5x到1.5x）
                }
                
                AddReward(proximityBonus);
                Debug.Log($"✅ 靠近药剂 +{proximityBonus:F2}");
            }
            else if (distanceChange < -0.5f)  // 远离了！
            {
                AddReward(-0.1f);  // 固定轻度惩罚
                Debug.Log($"❌ 远离药剂 -0.1");
            }
        }
        
        lastDistanceToMedicine = currentDistance;
        
        // === 核心奖励 2：移动奖励（最终平衡版） ===
        if (characterController.velocity.magnitude > 0.5f)
        {
            float velocityAlignment = Vector3.Dot(characterController.velocity.normalized, directionToMedicine);
            float speed = characterController.velocity.magnitude;
            
            if (speed > 3f && velocityAlignment > 0.5f)
            {
                AddReward(0.08f);  // 快速移动主奖励
            }
            else if (speed > 1.5f && velocityAlignment > 0.3f)
            {
                AddReward(0.03f);  // 中速移动
            }
            else if (speed > 2f && velocityAlignment < -0.5f)
            {
                AddReward(-0.1f);  // 倒退惩罚
            }
        }
        
        // === 核心奖励 3：惩罚方向抖动 ===
        Vector3 currentMoveDir = characterController.velocity.normalized;
        
        if (lastMoveDirection != Vector3.zero && currentMoveDir.magnitude > 0.1f)
        {
            float directionChange = Vector3.Dot(currentMoveDir, lastMoveDirection);
            
            if (directionChange > 0.9f)  // 方向一致（持续移动）
            {
                consistentMoveFrames++;
                
                if (consistentMoveFrames > 10)  // 持续10帧以上
                {
                    AddReward(0.005f);  // 奖励持续性
                }
            }
            else if (directionChange < 0.5f)  // 方向大幅改变
            {
                AddReward(-0.02f);  // 惩罚方向抖动
                consistentMoveFrames = 0;
                Debug.Log($"⚠️ AI 方向抖动，惩罚 -0.02");
            }
        }
        
        lastMoveDirection = currentMoveDir;
        
        // === 移除大部分密集奖励，减少噪音 ===
        // 不再使用过多细粒度的奖励，让 AI 聚焦于"朝向目标 → 靠近目标"
    }
    
    /// <summary>
    /// 🔥 奖励朝向与移动方向的一致性（修复倒退问题）
    /// </summary>
    private void RewardMovementConsistency()
    {
        if (nearestMedicine == null) return;
        
        Vector3 directionToMedicine = (nearestMedicine.position - transform.position).normalized;
        
        // 1. 检查朝向是否对准药剂
        float facingAlignment = Vector3.Dot(transform.forward, directionToMedicine);
        
        // 2. 检查移动方向是否朝向药剂
        if (characterController.velocity.magnitude < 0.5f) return;  // 速度太小，不评估
        
        Vector3 velocityDir = characterController.velocity.normalized;
        float velocityAlignment = Vector3.Dot(velocityDir, directionToMedicine);
        
        // 3. 移动一致性奖励（最终版）
        if (facingAlignment > 0.5f && velocityAlignment > 0.5f)
        {
            float speed = characterController.velocity.magnitude;
            float reward = 0.15f * (speed / moveSpeed);  // 最终平衡值
            AddReward(reward);
            Debug.Log($"✅ 朝向与移动一致，奖励 +{reward:F3}");
        }
        
        // 4. 惩罚"明确倒退"的行为
        else if (velocityAlignment < -0.5f)  // 移除朝向要求，只看是否倒退
        {
            // 明确远离药剂 = 惩罚
            AddReward(-0.15f);  // 降低惩罚（从-0.2到-0.15）
            Debug.Log($"❌ 远离药剂，惩罚 -0.15");
        }
        
        // 5. 移除横向移动惩罚 - 让AI有更多探索空间
    }
    
    /// <summary>
    /// 计数视野内的可见对象
    /// </summary>
    private int CountVisibleObjects(string tag, float range)
    {
        int count = 0;
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        
        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                float dist = Vector3.Distance(transform.position, obj.transform.position);
                if (dist <= range)
                {
                    // 简单的视野检查：在范围内即可
                    count++;
                }
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// 获取活动摄像机
    /// </summary>
    private Camera GetActiveCamera()
    {
        if (playerController != null)
        {
            return playerController.GetActiveCamera();
        }
        
        // Fallback: 查找第一人称相机
        Transform fpCam = transform.Find("FirstPersonCamera");
        if (fpCam != null)
        {
            return fpCam.GetComponent<Camera>();
        }
        
        return Camera.main;
    }

    /// <summary>
    /// 检查游戏状态
    /// </summary>
    private void CheckGameState()
    {
        if (GameManager.Instance == null) return;

        // 超时检测
        if (Time.time - episodeStartTime > maxEpisodeTime)
        {
            Debug.Log($"Episode 超时 ({maxEpisodeTime}s)，强制结束");
            AddReward(-1.0f);  // 超时惩罚
            EndEpisode();
            return;
        }

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
        // 训练模式下完全禁用 Heuristic
        // Heuristic 仅用于推理阶段的手动测试
        if (trainingMode)
        {
            // 返回零动作
            return;
        }
        
        // 人类控制模式（仅用于测试训练好的模型）
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
        if (!trainingMode) return;
        
        collectedMedicines++;
        AddReward(medicineReward);
        Debug.Log($"AI 收集药剂! 进度: {collectedMedicines}/{totalMedicines}, 获得奖励: {medicineReward}");
    }

    /// <summary>
    /// 受到伤害时调用
    /// </summary>
    public void OnDamageTaken(float damage)
    {
        if (!trainingMode) return;
        
        AddReward(damagePenalty);
        Debug.Log($"AI 受到伤害! 惩罚: {damagePenalty}");
    }

    /// <summary>
    /// 击杀敌人时调用
    /// </summary>
    public void OnEnemyKilled()
    {
        if (!trainingMode) return;
        
        AddReward(killEnemyReward);
        Debug.Log($"AI 击杀敌人! 奖励: {killEnemyReward}");
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!trainingMode) return;
        
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
        if (!trainingMode) return;
        
        if (collision.gameObject.CompareTag("DeathZone"))
        {
            AddReward(deathPenalty);
            EndEpisode();
        }
    }
}
