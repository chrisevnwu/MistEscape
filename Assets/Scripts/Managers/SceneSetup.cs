using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 场景设置工具 - 用于自动创建游戏地图
/// 在编辑器菜单中使用: Tools -> 创建游戏地图
/// </summary>
public class SceneSetup : MonoBehaviour
{
    [Header("地图尺寸")]
    public float mapWidth = 100f;
    public float mapHeight = 100f;
    public float wallHeight = 4f;
    public float wallThickness = 1f;

    [Header("材质")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material obstacleMaterial;

    [Header("预制件存储路径")]
    public string prefabPath = "Assets/Prefabs/";

#if UNITY_EDITOR
    [MenuItem("Tools/创建游戏地图")]
    public static void CreateGameMap()
    {
        // 首先确保所有需要的 Tags 存在
        EnsureTagsExist();
        
        // 创建父对象
        GameObject mapParent = new GameObject("GameMap");
        
        SceneSetup setup = mapParent.AddComponent<SceneSetup>();
        setup.GenerateMap();
        
        Debug.Log("游戏地图创建完成!");
    }

    [MenuItem("Tools/创建玩家")]
    public static void CreatePlayer()
    {
        EnsureTagsExist();
        CreatePlayerPrefab();
    }

    [MenuItem("Tools/创建敌人预制件")]
    public static void CreateEnemyPrefab()
    {
        EnsureTagsExist();
        CreateEnemy();
    }

    [MenuItem("Tools/创建药剂预制件")]
    public static void CreateMedicinePrefab()
    {
        EnsureTagsExist();
        CreateMedicine();
    }

    [MenuItem("Tools/创建子弹预制件")]
    public static void CreateBulletPrefab()
    {
        EnsureTagsExist();
        CreateBullet();
    }

    /// <summary>
    /// 确保所有需要的 Tags 存在
    /// </summary>
    private static void EnsureTagsExist()
    {
        string[] requiredTags = { "Wall", "Ground", "Obstacle", "Player", "Enemy", "Medicine", "Ammo", "Bullet", "EnemyAttack", "DeathZone" };
        
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        foreach (string tag in requiredTags)
        {
            bool found = false;
            
            // 检查内置 tags
            for (int i = 0; i < UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.tags[i].Equals(tag))
                {
                    found = true;
                    break;
                }
            }
            
            // 如果不存在，添加新 tag
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = tag;
                Debug.Log($"创建 Tag: {tag}");
            }
        }
        
        tagManager.ApplyModifiedProperties();
    }
#endif

    /// <summary>
    /// 生成完整地图
    /// </summary>
    public void GenerateMap()
    {
        // 创建材质
        CreateMaterials();

        // 创建地板
        CreateFloor();

        // 创建外墙
        CreateOuterWalls();

        // 创建内部结构
        CreateInnerStructure();

        // 创建光照
        CreateLighting();

        // 创建 GameManager
        CreateGameManager();
    }

    /// <summary>
    /// 创建材质并保存为资源文件
    /// </summary>
    private void CreateMaterials()
    {
#if UNITY_EDITOR
        // 确保 Materials 文件夹存在
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // 地板材质
        if (floorMaterial == null)
        {
            string floorMatPath = "Assets/Materials/FloorMaterial.mat";
            floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            if (floorMaterial == null)
            {
                floorMaterial = new Material(Shader.Find("Standard"));
                floorMaterial.color = new Color(0.3f, 0.3f, 0.35f);
                AssetDatabase.CreateAsset(floorMaterial, floorMatPath);
            }
        }

        // 墙体材质
        if (wallMaterial == null)
        {
            string wallMatPath = "Assets/Materials/WallMaterial.mat";
            wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
            if (wallMaterial == null)
            {
                wallMaterial = new Material(Shader.Find("Standard"));
                wallMaterial.color = new Color(0.5f, 0.5f, 0.55f);
                AssetDatabase.CreateAsset(wallMaterial, wallMatPath);
            }
        }

        // 障碍物材质
        if (obstacleMaterial == null)
        {
            string obstacleMatPath = "Assets/Materials/ObstacleMaterial.mat";
            obstacleMaterial = AssetDatabase.LoadAssetAtPath<Material>(obstacleMatPath);
            if (obstacleMaterial == null)
            {
                obstacleMaterial = new Material(Shader.Find("Standard"));
                obstacleMaterial.color = new Color(0.4f, 0.35f, 0.3f);
                AssetDatabase.CreateAsset(obstacleMaterial, obstacleMatPath);
            }
        }

        AssetDatabase.SaveAssets();
#endif
    }

    /// <summary>
    /// 创建地板
    /// </summary>
    private void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(transform);
        floor.transform.localScale = new Vector3(mapWidth / 10f, 1f, mapHeight / 10f);
        floor.transform.position = Vector3.zero;
        
        if (floorMaterial != null)
        {
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        }

        // 设置为导航静态
        floor.isStatic = true;
        floor.tag = "Ground";
        floor.layer = LayerMask.NameToLayer("Default");
    }

    /// <summary>
    /// 创建外墙
    /// </summary>
    private void CreateOuterWalls()
    {
        GameObject wallsParent = new GameObject("OuterWalls");
        wallsParent.transform.SetParent(transform);

        // 北墙
        CreateWall("North Wall", new Vector3(0, wallHeight / 2, mapHeight / 2), 
            new Vector3(mapWidth, wallHeight, wallThickness), wallsParent.transform);

        // 南墙
        CreateWall("South Wall", new Vector3(0, wallHeight / 2, -mapHeight / 2), 
            new Vector3(mapWidth, wallHeight, wallThickness), wallsParent.transform);

        // 东墙
        CreateWall("East Wall", new Vector3(mapWidth / 2, wallHeight / 2, 0), 
            new Vector3(wallThickness, wallHeight, mapHeight), wallsParent.transform);

        // 西墙
        CreateWall("West Wall", new Vector3(-mapWidth / 2, wallHeight / 2, 0), 
            new Vector3(wallThickness, wallHeight, mapHeight), wallsParent.transform);
    }

    /// <summary>
    /// 创建墙体
    /// </summary>
    private GameObject CreateWall(string name, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        
        if (wallMaterial != null)
        {
            wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        }

        wall.isStatic = true;
        wall.tag = "Wall";
        
        return wall;
    }

    /// <summary>
    /// 创建内部结构
    /// </summary>
    private void CreateInnerStructure()
    {
        GameObject structureParent = new GameObject("InnerStructure");
        structureParent.transform.SetParent(transform);

        // ========== 北区 - 实验室区 ==========
        CreateZone("Laboratory Zone", new Vector3(0, 0, 35), structureParent.transform, () =>
        {
            // 实验室房间
            CreateRoom(new Vector3(-30, 0, 35), 15, 12);
            CreateRoom(new Vector3(-10, 0, 35), 15, 12);
            CreateRoom(new Vector3(10, 0, 35), 15, 12);
            CreateRoom(new Vector3(30, 0, 35), 15, 12);
        });

        // ========== 中央区 ==========
        CreateZone("Central Zone", new Vector3(0, 0, 0), structureParent.transform, () =>
        {
            // 中央大厅的柱子
            CreatePillar(new Vector3(-15, 0, 0));
            CreatePillar(new Vector3(15, 0, 0));
            CreatePillar(new Vector3(-15, 0, 15));
            CreatePillar(new Vector3(15, 0, 15));
            CreatePillar(new Vector3(-15, 0, -15));
            CreatePillar(new Vector3(15, 0, -15));
        });

        // ========== 西区 - 仓库区 ==========
        CreateZone("Warehouse Zone", new Vector3(-35, 0, 0), structureParent.transform, () =>
        {
            // 货架/箱子
            CreateObstacle(new Vector3(-35, 1, 5), new Vector3(8, 2, 3));
            CreateObstacle(new Vector3(-35, 1, -5), new Vector3(8, 2, 3));
            CreateObstacle(new Vector3(-40, 1, 0), new Vector3(3, 2, 8));
            CreateObstacle(new Vector3(-30, 1, 10), new Vector3(5, 2, 5));
            CreateObstacle(new Vector3(-30, 1, -10), new Vector3(5, 2, 5));
        });

        // ========== 东区 - 警卫区 ==========
        CreateZone("Guard Zone", new Vector3(35, 0, 0), structureParent.transform, () =>
        {
            CreateRoom(new Vector3(35, 0, 0), 20, 20);
            CreateObstacle(new Vector3(35, 1, 5), new Vector3(6, 2, 4));
            CreateObstacle(new Vector3(35, 1, -5), new Vector3(6, 2, 4));
        });

        // ========== 南区 - 医疗区 ==========
        CreateZone("Medical Zone", new Vector3(0, 0, -35), structureParent.transform, () =>
        {
            // 病房
            CreateRoom(new Vector3(-30, 0, -35), 12, 10);
            CreateRoom(new Vector3(-10, 0, -35), 12, 10);
            CreateRoom(new Vector3(10, 0, -35), 12, 10);
            CreateRoom(new Vector3(30, 0, -35), 12, 10);
        });

        // ========== 走廊连接 ==========
        CreateCorridors(structureParent.transform);
    }

    /// <summary>
    /// 创建区域
    /// </summary>
    private void CreateZone(string name, Vector3 position, Transform parent, System.Action createContent)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = position;
        
        createContent?.Invoke();
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    private void CreateRoom(Vector3 center, float width, float depth)
    {
        // 左墙
        CreateWall("Room Wall", center + new Vector3(-width / 2, wallHeight / 2, 0), 
            new Vector3(wallThickness, wallHeight, depth), transform);
        // 右墙
        CreateWall("Room Wall", center + new Vector3(width / 2, wallHeight / 2, 0), 
            new Vector3(wallThickness, wallHeight, depth), transform);
        // 后墙
        CreateWall("Room Wall", center + new Vector3(0, wallHeight / 2, depth / 2), 
            new Vector3(width, wallHeight, wallThickness), transform);
        // 前墙（带门）- 分成两段
        CreateWall("Room Wall", center + new Vector3(-width / 4 - 1, wallHeight / 2, -depth / 2), 
            new Vector3(width / 2 - 2, wallHeight, wallThickness), transform);
        CreateWall("Room Wall", center + new Vector3(width / 4 + 1, wallHeight / 2, -depth / 2), 
            new Vector3(width / 2 - 2, wallHeight, wallThickness), transform);
    }

    /// <summary>
    /// 创建柱子
    /// </summary>
    private void CreatePillar(Vector3 position)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Pillar";
        pillar.transform.SetParent(transform);
        pillar.transform.position = position + new Vector3(0, wallHeight / 2, 0);
        pillar.transform.localScale = new Vector3(2, wallHeight / 2, 2);
        
        if (obstacleMaterial != null)
        {
            pillar.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
        }
        
        pillar.isStatic = true;
        pillar.tag = "Obstacle";
    }

    /// <summary>
    /// 创建障碍物
    /// </summary>
    private void CreateObstacle(Vector3 position, Vector3 scale)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Obstacle";
        obstacle.transform.SetParent(transform);
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        
        if (obstacleMaterial != null)
        {
            obstacle.GetComponent<Renderer>().sharedMaterial = obstacleMaterial;
        }
        
        obstacle.isStatic = true;
        obstacle.tag = "Obstacle";
    }

    /// <summary>
    /// 创建走廊
    /// </summary>
    private void CreateCorridors(Transform parent)
    {
        // 主走廊墙壁
        float corridorWidth = 10f;
        
        // 北向走廊
        CreateWall("Corridor Wall", new Vector3(-corridorWidth / 2, wallHeight / 2, 20), 
            new Vector3(wallThickness, wallHeight, 20), parent);
        CreateWall("Corridor Wall", new Vector3(corridorWidth / 2, wallHeight / 2, 20), 
            new Vector3(wallThickness, wallHeight, 20), parent);

        // 南向走廊
        CreateWall("Corridor Wall", new Vector3(-corridorWidth / 2, wallHeight / 2, -20), 
            new Vector3(wallThickness, wallHeight, 20), parent);
        CreateWall("Corridor Wall", new Vector3(corridorWidth / 2, wallHeight / 2, -20), 
            new Vector3(wallThickness, wallHeight, 20), parent);
    }

    /// <summary>
    /// 创建光照
    /// </summary>
    private void CreateLighting()
    {
        GameObject lightingParent = new GameObject("Lighting");
        lightingParent.transform.SetParent(transform);

        // 主方向光（已存在则跳过）
        if (FindObjectOfType<Light>() == null)
        {
            GameObject dirLight = new GameObject("Directional Light");
            dirLight.transform.SetParent(lightingParent.transform);
            Light light = dirLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.9f);
            light.intensity = 0.8f;
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 添加区域点光源
        CreatePointLight(new Vector3(0, 3, 0), Color.white, 20f);
        CreatePointLight(new Vector3(-35, 3, 0), new Color(1f, 0.9f, 0.8f), 15f);
        CreatePointLight(new Vector3(35, 3, 0), new Color(1f, 0.8f, 0.8f), 15f);
        CreatePointLight(new Vector3(0, 3, 35), new Color(0.8f, 0.9f, 1f), 15f);
        CreatePointLight(new Vector3(0, 3, -35), new Color(0.9f, 1f, 0.9f), 15f);
    }

    /// <summary>
    /// 创建点光源
    /// </summary>
    private void CreatePointLight(Vector3 position, Color color, float range)
    {
        GameObject lightObj = new GameObject("Point Light");
        lightObj.transform.SetParent(transform);
        lightObj.transform.position = position;
        
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = 1f;
    }

    /// <summary>
    /// 创建 GameManager
    /// </summary>
    private void CreateGameManager()
    {
        if (FindObjectOfType<GameManager>() == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
            gmObj.AddComponent<UIManager>();
        }
    }

    // =============== 预制件创建方法 ===============

    public static GameObject CreatePlayerPrefab()
    {
        // 创建玩家
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");

        // 添加胶囊体作为身体
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = Vector3.zero;
        
        // 移除碰撞器（使用 CharacterController）
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // 添加 CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1, 0);

        // 创建第一人称摄像机
        GameObject fpCam = new GameObject("FirstPersonCamera");
        Camera fpCamera = fpCam.AddComponent<Camera>();
        fpCam.transform.SetParent(player.transform);
        fpCam.transform.localPosition = new Vector3(0, 1.6f, 0);
        fpCam.tag = "MainCamera";
        fpCam.AddComponent<AudioListener>();

        // 创建第三人称摄像机
        GameObject tpCam = new GameObject("ThirdPersonCamera");
        Camera tpCamera = tpCam.AddComponent<Camera>();
        tpCam.transform.SetParent(player.transform);
        tpCam.transform.localPosition = new Vector3(0, 3, -5);
        tpCam.transform.LookAt(player.transform.position + Vector3.up);
        tpCam.SetActive(false);

        // 创建射击点
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(fpCam.transform);
        firePoint.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);

        // 创建地面检测点
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, 0.1f, 0);

        // 添加脚本
        PlayerController controller = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerShooting>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<AudioSource>();

        // 设置初始位置
        player.transform.position = new Vector3(0, 1, -40);

        Debug.Log("玩家创建完成!");
        return player;
    }

    public static GameObject CreateEnemy()
    {
#if UNITY_EDITOR
        // 确保 Materials 文件夹存在
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
#endif

        // 创建敌人
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Enemy";
        enemy.tag = "Enemy";
        enemy.transform.localScale = new Vector3(1, 1.5f, 1);

        // 设置材质
#if UNITY_EDITOR
        string enemyMatPath = "Assets/Materials/EnemyMaterial.mat";
        Material enemyMat = AssetDatabase.LoadAssetAtPath<Material>(enemyMatPath);
        if (enemyMat == null)
        {
            enemyMat = new Material(Shader.Find("Standard"));
            enemyMat.color = Color.green;
            AssetDatabase.CreateAsset(enemyMat, enemyMatPath);
            AssetDatabase.SaveAssets();
        }
        enemy.GetComponent<Renderer>().sharedMaterial = enemyMat;
#endif

        // 添加组件
        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = 3f;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1.5f;

        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        enemy.AddComponent<EnemyAI>();
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<EnemyAttack>();
        enemy.AddComponent<AudioSource>();

        Debug.Log("敌人创建完成!");
        return enemy;
    }

    public static GameObject CreateMedicine()
    {
#if UNITY_EDITOR
        // 确保 Materials 文件夹存在
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
#endif

        // 创建药剂
        GameObject medicine = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        medicine.name = "Medicine";
        medicine.tag = "Medicine";
        medicine.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);

        // 设置材质
#if UNITY_EDITOR
        string medicineMatPath = "Assets/Materials/MedicineMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(medicineMatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.8f, 1f);  // 青蓝色（区分于绿色敌人）
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 0.5f));  // 青蓝色发光
            AssetDatabase.CreateAsset(mat, medicineMatPath);
            AssetDatabase.SaveAssets();
        }
        medicine.GetComponent<Renderer>().sharedMaterial = mat;
#endif

        // 设置碰撞器为触发器
        medicine.GetComponent<CapsuleCollider>().isTrigger = true;

        // 添加脚本
        medicine.AddComponent<Collectible>();

        Debug.Log("药剂创建完成!");
        return medicine;
    }

    public static GameObject CreateBullet()
    {
#if UNITY_EDITOR
        // 确保 Materials 文件夹存在
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
#endif

        // 创建子弹
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet";
        bullet.tag = "Bullet";
        bullet.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // 设置材质
#if UNITY_EDITOR
        string bulletMatPath = "Assets/Materials/BulletMaterial.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(bulletMatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = Color.yellow;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.yellow * 0.5f);
            AssetDatabase.CreateAsset(mat, bulletMatPath);
            AssetDatabase.SaveAssets();
        }
        bullet.GetComponent<Renderer>().sharedMaterial = mat;

        // Trail 材质
        string trailMatPath = "Assets/Materials/BulletTrailMaterial.mat";
        Material trailMat = AssetDatabase.LoadAssetAtPath<Material>(trailMatPath);
        if (trailMat == null)
        {
            trailMat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(trailMat, trailMatPath);
            AssetDatabase.SaveAssets();
        }
#endif

        // 添加刚体
        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 添加轨迹（增加可见性）
        TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
        trail.time = 0.3f;           // 增加轨迹持续时间（原0.1f）
        trail.startWidth = 0.1f;     // 增加起始宽度（原0.05f）
        trail.endWidth = 0.02f;      // 保留微小尾部（原0）
#if UNITY_EDITOR
        trail.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulletTrailMaterial.mat");
#endif
        trail.startColor = Color.yellow;
        trail.endColor = new Color(1, 1, 0, 0);


        // 添加脚本
        bullet.AddComponent<Bullet>();

        Debug.Log("子弹创建完成!");
        return bullet;
    }
}
