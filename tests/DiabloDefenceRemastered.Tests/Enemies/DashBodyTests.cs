using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.BossKit;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 돌진 몸통(설계서 6.20 ⑦, 기획 8.3 V19·9.5): <c>charge</c> 가 달리는 동안 공격 클립을 반복, 지나간 길 판정(한 틱에 건너뛴 주인공도 맞음·돌진 하나에 한 번),
/// <c>dash</c> 연속 3번과 다시 노림, 꼬리 탄 48px 마다 양옆, 끝 내려찍기(줄 예고 시작 때 끝 자리 원 예고 + 폭발).
/// </summary>
public sealed class DashBodyTests
{
    private static readonly TelegraphDef Tel = new() { Effect = DefRef<EffectDef>.To(new EffectDef { Id = "fx_t_line" }), Time = 0.5f };

    private static (TestWorld T, Enemy Boss) DashWorld(DashParams p, Vector2 offset, float telegraph = 0.5f)
    {
        var def = Single("dasher", Pattern("dash", Line(telegraph), A("dash", p)));
        var t = World(Defs([def]));
        return (t, SpawnBoss(t, "dasher", t.Hero.Pos + offset));
    }

    [Fact]
    public void Charge_LoopsAttackClip()
    {
        var ghoul = Enemy("ghoulish", B("charge", new ChargeParams { Telegraph = Tel, Speed = 280f, Time = 1.0f, Damage = 10f, RangeBand = [0f, 260f], Cooldown = 30f }));
        var t = World(Defs([ghoul]));
        var e = Spawn(t, "ghoulish", t.Hero.Pos + new Vector2(0f, -200f));
        Assert.True(StepUntil(t, () => e.Action.Active && e.Action.Stage == 1, 120), "돌진에 들어가지 않았다");
        Assert.Equal(ClipKind.Attack, e.Anim.Clip);
        Assert.True(e.Anim.Loop);
        var looped = false;
        var last = e.Anim.Time;
        for (var i = 0; i < 50 && e.Action.Stage == 1; i++)                     // 공격 클립 0.48s 보다 길게 달림 — 처음으로 돌아가며 반복
        {
            t.Step();
            if (e.Anim.Time < last) looped = true;
            last = e.Anim.Time;
            if (e.Action.Stage == 1) Assert.Equal(ClipKind.Attack, e.Anim.Clip);
        }
        Assert.True(looped, "공격 클립이 반복되지 않았다");
    }

    [Fact]
    public void Sweep_HitsHeroSkippedInOneTick()
    {
        // 6000px/s = 틱 100px: 보스 자리 250 → 150 → 50 → −50(주인공 기준) — 어느 틱 자리도 닿는 거리(30 + 14 + 4 = 48) 안이 아니지만 지나간 길이 주인공을 지난다
        var (t, boss) = DashWorld(new DashParams { Speed = 6000f, Time = 0.1f, Damage = 25f }, new Vector2(250f, 0f));
        var hp = t.Hero.Hp;
        Assert.True(StepUntil(t, () => boss.Action.Active && boss.Action.Stage == 1, 120));
        var touched = false;
        for (var i = 0; i < 20 && boss.Action.Active; i++)
        {
            t.Step();
            touched |= Vector2.Distance(boss.Pos, t.Hero.Pos) <= boss.Radius + t.Hero.Radius + 4f;
        }
        Assert.False(touched, "시험 전제: 어느 틱 자리도 닿지 않아야 한다");
        Assert.Equal(hp - 25f, t.Hero.Hp);
    }

    [Fact]
    public void HitsOncePerDash()
    {
        // 느린 돌진(100px/s, 2s)이 주인공을 지나는 동안 여러 틱 겹쳐도 한 번만
        var (t, boss) = DashWorld(new DashParams { Speed = 100f, Time = 2f, Damage = 25f }, new Vector2(80f, 0f));
        Assert.True(StepUntil(t, () => boss.Action.Active && boss.Action.Stage == 1, 120));
        Assert.True(StepUntil(t, () => !boss.Action.Active, 300));
        Assert.Equal(75f, t.Hero.Hp);
    }

    [Fact]
    public void Chain3_Reaims()
    {
        var p = new DashParams { Speed = 400f, Time = 0.3f, Damage = 25f, Chain = 3, ChainPause = 0.45f, Reaim = true };
        var (t, boss) = DashWorld(p, new Vector2(400f, 0f));
        var rec = Spawner(t);
        Assert.True(StepUntil(t, () => boss.Action.Active && boss.Action.Stage == 1, 120));
        Assert.Equal(1, rec.Telegraphs.Count(x => x.Shape == TelegraphShape.Line));
        t.Hero.Pos += new Vector2(0f, 160f);                                     // 첫 돌진 중에 주인공이 비킴
        Assert.True(StepUntil(t, () => boss.Action.Count == 1, 60));
        Assert.Equal(0, boss.Action.Sub);                                        // 서서 줄 예고
        var line2 = rec.Telegraphs.Last(x => x.Shape == TelegraphShape.Line);
        Assert.True(Between(line2.End - line2.Pos, t.Hero.Pos - boss.Pos) < 1f, "둘째 돌진이 주인공을 다시 노리지 않았다");
        Assert.Equal(0.45f, line2.Time);
        Assert.InRange(Vector2.Distance(line2.Pos, line2.End), 119f, 121f);       // 길이 = 속도 × 시간
        Assert.Equal(boss.Radius, line2.Size);                                   // 반폭 = 몸 반경(폭 = 몸 지름)
        Assert.True(StepUntil(t, () => !boss.Action.Active, 200));
        Assert.Equal(3, rec.Telegraphs.Count(x => x.Shape == TelegraphShape.Line));
    }

    [Fact]
    public void Tail_Every48BothSides()
    {
        var tail = new DashTailParams { Projectile = DefRef<ProjectileDef>.To(Orb), Every = 48f, Speed = 70f, Damage = 12f, Range = 240f };
        var (t, boss) = DashWorld(new DashParams { Speed = 300f, Time = 1f, Damage = 25f, Tail = tail }, new Vector2(420f, 0f));
        var rec = Spawner(t);
        Assert.True(StepUntil(t, () => boss.Action.Active && boss.Action.Stage == 1, 120));
        var dir = boss.Action.Dir;
        Assert.True(StepUntil(t, () => !boss.Action.Active, 200));
        var shots = rec.Projectiles;
        Assert.InRange(shots.Count / 2, 5, 6);                                    // 300px / 48
        Assert.Equal(0, shots.Count % 2);
        for (var i = 0; i < shots.Count; i += 2)
        {
            var (a, b) = (shots[i], shots[i + 1]);
            Assert.Equal(a.Pos, b.Pos);
            Assert.InRange(Vector2.Dot(a.Dir, dir), -0.01f, 0.01f);              // 돌진 방향 ±90°
            Assert.InRange(Vector2.Dot(a.Dir, b.Dir), -1.01f, -0.99f);
            Assert.Equal((70f, 12, 240f), (a.Speed, a.Damage, a.Range));
            if (i >= 2) Assert.True(Vector2.Distance(a.Pos, shots[i - 2].Pos) >= 48f);
        }
    }

    [Fact]
    public void EndSlam_TelegraphThenBlastAtMark()
    {
        var slam = new DashEndSlamParams { Radius = 70f, Damage = 20f };
        var (t, boss) = DashWorld(new DashParams { Speed = 420f, Time = 0.5f, Damage = 25f, EndSlam = slam }, new Vector2(300f, 0f), telegraph: 0.6f);
        var rec = Spawner(t);
        var start = boss.Pos;
        Assert.True(StepUntil(t, () => rec.Telegraphs.Count > 0, 30));
        // 줄 예고가 시작되는 틱에 끝 자리 원 예고(같은 틱) — 폭발을 싣고, 시간 = 남은 줄 0.6 + 돌진 0.5 + 0.15
        var circle = Assert.Single(rec.Telegraphs, x => x.Shape == TelegraphShape.Circle);
        var line = Assert.Single(rec.Telegraphs, x => x.Shape == TelegraphShape.Line);
        var mark = start + Vector2.Normalize(line.End - line.Pos) * 210f;
        Assert.InRange(Vector2.Distance(circle.Pos, mark), 0f, 4.5f);              // Throw 4px 걸음
        Assert.Equal(70f, circle.Size);
        Assert.Equal(1.25f, circle.Time, 3);
        Assert.Equal((70f, 20), (circle.Blast.Radius, circle.Blast.Damage));
        Assert.InRange(Vector2.Distance(line.End, mark), 0f, 4.5f);              // 줄 길이 = 속도 × 시간
        Assert.True(StepUntil(t, () => !boss.Action.Active, 200));
        Assert.InRange(Vector2.Distance(boss.Pos, mark), 0f, 12f);                // 막히지 않으면 원 자리에서 멈춤
    }
}
