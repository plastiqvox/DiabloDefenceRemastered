using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;

namespace DiabloDefenceRemastered.Core.Sim.Enemies;

/// <summary>
/// 정의 + 접두사 조합 하나의 부품 표 (설계서 6.6 "슬롯 수"·"페이즈"). 처음 쓰일 때 한 번 만들고(<see cref="World.KitFor"/>) 같은 조합의 적이 같이 쓴다.
/// 슬롯 순서 = 정의 <c>behaviors</c> → 패턴 합집합(정의 <c>patterns</c>, 그다음 페이즈가 더한 ID 순) → 접두사 부품 → 시퀀서(순서가 있으면 하나). 12 를 넘으면 예외(로딩 검사가 먼저 막는다).
/// 페이즈 전환은 "지금 쓰는 목록 번호"(<see cref="Enemy.PhaseIdx"/>) 하나만 바꾼다 — 슬롯(쿨다운)은 같은 패턴 ID 에 남는다.
/// v0.9.4(6.20 ③): 어느 페이즈 순서(<c>sequence</c>)에든 든 패턴 ID 는 슬롯·능력 목록에 넣지 않고, 페이즈마다 <see cref="PatternSequence"/> 하나가
/// 그 페이즈 완성 목록에서 칸 패턴을 찾아 갖는다. 모든 페이즈의 시퀀서가 맨 뒤 슬롯 하나(<see cref="SequencerSlot"/>)를 나눠 쓴다.
/// </summary>
public sealed class EnemyKit
{
    private EnemyKit(EnemyDef def, int defIndex)
    {
        Def = def;
        DefIndex = defIndex;
    }

    public EnemyDef Def { get; }
    public int DefIndex { get; }
    /// <summary>슬롯 수.</summary>
    public int Count { get; private set; }
    /// <summary>[페이즈][슬롯] 부품(그 페이즈에 없는 패턴 슬롯은 null). 페이즈 0 = 정의 <c>patterns</c>.</summary>
    public IEnemyBehavior?[][] Phases { get; private set; } = [];
    /// <summary>[페이즈] 능력 슬롯 번호(슬롯 순서 — 위쪽이 먼저).</summary>
    public int[][] Abilities { get; private set; } = [];
    public int[] Positioning { get; private set; } = [];
    public int[] Auras { get; private set; } = [];
    public int[] SpawnHooks { get; private set; } = [];
    public int[] DeathHooks { get; private set; } = [];
    public int[] HitModifiers { get; private set; } = [];
    /// <summary>맞음 훅 슬롯(번개 강화, 2026-11-14).</summary>
    public int[] HurtHooks { get; private set; } = [];
    /// <summary>단일 대상 근접 능력의 가장 긴 사거리 여분(두 반경 위, px). 없으면 0 — <c>TouchReach</c>.</summary>
    public float MeleeReachOver { get; private set; }
    /// <summary>정의 페이즈 수 + 1.</summary>
    public int PhaseCount => Phases.Length;
    /// <summary>시퀀서가 나눠 쓰는 슬롯(−1 = 순서 없음, 6.20 ③).</summary>
    public int SequencerSlot { get; private set; } = -1;

    /// <summary>지금 페이즈의 슬롯 부품.</summary>
    public IEnemyBehavior? Part(int phase, int slot) => Phases[Math.Clamp(phase, 0, Phases.Length - 1)][slot];

    /// <summary>정의 + 접두사로 부품 표를 만든다.</summary>
    public static EnemyKit Build(EnemyDef def, int defIndex, IReadOnlyList<AffixDef> affixes)
    {
        var kit = new EnemyKit(def, defIndex);
        var fixedParts = new List<IEnemyBehavior>();
        foreach (var pd in def.Behaviors) fixedParts.Add(Wrap(pd));

        // 패턴 합집합: ID 가 없는 패턴은 자기 자리 하나(같은 페이즈 목록 안의 위치로 구별). 순서 패턴(어느 페이즈 순서에든 든 ID)은 슬롯을 쓰지 않는다
        var keys = new List<string>();
        var phaseLists = new List<IReadOnlyList<PatternDef>> { def.Patterns };
        // 페이즈 완성 목록은 로더가 채운다(6.5). 비었으면(패턴을 다 지운 페이즈가 아니라 채우지 않은 시험 정의) 앞 페이즈 목록 그대로
        foreach (var ph in def.Phases) phaseLists.Add(ph.Patterns.Count > 0 || ph.Remove.Count > 0 ? ph.Patterns : phaseLists[^1]);
        var sequences = new List<SequenceDef?> { def.Sequence };
        foreach (var ph in def.Phases) sequences.Add(ph.Sequence);
        var sequenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in sequences)
            if (s is not null)
                foreach (var step in s.Steps) sequenced.Add(step.Pattern);
        var hasSequence = sequences.Exists(s => s is { Steps.Count: > 0 });
        foreach (var list in phaseLists)
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id is { } id && sequenced.Contains(id)) continue;
                var key = KeyOf(list[i], i);
                if (!keys.Contains(key)) keys.Add(key);
            }

        var affixParts = new List<IEnemyBehavior>();
        foreach (var a in affixes)
            foreach (var pd in a.Behaviors) affixParts.Add(Wrap(pd));

        var count = fixedParts.Count + keys.Count + affixParts.Count + (hasSequence ? 1 : 0);
        if (count > PartSlots.SlotCount)
            throw new InvalidOperationException($"적 '{def.Id}' 의 부품 슬롯 {count} > {PartSlots.SlotCount}(6.6)");
        kit.Count = count;
        kit.SequencerSlot = hasSequence ? count - 1 : -1;

        var phases = new IEnemyBehavior?[phaseLists.Count][];
        for (var p = 0; p < phaseLists.Count; p++)
        {
            var row = new IEnemyBehavior?[count];
            for (var i = 0; i < fixedParts.Count; i++) row[i] = fixedParts[i];
            var list = phaseLists[p];
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id is { } id && sequenced.Contains(id)) continue;
                row[fixedParts.Count + keys.IndexOf(KeyOf(list[i], i))] = new PatternAbility(list[i]);
            }
            for (var i = 0; i < affixParts.Count; i++) row[fixedParts.Count + keys.Count + i] = affixParts[i];
            if (hasSequence && sequences[p] is { Steps.Count: > 0 } seq) row[count - 1] = Sequencer(def, p, seq, list);
            phases[p] = row;
        }
        kit.Phases = phases;

        var abilities = new int[phases.Length][];
        for (var p = 0; p < phases.Length; p++)
        {
            var list = new List<int>();
            for (var s = 0; s < count; s++)
                if (phases[p][s] is IAbilityPart) list.Add(s);
            abilities[p] = [.. list];
        }
        kit.Abilities = abilities;
        kit.Positioning = Indices<IPositioningPart>(phases[0], count);
        kit.Auras = Indices<IAuraPart>(phases[0], count);
        kit.SpawnHooks = Indices<ISpawnHookPart>(phases[0], count);
        kit.DeathHooks = Indices<IDeathHookPart>(phases[0], count);
        kit.HitModifiers = Indices<IHitModifierPart>(phases[0], count);
        kit.HurtHooks = Indices<IHurtHookPart>(phases[0], count);

        var reach = 0f;
        foreach (var part in phases[0])
            if (part is IMeleeReach m) reach = MathF.Max(reach, m.ReachOverContact);
        kit.MeleeReachOver = reach;
        return kit;
    }

    private static string KeyOf(PatternDef p, int index) => p.Id ?? "#" + index;

    /// <summary>페이즈 하나의 시퀀서: 칸마다 그 페이즈 완성 목록에서 같은 ID 패턴(없으면 예외 — 로딩 검사가 먼저 막는다, 6.20 ③).</summary>
    private static PatternSequence Sequencer(EnemyDef def, int phase, SequenceDef seq, IReadOnlyList<PatternDef> list)
    {
        var steps = new PatternAbility[seq.Steps.Count];
        for (var i = 0; i < steps.Length; i++)
        {
            var id = seq.Steps[i].Pattern;
            PatternDef? found = null;
            foreach (var p in list)
                if (string.Equals(p.Id, id, StringComparison.Ordinal)) { found = p; break; }
            if (found is null)
                throw new InvalidOperationException($"적 '{def.Id}' 페이즈 {phase} 순서 칸 {i} 의 패턴 '{id}' 가 그 페이즈 패턴 목록에 없다(6.20 ③)");
            steps[i] = new PatternAbility(found);
        }
        return new PatternSequence(seq, steps);
    }

    private static int[] Indices<T>(IEnemyBehavior?[] row, int count)
    {
        var list = new List<int>();
        for (var s = 0; s < count; s++)
            if (row[s] is T) list.Add(s);
        return [.. list];
    }

    /// <summary>
    /// <c>behaviors</c> 원소 → 부품. <c>trigger</c> 가 있으면 동작(<see cref="PartKind.EnemyAction"/>)을 <see cref="TriggeredAction"/> 으로 감싼다(훅 어댑터, 6.6).
    /// </summary>
    public static IEnemyBehavior Wrap(PartDef pd)
    {
        if (pd.Trigger is { } trigger)
        {
            if (pd.Instance is not IEnemyAction action)
                throw new InvalidOperationException($"부품 '{pd.Type}' 에 trigger '{trigger}' 가 있는데 동작(EnemyAction)이 아니다");
            return TriggeredAction.Create(trigger, action);
        }
        return pd.Instance as IEnemyBehavior
            ?? throw new InvalidOperationException($"부품 '{pd.Type}' 의 객체가 적 행동 부품이 아니다({pd.Instance.GetType().Name})");
    }
}

/// <summary>단일 대상 근접 능력(chase_melee, radius 없는 melee_slam 패턴) — 닿는 거리 여분(두 반경 위).</summary>
public interface IMeleeReach
{
    float ReachOverContact { get; }
}
