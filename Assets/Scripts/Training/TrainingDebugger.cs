using UnityEngine;

/// <summary>
/// AI训练调试工具
/// 在Scene视图中可视化AI的观察和决策
/// </summary>
public class TrainingDebugger : MonoBehaviour
{
    [Header("可视化设置")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showRewardLogs = false;
    
    private PlayerAgent agent;
    private Transform nearestMedicine;
    private Transform nearestEnemy;
    
    private void Awake()
    {
        agent = GetComponent<PlayerAgent>();
    }
    
    private void Update()
    {
        if (agent == null || !agent.IsTrainingMode) return;
        
        // 更新最近的目标引用
        UpdateNearestTargets();
    }
    
    private void UpdateNearestTargets()
    {
        // 查找最近的药剂
        nearestMedicine = null;
        float minMedicineDist = float.MaxValue;
        GameObject[] medicines = GameObject.FindGameObjectsWithTag("Medicine");
        foreach (GameObject med in medicines)
        {
            if (med != null && med.activeInHierarchy)
            {
                float dist = Vector3.Distance(transform.position, med.transform.position);
                if (dist < minMedicineDist)
                {
                    minMedicineDist = dist;
                    nearestMedicine = med.transform;
                }
            }
        }
        
        // 查找最近的敌人
        nearestEnemy = null;
        float minEnemyDist = float.MaxValue;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                if (health != null && !health.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < minEnemyDist)
                    {
                        minEnemyDist = dist;
                        nearestEnemy = enemy.transform;
                    }
                }
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || agent == null || !agent.IsTrainingMode) return;
        
        // 1. 到最近药剂的射线（绿色）
        if (nearestMedicine != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + Vector3.up, nearestMedicine.position + Vector3.up * 0.5f);
            
            // 绘制距离文本（通过球体大小表示距离）
            float dist = Vector3.Distance(transform.position, nearestMedicine.position);
            Gizmos.DrawWireSphere(nearestMedicine.position + Vector3.up * 2f, 0.5f);
            
            // 朝向指示
            Vector3 dirToMedicine = (nearestMedicine.position - transform.position).normalized;
            float alignment = Vector3.Dot(transform.forward, dirToMedicine);
            if (alignment > 0.7f)
            {
                // 朝向药剂，绘制粗线
                Gizmos.color = Color.cyan;
                DrawThickLine(transform.position + Vector3.up, nearestMedicine.position + Vector3.up * 0.5f, 0.3f);
            }
        }
        
        // 2. 到最近敌人的射线（红色）
        if (nearestEnemy != null)
        {
            float enemyDist = Vector3.Distance(transform.position, nearestEnemy.position);
            
            // 根据距离改变颜色
            if (enemyDist < 5f)
                Gizmos.color = Color.red;  // 危险距离
            else if (enemyDist >= 8f && enemyDist <= 15f)
                Gizmos.color = Color.yellow;  // 理想距离
            else
                Gizmos.color = new Color(1f, 0.5f, 0f);  // 橙色
            
            Gizmos.DrawLine(transform.position + Vector3.up, nearestEnemy.position + Vector3.up);
            Gizmos.DrawWireSphere(nearestEnemy.position + Vector3.up * 2.5f, 0.5f);
        }
        
        // 3. 视野范围（30米）
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.2f);
        DrawWireArc(transform.position, transform.forward, 180f, 30f);
        
        // 4. 前向指示器
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 5f);
    }
    
    /// <summary>
    /// 绘制粗线
    /// </summary>
    private void DrawThickLine(Vector3 start, Vector3 end, float thickness)
    {
        Vector3 offset = Vector3.Cross((end - start).normalized, Vector3.up) * thickness * 0.5f;
        Gizmos.DrawLine(start + offset, end + offset);
        Gizmos.DrawLine(start - offset, end - offset);
    }
    
    /// <summary>
    /// 绘制扇形视野
    /// </summary>
    private void DrawWireArc(Vector3 position, Vector3 forward, float angle, float radius)
    {
        int segments = 20;
        float angleStep = angle / segments;
        Vector3 prevPoint = Quaternion.Euler(0, -angle / 2f, 0) * forward * radius + position;
        
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle / 2f + angleStep * i;
            Vector3 nextPoint = Quaternion.Euler(0, currentAngle, 0) * forward * radius + position;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        
        // 绘制边界线
        Vector3 leftPoint = Quaternion.Euler(0, -angle / 2f, 0) * forward * radius + position;
        Vector3 rightPoint = Quaternion.Euler(0, angle / 2f, 0) * forward * radius + position;
        Gizmos.DrawLine(position, leftPoint);
        Gizmos.DrawLine(position, rightPoint);
    }
    
    /// <summary>
    /// 记录奖励信息
    /// </summary>
    public void LogReward(string rewardType, float value)
    {
        if (showRewardLogs)
        {
            Debug.Log($"[AI奖励] {rewardType}: {value:F4}");
        }
    }
}
