# Appetite — Unity 对话系统 Demo 项目

## 项目概述

Appetite 是一个 Unity 游戏项目（2D/3D 混合），核心玩法围绕"饥饿"概念展开。当前处于 Demo 阶段，重点是对话系统的框架搭建。

## 场景架构与流程

```
MainMenu → Hospital → ExplorationScene（首次）→ Fight → SpiritWorld → ExplorationScene（返回）→ MainMenu
```

### 5 个场景（都在 Build Settings 中）

| 场景 | 文件 | 用途 |
|---|---|---|
| MainMenu | Scenes/MainMenu.unity | 主菜单，按钮加载 Hospital |
| Hospital | Scenes/Hospital.unity | 医院开场（空白场景+对话），等美术视频 |
| ExplorationScene | Scenes/ExplorationScene.unity | 主世界，街道/家/探索 |
| Fight | Scenes/Fight.unity | 战斗场景（暂未实现），有按钮跳转 SpiritWorld |
| SpiritWorld | Scenes/SpiritWorld.unity | 精神世界，黑猫引子+黑狗对话 |

### 详细流程

1. **MainMenu** → 点击「开始新游戏」→ `SceneLoader.LoadSceneByName("Hospital")`
2. **Hospital** → `HospitalSceneSetup` 自动播放护士→医生→黑猫对话链 → `LoadScene("ExplorationScene")`
3. **ExplorationScene（首次）** → `ExplorationSceneSetup` 创建 4 个按顺序出现的 NPC（走近按 F）：前同事→面包师→社区阿姨→电脑 → 对话结束加载 Fight
4. **Fight** → 按钮 → `LoadScene("SpiritWorld")`
5. **SpiritWorld** → `SpiritWorldSetup` 先播放黑猫引子，结束后生成黑狗 NPC → 狗对话结束设 `GameProgress.hasReturnedFromSpiritWorld = true` → `LoadScene("ExplorationScene")`
6. **ExplorationScene（返回）** → 检测到 `hasReturnedFromSpiritWorld`，传送到电脑位置，自动播放觉醒对话链 → `LoadScene("MainMenu")`

## 核心脚本

### 对话系统（`Assets/_Game/Scripts/Dialogue/`）

| 脚本 | 职责 |
|---|---|
| **DialogueManager.cs** | 对话核心控制器，Singleton。管理对话面板、文本显示、选项按钮、场景跳转。`OnBeforeAutoAdvance` 钩子可中断自动串联。`AutoDiscoverAll()`/`AutoDiscoverOptions()` 按名称自愈 UI 引用。`Awake()` 里自动添加 Canvas（如果没有父 Canvas）。 |
| **DialogueNode.cs** | ScriptableObject 数据模型。字段：`text`, `speakerName`, `options[]`, `nextNode`, `endAction` (None/LoadScene/ReturnToPrevious), `endActionSceneName` |
| **DialogueTrigger.cs** | 挂到 GameObject 上触发对话。两种模式：`OnStart`（自动）和 `OnProximity`（玩家走进按 F） |
| **InputHandler.cs** | 监听鼠标左键/E 键，调用 `AdvanceDialogue()` |

### 场景设置脚本（`Assets/_Game/Scripts/`）

| 脚本 | 挂载场景 | 职责 |
|---|---|---|
| **HospitalSceneSetup.cs** | Hospital | 创建 EventSystem+InputHandler，自动开始 H1_Nurse1 对话链。`startNode` 字段拖入 H1_Nurse1 |
| **ExplorationSceneSetup.cs** | ExplorationScene | **首次进入**：创建 4 个 NPC（前同事/面包师/阿姨/电脑），按顺序逐个出现，上一个消失下一个才出现。用 `OnBeforeAutoAdvance` 钩子中断对话串联。**从精神世界返回**：传送到 `wakeUpPosition`，自动播放觉醒对话。Inspector 中可调 NPC 位置和 Wake Up Position |
| **SpiritWorldSetup.cs** | SpiritWorld | 先播放 SpiritCat 黑猫引子对话，结束后生成黑狗 NPC（走近按 F）。狗对话结束后设置 `GameProgress.hasReturnedFromSpiritWorld = true` |
| **GameProgress.cs** | 全局静态 | `hasReturnedFromSpiritWorld` 等状态标志，`ResetAll()` |
| **SceneLoader.cs** | MainMenu 等 | `LoadSceneByName()`, `LoadSceneByIndex()`, `QuitGame()` |

### 其他脚本

| 脚本 | 职责 |
|---|---|
| **FightSceneSetup.cs** | Fight 场景中创建 Canvas + 按钮跳转 SpiritWorld |
| **ExpCameraFollow.cs** | ExplorationScene 中摄像机跟随玩家 |

## 对话数据文件

对话节点是 Unity ScriptableObject `.asset` 文件，位于 `Assets/_Game/Data/Dialogues/`：

| 文件夹 | 内容 | 场景 |
|---|---|---|
| `Hospital/` | H1-H18 护士医生对话链 | Hospital |
| `BlackCat/` | HC1-HC16 黑猫出现对话 | Hospital |
| `Colleague/` | C1-C8 + C3选择/a/b/c 前同事分支对话 | ExplorationScene |
| `Bakery/` | B1-B5 面包店对话 | ExplorationScene |
| `Auntie/` | Auntie1-Auntie7 社区阿姨对话 | ExplorationScene |
| `Computer/` | PC1_PC7 电脑对话（最后加载 Fight） | ExplorationScene |
| `SpiritCat/` | SC1-SC2 精神世界黑猫引子 | SpiritWorld |
| `Dog/` | Dog1-Dog6 + 选择分支 黑狗对话（3 选项） | SpiritWorld |
| `Awakening/` | PW1-PW10 觉醒结局对话 | ExplorationScene |
| `Opening/` | Act0-Act5 旧版开场（已废弃但保留） | - |

### ⚠️ 重要：对话文件已手动修改

用户已经手动修改了大量对话 .asset 文件（重命名、拆分、改写内容）。**绝对不要运行 `Tools/generate_dialogues.py` 或任何自动生成脚本去覆盖这些文件。** 该脚本仅作为历史参考保留。

## UI 系统

- **DialoguePanel.prefab** (`Assets/_Game/UI/DialogueUI/`) — 对话框 prefab，含 speakerNameText、dialogueText、OptionsPanel（3 个 OptionButton）、nextIndicator
- 字体：`font2 SDF`（对话字体，"字体家AI造字春风"），**中文缺字问题**尚未完全解决，需要添加 fallback font 或扩展 atlas
- `DialogueManager.Awake()` 会自动添加 Canvas 组件（如果没有父 Canvas），确保 UI 在任何场景都能渲染

## 已知问题与注意事项

1. **中文缺字** — font2 SDF atlas（1024x1024）不够大，某些中文字符显示为 □。需要添加 fallback font asset 或扩大 atlas 到 4096
2. **对话文件不可覆盖** — 用户手动整理过，结构和命名与生成脚本不同
3. **NPC 位置** — ExplorationScene 中 4 个 NPC 位置在 `ExplorationSceneSetup` 的 Inspector 中调整（npc1-4Position），Wake Up Position 也是独立字段
4. **Fight 场景未实现** — 目前只有一个按钮跳转 SpiritWorld
5. **Hospital 场景空白** — 等美术资源（视频/动画）

## 工具脚本

`Tools/generate_dialogues.py` — 对话文件批量生成器，**不要运行**（会覆盖手动修改）。仅作参考。
