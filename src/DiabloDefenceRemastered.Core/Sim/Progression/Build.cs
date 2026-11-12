using System.Globalization;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Weapons;

namespace DiabloDefenceRemastered.Core.Sim.Progression;

/// <summary>후보 카드 하나(뽑기 전). <paramref name="Weight"/> = 가중.</summary>
internal readonly record struct Candidate(CardKind Kind, string Id, float Weight);

/// <summary>
/// 빌드 한 곳(기획 4.4·7·11.4): 칸 찾기·후보 거르기(<c>classes</c>·<c>requires</c>·특성·축복·진화 결과 무기)·카드 만들기·카드 적용·<c>단련</c>·진화.
/// 모든 판단은 열린 칸(<c>Hero.WeaponSlotsOpen</c>·<c>PassiveSlotsOpen</c>) 안에서 한다.
/// </summary>
internal static class Build
{
    // ── 칸 ───────────────────────────────────────────────────────

    public static int FirstEmptyWeapon(Hero hero)
    {
        for (var i = 0; i < Math.Min(hero.WeaponSlotsOpen, WeaponSlots.SlotCount); i++)
            if (hero.Weapons[i].IsEmpty) return i;
        return -1;
    }

    public static int FirstEmptyPassive(Hero hero)
    {
        for (var i = 0; i < Math.Min(hero.PassiveSlotsOpen, PassiveSlots.SlotCount); i++)
            if (hero.Passives[i].IsEmpty) return i;
        return -1;
    }

    public static int WeaponSlotOf(Hero hero, string id)
    {
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
            if (hero.Weapons[i].Def?.Id == id) return i;
        return -1;
    }

    public static int PassiveSlotOf(Hero hero, string id)
    {
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
            if (hero.Passives[i].Def?.Id == id) return i;
        return -1;
    }

    /// <summary>진화 아닌(레벨 표가 있는) 가진 무기 칸.</summary>
    public static bool Upgradable(in WeaponSlot s) => s.Def is { BaseWeapon: null } d && !s.Evolved && s.Level < d.MaxLevel;

    public static bool NonEvolved(in WeaponSlot s) => s.Def is { BaseWeapon: null } && !s.Evolved;

    // ── 거르기 ───────────────────────────────────────────────────

    /// <summary>고를 수 있는 새 무기(진화 결과·패턴 없는 무기 제외, <c>classes</c>·<c>requires ⊆ 스테이지 ∪ 직업</c> — 6.8·6.17).</summary>
    public static bool Selectable(World world, WeaponDef w) =>
        w.BaseWeapon is null && w.MaxLevel >= 1 && FirePatterns.TryGet(w.Pattern, out _) && ClassOk(world, w.Classes) && RequiresOk(world, w.Requires);

    /// <summary>레벨업·상자·<c>--passives</c> 후보 패시브(특성·축복 제외, <c>classes</c>).</summary>
    public static bool Selectable(World world, PassiveDef p) => !p.Trait && !p.Blessing && p.MaxLevel >= 1 && ClassOk(world, p.Classes);

    public static bool ClassOk<T>(World world, IReadOnlyList<DefRef<T>> classes) where T : class, IDef
    {
        if (classes.Count == 0) return true;
        var id = world.HeroClass?.Id;
        foreach (var c in classes)
            if (c.Id == id) return true;
        return false;
    }

    private static bool RequiresOk(World world, IReadOnlyList<ContentBundle> requires)
    {
        foreach (var r in requires)
        {
            var ok = false;
            if (world.Stage is { } st) foreach (var x in st.Requires) ok |= x == r;
            if (world.HeroClass is { } hc) foreach (var x in hc.Requires) ok |= x == r;
            if (!ok) return false;
        }
        return true;
    }

    // ── 후보 ─────────────────────────────────────────────────────

    /// <summary>레벨업 후보(가중: 가진 무기·패시브 강화, 칸이 남으면 새 무기·패시브).</summary>
    public static List<Candidate> LevelUpCandidates(World world, LevelUpWeightsDef w)
    {
        var list = new List<Candidate>();
        WeaponCandidates(world, list, w.OwnedWeapon, w.NewWeapon);
        var hero = world.Hero;
        for (var i = 0; i < PassiveSlots.SlotCount; i++)
            if (hero.Passives[i].Def is { } p && hero.Passives[i].Level < p.MaxLevel) list.Add(new Candidate(CardKind.PassiveUpgrade, p.Id, w.OwnedPassive));
        if (FirstEmptyPassive(hero) >= 0)
            foreach (var p in world.Defs.Passives.All)
                if (Selectable(world, p) && PassiveSlotOf(hero, p.Id) < 0) list.Add(new Candidate(CardKind.NewPassive, p.Id, w.NewPassive));
        return list;
    }

    /// <summary>무기 카드 후보(마법서·레벨업 공통).</summary>
    public static void WeaponCandidates(World world, List<Candidate> list, float owned, float fresh)
    {
        var hero = world.Hero;
        for (var i = 0; i < WeaponSlots.SlotCount; i++)
            if (Upgradable(hero.Weapons[i])) list.Add(new Candidate(CardKind.WeaponUpgrade, hero.Weapons[i].Def!.Id, owned));
        if (FirstEmptyWeapon(hero) >= 0)
            foreach (var w in world.Defs.Weapons.All)
                if (Selectable(world, w) && WeaponSlotOf(hero, w.Id) < 0) list.Add(new Candidate(CardKind.NewWeapon, w.Id, fresh));
    }

    /// <summary>가중 비복원 뽑기 <paramref name="count"/> 장(가중 0 이하는 빠짐). 순서 = 뽑힌 순서.</summary>
    public static List<Candidate> Draw(RandomStream rng, List<Candidate> pool, int count)
    {
        var picked = new List<Candidate>(count);
        var left = new List<Candidate>(pool.Count);
        foreach (var c in pool) if (c.Weight > 0f) left.Add(c);
        while (picked.Count < count && left.Count > 0)
        {
            var total = 0f;
            foreach (var c in left) total += c.Weight;
            var r = rng.NextFloat() * total;
            var k = 0;
            for (; k < left.Count - 1; k++)
            {
                r -= left[k].Weight;
                if (r < 0f) break;
            }
            picked.Add(left[k]);
            left.RemoveAt(k);
        }
        return picked;
    }

    // ── 카드 ─────────────────────────────────────────────────────

    public static LevelUpCard Card(World world, in Candidate c, int levels = 1)
    {
        var defs = world.Defs;
        var hero = world.Hero;
        switch (c.Kind)
        {
            case CardKind.NewWeapon:
            case CardKind.WeaponUpgrade:
            {
                var w = defs.Weapons.Get(c.Id);
                var slot = WeaponSlotOf(hero, c.Id);
                var from = slot >= 0 ? hero.Weapons[slot].Level : 0;
                var to = Math.Min(w.MaxLevel, from + levels);
                return new LevelUpCard(c.Kind, w.Id, w.Name, w.Icon, from, to, WeaponLines(world, w, from, to));
            }
            case CardKind.NewPassive:
            case CardKind.PassiveUpgrade:
            {
                var p = defs.Passives.Get(c.Id);
                var slot = PassiveSlotOf(hero, c.Id);
                var from = slot >= 0 ? hero.Passives[slot].Level : 0;
                return new LevelUpCard(c.Kind, p.Id, p.Name, p.Icon, from, from + 1, PassiveLines(world, p, from));
            }
            default:
            {
                var f = Filler(world, c.Id)!;
                return new LevelUpCard(CardKind.Filler, f.Id, f.Name, f.Icon, 0, 0, FillerLines(world, f));
            }
        }
    }

    /// <summary>카드 문구 "피해 14 → 18"(레벨 표 두 행을 <c>stat_labels</c> 로, 바뀐 키만, 최대 두 줄 — 6.10).</summary>
    private static IReadOnlyList<string> WeaponLines(World world, WeaponDef w, int from, int to)
    {
        var labels = world.Defs.Rules.LevelUp?.StatLabels;
        if (labels is null || from <= 0) return [];
        var lines = new List<string>(2);
        foreach (var (key, column) in w.Levels)
        {
            if (lines.Count == 2) break;
            if (!labels.TryGetValue(key, out var label) || to - 1 >= column.Length) continue;
            var a = column[from - 1];
            var b = column[to - 1];
            if (a == b) continue;
            lines.Add($"{label.Label} {Num(a, label)} → {Num(b, label)}");
        }
        return lines;
    }

    /// <summary>패시브 카드 줄: <c>per_level</c> 의 키마다 "라벨 +값", <c>levels</c> 가 있으면(6.20 ⑯) <b>다음 레벨</b>(<paramref name="from"/> + 1)의 항목. 최대 두 줄.</summary>
    private static IReadOnlyList<string> PassiveLines(World world, PassiveDef p, int from)
    {
        var labels = world.Defs.Rules.LevelUp?.StatLabels;
        var lines = new List<string>(2);
        var entries = p.Levels is { } levels ? from >= 0 && from < levels.Count ? levels[from] : null : p.PerLevel;
        if (entries is null) return lines;
        foreach (var (key, v) in entries)
        {
            if (lines.Count == 2) break;
            var name = StatKeys.JsonName(key);
            if (labels is null || !labels.TryGetValue(name, out var label)) continue;
            lines.Add($"{label.Label} {Signed(v, label)}");
        }
        return lines;
    }

    private static IReadOnlyList<string> FillerLines(World world, FillerCardDef f)
    {
        var lines = new List<string>(2);
        if (f.Heal > 0f) lines.Add($"체력 +{Num(f.Heal, null)}");
        var labels = world.Defs.Rules.LevelUp?.StatLabels;
        foreach (var (key, v) in f.Stats)
            if (labels is not null && labels.TryGetValue(StatKeys.JsonName(key), out var label)) lines.Add($"{label.Label} {Signed(v, label)}");
        return lines;
    }

    private static string Num(float? v, StatLabelDef? label)
    {
        if (v is null) return "∞";
        var x = v.Value;
        if (label?.Format == StatFormat.Pct) return (x * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        return x.ToString("0.##", CultureInfo.InvariantCulture) + (label?.Unit ?? "");
    }

    private static string Signed(float v, StatLabelDef label) => (v >= 0 ? "+" : "") + Num(v, label);

    public static FillerCardDef? Filler(World world, string? id)
    {
        if (id is null || world.Defs.Rules.LevelUp is not { } rules) return null;
        foreach (var f in rules.Fillers)
            if (f.Id == id) return f;
        return null;
    }

    // ── 적용 ─────────────────────────────────────────────────────

    /// <summary>카드 하나 적용(레벨업·마법서·성소 창 공통). <paramref name="levels"/> = 올릴 레벨(매혹의 성소 +2).</summary>
    public static void Apply(World world, LevelUpCard card, int levels = 1)
    {
        var hero = world.Hero;
        switch (card.Kind)
        {
            case CardKind.NewWeapon:
            {
                var i = FirstEmptyWeapon(hero);
                if (i < 0) return;
                var def = world.Defs.Weapons.Get(card.Id);
                hero.Weapons[i] = new WeaponSlot { Def = def, Level = 1, AcquiredTick = world.Tick, MaxedAtTick = def.MaxLevel <= 1 ? MaxedStamp(world) : -1 };
                break;
            }
            case CardKind.WeaponUpgrade:
            {
                var i = WeaponSlotOf(hero, card.Id);
                if (i >= 0) AddWeaponLevels(world, i, levels);
                break;
            }
            case CardKind.NewPassive:
            {
                var i = FirstEmptyPassive(hero);
                if (i < 0) return;
                var def = world.Defs.Passives.Get(card.Id);
                hero.Passives[i] = new PassiveSlot { Def = def, Level = 1, AcquiredTick = world.Tick };
                HeroStats.Recompute(world);
                if (def.OnAcquire is { } on)
                {
                    hero.Hp = MathF.Min(hero.MaxHp, hero.Hp + on.Heal);
                    Wallet.Earn(world, on.Gold);
                }
                break;
            }
            case CardKind.PassiveUpgrade:
            {
                var i = PassiveSlotOf(hero, card.Id);
                if (i >= 0 && hero.Passives[i].Def is { } p) hero.Passives[i].Level = Math.Min(p.MaxLevel, hero.Passives[i].Level + 1);
                break;
            }
            case CardKind.Filler:
                ApplyFiller(world, Filler(world, card.Id));
                return;
        }
        world.Combat.StatsDirty = true;
        HeroStats.Recompute(world);
    }

    /// <summary>
    /// 만렙이 된 순간의 <c>MaxedAtTick</c> = <c>Tick + CombatState.MaxedBase</c>(v0.9 — 짐에서 푼 만렙 순번 0..MaxedBase−1 보다 늘 뒤라, 새 방 첫 틱에 만렙이 된 무기가
    /// 앞 방에서 먼저 만렙이 된 무기보다 앞서지 않는다. 첫 방은 밑 0 = 틱 그대로).
    /// </summary>
    public static int MaxedStamp(World world) => world.Tick + world.Combat.MaxedBase;

    /// <summary>무기 칸 레벨 ± (1..max). 만렙이 되면 <c>MaxedAtTick</c>(<see cref="MaxedStamp"/>), 만렙 아래로 내려가면 −1.</summary>
    public static void AddWeaponLevels(World world, int slot, int delta)
    {
        ref var s = ref world.Hero.Weapons[slot];
        if (s.Def is not { } def || !NonEvolved(s)) return;
        var was = s.Level;
        s.Level = Math.Clamp(s.Level + delta, 1, def.MaxLevel);
        if (s.Level == def.MaxLevel && was < def.MaxLevel) s.MaxedAtTick = MaxedStamp(world);
        else if (s.Level < def.MaxLevel) s.MaxedAtTick = -1;
    }

    /// <summary>채움 카드(체력·판 끝까지 능력치 — 짐 <c>Mods</c>).</summary>
    public static void ApplyFiller(World world, FillerCardDef? f)
    {
        if (f is null) return;
        foreach (var (k, v) in f.Stats) world.Combat.Mods[(int)k] += v;
        HeroStats.Recompute(world);
        world.Hero.Hp = MathF.Min(world.Hero.MaxHp, world.Hero.Hp + f.Heal);
    }

    /// <summary><c>단련</c>(<c>rules.level_up.temper</c>) — 강화할 것이 없을 때의 공통 대체(기획 4.4).</summary>
    public static FillerCardDef? Temper(World world)
    {
        var f = Filler(world, world.Defs.Rules.LevelUp?.Temper);
        ApplyFiller(world, f);
        return f;
    }

    // ── 진화 ─────────────────────────────────────────────────────

    /// <summary>진화할 수 있는 것: 만렙 무기 + 짝 패시브(있으면), "먼저 만렙이 된 무기"(<c>MaxedAtTick</c> 작은 것, 같으면 칸 번호) 먼저.</summary>
    public static (int Slot, EvolutionDef Evo)? FindEvolution(World world)
    {
        var hero = world.Hero;
        (int Slot, EvolutionDef Evo)? best = null;
        var bestTick = int.MaxValue;
        foreach (var evo in world.Defs.Evolutions.All)
        {
            if (!ClassOk(world, evo.Classes)) continue;
            var ws = WeaponSlotOf(hero, evo.Weapon.Id);
            if (ws < 0) continue;
            ref var s = ref hero.Weapons[ws];
            if (!NonEvolved(s) || s.Level < s.Def!.MaxLevel || PassiveSlotOf(hero, evo.Passive.Id) < 0) continue;
            var t = s.MaxedAtTick < 0 ? int.MaxValue - 1 : s.MaxedAtTick;
            if (t < bestTick || (t == bestTick && best is { } b && ws < b.Slot)) { bestTick = t; best = (ws, evo); }
        }
        return best;
    }

    public static void Evolve(World world, int slot, EvolutionDef evo)
    {
        ref var s = ref world.Hero.Weapons[slot];
        s = new WeaponSlot { Def = evo.Result.Def, Level = s.Level, Evolved = true, AcquiredTick = s.AcquiredTick, MaxedAtTick = s.MaxedAtTick };
        world.Combat.StatsDirty = true;
    }

    // ── 남은 칸 레벨 ─────────────────────────────────────────────

    /// <summary>빌드에 남은 칸 레벨(v0.8 <c>picks_left</c>): 열린 칸마다 (최대 − 지금), 빈 칸은 그 종류 최대 레벨(고를 수 있는 정의 중 가장 큰 값).</summary>
    public static int PicksLeft(World world)
    {
        var hero = world.Hero;
        var maxW = 0;
        foreach (var w in world.Defs.Weapons.All) if (Selectable(world, w)) maxW = Math.Max(maxW, w.MaxLevel);
        var maxP = 0;
        foreach (var p in world.Defs.Passives.All) if (Selectable(world, p)) maxP = Math.Max(maxP, p.MaxLevel);
        var left = 0;
        for (var i = 0; i < Math.Min(hero.WeaponSlotsOpen, WeaponSlots.SlotCount); i++)
        {
            ref var s = ref hero.Weapons[i];
            if (s.Def is null) left += maxW;
            else if (NonEvolved(s)) left += Math.Max(0, s.Def.MaxLevel - s.Level);
        }
        for (var i = 0; i < Math.Min(hero.PassiveSlotsOpen, PassiveSlots.SlotCount); i++)
        {
            ref var s = ref hero.Passives[i];
            left += s.Def is null ? maxP : Math.Max(0, s.Def.MaxLevel - s.Level);
        }
        return left;
    }
}
