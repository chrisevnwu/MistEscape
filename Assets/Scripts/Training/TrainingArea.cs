using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 训练区域管理器
/// 管理单个独立的训练环境，支持多环境并行训练
/// </summary>
public class TrainingArea : MonoBehaviour
{
    [Header("预制件")]
    [SerializeField] private GameObject medicinePrefab;
    [SerializeField] private GameObject enemyPrefab;

    [Header("生成设置")]
    [SerializeField] private int medicineCount = 8;
    [SerializeField] private int enemyCount = 4;
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float spawnHeight = 0.5f;

    [Header("引用（自动查找）")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] medicineSpawnPoints;
    [SerializeField] private Transform[] enemySpawnPoints;

    // 动态生成的对象
    private List<GameObject> spawnedMedicines = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    
    // 区域边界
    private Vector3 areaCenter;
    private PlayerAgent playerAgent;

    private void Awake()
    {
        areaCenter = transform.position;
        playerAgent = GetComponentInChildren<PlayerAgent>();
        
        // 如果没有指定玩家生成点，使用区域中心
        if (playerSpawnPoint == null)
        {
            GameObject spawnObj = new GameObject("PlayerSpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = Vector3.zero;
            playerSpawnPoint = spawnObj.transform;
        }
    }

    /// <summary>
    /// 重置训练区域
    /// </summary>
    public void ResetArea()
    {
        // 清理旧对象
        ClearSpawnedObjects();
        
        // 重新生成
        SpawnMedicines();
        SpawnEnemies();
    }

    /// <summary>
    /// 清理生成的对象
    /// </summary>
    private void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedMedicines)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedMedicines.Clear();

        foreach (var obj in spawnedEnemies)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedEnemies.Clear();
    }

    /// <summary>
    /// 生成药剂
    /// </summary>
    private void SpawnMedicines()
    {
        if (medicinePrefab == null) return;

        for (int i = 0; i < medicineCount; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(medicineSpawnPoints, i);
            GameObject medicine = Instantiate(medicinePrefab, spawnPos, Quaternion.identity, transform);
            medicine.name = $"Medicine_{i}";
            spawnedMedicines.Add(medicine);
        }
    }

    /// <summary>
    /// 生成敌人
    /// </summary>
    private void SpawnEnemies()
    {
        if (enemyPrefab == null) return;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(enemySpawnPoints, i);
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
            enemy.name = $"Enemy_{i}";
            spawnedEnemies.Add(enemy);

            // 设置敌人目标为本区域的玩家
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null && playerAgent != null)
            {
                enemyAI.SetPlayer(playerAgent.transform);
            }
        }
    }

    /// <summary>
    /// 获取生成位置
    /// </summary>
    private Vector3 GetSpawnPosition(Transform[] spawnPoints, int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int pointIndex = index % spawnPoints.Length;
            return spawnPoints[pointIndex].position;
        }
        
        // 随机位置
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return areaCenter + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
    }

    /// <summary>
    /// 获取玩家生成位置
    /// </summary>
    public Vector3 GetPlayerSpawnPosition()
    {
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.position;
        }
        return areaCenter + Vector3.up;
    }

    /// <summary>
    /// 获取玩家生成旋转
    /// </summary>
    public Quaternion GetPlayerSpawnRotation()
    {
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.rotation;
        }
        return Quaternion.identity;
    }

    /// <summary>
    /// 获取区域内所有药剂
    /// </summary>
    public List<Transform> GetMedicines()
    {
        List<Transform> medicines = new List<Transform>();
        foreach (var obj in spawnedMedicines)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                medicines.Add(obj.transform);
            }
        }
        return medicines;
    }

    /// <summary>
    /// 获取区域内所有敌人
    /// </summary>
    public List<Transform> GetEnemies()
    {
        List<Transform> enemies = new List<Transform>();
        foreach (var obj in spawnedEnemies)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                enemies.Add(obj.transform);
            }
        }
        return enemies;
    }

    /// <summary>
    /// 获取已收集的药剂数量
    /// </summary>
    public int GetCollectedMedicineCount()
    {
        int remaining = 0;
        foreach (var obj in spawnedMedicines)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                remaining++;
            }
        }
        return medicineCount - remaining;
    }

    /// <summary>
    /// 检查是否收集完所有药剂
    /// </summary>
    public bool AllMedicinesCollected()
    {
        foreach (var obj in spawnedMedicines)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                return false;
            }
        }
        return true;
    }
}
