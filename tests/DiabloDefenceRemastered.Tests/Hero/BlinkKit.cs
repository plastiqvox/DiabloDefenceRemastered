using DiabloDefenceRemastered.Tests.Combat;

namespace DiabloDefenceRemastered.Tests.Heroes;

/// <summary>
/// 보스 묶음 B 시험 도우미: <see cref="CombatKit"/> 합성 정의에 순간이동 규칙(기획 v0.9.5 3절 첫 값)·순간이동 숙련(6절 레벨별 효과)·카드 라벨·이펙트를 더한다.
/// 실제 <c>Data/</c> 에 기대지 않는다(실제 데이터는 <see cref="HeroBlinkTests"/> 의 엄격 로딩 시험). 2026-11-13 에 실제 데이터는 1칸·96px·거리 숙련으로 바뀌었지만,
/// 여기 값(2칸·160px·시전 숙련)은 칸 여럿·시전 0 같은 경우를 시험하려고 그대로 둔다.
/// </summary>
public static class BlinkKit
{
    public const string Mastery = "t_blink_mastery";

    public static readonly EffectDef Fx = new() { Id = "t_fx_blink" };

    public static BlinkRulesDef Rules() => new()
    {
        Charges = 2, Recharge = 6f, Cast = 0.15f, Distance = 160f, Invuln = 0.2f, MinDistance = 24f,
        Effect = new EffectRef { Effect = DefRef<EffectDef>.To(Fx) },
    };

    /// <summary>순간이동 숙련(기획 6절 표): 시전 −0.05 · 칸 +1 · 시전 −0.05 · 충전 −12% · 시전 −0.05.</summary>
    public static PassiveDef MasteryDef() => new()
    {
        Id = Mastery, Name = "순간이동 숙련", MaxLevel = 5,
        Levels =
        [
            new Dictionary<StatKey, float> { [StatKey.BlinkCast] = -0.05f },
            new Dictionary<StatKey, float> { [StatKey.BlinkCharges] = 1f },
            new Dictionary<StatKey, float> { [StatKey.BlinkCast] = -0.05f },
            new Dictionary<StatKey, float> { [StatKey.BlinkRechargePct] = -0.12f },
            new Dictionary<StatKey, float> { [StatKey.BlinkCast] = -0.05f },
        ],
    };

    /// <summary>합성 정의 + 순간이동 규칙(<paramref name="rules"/> 가 null 이면 기본)·숙련 패시브·카드 라벨 셋.</summary>
    public static GameDefs Defs(BlinkRulesDef? rules = null, bool noBlink = false)
    {
        var d = CombatKit.Defs();
        var labels = new Dictionary<string, StatLabelDef>
        {
            ["blink_charges"] = new() { Label = "순간이동 칸" },
            ["blink_cast"] = new() { Label = "순간이동 시전", Unit = "s" },
            ["blink_recharge_pct"] = new() { Label = "순간이동 충전", Format = StatFormat.Pct },
        };
        return new GameDefs
        {
            Rules = d.Rules with { Hero = d.Rules.Hero! with { Blink = noBlink ? null : rules ?? Rules() }, LevelUp = d.Rules.LevelUp! with { StatLabels = labels } },
            Sprites = d.Sprites, SpriteSets = d.SpriteSets, Effects = new DefTable<EffectDef>([Fx]), Projectiles = d.Projectiles,
            Weapons = d.Weapons, Passives = new DefTable<PassiveDef>([.. d.Passives.All, MasteryDef()]), HeroClasses = d.HeroClasses,
            Tilesets = d.Tilesets, Maps = d.Maps, Stages = d.Stages, Enemies = d.Enemies, Rewards = d.Rewards, Pickups = d.Pickups,
        };
    }

    /// <summary>무기 없는 시험 월드(쏘지 않음), 주인공을 맵 가운데에. <paramref name="nav"/> 를 주면 내비를 바꿔 끼움.</summary>
    public static TestWorld World(GameDefs? defs = null, Func<World, INavWorld>? nav = null, ulong seed = 1)
    {
        var t = CombatKit.World(defs ?? Defs(), seed, b =>
        {
            b.WithPresentation();
            if (nav is not null) b.Setup(w => w.Nav = nav(w));
        });
        CombatKit.ClearWeapons(t.World);
        t.PlaceHero(t.World.MapSize * 0.5f);
        return t;
    }

    /// <summary>숙련을 칸 0 에 <paramref name="level"/> 로 두고 능력치를 다시 계산(칸 수가 늘면 <c>Recompute</c> 가 채움).</summary>
    public static void SetMastery(World world, int level)
    {
        world.Hero.Passives[0] = new PassiveSlot { Def = world.Defs.Passives.Get(Mastery), Level = level };
        Core.Sim.Heroes.HeroStats.Recompute(world);
    }

    public static PlayerCommand Blink(Vector2 move = default) => new(move, CommandButtons.Blink);

    public static PlayerCommand Walk(Vector2 move) => new(move);
}

/// <summary>x = <paramref name="wallX"/> 에 세로 벽이 있는 내비(그 왼쪽만 걸을 수 있음 — clearance = 벽까지 거리). 나머지는 <paramref name="inner"/>.</summary>
public sealed class WallXNav(INavWorld inner, float wallX) : INavWorld
{
    public float Clearance(Vector2 p) => MathF.Min(inner.Clearance(p), wallX - p.X);
    public bool HasClearLine(Vector2 a, Vector2 b, float radius) => Clearance(a) >= radius && Clearance(b) >= radius;
    public bool ProjectileLineClear(Vector2 a, Vector2 b) => a.X < wallX && b.X < wallX;
    public bool IsInsideMap(Vector2 p) => inner.IsInsideMap(p);
    public float PathLength(Vector2 from, Vector2 to, float radius, float maxLength) => inner.PathLength(from, to, radius, maxLength);
    public float PathLengthToHero(Vector2 from, float radius, float maxLength) => inner.PathLengthToHero(from, radius, maxLength);
    public bool TryPathStep(Vector2 from, Vector2 to, float radius, out Vector2 next) => inner.TryPathStep(from, to, radius, out next);
}
