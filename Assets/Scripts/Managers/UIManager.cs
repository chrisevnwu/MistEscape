using UnityEngine;

/// <summary>
/// UI管理器 - 使用 OnGUI 显示游戏信息
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI 设置")]
    [SerializeField] private GUISkin customSkin;           // 自定义皮肤

    [Header("颜色设置")]
    [SerializeField] private Color healthBarColor = Color.green;
    [SerializeField] private Color healthBarLowColor = Color.red;
    [SerializeField] private Color ammoColor = Color.yellow;
    [SerializeField] private Color medicineColor = Color.cyan;

    // 当前显示值
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private int currentAmmo = 30;
    private int maxAmmo = 30;
    private int collectedMedicines = 0;
    private int totalMedicines = 8;

    // UI 尺寸
    private float healthBarWidth = 200f;
    private float healthBarHeight = 20f;
    private float padding = 10f;

    // 游戏状态
    private bool showGameOver = false;
    private bool isGameWon = false;
    private bool showPauseMenu = false;

    private void Start()
    {
        // 订阅事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHealthChanged += UpdateHealth;
            GameManager.Instance.OnAmmoChanged += UpdateAmmo;
            GameManager.Instance.OnMedicineCollected += UpdateMedicine;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            
            // 获取初始值
            maxHealth = GameManager.Instance.MaxPlayerHealth;
            maxAmmo = GameManager.Instance.MaxAmmo;
            totalMedicines = GameManager.Instance.TotalMedicines;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHealthChanged -= UpdateHealth;
            GameManager.Instance.OnAmmoChanged -= UpdateAmmo;
            GameManager.Instance.OnMedicineCollected -= UpdateMedicine;
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    private void Update()
    {
        // 检查暂停状态
        if (GameManager.Instance != null)
        {
            showPauseMenu = GameManager.Instance.IsPaused;
        }
    }

    /// <summary>
    /// 更新生命值显示
    /// </summary>
    private void UpdateHealth(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
    }

    /// <summary>
    /// 更新弹药显示
    /// </summary>
    private void UpdateAmmo(int current, int max)
    {
        currentAmmo = current;
        maxAmmo = max;
    }

    /// <summary>
    /// 更新药剂收集显示
    /// </summary>
    private void UpdateMedicine(int current, int total)
    {
        collectedMedicines = current;
        totalMedicines = total;
    }

    /// <summary>
    /// 游戏状态变化
    /// </summary>
    private void OnGameStateChanged(bool isOver, bool won)
    {
        showGameOver = isOver;
        isGameWon = won;
    }

    private void OnGUI()
    {
        if (customSkin != null)
        {
            GUI.skin = customSkin;
        }

        // 左上角信息面板
        DrawInfoPanel();

        // 准星（屏幕中央）
        DrawCrosshair();

        // 暂停菜单
        if (showPauseMenu)
        {
            DrawPauseMenu();
        }

        // 游戏结束画面
        if (showGameOver)
        {
            DrawGameOverScreen();
        }
    }

    /// <summary>
    /// 绘制信息面板
    /// </summary>
    private void DrawInfoPanel()
    {
        float startX = padding;
        float startY = padding;
        float lineHeight = 30f;

        // 背景
        GUI.Box(new Rect(startX - 5, startY - 5, healthBarWidth + 40, lineHeight * 4 + 20), "");

        // 生命值标签
        GUI.Label(new Rect(startX, startY, 100, lineHeight), "生命值:");
        
        // 生命值条背景
        GUI.color = Color.gray;
        GUI.Box(new Rect(startX + 60, startY + 3, healthBarWidth, healthBarHeight), "");

        // 生命值条
        float healthPercent = currentHealth / maxHealth;
        GUI.color = healthPercent > 0.3f ? healthBarColor : healthBarLowColor;
        GUI.Box(new Rect(startX + 60, startY + 3, healthBarWidth * healthPercent, healthBarHeight), "");
        
        // 生命值数值
        GUI.color = Color.white;
        GUI.Label(new Rect(startX + 65, startY + 3, healthBarWidth, healthBarHeight), 
            $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}");

        // 弹药
        startY += lineHeight;
        GUI.color = ammoColor;
        GUI.Label(new Rect(startX, startY, 200, lineHeight), $"弹药: {currentAmmo} / {maxAmmo}");

        // 药剂收集
        startY += lineHeight;
        GUI.color = medicineColor;
        GUI.Label(new Rect(startX, startY, 200, lineHeight), $"药剂: {collectedMedicines} / {totalMedicines}");

        // 操作提示
        startY += lineHeight;
        GUI.color = Color.white;
        GUI.Label(new Rect(startX, startY, 200, lineHeight), "V: 切换视角 | ESC: 暂停");

        // 重置颜色
        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制准星
    /// </summary>
    private void DrawCrosshair()
    {
        float size = 8f;       // 准星线条长度（减小）
        float thickness = 2f;  // 准星线条粗细
        float gap = 4f;        // 中心间隙
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        GUI.color = Color.white;

        // 左线
        GUI.Box(new Rect(centerX - size - gap, centerY - thickness / 2, size, thickness), "");
        // 右线
        GUI.Box(new Rect(centerX + gap, centerY - thickness / 2, size, thickness), "");
        // 上线
        GUI.Box(new Rect(centerX - thickness / 2, centerY - size - gap, thickness, size), "");
        // 下线
        GUI.Box(new Rect(centerX - thickness / 2, centerY + gap, thickness, size), "");

        GUI.color = Color.white;
    }

    /// <summary>
    /// 绘制暂停菜单
    /// </summary>
    private void DrawPauseMenu()
    {
        float menuWidth = 300f;
        float menuHeight = 200f;
        float menuX = (Screen.width - menuWidth) / 2f;
        float menuY = (Screen.height - menuHeight) / 2f;
        float buttonWidth = 200f;
        float buttonHeight = 40f;

        // 半透明背景
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.color = Color.white;

        // 菜单框
        GUI.Box(new Rect(menuX, menuY, menuWidth, menuHeight), "游戏暂停");

        // 继续按钮
        if (GUI.Button(new Rect(menuX + (menuWidth - buttonWidth) / 2, menuY + 50, buttonWidth, buttonHeight), "继续游戏"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        // 重新开始按钮
        if (GUI.Button(new Rect(menuX + (menuWidth - buttonWidth) / 2, menuY + 100, buttonWidth, buttonHeight), "重新开始"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        // 退出按钮
        if (GUI.Button(new Rect(menuX + (menuWidth - buttonWidth) / 2, menuY + 150, buttonWidth, buttonHeight), "退出游戏"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }
    }

    /// <summary>
    /// 绘制游戏结束画面
    /// </summary>
    private void DrawGameOverScreen()
    {
        float menuWidth = 400f;
        float menuHeight = 250f;
        float menuX = (Screen.width - menuWidth) / 2f;
        float menuY = (Screen.height - menuHeight) / 2f;
        float buttonWidth = 200f;
        float buttonHeight = 40f;

        // 半透明背景
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
        GUI.color = Color.white;

        // 标题
        string title = isGameWon ? "任务完成！" : "任务失败";
        GUI.color = isGameWon ? Color.green : Color.red;
        
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 48;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        
        GUI.Label(new Rect(menuX, menuY, menuWidth, 80), title, titleStyle);
        GUI.color = Color.white;

        // 描述
        string description = isGameWon 
            ? $"你成功收集了所有 {totalMedicines} 个药剂！\n家人有救了！" 
            : "你被敌人击败了...\n再试一次吧！";
        
        GUIStyle descStyle = new GUIStyle(GUI.skin.label);
        descStyle.fontSize = 18;
        descStyle.alignment = TextAnchor.MiddleCenter;
        
        GUI.Label(new Rect(menuX, menuY + 80, menuWidth, 60), description, descStyle);

        // 重新开始按钮
        if (GUI.Button(new Rect(menuX + (menuWidth - buttonWidth) / 2, menuY + 160, buttonWidth, buttonHeight), "重新开始"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        // 退出按钮
        if (GUI.Button(new Rect(menuX + (menuWidth - buttonWidth) / 2, menuY + 210, buttonWidth, buttonHeight), "退出游戏"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }
    }
}
