using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人AI系统 - 三状态AI：巡逻、警戒、追击
/// </summary>
public class EnemyAI : MonoBehaviour
{
    // AI状态枚举
    public enum EnemyState
    {
        Patrol,     // 巡逻
        Alert,      // 警戒
        Chase,      // 追击
        Attack      // 攻击
    }

    [Header("状态")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;

    [Header("检测设置")]
    [SerializeField] private float sightRange = 15f;       // 视觉范围
    [SerializeField] private float sightAngle = 120f;      // 视野角度
    [SerializeField] private float hearingRange = 10f;     // 听觉范围 (玩家奔跑时)
    [SerializeField] private float attackRange = 2f;       // 攻击范围
    [SerializeField] private LayerMask playerMask;         // 玩家层级
    [SerializeField] private LayerMask obstacleMask;       // 障碍物层级

    [Header("巡逻设置")]
    [SerializeField] private Transform[] patrolPoints;     // 巡逻点
    [SerializeField] private float patrolSpeed = 2f;       // 巡逻速度
    [SerializeField] private float patrolWaitTime = 2f;    // 巡逻等待时间

    [Header("追击设置")]
    [SerializeField] private float chaseSpeed = 5f;        // 追击速度
    [SerializeField] private float maxChaseTime = 10f;     // 最大追击时间
    [SerializeField] private float lostPlayerTime = 3f;    // 丢失玩家后继续追击时间

    [Header("警戒设置")]
    [SerializeField] private float alertDuration = 3f;     // 警戒持续时间
    [SerializeField] private float alertRotateSpeed = 90f; // 警戒时旋转速度

    [Header("视觉效果")]
    [SerializeField] private Renderer bodyRenderer;        // 身体渲染器
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color alertColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;

    [Header("音效")]
    [SerializeField] private AudioClip alertSound;         // 警戒音效
    [SerializeField] private AudioClip chaseSound;         // 发现玩家音效

    // 组件引用
    private NavMeshAgent agent;
    private AudioSource audioSource;
    private Transform player;
    private EnemyAttack enemyAttack;
    private EnemyHealth enemyHealth;

    // 状态变量
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private float alertTimer = 0f;
    private float chaseTimer = 0f;
    private float lostPlayerTimer = 0f;
    private Vector3 lastKnownPlayerPosition;
    private bool playerInSight = false;
    private bool isWaiting = false;

    // 公共属性
    public EnemyState CurrentState => currentState;
    public bool PlayerInSight => playerInSight;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

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

        // 设置初始状态
        SetState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        // 检测玩家
        DetectPlayer();

        // 根据状态执行行为
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolBehavior();
                break;
            case EnemyState.Alert:
                AlertBehavior();
                break;
            case EnemyState.Chase:
                ChaseBehavior();
                break;
            case EnemyState.Attack:
                AttackBehavior();
                break;
        }
    }

    /// <summary>
    /// 检测玩家
    /// </summary>
    private void DetectPlayer()
    {
        if (player == null) return;

        playerInSight = false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 视觉检测
        if (distanceToPlayer <= sightRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle < sightAngle / 2f)
            {
                // 射线检测是否有障碍物
                if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer, distanceToPlayer, obstacleMask))
                {
                    playerInSight = true;
                    lastKnownPlayerPosition = player.position;
                }
            }
        }

        // 听觉检测 (当玩家奔跑时)
        if (!playerInSight && distanceToPlayer <= hearingRange)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsSneaking)
            {
                // 听到玩家声音，进入警戒状态
                if (currentState == EnemyState.Patrol)
                {
                    lastKnownPlayerPosition = player.position;
                    SetState(EnemyState.Alert);
                }
            }
        }

        // 根据检测结果切换状态
        if (playerInSight)
        {
            if (distanceToPlayer <= attackRange)
            {
                SetState(EnemyState.Attack);
            }
            else
            {
                SetState(EnemyState.Chase);
            }
            lostPlayerTimer = 0f;
        }
        else if (currentState == EnemyState.Chase)
        {
            lostPlayerTimer += Time.deltaTime;
            if (lostPlayerTimer >= lostPlayerTime)
            {
                SetState(EnemyState.Alert);
            }
        }
    }

    /// <summary>
    /// 巡逻行为
    /// </summary>
    private void PatrolBehavior()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            // 没有巡逻点，原地待命
            return;
        }

        if (isWaiting)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                isWaiting = false;
                patrolWaitTimer = 0f;
                // 移动到下一个巡逻点
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            return;
        }

        // 移动到当前巡逻点
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        agent.SetDestination(targetPoint.position);

        // 检查是否到达巡逻点
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            isWaiting = true;
        }
    }

    /// <summary>
    /// 警戒行为
    /// </summary>
    private void AlertBehavior()
    {
        agent.speed = 0f;
        agent.SetDestination(transform.position);

        alertTimer += Time.deltaTime;

        // 转向声音来源
        Vector3 direction = (lastKnownPlayerPosition - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, alertRotateSpeed * Time.deltaTime);
        }

        // 警戒结束后返回巡逻
        if (alertTimer >= alertDuration)
        {
            alertTimer = 0f;
            SetState(EnemyState.Patrol);
        }
    }

    /// <summary>
    /// 追击行为
    /// </summary>
    private void ChaseBehavior()
    {
        agent.speed = chaseSpeed;

        if (playerInSight)
        {
            agent.SetDestination(player.position);
            chaseTimer = 0f;
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);
            chaseTimer += Time.deltaTime;
        }

        // 超过最大追击时间，返回巡逻
        if (chaseTimer >= maxChaseTime)
        {
            chaseTimer = 0f;
            SetState(EnemyState.Patrol);
        }

        // 检查攻击范围
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange && playerInSight)
            {
                SetState(EnemyState.Attack);
            }
        }
    }

    /// <summary>
    /// 攻击行为
    /// </summary>
    private void AttackBehavior()
    {
        agent.speed = 0f;
        agent.SetDestination(transform.position);

        // 面向玩家
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // 执行攻击
            if (enemyAttack != null)
            {
                enemyAttack.TryAttack();
            }

            // 检查玩家是否逃离攻击范围
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer > attackRange)
            {
                SetState(EnemyState.Chase);
            }
        }
    }

    /// <summary>
    /// 设置AI状态
    /// </summary>
    public void SetState(EnemyState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        // 根据状态更新颜色
        UpdateStateColor();

        // 播放状态音效
        switch (newState)
        {
            case EnemyState.Alert:
                PlaySound(alertSound);
                break;
            case EnemyState.Chase:
                PlaySound(chaseSound);
                break;
        }

        Debug.Log($"敌人状态变更: {newState}");
    }

    /// <summary>
    /// 更新状态颜色
    /// </summary>
    private void UpdateStateColor()
    {
        if (bodyRenderer == null) return;

        Color targetColor = patrolColor;
        switch (currentState)
        {
            case EnemyState.Alert:
                targetColor = alertColor;
                break;
            case EnemyState.Chase:
            case EnemyState.Attack:
                targetColor = chaseColor;
                break;
        }

        bodyRenderer.material.color = targetColor;
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
    /// 设置玩家引用（用于训练）
    /// </summary>
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    /// <summary>
    /// 重置状态（用于训练）
    /// </summary>
    public void ResetEnemy()
    {
        currentState = EnemyState.Patrol;
        currentPatrolIndex = 0;
        patrolWaitTimer = 0f;
        alertTimer = 0f;
        chaseTimer = 0f;
        lostPlayerTimer = 0f;
        playerInSight = false;
        isWaiting = false;
        UpdateStateColor();
        
        if (agent != null)
        {
            agent.ResetPath();
        }
    }

    /// <summary>
    /// 可视化检测范围（编辑器）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 视觉范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 听觉范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // 视野角度
        Gizmos.color = Color.green;
        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle / 2f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBoundary * sightRange);
        Gizmos.DrawRay(transform.position, rightBoundary * sightRange);
    }
}
