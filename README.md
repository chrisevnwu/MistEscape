# MistEscape 迷雾逃生

一个基于 Unity 和 ML-Agents 的 AI 强化学习游戏项目。玩家需要在充满敌人的迷雾地图中收集药剂，也可以使用训练好的 AI 模型自动完成任务。

## 📋 项目概述

**游戏目标**：在限定时间内收集所有药剂，同时躲避或击败巡逻的敌人。

**技术栈**：
- Unity 2022.3+
- Unity ML-Agents 2.0
- C#

## 🎮 核心功能

- **第一/第三人称切换**：按 `V` 键切换视角
- **智能敌人 AI**：敌人具有巡逻、警戒、追击、攻击四种状态
- **ML-Agents 训练**：支持强化学习训练 AI 玩家
- **多环境并行训练**：支持多区域并行加速训练

## 📁 项目结构

```
Assets/
├── Scenes/          # 游戏场景
├── Scripts/         # 游戏脚本
│   ├── Player/      # 玩家相关
│   ├── Enemy/       # 敌人 AI
│   ├── Items/       # 道具系统
│   ├── Managers/    # 游戏管理器
│   └── Training/    # ML-Agents 训练
├── Prefabs/         # 预制件
└── ML-Agents/       # 训练配置
```

## 🕹️ 游戏操作

| 按键 | 功能 |
|------|------|
| `W/A/S/D` | 移动 |
| `鼠标` | 转向/瞄准 |
| `鼠标左键` | 射击 |
| `Shift` | 奔跑 |
| `V` | 切换视角 |
| `ESC` | 暂停 |

## 🚀 快速开始

1. 使用 Unity 2022.3+ 打开项目
2. 通过菜单栏 **Tools** 创建游戏地图和游戏对象
3. 配置预制件（Enemy、Medicine、Bullet）
4. 烘焙 NavMesh
5. 点击 Play 开始游戏

## 🤖 ML-Agents 训练

### 环境要求

- Python 3.8+
- `pip install mlagents==0.30.0`

### 训练命令

```bash
# 本地训练（需要在 Unity 中点击 Play）
mlagents-learn Assets/ML-Agents/trainer_config.yaml --run-id=MistEscape_v1

# 服务器训练（无 GUI）
mlagents-learn trainer_config.yaml --env=./Build/MistEscape --run-id=MistEscape_v1 --no-graphics
```

### 使用训练模型

训练完成后，将 `results/` 目录下的 `.onnx` 文件导入 Unity，配置到 Player 的 Behavior Parameters 中即可。

## 📊 训练参数

关键奖励/惩罚设置（可在 PlayerAgent 中调整）：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| MedicineReward | 1.0 | 收集药剂奖励 |
| WinReward | 5.0 | 胜利奖励 |
| DamagePenalty | -0.5 | 受伤惩罚 |
| DeathPenalty | -2.0 | 死亡惩罚 |

## 📝 License

This project is for educational purposes.
