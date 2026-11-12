using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using static DiabloDefenceRemastered.Tests.Enemies.BossKit;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 보스 순간이동 <c>teleport</c>(설계서 6.20 ⑧, 기획 9.5 P3): 자리 = 주인공에서 150~210·걸을 수 있음, 사라짐부터 나타날 때까지 조준·피해 없음,
/// 나타날 자리 원 예고(appear 0.6s) 뒤 나타남, 자리가 없으면 제자리.
/// </summary>
public sealed class EnemyTeleportTests
{
    private static (TestWorld T, Enemy Boss) BlinkWorld(TeleportParams p, ulong seed = 1)
    {
        var def = Single("blinker", Pattern("blink", null, A("teleport", p)) with { EventFrame = 1 });
        var t = World(Defs([def]), seed: seed);
        return (t, SpawnBoss(t, "blinker", t.Hero.Pos + new Vector2(300f, 0f)));
    }

    [Theory]
    [InlineData(1ul)]
    [InlineData(2ul)]
    [InlineData(3ul)]
    [InlineData(4ul)]
    public void Destination_150to210_Walkable(ulong seed)
    {
        var (t, boss) = BlinkWorld(new TeleportParams(), seed);
        var start = boss.Pos;
        Assert.True(StepUntil(t, () => boss.Life == LifeState.Emerging, 120), "사라지지 않았다");
        Assert.NotEqual(start, boss.Pos);
        Assert.InRange(Vector2.Distance(boss.Pos, t.Hero.Pos), 150f - 0.01f, 210f + 0.01f);
        Assert.True(t.World.Nav.IsInsideMap(boss.Pos));
        Assert.True(t.World.Nav.Clearance(boss.Pos) >= boss.Radius);
        Assert.Equal(boss.Pos, boss.PrevPos);                                   // 끌리지 않게(보간 없이 옮김)
    }

    [Fact]
    public void Hidden_UntargetableAndInvulnerable()
    {
        var (t, boss) = BlinkWorld(new TeleportParams());
        Assert.True(StepUntil(t, () => boss.FadeOutTicks > 0, 60));
        var hp = boss.Hp;
        var invulnUntil = boss.InvulnUntilTick;
        Assert.True(invulnUntil >= t.World.Tick + 15 + 36 - 2);                   // vanish 0.25 + appear 0.6
        // 사라지는 동안: 흐려짐(1 → 0), 맞지 않음
        var alphas = new List<float>();
        while (boss.Life != LifeState.Emerging)
        {
            t.World.Damage.ToEnemy(boss.Id, 50f, 0f, 0f, 0);
            t.Step();
            alphas.Add(boss.Visual.AlphaMul);
        }
        Assert.True(alphas.Count >= 14);
        Assert.True(alphas[^2] < alphas[0]);
        // 숨은 동안: 조준 안 됨·그리지 않음·맞지 않음
        Assert.False(boss.IsTargetable);
        for (var i = 0; i < 20; i++)
        {
            t.World.Damage.ToEnemy(boss.Id, 50f, 0f, 0f, 0);
            t.Step();
            Assert.False(boss.IsTargetable);
            Assert.Equal(0f, boss.Visual.AlphaMul);
        }
        Assert.Equal(hp, boss.Hp);
    }

    [Fact]
    public void Appears_AfterTelegraph()
    {
        var (t, boss) = BlinkWorld(new TeleportParams());
        var rec = Spawner(t);
        Assert.True(StepUntil(t, () => boss.Life == LifeState.Emerging, 120));
        var moved = t.World.Tick - 1;
        var ring = Assert.Single(rec.Telegraphs);
        Assert.Equal(TelegraphShape.Circle, ring.Shape);
        Assert.Equal(boss.Pos, ring.Pos);                                        // 나타날 자리에
        Assert.Equal(boss.Radius + 30f, ring.Size);
        Assert.Equal(0.6f, ring.Time);
        Assert.Equal(0f, ring.Blast.Radius);                                     // 폭발 없음
        Assert.Equal(moved + 36, boss.WakeTick);
        var at = boss.Pos;
        var rising = new List<float>();
        while (boss.Life == LifeState.Emerging)
        {
            t.Step();
            if (boss.Life != LifeState.Emerging) break;
            Assert.Equal(at, boss.Pos);                                          // 나타날 때까지 서 있음
            rising.Add(boss.Visual.AlphaMul);
        }
        Assert.Equal(moved + 36, t.World.Tick - 1);                              // 0.6s 뒤 나타남
        Assert.True(boss.IsTargetable);
        Assert.Contains(rising, a => a is > 0f and < 1f);                       // 마지막 0.15s 에 밝아짐
        Assert.Equal(0f, rising[0]);
    }

    [Fact]
    public void NoSpot_StaysInPlace()
    {
        var (t, boss) = BlinkWorld(new TeleportParams { Min = 5000f, Max = 5000f, Tries = 3 });
        Assert.True(StepUntil(t, () => boss.Action.Active, 30));
        var start = boss.Pos;
        Assert.True(StepUntil(t, () => boss.Life == LifeState.Emerging, 120));
        Assert.Equal(start, boss.Pos);                                           // 제자리에서 같은 연출
        Assert.Single(Spawner(t).Telegraphs);
    }
}
