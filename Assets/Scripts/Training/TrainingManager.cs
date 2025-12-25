using UnityEngine;
using System.Linq;

/// <summary>
/// 训练环境管理器
/// 用于在训练过程中重置和管理环境
/// </summary>
public class TrainingManager : MonoBehaviour
{
    [Header("预制件引用")]
    [SerializeField] private GameObject medicinePrefab;    // 药剂预制件
    [SerializeField] private GameObject enemyPrefab;       // 敌人预制件
    [SerializeField] private GameObject ammoPrefab;        // 弹药预制件

    [Header("生成点")]
    [SerializeField] private Transform[] medicineSpawnPoints;  // 药剂生成点
    [SerializeField] private Transform[] enemySpawnPoints;     // 敌人生成点
    [SerializeField] private Transform[] ammoSpawnPoints;      // 弹药生成点

    [Header("训练设置")]
    [SerializeField] private int medicineCount = 8;        // 药剂数量
    [SerializeField] private int enemyCount = 8;           // 敌人数量
    [SerializeField] private int ammoCount = 3;            // 弹药数量
    [SerializeField] private bool randomizeSpawns = true;  // 随机化生成位置

    [Header("Agent 引用")]
    [SerializeField] private PlayerAgent playerAgent;

    // 动态生成的对象列表
    private GameObject[] spawnedMedicines;
    private GameObject[] spawnedEnemies;
    private GameObject[] spawnedAmmo;

    private void Awake()
    {
        if (playerAgent == null)
        {
            playerAgent = FindObjectOfType<PlayerAgent>();
        }
    }

    private void Start()
    {
        // 初始化环境
        InitializeEnvironment();
    }

    /// <summary>
    /// 初始化环境
    /// </summary>
    public void InitializeEnvironment()
    {
        // 清理旧的对象
        ClearEnvironment();

        // 生成新的对象
        SpawnMedicines();
        SpawnEnemies();
        SpawnAmmo();
    }

    /// <summary>
    /// 清理环境
    /// </summary>
    public void ClearEnvironment()
    {
        // 清理药剂
        if (spawnedMedicines != null)
        {
            foreach (var obj in spawnedMedicines)
            {
                if (obj != null) Destroy(obj);
            }
        }

        // 清理敌人
        if (spawnedEnemies != null)
        {
            foreach (var obj in spawnedEnemies)
            {
                if (obj != null) Destroy(obj);
            }
        }

        // 清理弹药
        if (spawnedAmmo != null)
        {
            foreach (var obj in spawnedAmmo)
            {
                if (obj != null) Destroy(obj);
            }
        }

        // 也清理场景中可能存在的对象
        foreach (var obj in GameObject.FindGameObjectsWithTag("Medicine"))
        {
            Destroy(obj);
        }

        foreach (var obj in GameObject.FindGameObjectsWithTag("Ammo"))
        {
            Destroy(obj);
        }
    }

    /// <summary>
    /// 生成药剂
    /// </summary>
    private void SpawnMedicines()
    {
        if (medicinePrefab == null) return;

        spawnedMedicines = new GameObject[medicineCount];
        
        for (int i = 0; i < medicineCount; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(medicineSpawnPoints, i, randomizeSpawns);
            spawnedMedicines[i] = Instantiate(medicinePrefab, spawnPos, Quaternion.identity);
            spawnedMedicines[i].name = $"Medicine_{i}";
        }
    }

    /// <summary>
    /// 生成敌人
    /// </summary>
    private void SpawnEnemies()
    {
        if (enemyPrefab == null) return;

        spawnedEnemies = new GameObject[enemyCount];
        
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(enemySpawnPoints, i, randomizeSpawns);
            spawnedEnemies[i] = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawnedEnemies[i].name = $"Enemy_{i}";

            // 设置敌人的玩家引用
            EnemyAI enemyAI = spawnedEnemies[i].GetComponent<EnemyAI>();
            if (enemyAI != null && playerAgent != null)
            {
                enemyAI.SetPlayer(playerAgent.transform);
            }
        }
    }

    /// <summary>
    /// 生成弹药
    /// </summary>
    private void SpawnAmmo()
    {
        if (ammoPrefab == null) return;

        spawnedAmmo = new GameObject[ammoCount];
        
        for (int i = 0; i < ammoCount; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(ammoSpawnPoints, i, randomizeSpawns);
            spawnedAmmo[i] = Instantiate(ammoPrefab, spawnPos, Quaternion.identity);
            spawnedAmmo[i].name = $"Ammo_{i}";
        }
    }

    /// <summary>
    /// 获取生成位置
    /// </summary>
    private Vector3 GetSpawnPosition(Transform[] spawnPoints, int index, bool randomize)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // 没有预设生成点，使用随机位置
            return new Vector3(
                Random.Range(-40f, 40f),
                0.5f,
                Random.Range(-40f, 40f)
            );
        }

        if (randomize)
        {
            // 随机选择一个生成点
            int randomIndex = Random.Range(0, spawnPoints.Length);
            return spawnPoints[randomIndex].position;
        }
        else
        {
            // 按顺序使用生成点
            int pointIndex = index % spawnPoints.Length;
            return spawnPoints[pointIndex].position;
        }
    }

    /// <summary>
    /// 重置单个敌人
    /// </summary>
    public void ResetEnemy(int index)
    {
        if (index < 0 || index >= spawnedEnemies.Length) return;
        
        if (spawnedEnemies[index] != null)
        {
            Destroy(spawnedEnemies[index]);
        }

        Vector3 spawnPos = GetSpawnPosition(enemySpawnPoints, index, randomizeSpawns);
        spawnedEnemies[index] = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        spawnedEnemies[index].name = $"Enemy_{index}";

        EnemyAI enemyAI = spawnedEnemies[index].GetComponent<EnemyAI>();
        if (enemyAI != null && playerAgent != null)
        {
            enemyAI.SetPlayer(playerAgent.transform);
        }
    }

    /// <summary>
    /// 获取所有药剂的 Transform
    /// </summary>
    public Transform[] GetMedicineTransforms()
    {
        if (spawnedMedicines == null) return new Transform[0];
        
        return System.Array.FindAll(spawnedMedicines, m => m != null)
            .Select(m => m.transform)
            .ToArray();
    }

    /// <summary>
    /// 获取所有敌人的 Transform
    /// </summary>
    public Transform[] GetEnemyTransforms()
    {
        if (spawnedEnemies == null) return new Transform[0];
        
        return System.Array.FindAll(spawnedEnemies, e => e != null)
            .Select(e => e.transform)
            .ToArray();
    }
}
