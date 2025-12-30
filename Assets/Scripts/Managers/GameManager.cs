using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器 - 单例模式
/// 负责管理游戏全局状态、玩家数据、胜负判断
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏设置")]
    [SerializeField] private int totalMedicines = 8;      // 总共需要收集的药剂数量
    [SerializeField] private int maxEnemies = 8;          // 最大敌人数量
    [SerializeField] private float enemyRespawnTime = 10f; // 敌人重生时间

    [Header("玩家数据")]
    private int collectedMedicines = 0;                   // 已收集的药剂数量
    private int currentAmmo = 30;                         // 当前弹药
    private int maxAmmo = 30;                             // 最大弹药
    private float playerHealth = 100f;                    // 玩家生命值
    private float maxPlayerHealth = 100f;                 // 最大生命值

    [Header("敌人管理")]
    private int currentEnemyCount = 0;                    // 当前敌人数量
    [SerializeField] private GameObject enemyPrefab;      // 敌人预制件
    [SerializeField] private Transform[] enemySpawnPoints; // 敌人生成点

    [Header("游戏状态")]
    private bool isGameOver = false;
    private bool isGameWon = false;
    private bool isPaused = false;

    // 属性访问器
    public int CollectedMedicines
    {
        get { return collectedMedicines; }
        private set
        {
            collectedMedicines = value;
            OnMedicineCollected?.Invoke(collectedMedicines, totalMedicines);
            CheckWinCondition();
        }
    }

    public int CurrentAmmo
    {
        get { return currentAmmo; }
        private set
        {
            currentAmmo = Mathf.Clamp(value, 0, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }
    }

    public float PlayerHealth
    {
        get { return playerHealth; }
        private set
        {
            playerHealth = Mathf.Clamp(value, 0, maxPlayerHealth);
            OnHealthChanged?.Invoke(playerHealth, maxPlayerHealth);
            if (playerHealth <= 0)
            {
                GameOver();
            }
        }
    }

    public bool IsGameOver => isGameOver;
    public bool IsGameWon => isGameWon;
    public bool IsPaused => isPaused;
    public int TotalMedicines => totalMedicines;
    public int MaxAmmo => maxAmmo;
    public float MaxPlayerHealth => maxPlayerHealth;

    // 事件
    public delegate void MedicineCollectedHandler(int current, int total);
    public event MedicineCollectedHandler OnMedicineCollected;

    public delegate void AmmoChangedHandler(int current, int max);
    public event AmmoChangedHandler OnAmmoChanged;

    public delegate void HealthChangedHandler(float current, float max);
    public event HealthChangedHandler OnHealthChanged;

    public delegate void GameStateChangedHandler(bool isOver, bool isWon);
    public event GameStateChangedHandler OnGameStateChanged;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeGame();
        SpawnInitialEnemies();
    }

    private void Update()
    {
        // ESC 暂停游戏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// 初始化游戏
    /// </summary>
    public void InitializeGame()
    {
        collectedMedicines = 0;
        currentAmmo = maxAmmo;
        playerHealth = maxPlayerHealth;
        isGameOver = false;
        isGameWon = false;
        isPaused = false;
        Time.timeScale = 1f;

        // 动态检测地图上的药剂数量
        UpdateTotalMedicineCount();

        // 触发初始事件
        OnMedicineCollected?.Invoke(collectedMedicines, totalMedicines);
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        OnHealthChanged?.Invoke(playerHealth, maxPlayerHealth);
    }

    /// <summary>
    /// 动态更新地图上的药剂总数
    /// </summary>
    public void UpdateTotalMedicineCount()
    {
        GameObject[] medicines = GameObject.FindGameObjectsWithTag("Medicine");
        if (medicines.Length > 0)
        {
            totalMedicines = medicines.Length;
            Debug.Log($"检测到地图上有 {totalMedicines} 个药剂");
        }
        else
        {
            Debug.LogWarning("未检测到任何药剂！保持默认值: " + totalMedicines);
        }
    }

    /// <summary>
    /// 初始生成敌人
    /// </summary>
    private void SpawnInitialEnemies()
    {
        if (enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("敌人生成未配置：缺少预制件或生成点。请在 GameManager 的 Inspector 中配置 Enemy Prefab 和 Enemy Spawn Points。");
            return;
        }

        int enemiesToSpawn = Mathf.Min(maxEnemies, enemySpawnPoints.Length);
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = enemySpawnPoints[i % enemySpawnPoints.Length];
            Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            currentEnemyCount++;
        }
        Debug.Log($"初始生成 {enemiesToSpawn} 个敌人");
    }

    /// <summary>
    /// 收集药剂
    /// </summary>
    public void CollectMedicine()
    {
        if (!isGameOver)
        {
            CollectedMedicines++;
            Debug.Log($"收集药剂: {collectedMedicines}/{totalMedicines}");
            
            // 胜利接近提示
            int remaining = totalMedicines - collectedMedicines;
            if (remaining > 0 && remaining <= 3)
            {
                Debug.Log($"🎯 即将胜利！还剩 {remaining} 个药剂！");
            }
        }
    }

    /// <summary>
    /// 添加弹药
    /// </summary>
    public void AddAmmo(int amount)
    {
        CurrentAmmo += amount;
        Debug.Log($"获得弹药: +{amount}, 当前: {currentAmmo}/{maxAmmo}");
    }

    /// <summary>
    /// 消耗弹药
    /// </summary>
    public bool UseAmmo()
    {
        if (currentAmmo > 0)
        {
            CurrentAmmo--;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 玩家受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!isGameOver)
        {
            PlayerHealth -= damage;
            Debug.Log($"受到伤害: -{damage}, 当前生命值: {playerHealth}/{maxPlayerHealth}");
        }
    }

    /// <summary>
    /// 恢复生命值
    /// </summary>
    public void Heal(float amount)
    {
        PlayerHealth += amount;
    }

    /// <summary>
    /// 敌人死亡
    /// </summary>
    public void OnEnemyDeath(Vector3 deathPosition)
    {
        currentEnemyCount--;
        Debug.Log($"敌人死亡，当前敌人数量: {currentEnemyCount}");
        
        // 延迟重生敌人
        Invoke(nameof(RespawnEnemy), enemyRespawnTime);
    }

    /// <summary>
    /// 重生敌人
    /// </summary>
    private void RespawnEnemy()
    {
        if (isGameOver || enemyPrefab == null || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            return;

        if (currentEnemyCount < maxEnemies)
        {
            int randomIndex = Random.Range(0, enemySpawnPoints.Length);
            Transform spawnPoint = enemySpawnPoints[randomIndex];
            Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            currentEnemyCount++;
            Debug.Log($"敌人重生，当前敌人数量: {currentEnemyCount}");
        }
    }

    /// <summary>
    /// 检查胜利条件
    /// </summary>
    private void CheckWinCondition()
    {
        if (collectedMedicines >= totalMedicines)
        {
            GameWon();
        }
    }

    /// <summary>
    /// 游戏胜利
    /// </summary>
    private void GameWon()
    {
        if (!isGameOver)
        {
            isGameOver = true;
            isGameWon = true;
            
            Debug.Log("=================================================");
            Debug.Log($"🎉🎉🎉 游戏胜利！已收集全部 {totalMedicines} 个药剂！🎉🎉🎉");
            Debug.Log("=================================================");
            
            // 慢动作效果
            Time.timeScale = 0.3f;
            
            // 显示鼠标光标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // 触发事件
            OnGameStateChanged?.Invoke(true, true);
        }
    }

    /// <summary>
    /// 游戏失败
    /// </summary>
    private void GameOver()
    {
        if (!isGameOver)
        {
            isGameOver = true;
            isGameWon = false;
            Debug.Log("=== 游戏失败！ ===");
            OnGameStateChanged?.Invoke(true, false);
        }
    }

    /// <summary>
    /// 切换暂停状态
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        
        // 控制鼠标光标
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
