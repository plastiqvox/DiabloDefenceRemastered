using DiabloDefenceRemastered.Core.Sim.Enemies;
using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using DiabloDefenceRemastered.Tests.Sim;
using static DiabloDefenceRemastered.Tests.Enemies.BossKit;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 정해진 순서 <c>sequence</c>·<see cref="PatternSequence"/>(설계서 6.20 ③④, 기획 9.5): 칸 차례와 칸마다 쉼, 끝 칸 다음은 처음, 내려치기(순서 밖)는 쉬는 동안만,
/// 순서 패턴은 슬롯을 쓰지 않음(시퀀서 하나가 슬롯 하나), 페이즈 전환 때 처음부터·이 적이 쏜 탄만 지움.
/// </summary>
public sealed class PatternSequenceTests
{
    private static readonly ProjectileDef OrbC = Orb with { Id = "pj_t_orb3" };

    private static PatternDef P(string id, ProjectileDef def) => Pattern(id, Circle, Barrage(Ring(4, def)));

    private static EnemyDef Boss(string id, SequenceDef seq, params PatternDef[] patterns) =>
        Enemy(id) with { Rank = DefRank.Boss, Radius = 30f, Hp = 10000f, Speed = 55f, Patterns = patterns, Sequence = seq };

    /// <summary>행동 시작·끝 틱(시작 틱, 끝 틱, 탄 정의)을 모은다.</summary>
    private static List<(int Start, int End, ProjectileDef? Def)> Record(TestWorld t, Enemy e, RecordingSpawner rec, int ticks)
    {
        var list = new List<(int Start, int End, ProjectileDef? Def)>();
        var active = false;
        var start = 0;
        var shots = 0;
        for (var i = 0; i < ticks; i++)
        {
            t.Step();
            var tick = t.World.Tick - 1;
            if (!active && e.Action.Active) { active = true; start = tick; shots = rec.Projectiles.Count; }
            else if (active && !e.Action.Active)
            {
                active = false;
                list.Add((start, tick, rec.Projectiles.Count > shots ? rec.Projectiles[shots].Def : null));
            }
        }
        return list;
    }

    [Fact]
    public void Steps_InOrderWithWaits()
    {
        var def = Boss("seq", Seq(0.5f, ("a", 0.5f), ("b", 1.0f)), P("a", Orb), P("b", Orb2));
        var t = World(Defs([def]));
        var boss = SpawnBoss(t, "seq", t.Hero.Pos + new Vector2(300f, 0f));
        var runs = Record(t, boss, Spawner(t), 360);
        Assert.True(runs.Count >= 4);
        Assert.Equal(Orb, runs[0].Def);
        Assert.Equal(Orb2, runs[1].Def);
        Assert.InRange(runs[0].Start, 29, 32);                                  // start 0.5s
        // 끝난 틱 → 다음 시작 = 그 칸 wait(a 0.5s = 30틱, b 1.0s = 60틱)
        Assert.InRange(runs[1].Start - runs[0].End, 29, 31);
        Assert.InRange(runs[2].Start - runs[1].End, 59, 61);
    }

    [Fact]
    public void Loops()
    {
        var def = Boss("seq", Seq(0f, ("a", 0.2f), ("b", 0.2f), ("c", 0.2f)), P("a", Orb), P("b", Orb2), P("c", OrbC));
        var t = World(Defs([def]));
        var boss = SpawnBoss(t, "seq", t.Hero.Pos + new Vector2(300f, 0f));
        var runs = Record(t, boss, Spawner(t), 600);
        Assert.True(runs.Count >= 6);
        ProjectileDef[] order = [Orb, Orb2, OrbC, Orb, Orb2, OrbC];
        for (var i = 0; i < 6; i++) Assert.Equal(order[i], runs[i].Def);
        Assert.Equal(runs.Count % 3, boss.Parts[boss.Kit.SequencerSlot].Counter);   // 다음 칸
    }

    [Fact]
    public void Slam_OnlyDuringWait()
    {
        // 내려치기(순서 밖, 쿨 1.0·거리 ≤ 400) + 긴 순서 패턴(파도 5 × 0.2s): 내려치기는 순서 패턴 도중에 끼어들지 않고, 칸 사이 쉼에 들어간다
        var slam = Cooled("slam", 1.0f, null, A("melee_slam", new MeleeSlamParams { Damage = 5f })) with { RangeBand = [0f, 400f] };
        var longRing = Pattern("a", Circle, Barrage(Ring(4) with { Waves = 5, Interval = 0.2f }));
        var def = Boss("seq", Seq(0f, ("a", 1.0f)), slam, longRing);
        var t = World(Defs([def]));
        var boss = SpawnBoss(t, "seq", t.Hero.Pos + new Vector2(200f, 0f));
        var seqSlot = boss.Kit.SequencerSlot;
        var slamSlot = 0;
        var slamsInWait = 0;
        var seqRuns = 0;
        var prev = -1;
        for (var i = 0; i < 900; i++)
        {
            t.Step();
            var now = boss.Action.Active ? boss.Action.PartIndex : -1;
            if (now != prev && now == slamSlot && boss.Parts[seqSlot].Cooldown > 0f) slamsInWait++;
            if (prev == seqSlot && now != seqSlot) seqRuns++;
            // 순서 패턴이 도는 중이면 내려치기로 바뀌지 않는다(패턴 하나가 끝까지 — 파도 5 개를 다 냄)
            if (prev == seqSlot && now == slamSlot) Assert.Fail("순서 패턴 도중에 내려치기가 끼어들었다");
            prev = now;
        }
        Assert.True(slamsInWait > 0, "쉼 동안 내려치기가 없었다");
        Assert.True(seqRuns >= 3);
    }

    [Fact]
    public void SequencePatterns_UseNoSlot()
    {
        var slam = Cooled("slam", 2f, null, A("melee_slam", new MeleeSlamParams { Damage = 5f })) with { RangeBand = [0f, 90f] };
        PatternDef[] p1 = [slam, P("a", Orb), P("b", Orb2), P("c", OrbC)];
        PatternDef[] p2 = [slam, P("d", Orb), P("e", Orb2), P("f", OrbC)];
        PatternDef[] p3 = [slam, P("g", Orb), P("h", Orb2), P("i", OrbC), P("j", Orb), P("k", Orb2), P("l", OrbC), P("m", Orb), P("n", Orb2), P("o", OrbC), P("q", Orb)];
        var def = Boss("seq", Seq(0f, ("a", 0f), ("b", 0f), ("c", 0f)), p1) with
        {
            Phases =
            [
                new PhaseDef { Id = "p2", HpBelow = 0.7f, Patterns = p2, Remove = ["a", "b", "c"], Sequence = Seq(0f, ("d", 0f), ("e", 0f), ("f", 0f)) },
                new PhaseDef { Id = "p3", HpBelow = 0.4f, Patterns = p3, Remove = ["d", "e", "f"],
                               Sequence = Seq(0f, ("g", 0f), ("h", 0f), ("i", 0f), ("j", 0f), ("k", 0f), ("l", 0f), ("m", 0f), ("n", 0f), ("o", 0f), ("q", 0f)) },
            ],
        };
        var kit = EnemyKit.Build(def, 0, []);                                    // 패턴 합 17 이지만 슬롯은 내려치기 + 시퀀서 = 2(12 안)
        Assert.Equal(2, kit.Count);
        Assert.Equal(1, kit.SequencerSlot);
        for (var p = 0; p < 3; p++)
        {
            Assert.Equal([0, 1], kit.Abilities[p]);                              // 시퀀서는 능력 목록 맨 뒤
            var seq = Assert.IsType<PatternSequence>(kit.Part(p, 1));
            Assert.Equal(p == 2 ? 10 : 3, seq.Steps.Count);
        }
        Assert.Empty(new EnemyDataValidatorTestsAccess().Validate(def));
    }

    [Fact]
    public void PhaseChange_RestartsAndClearsOwnProjectiles()
    {
        var a = P("a", Orb);
        var b = P("b", Orb2);
        var c = P("c", OrbC);
        var def = Boss("seq", Seq(0f, ("a", 0.1f), ("b", 0.1f)), a, b) with
        {
            Phases = [new PhaseDef { Id = "p2", HpBelow = 0.5f, Invulnerable = 1.5f, ClearProjectiles = true, Patterns = [a, b, c], Remove = [], Sequence = Seq(0.8f, ("c", 0.2f)) }],
        };
        // P2 목록에 a·b 가 남아도(로더가 지우지 않은 시험 정의) 순서 패턴이라 슬롯을 쓰지 않는다 — 검사는 조건 없는 패턴이 그 페이즈 순서에 없다고 알린다
        var t = World(Defs([def, Enemy("plain")]));
        var boss = SpawnBoss(t, "seq", t.Hero.Pos + new Vector2(300f, 0f));
        var rec = Spawner(t);
        Assert.True(StepUntil(t, () => rec.Projectiles.Count > 0 && boss.Parts[boss.Kit.SequencerSlot].Counter == 1 && boss.Action.Active, 300));

        // 날고 있는 탄: 이 보스 것 셋 + 다른 적 것 하나
        var other = Spawn(t, "plain", t.Hero.Pos + new Vector2(-400f, 0f));
        Projectile Fly(EntityId owner, float x)
        {
            var p = t.World.Projectiles.Add();
            p.Def = Orb;
            p.Faction = Faction.Enemy;
            p.Owner = owner;
            p.Pos = p.PrevPos = t.Hero.Pos + new Vector2(x, 200f);
            p.Vel = new Vector2(0f, 1f);
            p.Speed = 1f;
            p.RangeLeft = 1000f;
            p.TimeLeft = 100f;
            p.SpawnTick = t.World.Tick;
            return p;
        }
        EntityId[] ownIds = [Fly(boss.Id, -50f).Id, Fly(boss.Id, 0f).Id, Fly(boss.Id, 50f).Id];
        var theirId = Fly(other.Id, 100f).Id;

        boss.Hp = boss.MaxHp * 0.45f;
        rec.Projectiles.Clear();
        t.Step();
        Assert.Equal(1, boss.PhaseIdx);
        var store = t.World.Projectiles;
        Assert.All(ownIds, id => Assert.False(store.Contains(id)));              // 이 보스가 쏜 탄만 지움(틱 끝에 압축됨)
        Assert.True(store.Contains(theirId));
        Assert.False(boss.Action.Active);                                        // 진행 중 패턴은 끊김
        var slot = boss.Parts[boss.Kit.SequencerSlot];
        Assert.Equal(0, slot.Counter);                                           // 순서 처음부터
        Assert.InRange(slot.Cooldown, 0.8f - 2f / 60f, 0.8f);                     // 쉼 = 새 sequence.start
        Assert.Equal(t.World.Tick - 1 + 90, boss.InvulnUntilTick);               // 1.5s 무적

        Assert.True(StepUntil(t, () => rec.Projectiles.Count > 0, 300));
        Assert.All(rec.Projectiles, p => Assert.Equal(OrbC, p.Def));             // P2 첫 칸
    }
}

/// <summary>검사기 하나만 돌리는 짧은 길(시험 안).</summary>
internal sealed class EnemyDataValidatorTestsAccess
{
    public IReadOnlyList<DataError> Validate(EnemyDef def)
    {
        var defs = Defs([def]);
        var ctx = new ValidationContext { Defs = defs };
        new EnemyDataValidator().Validate(defs, ctx);
        return [.. ctx.Errors.Where(e => e.Message.Contains($"'{def.Id}'", StringComparison.Ordinal))];
    }
}
