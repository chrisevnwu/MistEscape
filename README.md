# 迷雾逃生：药剂收集者 - Unity 项目使用指南

## 📁 项目结构

```
Assets/
├── Scenes/
│   └── SampleScene.unity          <- 主游戏场景
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs    <- 玩家移动控制（支持第一/第三人称切换）
│   │   ├── PlayerShooting.cs      <- 射击系统
│   │   └── PlayerHealth.cs        <- 玩家生命值
│   ├── Enemy/
│   │   ├── EnemyAI.cs             <- 敌人AI（巡逻/警戒/追击/攻击）
│   │   ├── EnemyHealth.cs         <- 敌人生命值
│   │   ├── EnemyAttack.cs         <- 敌人攻击
│   │   └── EnemyProjectile.cs     <- 敌人远程攻击弹丸
│   ├── Items/
│   │   ├── Collectible.cs         <- 可收集道具（药剂/血包/弹药）
│   │   └── Bullet.cs              <- 子弹
│   ├── Managers/
│   │   ├── GameManager.cs         <- 游戏全局状态管理
│   │   ├── UIManager.cs           <- UI显示（OnGUI）
│   │   └── SceneSetup.cs          <- 场景自动创建工具
│   └── Training/
│       ├── PlayerAgent.cs         <- ML-Agents 玩家代理
│       └── TrainingManager.cs     <- 训练环境管理
├── Prefabs/                       <- 预制件存放位置
├── Materials/                     <- 材质存放位置
├── Audio/                         <- 音效存放位置
└── ML-Agents/
    └── trainer_config.yaml        <- ML-Agents 训练配置
```

---

## 🚀 快速开始

### 步骤 1：打开 Unity 项目

1. 打开 Unity Hub
2. 点击 "Open" 并选择 `d:\Users\15407\final project` 文件夹
3. 等待 Unity 导入所有资源（首次可能需要几分钟）

### 步骤 2：创建游戏地图

1. 在 Unity 编辑器中，点击菜单栏的 **Tools**
2. 选择 **创建游戏地图**
3. 地图将自动生成，包括：
   - 地板（100x100单位）
   - 外墙边界
   - 四个功能区域（实验室、仓库、警卫、医疗）
   - 走廊连接
   - 光照系统

### 步骤 3：创建游戏对象

> **注意**：每个命令只需执行一次

1. **Tools → 创建玩家** - 创建玩家对象
2. **Tools → 创建敌人预制件** - 创建敌人
3. **Tools → 创建药剂预制件** - 创建药剂道具
4. **Tools → 创建子弹预制件** - 创建子弹

### 步骤 4：设置预制件（详细教程）

#### 4.1 保存预制件

完成步骤 3 后，您的 **Hierarchy** 窗口中应该有以下新对象：
- `Player`（玩家）
- `Enemy`（敌人）
- `Medicine`（药剂）
- `Bullet`（子弹）

**将对象保存为预制件：**

1. 在 **Project** 窗口中，展开 `Assets` 文件夹
2. 找到并点击 `Prefabs` 文件夹（如果不存在，右键 Assets → Create → Folder，命名为 `Prefabs`）
3. 从 **Hierarchy** 窗口中，**逐个拖拽**以下对象到 `Prefabs` 文件夹：
   - 拖拽 `Enemy` → 松开鼠标 → 预制件创建成功（图标变蓝）
   - 拖拽 `Medicine` → 松开鼠标
   - 拖拽 `Bullet` → 松开鼠标
4. **注意**：`Player` 对象保留在场景中，**不需要**保存为预制件

#### 4.2 清理场景中的临时对象

保存预制件后，删除场景中的临时对象（保留 Player）：

1. 在 **Hierarchy** 中选中 `Enemy`，按 `Delete` 键删除
2. 选中 `Medicine`，按 `Delete` 键删除
3. 选中 `Bullet`，按 `Delete` 键删除
4. **保留** `Player` 对象（这是您在场景中的玩家）

#### 4.3 配置 GameManager

1. 在 **Hierarchy** 窗口中，找到并**单击**选中 `GameManager` 对象
2. 查看右侧的 **Inspector** 窗口，找到 `GameManager (Script)` 组件
3. 找到 `Enemy Prefab` 字段（显示为 None）
4. 从 **Project** 窗口的 `Assets/Prefabs` 文件夹中，**拖拽** `Enemy` 预制件到 `Enemy Prefab` 字段

#### 4.3.1 配置 PlayerShooting（子弹预制件）

1. 在 **Hierarchy** 窗口中，找到并选中 `Player` 对象
2. 在 **Inspector** 中找到 `PlayerShooting (Script)` 组件
3. 找到 `Bullet Prefab` 字段
4. 从 `Assets/Prefabs` 拖拽 `Bullet` 预制件到该字段

#### 4.4 创建敌人生成点

1. 在 **Hierarchy** 窗口中右键，选择 **Create Empty**
2. 将新对象重命名为 `EnemySpawnPoint1`
3. 在 **Inspector** 中，设置 Transform 的 Position 为地图上的一个位置，例如：
   - Position: X = 20, Y = 1, Z = 20
4. 重复步骤 1-3，创建更多生成点（建议 4-6 个），位置分布在地图各区域：
   - `EnemySpawnPoint2`: X = -20, Y = 1, Z = 20
   - `EnemySpawnPoint3`: X = 30, Y = 1, Z = -30
   - `EnemySpawnPoint4`: X = -30, Y = 1, Z = -30
5. 选中 `GameManager`，在 Inspector 中找到 `Enemy Spawn Points` 数组
6. 将 `Size` 设置为生成点数量（例如 4）
7. 将每个 `EnemySpawnPointX` 对象**拖拽**到对应的数组槽位

---

### 步骤 5：放置道具（详细教程）

#### 5.1 放置药剂

1. 在 **Project** 窗口中，展开 `Assets/Prefabs` 文件夹
2. 将 `Medicine` 预制件**拖拽**到 **Scene** 视图或 **Hierarchy** 窗口
3. 选中新放置的药剂对象
4. 在 **Inspector** 中，调整 Position 将药剂放到合适位置，例如：
   ```
   第1个: X = 5,  Y = 0.5, Z = 10
   第2个: X = -5, Y = 0.5, Z = 15
   第3个: X = 20, Y = 0.5, Z = -5
   第4个: X = -20, Y = 0.5, Z = -5
   第5个: X = 35, Y = 0.5, Z = 30
   第6个: X = -35, Y = 0.5, Z = 30
   第7个: X = 10, Y = 0.5, Z = -35
   第8个: X = -10, Y = 0.5, Z = -35
   ```
5. 重复拖拽操作，放置**共 8 个**药剂

> **提示**：您也可以使用 `Ctrl+D` 快捷键复制已放置的药剂，然后移动到新位置

#### 5.2 放置初始敌人（可选）

敌人会由 GameManager 自动生成，但您也可以手动放置几个：

1. 从 `Assets/Prefabs` 拖拽 `Enemy` 到场景
2. 调整位置，例如：X = 30, Y = 1, Z = 0
3. 可以放置 2-3 个初始敌人

---

### 步骤 6：烘焙 NavMesh（详细教程 - Unity 2022+ 新版本）

NavMesh 是敌人 AI 使用的导航网格，**必须正确烘焙**敌人才能移动。

> **注意**：Unity 2022+ 版本使用新的 **AI Navigation** 包，采用基于组件的工作流程，不再使用旧的 Window → AI → Navigation 菜单。

#### 6.1 确认已安装 AI Navigation 包

1. 点击菜单栏 **Window → Package Manager**
2. 在左上角下拉菜单选择 **Unity Registry**
3. 搜索 `AI Navigation`
4. 如果未安装，点击 **Install** 按钮
5. 安装完成后关闭 Package Manager

#### 6.2 添加 NavMesh Surface 组件

新版本需要在场景中添加 NavMeshSurface 组件来烘焙导航网格：

1. 在 **Hierarchy** 中右键，选择 **Create Empty**
2. 将新对象重命名为 `NavMesh`
3. 选中 `NavMesh` 对象
4. 在 **Inspector** 中点击 **Add Component**
5. 搜索并添加 `NavMesh Surface`

#### 6.3 配置 NavMesh Surface

根据截图，您的 NavMesh Surface 组件应该已经有正确的默认设置：
- **Agent Type**: Humanoid ✓
- **Default Area**: Walkable ✓
- **Use Geometry**: Render Meshes ✓
- **Object Collection → Collect Objects**: All Game Objects ✓
- **Include Layers**: Everything ✓

> **注意**：这些默认设置已经足够，无需额外修改！

#### 6.4 直接烘焙 NavMesh

新版 AI Navigation 会自动识别场景中的几何体：
- 水平面（如地板）→ 自动标记为可行走
- 垂直面（如墙壁）→ 自动标记为不可行走

**无需手动添加 NavMesh Modifier 组件！**

1. 确保 NavMesh Surface 组件的 **Use Geometry** 设置为 **Render Meshes**
2. 直接点击 **Bake** 按钮
3. 等待烘焙完成
4. 烘焙成功后，**Scene** 视图中会显示**蓝色/青色的可行走区域**

#### 6.5 验证 NavMesh

- 蓝色/青色区域 = 敌人可以行走的区域
- 确保地板大部分区域显示为蓝色
- 墙壁和障碍物周围应该没有蓝色（表示不可通过）

> **提示**：如果没有看到导航网格可视化，确保 Scene 视图工具栏中的 **Gizmos** 按钮已打开

#### 可选：使用 NavMesh Modifier（仅在需要时）

如果自动烘焙结果不理想（例如某些区域不应该可行走），可以：

1. 选中需要调整的对象
2. 添加 `NavMesh Modifier` 组件
3. 勾选 **Ignore From Build** 来排除该对象
4. 或勾选 **Override Area** 并选择区域类型
5. 重新点击 **Bake**

### 步骤 7：测试游戏

按 **Play** 按钮开始测试游戏！

---

## 🎮 游戏操作

| 按键 | 功能 |
|------|------|
| `W/A/S/D` | 移动 |
| `鼠标` | 转向/瞄准 |
| `鼠标左键` | 射击 |
| `Shift` | 奔跑 |
| `Ctrl/C` | 潜行 |
| `V` | 切换第一/第三人称视角 |
| `ESC` | 暂停菜单 |
| `Space` | 跳跃 |

---

## 🤖 ML-Agents 训练指南

### 前提条件

1. **安装 Python 3.8+**
2. **安装 ML-Agents Python 包**：
   ```bash
   pip install mlagents==0.30.0
   ```

### 准备训练场景（详细教程）

> **什么是训练场景？**
> ML-Agents 是 Unity 的机器学习框架。训练场景是指配置好的 Unity 场景，让 AI 代理（Agent）可以在其中学习如何玩游戏。训练完成后，AI 可以自动控制玩家角色收集药剂、躲避敌人。

#### 步骤 1：安装 ML-Agents Unity 包

1. 点击菜单栏 **Window → Package Manager**
2. 点击左上角 **+** 按钮，选择 **Add package from git URL...**
3. 输入以下地址并点击 **Add**：
   ```
   com.unity.ml-agents
   ```
4. 等待安装完成（可能需要几分钟）
5. 安装成功后，Package Manager 中会显示 "ML Agents" 包

#### 步骤 2：在 Player 上添加 PlayerAgent 脚本

1. 在 **Hierarchy** 窗口中，选中 **Player** 对象
2. 在 **Inspector** 窗口中，点击 **Add Component** 按钮
3. 在搜索框中输入 `PlayerAgent`
4. 点击 **PlayerAgent** 将脚本添加到 Player

> **注意**：添加 PlayerAgent 后，Unity 会**自动添加** `Behavior Parameters` 组件，无需手动添加。

#### 步骤 3：添加 Decision Requester 组件

1. 保持 Player 选中状态
2. 点击 **Add Component**
3. 搜索并添加 `Decision Requester`
4. 在组件中设置：
   - **Decision Period**: `5`（AI 每 5 帧做一次决策）
   - **Take Actions Between Decisions**: ✓ 勾选

#### 步骤 4：添加 Ray Perception Sensor 3D（可选但强烈推荐）

这个组件让 AI 能够"看到"周围的物体（敌人、药剂、墙壁）。

1. 保持 Player 选中状态
2. 点击 **Add Component**
3. 搜索并添加 `Ray Perception Sensor 3D`
4. 配置参数：

| 参数 | 值 | 说明 |
|------|-----|------|
| Sensor Name | RayPerceptionSensor | 传感器名称 |
| Detectable Tags | 点击 + 添加以下标签 | AI 可以检测的对象类型 |
| | Enemy | 敌人 |
| | Medicine | 药剂 |
| | Wall | 墙壁 |
| | Obstacle | 障碍物 |
| Rays Per Direction | 5 | 每个方向的射线数量 |
| Max Ray Degrees | 180 | 射线覆盖角度 |
| Sphere Cast Radius | 0.5 | 射线半径 |
| Ray Length | 20 | 射线长度（米） |
| Stacked Raycasts | 1 | 堆叠射线数 |

#### 步骤 5：配置 Behavior Parameters

1. 在 Player 的 Inspector 中，找到 **Behavior Parameters** 组件
2. 配置以下参数：

| 参数 | 值 | 说明 |
|------|-----|------|
| **Behavior Name** | `PlayerAgent` | 必须与训练配置文件匹配 |
| **Vector Observation → Space Size** | `20` | 观察空间大小 |
| **Actions → Continuous Actions** | `3` | 连续动作数（移动X、移动Z、旋转） |
| **Actions → Discrete Branches** | Size = 1, Branch 0 Size = 2 | 离散动作（射击：是/否） |
| **Model** | None | 训练时留空 |
| **Inference Device** | CPU | 推理设备 |
| **Behavior Type** | Default | 训练时保持默认 |

#### 步骤 6：验证配置

配置完成后，Player 对象的 Inspector 应该包含以下组件：

- ✅ PlayerController
- ✅ PlayerShooting  
- ✅ PlayerHealth
- ✅ **PlayerAgent**（新添加）
- ✅ **Behavior Parameters**（自动添加）
- ✅ **Decision Requester**（新添加）
- ✅ **Ray Perception Sensor 3D**（新添加，可选）
- ✅ Character Controller
- ✅ Audio Source

> **提示**：如果看到红色错误提示，说明某些配置不正确。请仔细检查上述参数。

---

### 开始训练（详细步骤）

#### 步骤 1：安装 Python 环境

如果你还没有安装 Python 和 ML-Agents Python 包：

1. **下载并安装 Python 3.8+**
   - 访问 https://www.python.org/downloads/
   - 下载 Python 3.8 或更高版本
   - 安装时**勾选** "Add Python to PATH"

2. **安装 ML-Agents Python 包**
   - 按 `Win + R`，输入 `cmd`，按回车打开命令提示符
   - 输入以下命令并回车：
   ```bash
   pip install mlagents==0.30.0
   ```
   - 等待安装完成（可能需要几分钟）

#### 步骤 2：保存 Unity 场景

1. 在 Unity 中，确保已完成上述所有配置（PlayerAgent、Decision Requester 等）
2. 按 `Ctrl + S` 保存场景
3. **暂时不要按 Play**

#### 步骤 3：打开命令行并导航到项目目录

1. 按 `Win + R`，输入 `cmd`，按回车
2. 使用 `cd` 命令导航到项目目录：
   ```bash
   cd "d:\Users\15407\final project"
   ```
3. 确认你在正确的目录（输入 `dir` 应该能看到 `Assets` 文件夹）

#### 步骤 4：启动训练

在命令行中输入以下命令并回车：

```bash
mlagents-learn Assets/ML-Agents/trainer_config.yaml --run-id=MistEscape_v1
```

**命令解释：**
- `mlagents-learn`：ML-Agents 训练程序
- `Assets/ML-Agents/trainer_config.yaml`：训练配置文件路径
- `--run-id=MistEscape_v1`：本次训练的唯一标识符（可自定义）

#### 步骤 5：等待训练程序就绪

命令执行后，你会看到一些输出信息。当看到以下提示时：

```
[INFO] Listening on port 5004. Start training by pressing the Play button in the Unity Editor.
```

这表示训练程序已准备好，正在等待 Unity 连接。

#### 步骤 6：在 Unity 中开始训练

1. 切换到 Unity 编辑器
2. 点击顶部的 **▶ Play** 按钮
3. 训练开始！你会看到：
   - Unity 中玩家自动移动
   - 命令行中显示训练进度

#### 步骤 7：等待训练完成

- 训练时间取决于配置，通常需要 **30分钟到几小时**
- 你可以随时按 Unity 的 **停止按钮** 中断训练
- 训练过程中可以看到奖励值逐渐提升

> **提示**：第一次训练时间较长是正常的。如果想快速测试，可以先训练 10-15 分钟。

---

### 监控训练（可选但推荐）

TensorBoard 可以可视化训练进度：

1. **打开新的命令行窗口**（保持原训练窗口运行）
2. 导航到项目目录：
   ```bash
   cd "d:\Users\15407\final project"
   ```
3. 启动 TensorBoard：
   ```bash
   tensorboard --logdir results
   ```
4. 打开浏览器，访问 http://localhost:6006
5. 你可以看到：
   - **Cumulative Reward**：累积奖励（应逐渐上升）
   - **Episode Length**：回合长度
   - **Policy Loss**：策略损失

---

### 使用训练好的模型

训练完成后：

1. 在命令行中按 `Ctrl + C` 停止训练
2. 在项目目录中，找到 `results/MistEscape_v1/` 文件夹
3. 里面有一个 `.onnx` 文件（例如 `PlayerAgent.onnx`）
4. 将 `.onnx` 文件拖到 Unity 的 `Assets` 文件夹中
5. 在 Unity 中选择 **Player** 对象
6. 在 **Behavior Parameters** 组件中：
   - 将 **Model** 设置为刚导入的 ONNX 文件
   - 将 **Behavior Type** 改为 **Inference Only**
7. 按 Play，AI 将自动控制玩家！

---

## 📝 脚本配置说明

### GameManager

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Total Medicines | 需要收集的药剂总数 | 8 |
| Max Enemies | 最大敌人数量 | 8 |
| Enemy Respawn Time | 敌人重生时间（秒） | 10 |
| Enemy Prefab | 敌人预制件引用 | - |
| Enemy Spawn Points | 敌人生成点数组 | - |

### PlayerController

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Is First Person | 是否使用第一人称 | true |
| Walk Speed | 行走速度 | 5 |
| Sprint Speed | 奔跑速度 | 8 |
| Sneak Speed | 潜行速度 | 2.5 |
| Jump Force | 跳跃力度 | 5 |
| Mouse Sensitivity | 鼠标灵敏度 | 2 |

### EnemyAI

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Sight Range | 视觉范围 | 15 |
| Sight Angle | 视野角度 | 120 |
| Hearing Range | 听觉范围 | 10 |
| Attack Range | 攻击范围 | 2 |
| Patrol Speed | 巡逻速度 | 2 |
| Chase Speed | 追击速度 | 5 |
| Patrol Points | 巡逻点数组 | - |

### PlayerAgent (ML-Agents)

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Move Force | 移动力度 | 10 |
| Rotate Speed | 旋转速度 | 200 |
| Medicine Reward | 收集药剂奖励 | 1.0 |
| Win Reward | 胜利奖励 | 5.0 |
| Damage Penalty | 受伤惩罚 | -0.5 |
| Death Penalty | 死亡惩罚 | -2.0 |

---

## 🏷️ 必需的 Tags

确保在 Unity 中创建以下 Tags：

- `Player` - 玩家
- `Enemy` - 敌人
- `Medicine` - 药剂
- `Ammo` - 弹药
- `Bullet` - 子弹
- `Wall` - 墙体
- `Ground` - 地面
- `Obstacle` - 障碍物
- `EnemyAttack` - 敌人攻击触发器
- `DeathZone` - 死亡区域

---

## ⚠️ 常见问题

### Q: 敌人不会移动
**A:** 检查是否已烘焙 NavMesh，并确保敌人有 NavMeshAgent 组件

### Q: 子弹不会消失
**A:** 确保 Bullet 脚本已添加到子弹预制件

### Q: ML-Agents 训练无法启动
**A:** 确保安装了正确版本的 Python 和 mlagents 包

### Q: 视角切换不工作
**A:** 确保玩家对象下有两个摄像机：FirstPersonCamera 和 ThirdPersonCamera

---

## 📚 提交清单

按照实验要求，最终提交应包含：

- [ ] 白盒设计稿
- [ ] Unity 场景截图
- [ ] 单页设计文档
- [ ] 完整工程文件/Package
- [ ] 训练好的 ONNX 模型
- [ ] AI 训练录屏（≤15秒）
- [ ] 模型效果视频（≤15秒）
- [ ] 最终实验报告

**截止日期：2026年1月6日 16:00 前**
