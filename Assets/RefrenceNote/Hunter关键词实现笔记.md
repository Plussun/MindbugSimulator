# Hunter 关键词实现笔记

## 1. Hunter 为什么比普通关键词复杂

Hunter 不是一次简单的数值修改，它会同时影响：

- 攻击方选择攻击牌时的操作流程；
- 攻击方是否可以继续选择敌方目标；
- 客户端提交给服务器的攻击请求；
- 服务器对攻击牌和目标牌的合法性检查；
- 等待阻挡期间保存的游戏状态；
- 防守方是否可以选择“不阻挡”；
- 哪张生物能够参与阻挡；
- 双方客户端对攻击目标的显示；
- 战斗结束和游戏结束时的状态清理。

因此，Hunter 的实现会横跨 View、Network 和 GameEngine。代码涉及的文件较多并不一定表示设计错误，关键是每一层只承担自己的职责。

当前项目采用的核心思路是：

> 客户端先在本地选择攻击牌和 Hunter 目标，点击 Attack 后一次性提交两个实例 ID；服务器验证并保存目标，然后把确认后的攻击状态同步给双方。

当前阶段不需要为了 Hunter 单独增加游戏阶段，也不需要先建立通用 Choice 系统。

---

## 2. 各层职责

| 层级 | 主要职责 | 不应该负责的内容 |
| --- | --- | --- |
| `CardData` / `CardInstance` | 保存基础关键词和当前生效关键词 | 保存某一次攻击选择的目标 |
| `CardView` | 保存当前展示数据，显示 Selected、Aimed 等效果 | 决定目标是否合法 |
| `ViewController` | 处理本地点击、临时选择和按钮显示 | 最终裁定攻击是否合法 |
| `NetworkController` | 传递攻击意图、转换和同步网络数据 | 自己修改游戏规则状态 |
| `GameState` | 保存服务器已确认的攻击牌和 Hunter 目标 | 保存客户端尚未提交的临时选择 |
| `GameEngine` | 验证请求、修改状态、判断合法阻挡 | 直接操作界面对象 |

最重要的原则是：

> 客户端负责让操作方便，服务器负责保证操作正确。

即使客户端只允许 Hunter 选择目标，服务器仍然必须再次检查攻击牌是否真的具有 Hunter。

---

## 3. Hunter 涉及的几种数据

### 3.1 卡牌当前关键词

Hunter 的判断应该读取 `CardInstance.CurrentKeywords`，而不是只读取 `CardData.CardKeywords`。

原因是当前项目允许光环等效果临时添加或移除关键词：

```text
BaseKeywords + TempKeywords = CurrentKeywords
```

服务器同步卡牌时，将当前关键词转换成 `int`：

```csharp
keywords = (int)card.CurrentKeywords;
```

客户端生成 `CardView` 时再转回枚举：

```csharp
CurrentKeywords = (Keywords)currentKeywords;
```

这份客户端关键词只用于显示和决定是否开放目标选择。服务器不能信任它。

### 3.2 客户端临时目标 `targetCard`

`ViewController.targetCard` 表示：

> 攻击方已经在本地点击，但尚未提交给服务器的目标卡牌视图。

它具有以下特点：

- 类型是 `CardView`；
- 只存在于本地客户端；
- 可以随时取消或切换；
- 只负责控制本地 Aimed 显示；
- 刷新界面后旧的 `CardView` 会被销毁，因此不能作为长期状态保存；
- 不能作为服务器规则判断的依据。

### 3.3 服务器目标 `PendingHunterTargetCardInstance`

`GameState.PendingHunterTargetCardInstance` 表示：

> 服务器已经验证并接受的本次 Hunter 攻击目标。

它具有以下用途：

- 在等待阻挡阶段限制阻挡对象；
- 判断防守方是否能够选择不阻挡；
- 同步给双方客户端显示；
- 在本次攻击结束后清空。

### 3.4 网络目标 `pendingTarget`

`pendingTarget` 是服务器目标转换后的 `CardNetworkState`。

它不是本地临时选择，而是服务器状态的客户端快照。双方客户端都可以使用它：

- 在对应卡牌上显示 Aimed；
- 决定是否显示 NoBlock 按钮；
- 显示攻击日志或提示文字。

这三种目标不能混用：

```text
targetCard
    = 提交前的本地临时选择

PendingHunterTargetCardInstance
    = 服务器中的真实状态

pendingTarget
    = 真实状态同步到客户端后的数据
```

---

## 4. 完整执行流程

### 4.1 客户端选择攻击牌

玩家点击本方场上卡牌后：

1. 将卡牌保存为 `selectedCard`；
2. 显示 Selected 状态；
3. 显示 Attack 按钮；
4. 检查该卡牌的客户端 `CurrentKeywords`；
5. 如果具有 Hunter，为敌方场上卡牌绑定 `TargetDecision`。

Flags 枚举的判断必须带括号：

```csharp
if ((selectedCard.CurrentKeywords & Keywords.Hunter) != 0)
{
    // 开放目标选择
}
```

错误写法：

```csharp
if (selectedCard.CurrentKeywords & Keywords.Hunter != 0)
```

因为比较运算和位运算的结合顺序会导致表达式无法按预期计算。

### 4.2 客户端选择目标

目标选择只改变本地界面，不需要立即发送 RPC。

合理的操作规则是：

- 没有目标时点击 A：选择 A；
- 已选择 A 时再次点击 A：取消 A；
- 已选择 A 时点击 B：取消 A 并直接选择 B。

示例：

```csharp
public void TargetDecision(CardView cardView)
{
    if (targetCard == null)
    {
        targetCard = cardView;
        targetCard.SetAimed(true);
    }
    else if (targetCard == cardView)
    {
        targetCard.SetAimed(false);
        targetCard = null;
    }
    else
    {
        targetCard.SetAimed(false);
        targetCard = cardView;
        targetCard.SetAimed(true);
    }
}
```

### 4.3 客户端提交攻击

点击 Attack 后，一次性提交：

- 攻击牌实例 ID；
- 目标牌实例 ID；
- 没有目标时使用 `-1`。

```csharp
networkController.AttackDecisionRequest(
    selectedCard.CardInstanceID,
    targetCard != null ? targetCard.CardInstanceID : -1);
```

这样不需要为了询问“攻击牌是否有 Hunter”额外请求服务器，也不需要增加 `WaitingForChoice` 阶段。

客户端提交的是意图，不是直接修改状态的命令。

### 4.4 服务器验证攻击

服务器收到请求后的推荐检查顺序：

1. 当前是否为主要行动阶段；
2. 请求者是否为当前行动玩家；
3. 攻击牌是否确实存在于该玩家场上；
4. 先确认攻击牌存在，再访问它的 `CardData`；
5. 攻击牌是否具有当前生效的 Hunter；
6. 目标 ID 是否有效；
7. 目标是否存在于对方场上；
8. 验证成功后才写入 `PendingHunterTargetCardInstance`；
9. 进入等待阻挡阶段。

服务器不能仅根据“客户端传了目标 ID”就接受目标：

```csharp
if (State.PendingAttackCardInstance.HasKeyword(Keywords.Hunter)
    && targetCardInstanceID >= 0)
{
    State.PendingHunterTargetCardInstance =
        opponent.Field.Find(card =>
            card.CardInstanceID == targetCardInstanceID);
}
```

读取攻击牌信息之前必须先判空，否则错误或过期的实例 ID 可能造成 `NullReferenceException`。

### 4.5 服务器同步状态

服务器把以下数据一起同步给双方：

- `pendingAttack`；
- `pendingTarget`；
- 当前阶段；
- 当前等待操作的玩家；
- 双方公开场地信息。

双方收到相同的 `pendingTarget`，因此攻击方和防守方都能看到被 Hunter 指定的卡牌。

空目标使用统一哨兵值：

```csharp
CardInstanceID = -1;
```

判断是否有目标时应该写：

```csharp
bool hasHunterTarget = pendingTarget.CardInstanceID != -1;
```

不能写：

```csharp
pendingTarget.CardInstanceID != null
```

因为 `CardInstanceID` 是 `int` 值类型，不会是 `null`。将它和 `null` 比较不能表示“是否存在卡牌”，并可能让判断永远得到同一个结果。

### 4.6 防守方进行阻挡

如果服务器已经保存 Hunter 目标，需要处理两件事：

1. 防守方是否允许选择 NoBlock；
2. 防守方选择的阻挡牌是否就是 Hunter 目标。

当前最简单的规则是：存在有效 Hunter 目标时禁止不阻挡。

```csharp
if (!useBlock && State.PendingHunterTargetCardInstance != null)
{
    return;
}
```

同时在 `CanBlock()` 中限制其他卡牌：

```csharp
if (attackCard.HasKeyword(Keywords.Hunter)
    && targetCard != null
    && blockCard != targetCard)
{
    return false;
}
```

界面隐藏 NoBlock 只是操作提示，服务器拒绝非法请求才是真正规则保障。

---

## 5. Hunter 与其他关键词组合

关键词使用 Flags 枚举，因此一张卡可以同时具有 Hunter、Sneaky 等多个关键词。

规则检查不能在第一个关键词成立时直接返回 `true`，否则会跳过后续关键词。

错误思路：

```csharp
if (attackCard.HasKeyword(Keywords.Sneaky))
{
    return blockCard.HasKeyword(Keywords.Sneaky);
}
```

当攻击牌同时具有 Sneaky 和 Hunter 时，这会完全跳过 Hunter 判断。

更合适的写法是逐项排除非法情况：

```csharp
if (attackCard.HasKeyword(Keywords.Sneaky)
    && !blockCard.HasKeyword(Keywords.Sneaky))
{
    return false;
}

if (attackCard.HasKeyword(Keywords.Hunter)
    && targetCard != null
    && blockCard != targetCard)
{
    return false;
}

return true;
```

这种结构表达的是：

> 所有相关规则都通过以后，阻挡才合法。

未来加入更多限制阻挡的关键词时，也可以继续在 `CanBlock()` 中增加明确的非法条件。

---

## 6. 本地选择与同步状态不要混用

曾经出现过一种错误做法：刷新每张卡牌时，根据 `pendingTarget` 给 `targetCard` 赋值。

```csharp
if (pendingTarget.CardInstanceID == cards[i].CardInstanceID)
{
    targetCard = cardView;
}
else
{
    targetCard = null;
}
```

这个写法有两个问题。

第一，`targetCard` 原本表示提交前的本地临时选择，现在却被拿来表示服务器状态，职责发生混淆。

第二，界面会遍历很多张卡。即使目标牌匹配成功，后面遇到一张非目标牌时，`else` 又会把 `targetCard` 清空。最终结果取决于目标牌是不是最后一张被遍历的卡牌。

正确做法是：

- `targetCard` 只用于本地选择；
- 刷新卡牌时只根据 `pendingTarget` 开关 Aimed；
- 按钮直接根据 `pendingTarget.CardInstanceID` 判断，不经过 `targetCard`。

```csharp
cardView.SetAimed(
    pendingTarget.CardInstanceID == cards[i].CardInstanceID);
```

```csharp
bool hasHunterTarget = pendingTarget.CardInstanceID != -1;
NoBlockButton.gameObject.SetActive(!hasHunterTarget);
```

---

## 7. 必须清理的状态

Hunter 同时包含本地状态和服务器状态，两边都需要清理。

### 7.1 客户端需要清理的内容

以下情况需要取消 Selected、Aimed 和点击事件：

- 再次点击攻击牌，取消本次攻击选择；
- 点击 Attack，提交请求；
- 收到新的服务器状态并重建卡牌界面；
- 游戏进入其他阶段；
- 连接断开或重新开始游戏。

至少应清理：

```text
selectedCard
targetCard
AttackButton
敌方卡牌的 TargetDecision 点击回调
Selected 显示
Aimed 显示
```

由于当前界面刷新会销毁并重建全部 `CardView`，`RefreshView()` 开始时应将旧引用设为 `null`，不能继续使用已经销毁的对象。

### 7.2 服务器需要清理的内容

以下情况需要清空 `PendingHunterTargetCardInstance`：

- 一次攻击和阻挡完成；
- 进入下一回合；
- 不阻挡导致玩家死亡，方法提前返回；
- 其他效果使游戏直接结束；
- 重新初始化游戏。

不能只依赖 `StartNextTurn()` 清理，因为游戏结束分支可能在调用它之前直接 `return`。

---

## 8. “如果能够阻挡”的规则问题

当前简单实现只要存在 Hunter 目标，就禁止 NoBlock，并且只允许该目标阻挡。

当 Hunter 与其他阻挡规则组合后，需要进一步明确“目标是否能够阻挡”。例如：

- Hunter + Sneaky 指定了一张没有 Sneaky 的生物；
- 目标获得了“不能阻挡”的临时效果；
- Hunter 目标在 OnAttack 效果中离场；
- 目标被其他效果横置，而未来规则规定横置生物不能阻挡。

此时不能只判断 `PendingHunterTargetCardInstance != null`，还需要判断目标当前是否仍在场以及是否满足全部阻挡条件。

未来可以将判断收敛到一个方法中：

```csharp
bool MustHunterTargetBlock()
{
    CardInstance target = State.PendingHunterTargetCardInstance;

    return target != null
        && State.Players[State.ExpectedPlayerID].Field.Contains(target)
        && CanBlock(State.PendingAttackCardInstance, target, target);
}
```

然后服务器和界面都依据服务器计算出的结果处理 NoBlock。

当前测试阶段可以先假设玩家只能选择有效目标，但必须记住这是一个未来需要补充的规则边界。

---

## 9. 常见错误总结

### 错误一：客户端判断过了，服务器就不再判断

客户端数据和界面都可以被绕过。服务器必须重新确认阶段、玩家、攻击牌、关键词和目标归属。

### 错误二：先访问攻击牌，再检查攻击牌是否存在

错误实例 ID 会导致空引用。查找和判空必须发生在读取 `CardData` 之前。

### 错误三：空目标使用 `null` 判断实例 ID

`CardInstanceID` 是 `int`，当前协议使用 `-1` 表示不存在，因此必须和 `-1` 比较。

### 错误四：把本地 `targetCard` 当成服务器状态

本地选择会取消、对象会销毁，也不会自动同步。服务器状态必须使用 `pendingTarget` 表示。

### 错误五：在卡牌遍历的 `else` 中清空共享变量

共享变量最终会被最后一张卡决定，产生与卡牌顺序有关的错误。

### 错误六：关键词判断提前返回成功

Flags 允许多个关键词并存。规则判断应该逐项排除失败，最后统一返回成功。

### 错误七：只在正常回合结束时清理目标

游戏结束和异常分支可能提前返回，必须覆盖这些出口。

### 错误八：隐藏按钮就等于规则实现完成

按钮隐藏只是 View。服务器仍然必须拒绝伪造的 NoBlock 或非法阻挡请求。

---

## 10. 推荐测试清单

### 基础流程

- 普通卡攻击时不能选择 Hunter 目标；
- Hunter 卡可以选择敌方目标；
- 再次点击同一目标可以取消；
- 点击另一目标可以直接切换；
- 不选择目标时可以正常攻击；
- 点击 Attack 后双方都能看到同一个 Aimed 目标。

### 阻挡规则

- 有 Hunter 目标时，其他生物不能阻挡；
- 有有效 Hunter 目标时，防守方不能提交 NoBlock；
- 没有 Hunter 目标时，NoBlock 正常显示并可以使用；
- 客户端伪造非 Hunter 目标 ID 时，服务器不会保存目标；
- 客户端提交不存在或不属于对方的目标 ID 时，服务器不会保存目标。

### 组合关键词

- Hunter + Sneaky 时，两条规则都会执行；
- Hunter 目标不满足 Sneaky 条件时，按正式规则处理“如果能够阻挡”；
- Hunter 目标被临时增加或移除关键词后，读取的是 `CurrentKeywords`。

### 状态清理

- 取消攻击选择后，目标 Aimed 消失；
- 取消攻击选择后，敌方卡牌不能继续触发目标选择；
- 战斗结束后目标状态清空；
- 不阻挡导致游戏结束后目标状态清空；
- 下一次普通攻击不会沿用上一次 Hunter 目标；
- 下一次 Hunter 攻击不选择目标时，不会沿用旧目标；
- 界面重建后不会保留已经销毁的 `CardView` 引用。

### 双端显示

- Host 和 Client 看到相同的攻击牌；
- Host 和 Client 看到相同的 Hunter 目标；
- 只有需要操作的防守方显示阻挡操作；
- NoBlock 的显示由服务器同步状态决定，而不是由本地点击状态决定。

---

## 11. 当前方案何时需要升级

当前只有 Hunter 一种攻击前选目标效果时，使用：

```text
selectedCard + targetCard
PendingAttackCardInstance + PendingHunterTargetCardInstance
```

是足够清晰的。

当未来出现以下情况时，再考虑引入通用 `PendingChoice` 或 `AttackContext`：

- 一次效果需要选择多张牌；
- 可以同时选择卡牌、玩家或弃牌区对象；
- 选择需要分成多个连续步骤；
- 多个关键词都需要给一次攻击附加额外数据；
- OnAttack 效果会改变目标并需要继续结算；
- 断线重连后必须恢复尚未完成的选择过程。

可能的未来结构：

```csharp
public class AttackContext
{
    public CardInstance Attacker;
    public CardInstance HunterTarget;
    public CardInstance Blocker;
}
```

但在当前最小测试阶段，没有必要提前重构。先让 Hunter 的完整流程稳定、可读、可调试，再根据真实需求扩展。

---

## 12. 最终记忆方式

Hunter 的实现可以记成一条完整的数据链：

```text
客户端读取攻击牌 CurrentKeywords
    ↓
开放本地目标选择并显示 Aimed
    ↓
点击 Attack，一次提交攻击牌 ID 和目标 ID
    ↓
服务器验证阶段、玩家、攻击牌、Hunter 和目标归属
    ↓
服务器保存 PendingHunterTargetCardInstance
    ↓
同步 pendingAttack 和 pendingTarget 给双方
    ↓
防守方只能按服务器规则阻挡
    ↓
战斗或游戏结束时清理本地状态与服务器状态
```

Hunter 制作中最重要的三个注意点是：

1. 本地选择不是游戏真实状态；
2. 所有规则必须由服务器最终验证；
3. 每次流程结束都必须清理临时状态。
