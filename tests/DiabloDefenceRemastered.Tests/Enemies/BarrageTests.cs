using DiabloDefenceRemastered.Core.Sim.Enemies.Parts;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Sim;
using static DiabloDefenceRemastered.Tests.Enemies.BossKit;
using static DiabloDefenceRemastered.Tests.Enemies.Wp4Defs;

namespace DiabloDefenceRemastered.Tests.Enemies;

/// <summary>
/// 탄막 동작 <c>barrage</c>(설계서 6.20 ①, 기획 9.5·38.1): 고리 등간격·빈틈 조준, 나선 파도 시각(BeginTick + round(k × interval / Dt))과 돎, 노린 연사의 파도마다 다시 노림,
/// 빈틈 벽의 빈틈 폭·이동 한도·막힌/맵 밖 자리 빠짐, 생성 금지 48px, 한 패턴의 여러 <c>barrage</c> 함께, 바닥액 탄마다 사거리. 투사체는 가짜 이웃이 기록만 한다.
/// </summary>
public sealed class BarrageTests
{
    private static (TestWorld T, Enemy Boss, RecordingSpawner Rec) Run(PatternDef pattern, Vector2 offset, int megaTiles = TestDefs.DefaultMegaTiles)
    {
        var def = Single("brg", pattern);
        var t = World(Defs([def], megaTiles: megaTiles));
        var boss = SpawnBoss(t, "brg", t.Hero.Pos + offset);
        return (t, boss, Spawner(t));
    }

    /// <summary>첫 발이 나올 때까지(이벤트 프레임) 돌린다.</summary>
    private static int FirstShotTick(TestWorld t, RecordingSpawner rec)
    {
        Assert.True(StepUntil(t, () => rec.Projectiles.Count > 0, 300), "첫 발이 없다");
        return t.World.Tick - 1;
    }

    [Fact]
    public void Ring_EvenSpacing()
    {
        var (t, boss, rec) = Run(Pattern("ring", Circle, Barrage(Ring(16))), new Vector2(300f, 0f));
        FirstShotTick(t, rec);
        Assert.Equal(16, rec.Projectiles.Count);
        var angles = rec.Projectiles.Select(p => Deg(p.Dir)).Order().ToList();
        for (var i = 1; i < angles.Count; i++) Assert.InRange(angles[i] - angles[i - 1], 22.4f, 22.6f);
        // 나오는 자리 = 보스 발 + 방향 × 몸 반경(30)
        Assert.All(rec.Projectiles, p => Assert.InRange(Vector2.Distance(p.Pos, boss.Pos), 29.5f, 30.5f));
        Assert.All(rec.Projectiles, p => Assert.Equal(6f, p.Def.HeroHitRadius));
        // 주인공 조준(offset 0): 한 발이 주인공 쪽
        var toHero = t.Hero.Pos - boss.Pos;
        Assert.Contains(rec.Projectiles, p => Between(p.Dir, toHero) < 0.5f);
    }

    [Fact]
    public void Ring_GapAimOffset()
    {
        var (t, boss, rec) = Run(Pattern("ring", Circle, Barrage(Ring(16) with { Offset = 11.25f })), new Vector2(300f, 0f));
        FirstShotTick(t, rec);
        var toHero = t.Hero.Pos - boss.Pos;
        // 빈틈 조준: 주인공 쪽이 두 탄 사이 가운데(가장 가까운 탄도 11.25°)
        var nearest = rec.Projectiles.Min(p => Between(p.Dir, toHero));
        Assert.InRange(nearest, 11.0f, 11.5f);
        Assert.Equal(2, rec.Projectiles.Count(p => Between(p.Dir, toHero) < 11.5f));
    }

    [Fact]
    public void Spiral_WaveTicksAndTurn()
    {
        var spiral = Ring(2) with { Waves = 15, Interval = 0.2f, Turn = 24f };
        var (t, boss, rec) = Run(Pattern("spiral", Circle, Barrage(spiral)), new Vector2(300f, 0f));
        var begin = FirstShotTick(t, rec);
        var counts = new List<(int Tick, int Count)> { (begin, rec.Projectiles.Count) };
        for (var i = 0; i < 200; i++)
        {
            var before = rec.Projectiles.Count;
            t.Step();
            if (rec.Projectiles.Count != before) counts.Add((t.World.Tick - 1, rec.Projectiles.Count - before));
        }
        Assert.Equal(15, counts.Count);
        Assert.All(counts, c => Assert.Equal(2, c.Count));
        for (var k = 0; k < counts.Count; k++) Assert.Equal(begin + 12 * k, counts[k].Tick);      // 0.2s = 12틱, round(k × 0.2 / Dt)
        // 파도 k 의 첫 갈래 각 = 파도 0 + 24k(기준 방향은 이벤트 프레임 때 굳음 — 보스·주인공이 서 있어 같다)
        var first = Deg(rec.Projectiles[0].Dir);
        for (var k = 1; k < 15; k++)
        {
            var wave = rec.Projectiles.Skip(2 * k).Take(2).Select(p => Deg(p.Dir)).ToList();
            Assert.Contains(wave, a => MathF.Abs(Wrap(a - first - 24f * k)) < 0.5f);
        }
        Assert.False(boss.Action.Active);                                                    // 마지막 파도 뒤 패턴 끝
    }

    [Fact]
    public void Fan_ReaimEachWave()
    {
        var burst = Fan(7, 70f) with { Waves = 3, Interval = 0.35f, Reaim = true };
        var (t, boss, rec) = Run(Pattern("burst", Line(), Barrage(burst)), new Vector2(300f, 0f));
        FirstShotTick(t, rec);
        Assert.Equal(7, rec.Projectiles.Count);
        // 다음 파도 전에 주인공을 옮긴다 — 파도마다 그 틱의 주인공 쪽이 가운데
        var moved = new List<Vector2>();
        for (var k = 1; k < 3; k++)
        {
            t.Hero.Pos += new Vector2(0f, 90f);
            moved.Add(t.Hero.Pos);
            Assert.True(StepUntil(t, () => rec.Projectiles.Count >= 7 * (k + 1), 40));
        }
        for (var k = 1; k < 3; k++)
        {
            var wave = rec.Projectiles.Skip(7 * k).Take(7).ToList();
            var center = wave[3];                                                              // 7발의 가운데
            Assert.True(Between(center.Dir, moved[k - 1] - boss.Pos) < 1f, $"파도 {k} 가 주인공을 다시 노리지 않았다");
            Assert.InRange(Between(wave[0].Dir, wave[6].Dir), 69.5f, 70.5f);
        }
    }

    private static BarrageParams WallParams(float distance = 260f, float width = 560f) => new()
    {
        Shape = BarrageShape.Wall, Projectile = DefRef<ProjectileDef>.To(Orb), Speed = 85f, Damage = 16f, Waves = 3, Interval = 1.6f,
        Wall = new BarrageWallParams { Distance = distance, Width = width, Spacing = 24f, Gap = 3, GapRange = 160f, GapShift = 100f },
    };

    [Theory]
    [InlineData(1ul)]
    [InlineData(2ul)]
    [InlineData(3ul)]
    public void Wall_GapWidthAndShiftLimit(ulong seed)
    {
        var def = Single("wall", Pattern("wall", Circle, Barrage(WallParams())));
        var t = World(Defs([def], megaTiles: 24), seed: seed);                                   // 큰 맵 — 줄이 모두 맵 안
        var boss = SpawnBoss(t, "wall", t.Hero.Pos + new Vector2(400f, 0f));
        var rec = Spawner(t);
        var heroAt = t.Hero.Pos;
        FirstShotTick(t, rec);
        t.Step(220);
        Assert.Equal(63, rec.Projectiles.Count);                                                  // 파도마다 24 − 3

        var dir = Vector2.Normalize(heroAt - boss.Pos);
        var perp = new Vector2(-dir.Y, dir.X);
        var center = heroAt - dir * 260f;
        float? prevGap = null;
        for (var k = 0; k < 3; k++)
        {
            var wave = rec.Projectiles.Skip(21 * k).Take(21).ToList();
            Assert.All(wave, p => Assert.True(Between(p.Dir, dir) < 0.5f));                     // 주인공 쪽으로 밀려옴
            Assert.All(wave, p => Assert.InRange(Vector2.Dot(p.Pos - center, dir), -0.5f, 0.5f)); // 줄 위
            var xs = wave.Select(p => Vector2.Dot(p.Pos - center, perp)).Order().ToList();
            // 빈틈: 이웃 탄 사이가 24 아닌 곳 하나 = 4칸(96px) — 통로 96 − 탄 지름 10 − 판정 12 = 74
            var gaps = new List<(float From, float To)>();
            for (var i = 1; i < xs.Count; i++)
                if (xs[i] - xs[i - 1] > 25f) gaps.Add((xs[i - 1], xs[i]));
            var g = Assert.Single(gaps);
            Assert.InRange(g.To - g.From, 95.5f, 96.5f);
            var mid = (g.From + g.To) * 0.5f;
            Assert.InRange(mid, -160f - 24f, 160f + 24f);
            if (prevGap is { } pg) Assert.InRange(MathF.Abs(mid - pg), 0f, 100f + 24f);            // 다음 파도 빈틈은 앞 빈틈 ±100 안(탄 자리로 반올림 ±12)
            prevGap = mid;
        }
    }

    [Fact]
    public void Wall_SkipsBlockedAndOutside()
    {
        // 맵 밖: 작은 맵에서 줄 양 끝이 마름모 밖 — 그 탄만 빠지고 나온 탄은 모두 맵 안
        var def = Single("wall", Pattern("wall", Circle, Barrage(WallParams(width: 1100f))));
        var t = World(Defs([def]));
        SpawnBoss(t, "wall", t.Hero.Pos + new Vector2(0f, -330f));
        var rec = Spawner(t);
        FirstShotTick(t, rec);
        var all = (int)MathF.Floor(1100f / 24f) + 1 - 3;
        Assert.InRange(rec.Projectiles.Count, 1, all - 1);
        Assert.All(rec.Projectiles, p => Assert.True(t.World.Nav.IsInsideMap(p.Pos)));

        // 막힘: 줄 가운데에서 그 자리까지 투사체 길이 막힌 쪽(가로 x < 벽) 탄은 빠진다
        var t2 = World(Defs([def], megaTiles: 24));
        var heroX = t2.Hero.Pos.X;
        t2.World.Nav = new WallNav(t2.World.Nav, heroX - 100f);
        SpawnBoss(t2, "wall", t2.Hero.Pos + new Vector2(0f, -330f));
        var rec2 = Spawner(t2);
        FirstShotTick(t2, rec2);
        Assert.NotEmpty(rec2.Projectiles);
        Assert.All(rec2.Projectiles, p => Assert.True(p.Pos.X >= heroX - 100f));
        Assert.True(rec2.Projectiles.Count < all);
    }

    [Fact]
    public void SpawnClear_SkipsNearHero()
    {
        // 주인공이 보스에 붙음(보스 가운데에서 50px) — 보스 둘레 30px 에서 나오는 고리 중 주인공 48px 안 자리는 빠진다
        var (t, boss, rec) = Run(Pattern("ring", Circle, Barrage(Ring(16))), new Vector2(50f, 0f));
        FirstShotTick(t, rec);
        Assert.InRange(rec.Projectiles.Count, 1, 15);
        Assert.All(rec.Projectiles, p => Assert.True(Vector2.Distance(p.Pos, t.Hero.Pos) >= 48f));
        Assert.Equal(48f, t.World.Defs.Rules.Enemies!.Barrage.SpawnClear);
    }

    [Fact]
    public void TwoBarrages_RunTogether()
    {
        // 고리+나선 겹침: 고리 12 × 3번(1.0s) 과 3갈래 × 15번(0.2s) 이 같은 행동 상태로 함께 돈다
        var ring = Ring(12) with { Waves = 3, Interval = 1.0f, Turn = 15f };
        var spiral = Ring(3, Orb2) with { Waves = 15, Interval = 0.2f, Turn = -20f };
        var (t, boss, rec) = Run(Pattern("both", Circle, Barrage(ring), Barrage(spiral)), new Vector2(300f, 0f));
        var begin = FirstShotTick(t, rec);
        Assert.Equal(15, rec.Projectiles.Count);                                                 // 둘 다 파도 0
        Assert.True(StepUntil(t, () => !boss.Action.Active, 400));
        Assert.Equal(36, rec.Projectiles.Count(p => p.Def == Orb));
        Assert.Equal(45, rec.Projectiles.Count(p => p.Def == Orb2));
        Assert.InRange(t.World.Tick - begin, 14 * 12 + 1, 14 * 12 + 60);                         // 긴 쪽(나선 2.8s) 이 끝난 뒤
    }

    [Fact]
    public void Glob_RangeToHero()
    {
        var glob = Fan(5, 70f) with { RangeToHero = [0.7f, 1.3f] };
        var (t, boss, rec) = Run(Pattern("spit", Line(), Barrage(glob)), new Vector2(200f, 0f));
        FirstShotTick(t, rec);
        var dist = Vector2.Distance(boss.Pos, t.Hero.Pos);
        var ranges = rec.Projectiles.Select(p => p.Range).ToList();
        float[] mul = [0.7f, 0.85f, 1f, 1.15f, 1.3f];
        for (var i = 0; i < 5; i++) Assert.InRange(ranges[i], dist * mul[i] - 0.5f, dist * mul[i] + 0.5f);

        // 40~정의 사거리로 자름: 아주 먼 주인공이면 정의 사거리(640)
        var (t2, _, rec2) = Run(Pattern("spit", Line(), Barrage(glob)), new Vector2(600f, 0f));
        FirstShotTick(t2, rec2);
        Assert.Equal(640f, rec2.Projectiles[^1].Range);
    }
}
