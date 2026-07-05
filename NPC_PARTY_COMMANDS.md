# NPC 小队命令说明

本文说明测试服中 AI 冒险小队相关 GM 命令的作用。所有命令都需要 `GameMaster` 权限。

相关源码:

- `Scripts/Custom/AdventureParty/AdventureParty.cs`
- `Scripts/Custom/AdventureParty/AdventurePartyAI.cs`
- `Scripts/Custom/AdventureParty/AdventurePartyMonitor.cs`
- `Scripts/Custom/AdvancedCombatAI/AdvancedCombatBrain.cs`

## 基本概念

NPC 小队由一个隐藏的 `AdventurePartyController` 控制器和四名队员组成:

- `Fighter`: 前排战士
- `Archer`: 弓手/吟游辅助
- `Mage`: 法师/死灵/织法输出
- `Healer`: 治疗者

生成小队时，控制器会出现在 GM 当前坐标，并建立一条默认巡逻路线。默认路线从生成点开始，在附近几个点之间移动。

`AIEnabled` 不是“让 NPC 动起来”的总开关。小队本身的本地控制、队形、拉怪、恢复、复活和战斗脑会照常工作。`AIEnabled` 控制的是额外的小队级 AI 决策层，可能来自本地 Mock，也可能来自 HTTP 桥接服务。

## 生成与清理命令

### `[AdventureParty`

在 GM 当前所在位置生成一支四人 NPC 冒险小队。

效果:

- 创建一个隐藏的 `AdventurePartyController`。
- 生成战士、弓手、法师、治疗者四名队员。
- 小队进入 `Exploring` 状态。
- 默认不会强制打开 AI 决策层，是否默认开启取决于 `Config/AdventurePartyAI.cfg` 里的 `EnabledByDefault`。

适合用途:

- 只想生成小队，先观察本地控制器和战斗脑行为。
- 手动再用 `[AdventurePartyAI on` 打开 AI 决策层。

### `[SpawnAdventureParty`

作用和 `[AdventureParty` 相同，是同一个生成命令的别名。

### `[SpawnAIAdventureParty`

在 GM 当前所在位置生成一支四人 NPC 冒险小队，并立刻启用 AI 决策层。

效果:

- 创建控制器并生成四名队员。
- 设置 `AIEnabled = true`。
- 使用当前控制器的 `AIProvider`，默认来自 `Config/AdventurePartyAI.cfg`。
- 当前配置默认 `DefaultProvider=Mock`，所以不启动桥接服务时也能离线测试。

适合用途:

- 快速生成一队可直接进入 AI 决策测试的小队。
- 测试 Mock 决策或 HTTP 桥接决策是否影响小队战术。

### `[ClearAdventureParties`

清理世界中所有 NPC 冒险小队。

效果:

- 删除所有 `AdventurePartyController`。
- 删除所有残留的 `BaseAdventurer` 队员。
- 命令会回报清理了多少控制器和多少个散落队员。

注意:

- 这是全世界范围清理，不只清理 GM 附近的小队。
- 测试结束、刷出过多小队、或控制器丢失时使用。

## AI 决策命令

命令格式:

```text
[AdventurePartyAI status
[AdventurePartyAI on
[AdventurePartyAI off
[AdventurePartyAI mock
[AdventurePartyAI http
[AdventurePartyAI config
[AdventurePartyAI endpoint [url]
[AdventurePartyAI all status
[AdventurePartyAI all on
[AdventurePartyAI all off
[AdventurePartyAI all mock
[AdventurePartyAI all http
```

不带 `all` 时，命令会寻找 GM 同地图 20 格内最近的 `AdventurePartyController`。带 `all` 时，会作用于世界中所有 NPC 小队控制器。

### `[AdventurePartyAI status`

查看最近一支 NPC 小队的 AI 状态。

显示内容包括:

- `enabled`: AI 决策层是否开启。
- `provider`: 当前使用 `Mock` 还是 `HttpEndpoint`。
- `pending`: 是否有正在等待返回的 AI 请求。
- `requests`: 已发送请求数。
- `decisions`: 已收到有效决策数。
- `failures`: 请求失败或返回无效决策次数。
- `status`: 最近一次 AI 状态说明。

### `[AdventurePartyAI all status`

查看所有 NPC 小队控制器的 AI 状态。

适合同时测试多支小队时使用。

### `[AdventurePartyAI on`

开启最近小队的 AI 决策层。

开启后，控制器会按 `RequestCooldown` 定期生成小队快照，并交给当前 provider 决策。

### `[AdventurePartyAI all on`

开启所有小队的 AI 决策层。

注意:

- 如果 provider 是 HTTP，所有小队都会请求本地桥接服务。
- 多队同时测试时建议先看性能和日志量。

### `[AdventurePartyAI off`

关闭最近小队的 AI 决策层。

关闭后:

- 小队仍会保留本地控制器、队形、战斗脑和恢复逻辑。
- 不再向 Mock 或 HTTP provider 请求小队级决策。

### `[AdventurePartyAI all off`

关闭所有小队的 AI 决策层。

适合停止大规模 AI 决策测试，但保留已生成小队继续观察本地行为。

### `[AdventurePartyAI mock`

把最近小队的 provider 切换为 `Mock`。

`Mock` 是本地离线决策，不需要网络，不需要 API key。它会根据小队快照返回保守的小队级命令，例如拉到卡口、保护治疗者、恢复、集火等。

注意:

- `mock` 只切换 provider，不自动开启 AI。
- 如果小队 AI 还没开，需要再执行 `[AdventurePartyAI on`。

### `[AdventurePartyAI all mock`

把所有小队的 provider 切换为 `Mock`。

### `[AdventurePartyAI http`

把最近小队的 provider 切换为 `HttpEndpoint`。

HTTP 模式会把小队快照发给 `AdventurePartyAI.HttpEndpointUrl` 指向的本地服务。默认地址是:

```text
http://127.0.0.1:8787/decide
```

注意:

- `http` 只切换 provider，不自动开启 AI。
- 需要先启动 `Tools/AdventurePartyAI/adventure_party_ai_bridge.py` 或其他兼容服务。
- API key 应该放在桥接服务环境变量里，不要写进 ServUO 配置。

### `[AdventurePartyAI all http`

把所有小队的 provider 切换为 `HttpEndpoint`。

### `[AdventurePartyAI endpoint`

查看当前 HTTP endpoint 地址。

### `[AdventurePartyAI endpoint http://127.0.0.1:8787/decide`

修改当前运行中的 HTTP endpoint 地址。

注意:

- 这是运行时修改，方便测试。
- 重启服务器后仍以 `Config/AdventurePartyAI.cfg` 为准。

### `[AdventurePartyAI config`

查看全局 AI 配置。

显示内容包括:

- endpoint 地址
- 默认 provider
- 是否默认启用
- 请求冷却
- 失败冷却
- 请求超时
- 决策有效期

## 监控命令

命令格式:

```text
[AdventurePartyMonitor on [seconds]
[AdventurePartyMonitor all on [seconds]
[AdventurePartyMonitor off
[AdventurePartyMonitor status
[AdventurePartyMonitor once
[AdventurePartyMonitor check
[AdventurePartyMonitor all once
[AdventurePartyMonitor interval [seconds]
```

不带 `all` 时，监控命令寻找 GM 同地图 20 格内最近的小队控制器。带 `all` 时，监控所有小队。

监控日志写入:

```text
Logs/AdventurePartyMonitor/state-yyyy-MM-dd.log
```

这些日志不会上传到 GitHub。

### `[AdventurePartyMonitor on`

开启最近小队的定时监控。

默认每 2 秒写一次快照。开启时会立刻写入一条 `monitor-start` 快照。

### `[AdventurePartyMonitor on 5`

开启最近小队监控，并把间隔设置为 5 秒。

间隔范围会被限制在 1 到 30 秒之间。

### `[AdventurePartyMonitor all on 2`

每 2 秒记录所有小队控制器的状态。

适合同时测试多支小队，但日志量会明显增加。

### `[AdventurePartyMonitor off`

关闭定时监控。

### `[AdventurePartyMonitor status`

查看监控是否开启、当前监控范围、记录间隔和日志路径。

### `[AdventurePartyMonitor once`

立即写入最近小队的一次快照，不开启定时器。

`check` 是 `once` 的别名:

```text
[AdventurePartyMonitor check
```

### `[AdventurePartyMonitor all once`

立即为所有小队写入一次快照。

### `[AdventurePartyMonitor interval`

查看当前监控间隔。

### `[AdventurePartyMonitor interval 3`

把监控间隔改为 3 秒。如果监控正在运行，会用新间隔继续运行。

## 常用测试流程

### 只测试本地小队行为

```text
[AdventureParty
[AdventurePartyMonitor on 2
```

用途:

- 生成一队 NPC。
- 不强制开启额外 AI 决策层。
- 每 2 秒记录一次队伍状态，观察本地战斗脑、拉怪、恢复和复活。

### 测试离线 Mock AI

```text
[SpawnAIAdventureParty
[AdventurePartyAI status
[AdventurePartyMonitor on 2
```

用途:

- 快速生成一队并开启 AI 决策层。
- 默认 provider 是 `Mock`。
- 不需要网络服务。

### 切换到 HTTP 桥接 AI

先在服务器外启动桥接服务，然后在游戏中执行:

```text
[AdventurePartyAI endpoint http://127.0.0.1:8787/decide
[AdventurePartyAI http
[AdventurePartyAI on
[AdventurePartyAI status
```

用途:

- 让小队快照发送给本地桥接服务。
- 桥接服务再调用兼容 Chat Completions 的模型。
- ServUO 本地仍负责路径、距离、技能合法性和目标验证。

### 停止 AI 决策但保留小队

```text
[AdventurePartyAI off
```

用途:

- 停止 Mock/HTTP 决策请求。
- 继续观察本地控制器和战斗脑。

### 清理测试现场

```text
[AdventurePartyMonitor off
[ClearAdventureParties
```

用途:

- 停止日志。
- 删除所有小队控制器和残留队员。

## GM 属性查看

除了命令，也可以对隐藏控制器或队员使用 `[props` 查看属性。

控制器常用属性:

- `State`: 当前状态，例如 `Exploring`、`Engaging`、`Resting`、`Retreating`、`Wiped`。
- `MemberCount`: 队员数量。
- `WaypointIndex`: 当前巡逻点。
- `AIEnabled`: AI 决策层是否开启。
- `AIProvider`: `Mock` 或 `HttpEndpoint`。
- `AIRequestPending`: 是否正在等待 AI 返回。
- `LastAIStatus`: 最近 AI 状态。
- `AIRequests`: 请求次数。
- `AIDecisions`: 成功决策次数。
- `AIFailures`: 失败次数。
- `CurrentTactic`: 当前战术，例如 `FocusFire`、`ProtectHealer`、`PullToChokePoint`、`Recovering`。
- `TacticExpires`: 当前战术过期时间。
- `TacticTarget`: 当前战术目标说明。

队员常用属性:

- `Controller`: 所属小队控制器。
- `Role`: 队员角色，例如 `Fighter`、`Archer`、`Mage`、`Healer`。

## 注意事项

- `[ClearAdventureParties` 是全局清理命令，使用前确认不需要保留其他测试小队。
- HTTP 模式不要把 API key 写进 ServUO 配置或脚本，放在桥接服务环境变量里。
- 同时开启多队 HTTP AI 会增加请求量和日志量。
- 监控日志在 `Logs/AdventurePartyMonitor`，属于运行数据，不上传到 GitHub。
- 测试服稳定后，再考虑把相关改动从 `test-57.3` 合并到 `prod-57.3`。
