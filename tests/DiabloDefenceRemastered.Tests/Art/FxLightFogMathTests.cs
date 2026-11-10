using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>표시 이펙트·흔들림·피해 숫자·빛 상한·안개 판단·문 봉인의 그래픽 없는 계산(WP5 — 5.6·6.4·7.2·7.7·7.8·7.10·7.11).</summary>
public class FxLightFogMathTests
{
    private static SpriteDef Sprite(string id, int frames) => TestDefs.Sprite(id, 1, frames);

    [Fact]
    public void EffectPlayback_IntroThenLoop_OneShotEnds()
    {
        var fx = new EffectDef
        {
            Id = "fx_pool", Sprite = DefRef<SpriteDef>.To(Sprite("firewal2", 11)), Frames = [1, 11], Loop = true, FrameTime = 0.05f,
            Intro = new EffectIntroDef { Sprite = DefRef<SpriteDef>.To(Sprite("firewal1", 13)) },
        };
        Assert.Equal(13 * 0.05f, EffectPlayback.IntroLength(fx), 4);
        Assert.Equal(new EffectFrame(true, 1, false), EffectPlayback.Sample(fx, 0f));
        Assert.Equal(new EffectFrame(true, 13, false), EffectPlayback.Sample(fx, 0.64f));
        Assert.Equal(new EffectFrame(false, 1, false), EffectPlayback.Sample(fx, 0.651f));
        Assert.Equal(new EffectFrame(false, 1, false), EffectPlayback.Sample(fx, 0.651f + 11 * 0.05f));   // 한 바퀴 뒤 다시 1
        Assert.Equal(new EffectFrame(false, 4, false), EffectPlayback.Sample(fx, 0.651f, startFrame: 3));

        var boom = new EffectDef { Id = "fx_boom", Sprite = DefRef<SpriteDef>.To(Sprite("bigexp", 16)), Frames = [3, 6], FrameTime = 0.1f };
        Assert.Equal(new EffectFrame(false, 3, false), EffectPlayback.Sample(boom, 0f));
        Assert.Equal(new EffectFrame(false, 6, false), EffectPlayback.Sample(boom, 0.39f));
        Assert.True(EffectPlayback.Sample(boom, 0.41f).Done);
        Assert.Equal((1, 16), EffectPlayback.Range(null, boom.Sprite!.Def));

        // 크기: fit_radius 가 있고 반경을 주면 반경 / fit_radius, 아니면 scale. 거기에 배율
        Assert.Equal(2f, EffectPlayback.Scale(boom with { FitRadius = 30 }, 60, 1f));
        Assert.Equal(0.5f, EffectPlayback.Scale(boom with { Scale = 0.5f }, 60, 1f));
        Assert.Equal(1.5f, EffectPlayback.Scale(boom, 0, 1.5f));
    }

    [Fact]
    public void Scatter_CountPositionsDeterministic()
    {
        var s = new ScatterDef { PerRadius = 20, Min = 3, Center = true };
        Assert.Equal(4, ScatterLayout.Count(s, 30));       // max(3, round(1.5)=2) + 1
        Assert.Equal(6, ScatterLayout.Count(s, 100));      // round(5) + 1
        Assert.Equal(Vector2.Zero, ScatterLayout.Offset(s, 7u, 0, 100));
        for (var i = 1; i < 6; i++)
        {
            var o = ScatterLayout.Offset(s, 7u, i, 100);
            Assert.True(o.Length() <= 100.001f);
            Assert.Equal(o, ScatterLayout.Offset(s, 7u, i, 100));   // 같은 키 같은 자리
        }
        Assert.NotEqual(ScatterLayout.Offset(s, 7u, 1, 100), ScatterLayout.Offset(s, 8u, 1, 100));
        Assert.InRange(ScatterLayout.StartFrame(7u, 2, 11), 0, 10);
        Assert.Equal(0, ScatterLayout.StartFrame(7u, 2, 1));
    }

    [Fact]
    public void Shake_StrongestRemaining_HashOffset()
    {
        ShakeEvent[] shakes = [new(0, 2f, 0.5f), new(10, 5f, 0.1f), new(20, 1f, 1f)];
        Assert.Equal(5f, ShakeMath.Strength(shakes, 12));
        Assert.Equal(2f, ShakeMath.Strength(shakes, 20));   // 5px 는 6틱 뒤 끝남
        Assert.Equal(1f, ShakeMath.Strength(shakes, 40));
        Assert.Equal(0f, ShakeMath.Strength(shakes, 100));

        var a = ShakeMath.Offset(42, 7, 3f);
        Assert.Equal(a, ShakeMath.Offset(42, 7, 3f));      // 같은 씨앗·틱 = 같은 오프셋(프레임 난수 아님)
        Assert.InRange(a.X, -3, 3);
        Assert.InRange(a.Y, -3, 3);
        Assert.Equal(Int2.Zero, ShakeMath.Offset(42, 7, 0f));
    }

    [Fact]
    public void DamageNumbers_MergeWithinWindow_RiseAndFade_Expire()
    {
        var pool = new DamageNumberPool(4);
        var target = new EntityId(3, 1);
        pool.Add(new DamageNumber(10, target, new Vector2(100, 100), 5, false, false));
        pool.Add(new DamageNumber(14, target, new Vector2(110, 100), 7, true, false));   // 0.1s 안 → 합침
        Assert.Equal(1, pool.ActiveCount);
        var e = pool.Items.ToArray().Single(x => x.Active);
        Assert.Equal(12, e.Value);
        Assert.True(e.Crit);
        Assert.Equal(new Vector2(100, 100), e.Pos);

        pool.Add(new DamageNumber(30, target, new Vector2(100, 100), 1, false, false));   // 창 밖 → 새 숫자
        Assert.Equal(2, pool.ActiveCount);
        pool.Add(new DamageNumber(30, default, new Vector2(0, 0), 1, false, true));       // 대상 없음은 합치지 않음
        pool.Add(new DamageNumber(30, default, new Vector2(0, 0), 1, false, true));
        Assert.Equal(4, pool.ActiveCount);
        pool.Add(new DamageNumber(31, new EntityId(9, 1), Vector2.Zero, 1, false, false)); // 가득 → 가장 오래된 것(10)을 바꿈
        Assert.Equal(4, pool.ActiveCount);
        Assert.DoesNotContain(pool.Items.ToArray(), x => x.Active && x.StartTick == 10);

        Assert.Equal(new Vector2(100, 88), DamageNumberPool.DrawPos(e, 18));   // 0.3s → 12px 위
        Assert.Equal(0.5f, DamageNumberPool.Alpha(18), 4);
        pool.Expire(30 + DamageNumberPool.LifeTicks);
        Assert.Equal(1, pool.ActiveCount);   // 31 에 생긴 것만 남음
    }

    [Fact]
    public void Lights_BaseMemory_Intensity_PickCap()
    {
        var b = LightMath.Base(new Rgb(255, 255, 255), 0.5f);
        Assert.Equal(0.5f, b.X, 4);
        var m = LightMath.Memory(new Rgb(255, 128, 0), 0.6f, 0.5f);
        Assert.Equal(0.2f, m.X, 3);
        Assert.Equal(0f, m.Z);

        Assert.Equal(1f, LightMath.Intensity(1, "torch", 5, 0f, 0f, 0f));
        var f = LightMath.Intensity(1, "torch", 5, 0.3f, 0f, 0f);
        Assert.InRange(f, 0.7f, 1f);
        Assert.Equal(f, LightMath.Intensity(1, "torch", 5, 0.3f, 0f, 0f));
        Assert.Equal(1.5f, LightMath.Intensity(1, "torch", 15, 0f, 1f, 0.5f), 3);   // 0.25s / 주기 1 → sin = 1

        // 주인공·점 빛 먼저, 짧은 빛은 가까운 순 — 상한 4
        LightCandidate[] lights =
        [
            new(LightKind.Short, new Vector2(300, 0)), new(LightKind.Hero, Vector2.Zero), new(LightKind.Short, new Vector2(100, 0)),
            new(LightKind.Point, new Vector2(900, 0)), new(LightKind.Short, new Vector2(200, 0)), new(LightKind.Short, new Vector2(50, 0)),
        ];
        Span<int> picked = stackalloc int[4];
        Span<int> scratch = stackalloc int[lights.Length];
        var n = LightMath.Pick(lights, Vector2.Zero, picked, scratch, 4);
        Assert.Equal(4, n);
        Assert.Equal([1, 3, 5, 2], picked.ToArray());
    }

    [Fact]
    public void Fog_Band_WallLevel_NeedsShape_OnRealMap()
    {
        Assert.Equal(1f, FogShade.Band(300, 420, 40));
        Assert.Equal(0.5f, FogShade.Band(400, 420, 40), 4);
        Assert.Equal(0f, FogShade.Band(430, 420, 40));
        Assert.Equal(1f, FogShade.Band(420, 420, 0));

        var w = RealData.CathedralWorld();
        w.Step();   // 첫 BeginTick 이 시야를 계산한다
        var world = w.World;
        var f = world.Visibility;
        var hero = world.Hero.Pos;
        var tick = world.Tick + 30;   // 바뀜(9틱) 끝난 뒤
        var heroCell = f.CellOf(hero);
        Assert.Equal(1f, FogShade.WallLevel(f, world.Arena!.Iso, heroCell.X, heroCell.Y, hero, tick));

        // 주인공 바로 위 작은 사각형 = 다 보임 → 덧그릴 필요 없음. 시야 원 밖까지 걸친 사각형 → 필요
        Assert.False(FogShade.NeedsShape(f, new RectF(hero.X - 20, hero.Y - 70, hero.X + 20, hero.Y), hero, hero, tick));
        Assert.True(FogShade.NeedsShape(f, new RectF(hero.X - 20, hero.Y - 500, hero.X + 20, hero.Y), hero, hero, tick));
    }

    [Fact]
    public void GateSeals_Labels()
    {
        Assert.False(GateSeals.IsSealed(null, "gate_nw"));
        var (rewards, _) = TestDefs.Rewards();
        var tome = rewards[0];
        Assert.Equal(tome.Name, GateSeals.Label(new RoomExit("gate_nw", tome, RoomKind.Normal, null, false, null)));
        Assert.Equal($"정예 상자 + {tome.Name}", GateSeals.Label(new RoomExit("gate_nw", tome, RoomKind.Elite, null, false, null)));
        Assert.Equal("정예", GateSeals.Label(new RoomExit("gate_nw", null, RoomKind.Elite, null, false, null)));
        Assert.Equal("지하묘지로", GateSeals.Label(new RoomExit("gate_ne", null, RoomKind.Normal, null, true, "지하묘지")));
        Assert.Equal("▼ 2층", GateSeals.FloorLabel(new RoomExit("gate_ne", null, RoomKind.Normal, null, false, null, 2)));
        Assert.Null(GateSeals.FloorLabel(new RoomExit("gate_ne", null, RoomKind.Normal, null, false, null)));
    }
}
