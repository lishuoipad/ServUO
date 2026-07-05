# 测试服 57.3 更新说明


本文说明 `test-57.3` 测试服分支相对官方原版 `servuo-57.3-original` 的主要变化。

- 官方原版参考分支: `servuo-57.3-original`
- 正式服分支: `prod-57.3`
- 测试服分支: `test-57.3`
- GitHub 对比入口: <https://github.com/lishuoipad/ServUO/compare/servuo-57.3-original...test-57.3>

## 总览

测试服是在官方 ServUO 57.3 基础上扩展出来的实验分支。主要目标是验证 AI 冒险小队、复杂战斗 AI、远程怪压力处理、复活恢复流程、部分奖励和经济工具的改动。

这不是纯原版分支。它包含大量测试性质的 AI 行为、GM 测试命令和本地桥接工具，适合先在测试服验证，再决定哪些内容合并到正式服。

## 新增功能

### AI 冒险小队

新增一套四人 AI 冒险小队系统，核心文件在:

- `Scripts/Custom/AdventureParty/AdventureParty.cs`
- `Scripts/Custom/AdventureParty/AdventurePartyAI.cs`
- `Scripts/Custom/AdventureParty/AdventurePartyMonitor.cs`
- `Scripts/Custom/AdventureParty/CHANGELOG.zh-CN.md`

GM 命令的详细用途见 [NPC_PARTY_COMMANDS.md](NPC_PARTY_COMMANDS.md)。

主要能力:

- 生成由战士、弓手、法师、治疗者组成的小队。
- 支持探索、接怪、集火、拉怪、卡口、防守、撤退、恢复、复活等战术状态。
- 支持队形收拢，避免探索和撤退时队员被 waypoint 拉散。
- 支持战斗中监控队员状态、治疗动作、技能释放、最近战术原因。
- 新增 GM 命令:
  - `[AdventureParty`
  - `[SpawnAdventureParty`
  - `[SpawnAIAdventureParty`
  - `[ClearAdventureParties`
  - `[AdventurePartyAI ...`

### 高级战斗 AI

新增 `AdvancedCombatBrain`，用于给 AI 小队成员提供更接近玩家习惯的战斗执行层。

核心文件:

- `Scripts/Custom/AdvancedCombatAI/AdvancedCombatBrain.cs`

主要能力:

- 战士支持 Sampire 风格战斗逻辑，包括武器切换、爆发、减压和拉怪。
- 弓手支持 Discordance、Provocation 和远程输出配合。
- 法师支持 Necromancy、Spellweaving、Wither 群怪输出和保命判断。
- 治疗者支持绷带治疗、魔法治疗、移动靠近治疗目标、危险时先自保。
- AI 在撤退、恢复、复活等状态下会停火和清空目标，避免被本地战斗脑重新拉回战斗。

### AI 决策桥接工具

新增本地 AI 桥接工具，允许 ServUO 把小队快照发给本地 HTTP 服务，再由本地服务调用兼容 Chat Completions 的模型。

相关文件:

- `Config/AdventurePartyAI.cfg`
- `Tools/AdventurePartyAI/README.md`
- `Tools/AdventurePartyAI/adventure_party_ai_bridge.py`
- `Tools/AdventurePartyAI/start_bailian_bridge.example.ps1`

设计原则:

- ServUO 默认使用 `Mock` 离线模式。
- HTTP 模式默认请求 `http://127.0.0.1:8787/decide`。
- API key 不写入 ServUO 配置，也不进入存档。
- 模型只给出小队级命令，路径、距离、技能是否合法、目标是否合法仍由 ServUO 本地逻辑负责。

允许的网络 AI 命令包括:

- `focus_fire`
- `protect_healer`
- `pull_to_choke_point`
- `break_pursuit`
- `recover`

### Sovereign Voucher 商人

新增 Sovereign 点数兑换券和商人。

核心文件:

- `Scripts/Custom/SovereignVendor/SovereignVendor.cs`

主要能力:

- 新增 `SovereignVoucher1`、`SovereignVoucher10`、`SovereignVoucher100`。
- 玩家购买后，兑换券绑定购买账号。
- 双击兑换券可把 sovereign 点数存入玩家账户。
- 使用 `Saves/Misc/SovereignVoucherLedger.bin` 记录已兑换 key，避免重复兑换。
- 新增 NPC: `Sovereign Voucher Broker`。

## AI 与战斗机制调整

### 远程压力与撤退

测试服大幅强化 AI 小队面对远程怪、法系怪、弓系怪和龙息类怪物时的撤退逻辑。

主要变化:

- 多个远程压力怪能看到或锁定小队时，优先进入 `RangedDisengage`。
- 撤退时清空 `Combatant` 并退出 `Warmode`，减少边跑边反向接怪。
- 撤退点不只选择远处，也会评估近距离拐角、断视野点和路线风险。
- 路线附近存在 dragon、法系、弓系等威胁时会降低该路线评分。
- 如果撤退路线卡住，会短时间拉黑当前撤退点并重新选方向。
- 撤退后如果只剩少量可清理目标，小队会转回 `PowerUp` 反打。

### 龙息窗口反打

测试服加入对龙息爆发窗口的识别和反打逻辑。

主要变化:

- 观察队员短时间大额掉血，推断附近 dragon breath 类敌人刚进入冷却。
- 在安全条件满足时，利用龙息冷却窗口转入 `PowerUp` 集中输出。
- 同一个龙息窗口避免反复触发同一轮反打命令。

### 拉怪与卡口

测试服强化 `PullToChokePoint`、`HoldChokePoint`、`BreakPursuit` 等战术。

主要变化:

- 战士低血且被多个目标追击时，会更早进入减压拉怪。
- 拉怪过量时强制 reset，避免小队和整包怪硬顶。
- 卡口击杀阶段，战士不会在同一轮既追目标又被拉回队形点。
- 对残血孤立目标，小队会更积极结束拉怪并反打收割。

### 复活与恢复

测试服对复活后的恢复阶段做了专门处理。

主要变化:

- 支持移动复活，复活者和幽灵会一起向更安全的位置移动。
- 复活者被贴身或被远程压力锁定时，会先脱离威胁再尝试复活。
- 队员复活后进入短暂 `Post-resurrection recovery`，先回血、收队形、拉开威胁。
- 没有活着的复活者时，幸存队员会真正尝试脱战，而不是继续普通输出。

### 治疗与绷带

高级战斗 AI 改进了治疗者行为。

主要变化:

- 治疗者自身危险时先绕战士或拉开距离，再继续支援。
- 战士重压时，治疗者会更早尝试绷带，并在需要时靠近到绷带范围。
- 魔法治疗仍保留，但避免只依赖 Greater Heal 造成治疗节奏断档。
- 移动和压力场景中会更谨慎使用 Spirit Speak，避免因为技能动作打断逃跑。

### 法师群怪输出

测试服让法师在安全条件满足时更积极使用 Wither。

主要变化:

- 战士附近聚集多个怪物时，法师会寻找安全 Wither 点。
- 队友处于紧急濒死状态时仍优先救人。
- Wither 点位会评估敌人数量、距离、队友安全和自身站位风险。

## 奖励与玩法调整

### Despise 奖励

修改文件:

- `Scripts/Items/Artifacts/DespiseArtifacts.cs`

变化:

- `DespicableQuiver` 设置为 Blessed。
- `UnforgivenVeil` 设置为 Blessed。
- `HailstormHuman` 和 `HailstormGargoyle` 新增随机 Super Slayer。

### Void Pool 奖励

修改文件:

- `Scripts/Services/Revamped Dungeons/Covetous Void Spawn/Items/VoidPoolRewards.cs`

变化:

- 随机 artifact 掉落池从 5 项扩展为 9 项。
- 增加 Gargish 版本奖励:
  - `GargishPrismaticLenses`
  - `GargishBrightblade`
  - `GargishBlightOfTheTundra`
  - `GargishHephaestus`

### BOD 大宗订单

修改文件:

- `Scripts/Services/BulkOrders/BulkOrderSystem.cs`

变化:

- `MaxCachedDeeds` 从 `2` 调整为 `47`。
- `Delay` 从 `6` 小时调整为 `1` 小时。

这会明显提高测试服上大宗订单获取和缓存节奏。

### Paragon 判定修正

修改文件:

- `Scripts/Services/Paragon.cs`

变化:

- `CheckConvert` 新增 `!bc.CanBeParagon` 判断。
- 目的: 避免 `CanBeParagon == false` 的生物仍进入隐藏 Paragon 状态。

这与之前测试服上 Swoop / ML named 类型怪物的 Paragon 状态验证有关。

### Despise Discordance 生物与宠物训练

相关文件:

- `Scripts/Skills/Discordance.cs`
- `Scripts/Mobiles/Normal/BaseCreature.cs`
- `Scripts/Mobiles/Normal/DespiseGoodCreatures.cs`
- `Scripts/Mobiles/Normal/DespiseEvilCreatures.cs`

变化:

- Despise 中带 `MagicalAbility.Discordance` 的生物，例如 `Silenii` 和 `Phantom`，现在可以更可靠地执行 Discordance。
- `CanDiscord` 的 BaseCreature 使用 Discordance 时，不再强制走普通玩家 Musicianship 检查。
- `BaseCreature.CheckInstrument` 在需要 bard 技能但没有背包时，会自动补一个背包并放入不可移动的 exceptional harp。
- 这避免了 Despise 阵营怪或训练宠物因为没有玩家式背包/乐器流程而无法稳定释放 Discordance。

### 其他小调整

修改文件:

- `Scripts/Mobiles/Normal/SabertoothedTiger.cs`

变化:

- 名称从 `saber-toothed tiger` 调整为 `a sabre-toothed tiger`。
- 尸体名同步调整为 `a sabre-toothed tiger corpse`。

## 配置变化

### 账号

修改文件:

- `Config/Accounts.cfg`

变化:

- `AccountsPerIp` 从 `1` 调整为 `10`。

### 服务器名称

修改文件:

- `Config/Server.cfg`

变化:

- shard 名称从 `My Shard` 调整为 `LISHUO Shard`。

### 客户端数据路径

修改文件:

- `Config/DataPath.cfg`

变化:

- 设置测试服使用本地 `Publish.117` 客户端数据路径。

## 删除或排除的内容

### Reports 旧服务文件

测试服相对原版删除了 `Scripts/Services/Reports` 下的一批旧报告服务文件，包括图表、报表对象、持久化和 HTML 渲染相关代码。

删除目录包括:

- `Scripts/Services/Reports/Objects/Charts`
- `Scripts/Services/Reports/Objects/Reports`
- `Scripts/Services/Reports/Objects/Snapshots`
- `Scripts/Services/Reports/Objects/Staffing`
- `Scripts/Services/Reports/Persistence`
- `Scripts/Services/Reports/Rendering`

### 未上传的运行数据

GitHub 仓库没有上传以下运行时或备份内容:

- `Saves`
- `Backups`
- `Logs`
- `Scripts/Output`
- `Server/bin`
- `Server/obj`
- `Scripts/bin`
- `Scripts/obj`
- `Ultima/bin`
- `Ultima/obj`
- `.zip`
- `.pdb`
- `ServUO.exe`
- Python `__pycache__` 和 `.pyc`

## 建议使用方式

建议继续保持三分支结构:

- `servuo-57.3-original`: 只作为官方原版参考，不修改。
- `test-57.3`: 所有实验性功能先放这里测试。
- `prod-57.3`: 只合并已经验证稳定的测试服改动。

如果测试服某个功能确认稳定，可以从 GitHub 创建 Pull Request:

```text
base: prod-57.3
compare: test-57.3
```

合并前建议重点检查 AI 小队是否会影响正式服性能、是否会生成过多日志、是否会把测试配置误带入正式服。
