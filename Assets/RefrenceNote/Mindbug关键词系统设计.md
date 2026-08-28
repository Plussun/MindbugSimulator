# Mindbug 关键词系统设计

## 1. 设计目标

关键词用于修改游戏的基础规则，例如：

- 哪些卡牌可以阻挡本次攻击；
- 一张卡牌是否真的会被击败；
- 战斗双方最终谁会被击败；
- 一次攻击结束后是否继续攻击；
- 攻击者能否指定阻挡卡牌。

关键词和普通卡牌效果的职责不同：

```text
CardEffect：执行一次具体行为
例如抽牌、加血、增加战力、消灭卡牌

Keyword：修改某一条基础游戏规则
例如潜行限制阻挡、坚韧抵消击败、狂暴允许再次攻击
```

因此，不建议把所有关键词都实现成 `CardEffect.Resolve()`，也不建议为了完全数据驱动而建立一个包含大量空方法的通用关键词处理器。

当前项目只有少量核心关键词，最清晰的方案是：

> 将关键词判断集中在对应的规则节点中，并通过语义明确的小方法封装，避免直接堆进 `AttackDecision()` 和 `BlockDecision()` 的主流程。

---

## 2. 关键词数据

当前关键词使用 Flags 枚举保存：

```csharp
[System.Flags]
public enum Keywords
{
    None      = 0,
    Sneaky    = 1 << 0,
    Poisonous = 1 << 1,
    Tough     = 1 << 2,
    Frenzy    = 1 << 3,
    Hunter    = 1 << 4
}
```

`CardInstance` 中存在三层关键词：

```text
BaseKeywords：卡牌永久拥有的关键词
TempKeywords：光环等持续效果临时赋予的关键词
CurrentKeywords：当前实际生效的关键词
```

计算方式：

```csharp
CurrentKeywords = BaseKeywords | TempKeywords;
```

规则判断必须读取 `CurrentKeywords`，这样光环临时赋予的关键词才能生效。

建议在 `CardInstance` 中提供统一方法：

```csharp
public bool HasKeyword(Keywords keyword)
{
    return (CurrentKeywords & keyword) != 0;
}
```

以后规则代码统一写成：

```csharp
if (card.HasKeyword(Keywords.Tough))
{
    // 处理坚韧
}
```

而不是在各处重复位运算，也不要直接检查 `CardData.CardKeywords`。

---

## 3. 关键词与运行时状态的区别

关键词只表示卡牌当前是否具有某种能力，不负责记录能力已经使用了几次。

例如：

```text
CurrentKeywords包含Tough：卡牌当前具有坚韧
ToughUsed为true：该卡牌的坚韧已经被使用

CurrentKeywords包含Frenzy：卡牌当前具有狂暴
AttacksThisTurn为1：该卡牌本回合已经攻击一次
```

建议在 `CardInstance` 中单独保存：

```csharp
public bool ToughUsed;
public int AttacksThisTurn;
```

不要通过删除 `CurrentKeywords` 中的 Tough 表示坚韧已经使用，因为：

- Tough 可能来自临时光环；
- 光环刷新会重新计算关键词；
- “是否拥有能力”和“能力是否已经使用”是两个不同概念；
- 客户端可能仍然需要显示卡牌具有 Tough，但已经横置。

---

## 4. 按规则节点处理关键词

五个关键词分别介入不同的规则节点：

| 关键词 | 规则节点 | 推荐处理位置 |
|---|---|---|
| Sneaky | 合法阻挡者筛选 | `CanBlock()` / `GetLegalBlockers()` |
| Hunter | 攻击目标和合法阻挡者筛选 | `AttackContext`、`CanBlock()` |
| Tough | 击败替代 | `DefeatCard()` |
| Poisonous | 战斗结果计算 | `ResolveCombat()` |
| Frenzy | 攻击结束后的流程续接 | `FinishAttack()` / `CanAttackAgain()` |

主流程只负责依次调用这些规则方法，不直接包含所有关键词细节。

推荐的攻击流程：

```text
AttackDecision
    ↓
验证攻击者和攻击阶段
    ↓
处理攻击效果
    ↓
Hunter选择目标（如果需要）
    ↓
生成合法阻挡列表
    ↓
BlockDecision
    ↓
验证选择的阻挡卡是否合法
    ↓
计算战斗结果
    ↓
调用DefeatCard处理需要被击败的卡牌
    ↓
结算阵亡效果并刷新光环
    ↓
检查Frenzy是否继续攻击
    ↓
继续攻击或切换回合
```

---

## 5. Sneaky：限制合法阻挡者

Sneaky 不直接产生效果，而是修改“谁可以阻挡”的规则。

推荐统一通过以下方法判断：

```csharp
public bool CanBlock(
    CardInstance attacker,
    CardInstance blocker,
    CardInstance hunterTarget)
{
    if (hunterTarget != null && blocker != hunterTarget)
    {
        return false;
    }

    if (attacker.HasKeyword(Keywords.Sneaky) &&
        !blocker.HasKeyword(Keywords.Sneaky))
    {
        return false;
    }

    return true;
}
```

服务器可以用它生成所有合法阻挡者：

```csharp
public List<CardInstance> GetLegalBlockers(
    int defendingPlayerID,
    CardInstance attacker,
    CardInstance hunterTarget)
{
    return State.Players[defendingPlayerID].Field.FindAll(
        blocker => CanBlock(attacker, blocker, hunterTarget));
}
```

客户端可以根据服务器同步的信息限制高亮和点击，但服务器仍然必须再次验证，不能只依赖客户端界面。

---

## 6. Hunter：指定阻挡目标

Hunter 会给一次攻击增加额外上下文：攻击者指定某张敌方卡牌参与阻挡。

测试阶段可以暂时在 `GameState` 中保存：

```csharp
public CardInstance PendingHunterTargetInstance;
```

但攻击相关状态继续增加后，建议合并为：

```csharp
[System.Serializable]
public class AttackContext
{
    public CardInstance Attacker;
    public CardInstance Blocker;
    public CardInstance HunterTarget;
    public int AttackNumber;
}
```

然后在 `GameState` 中保存：

```csharp
public AttackContext PendingAttack;
```

Hunter 目标应该由服务器验证：

- 目标必须存在；
- 目标必须位于对方场地；
- 目标必须符合正式规则要求；
- `BlockDecision()` 中只能接受该目标。

Hunter 和 Sneaky 同时存在时，必须根据正式规则明确优先级。例如 Hunter 是否可以强制非 Sneaky 卡牌阻挡 Sneaky 攻击，应当只在 `CanBlock()` 中定义一次，不能在多个流程中分别判断。

---

## 7. Tough：替代一次击败

Tough 应该放在 `DefeatCard()` 中处理，因为所有击败来源都应该经过这个入口：

- 普通战斗；
- Poisonous；
- 卡牌效果；
- 未来的特殊规则。

示例：

```csharp
public void DefeatCard(int playerID, int cardInstanceID)
{
    CardInstance card = FindFieldCard(playerID, cardInstanceID);

    if (card == null)
    {
        return;
    }

    if (card.HasKeyword(Keywords.Tough) && !card.ToughUsed)
    {
        card.ToughUsed = true;
        Debug.Log(card.CardData.CardName + "的坚韧抵消了本次击败");
        return;
    }

    MoveCardToDiscard(playerID, card);
}
```

注意：

- Tough 抵消击败时，卡牌没有离场，因此不能触发 OnDefeat；
- 真正进入弃牌堆时，才触发 OnDefeat；
- `ToughUsed` 是否在离场、复活或变形后重置，需要按照正式规则决定；
- 显示横置属于 View，根据 `ToughUsed` 更新，不应由 GameEngine 直接旋转对象。

---

## 8. Poisonous：修改战斗结果

Poisonous 应在 `ResolveCombat()` 中处理，而不是在选择阻挡时处理。

战斗双方是否被击败应该先一起计算，再执行状态修改：

```csharp
bool attackerDefeated =
    blocker.CurrentPower >= attacker.CurrentPower ||
    blocker.HasKeyword(Keywords.Poisonous);

bool blockerDefeated =
    attacker.CurrentPower >= blocker.CurrentPower ||
    attacker.HasKeyword(Keywords.Poisonous);
```

然后分别调用：

```csharp
if (attackerDefeated)
{
    DefeatCard(attackerOwnerID, attacker.CardInstanceID);
}

if (blockerDefeated)
{
    DefeatCard(blockerOwnerID, blocker.CardInstanceID);
}
```

必须先计算双方结果，原因是：

```text
先移除第一张卡
→ 场上光环刷新
→ 第二张卡战力发生变化
→ 使用变化后的战力重新判断
```

这会破坏“双方同时战斗”的规则。

先保存战斗结果，再依次执行 `DefeatCard()`，Tough 也会自然介入击败处理。

---

## 9. Frenzy：决定攻击是否结束

Frenzy 不应该直接写成“阻挡后跳过回合切换”，而应该统一询问攻击者是否还能再次攻击。

攻击开始时记录：

```csharp
attacker.AttacksThisTurn++;
```

统一判断：

```csharp
public bool CanAttackAgain(CardInstance attacker)
{
    if (!IsCardStillOnField(attacker))
    {
        return false;
    }

    if (!attacker.HasKeyword(Keywords.Frenzy))
    {
        return false;
    }

    return attacker.AttacksThisTurn < 2;
}
```

攻击结束后：

```csharp
if (CanAttackAgain(attacker))
{
    // 进入同一张卡的第二次攻击流程
}
else
{
    StartNextTurn(false);
}
```

判断时机应该位于战斗和相关效果全部结算之后：

```text
完成战斗
→ 结算阵亡效果
→ 刷新场上光环
→ 确认攻击者仍在场
→ 判断是否继续Frenzy攻击
```

否则攻击者可能已经被战斗或阵亡效果击败，却仍然开始第二次攻击。

每个新回合开始时，应重置相关的回合状态：

```csharp
card.AttacksThisTurn = 0;
```

---

## 10. 推荐的方法职责

可以先继续把这些方法放在 `GameEngine` 中，不需要立即增加大量类：

```text
AttackDecision()       组织攻击声明流程
GetLegalBlockers()     生成合法阻挡者
CanBlock()             判断单张卡能否阻挡
BlockDecision()        接收并验证阻挡决定
ResolveCombat()        计算战斗结果
DefeatCard()           处理Tough和真正的卡牌退场
CanAttackAgain()       判断Frenzy
FinishAttack()         继续攻击或切换回合
```

这样 GameEngine 中仍然存在关键词规则，但不会全部堆在一个方法中。

如果以后 `GameEngine` 过长，可以把纯判断逐步移动到 `CombatRules`：

```csharp
public static class CombatRules
{
    public static bool CanBlock(...);
    public static CombatResult CalculateCombatResult(...);
    public static bool CanAttackAgain(...);
}
```

`CombatRules` 尽量只计算并返回结果，真正修改 `GameState` 的操作仍然由 `GameEngine` 完成。

---

## 11. 与效果队列的关系

关键词判断和效果队列不能完全互相替代。

事件队列适合：

```text
入场后抽牌
攻击时增加战力
阵亡后回复生命
失去生命后触发其他效果
```

关键词规则需要同步返回答案：

```text
Sneaky：这张卡能否阻挡？
Tough：本次击败是否被取消？
Poisonous：战斗结果是什么？
Hunter：本次攻击允许选择哪些目标？
Frenzy：攻击流程是否结束？
```

因此推荐流程是：

```text
GameEngine进入规则节点
→ 使用关键词规则计算结果
→ GameEngine执行原子状态修改
→ 将产生的卡牌效果加入效果队列
→ 队列清空后继续攻击或回合流程
```

---

## 12. 未来关键词增加时如何扩展

当前只有五个关键词时，不建议建立复杂的注册系统。直接按照规则节点封装最容易阅读和调试。

当未来出现十几种以上关键词，并且同一规则节点出现大量分支时，可以按规则能力拆分接口：

```csharp
public interface IBlockRestriction
{
    bool CanBlock(AttackContext context, CardInstance blocker);
}

public interface IDefeatReplacement
{
    bool PreventDefeat(GameEngine gameEngine, CardInstance card);
}

public interface ICombatModifier
{
    void ModifyCombatResult(CombatContext context);
}

public interface IAfterCombatRule
{
    void AfterCombat(GameEngine gameEngine, CombatContext context);
}
```

不建议建立一个包含所有钩子的巨大接口：

```csharp
public interface IKeywordHandler
{
    void BeforeAttack();
    bool CanBlock();
    void BeforeDefeat();
    void AfterDefeat();
    void AfterCombat();
    void OnTurnStart();
}
```

因为每个关键词通常只使用其中一个规则节点，最终会产生大量空方法，并增加关键词之间的执行顺序问题。

---

## 13. 当前阶段的推荐实施顺序

1. 在 `CardInstance` 中加入 `HasKeyword()`。
2. 加入 `ToughUsed` 和 `AttacksThisTurn`。
3. 提取 `CanBlock()` 和 `GetLegalBlockers()`，先实现 Sneaky。
4. 在 `DefeatCard()` 中实现 Tough。
5. 提取 `ResolveCombat()`，同时计算双方结果并实现 Poisonous。
6. 提取 `FinishAttack()` 和 `CanAttackAgain()`，实现 Frenzy。
7. 最后加入 Hunter 目标选择，并考虑建立 `AttackContext`。
8. 所有关键词逻辑在服务器端再次验证，客户端只负责展示可选目标。
9. 等关键词数量和交互明显增多后，再决定是否拆出 `CombatRules` 或关键词规则接口。

---

## 14. 最终原则

```text
CardData                 保存基础关键词
CardInstance             保存当前关键词和使用状态
CardEffect               执行数据驱动的一次性效果
FieldEffect              重新计算持续光环修正
GameEngine               执行状态修改并组织游戏流程
关键词规则方法           修改合法性、战斗、击败和流程续接规则
EffectQueue              控制触发效果的执行顺序
NetworkController        传输玩家请求和服务器结果
ViewController           根据状态显示关键词及运行时状态
```

关键词被写在规则代码中并不等于硬编码失控。只要每个关键词位于正确的规则节点，主流程只调用语义明确的方法，这种实现就具有良好的可读性和扩展性。
