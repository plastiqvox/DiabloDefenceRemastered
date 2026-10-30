namespace DiabloDefenceRemastered.Tests.Common;

public class BuffSlotsTests
{
    private static readonly BuffDef Command = new()
    {
        Id = "command",
        Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.SpeedPct] = 0.25f, [EnemyStatKey.DamagePct] = 0.25f },
    };
    private static readonly BuffDef WarCry = new() { Id = "war_cry", Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.DamagePct] = 0.30f } };
    private static readonly BuffDef Slow = new() { Id = "slow_hellfire", Stats = new Dictionary<EnemyStatKey, float> { [EnemyStatKey.SpeedPct] = -0.25f }, Debuff = "slow" };
    private static readonly DefTable<BuffDef> Table = new([Command, WarCry, Slow]);

    [Fact]
    public void DifferentBuffs_Add_SameBuff_KeepsLarger()
    {
        var slots = new BuffSlots();
        slots.Apply(Command, 0, 10, 1f, Immunity.None);
        slots.Apply(WarCry, 1, 10, 1f, Immunity.None);
        Assert.Equal(0.55f, slots.Sum(EnemyStatKey.DamagePct, Table), 1e-5f);   // 오라 + 함성 = ×1.55

        slots.Apply(WarCry, 1, 20, 0.5f, Immunity.None);                         // 같은 버프, 작은 값 → 큰 값 유지
        Assert.Equal(0.55f, slots.Sum(EnemyStatKey.DamagePct, Table), 1e-5f);
        slots.RemoveExpired(10);
        Assert.False(slots.Has(0));
        Assert.True(slots.Has(1));                                               // 끝 시각은 늦은 쪽(20)
        Assert.Equal(0.30f, slots.Sum(EnemyStatKey.DamagePct, Table), 1e-5f);
    }

    [Fact]
    public void Debuff_RespectsImmunity()
    {
        var slots = new BuffSlots();
        Assert.False(slots.Apply(Slow, 2, 10, 1f, Immunity.Slow | Immunity.Knockback));
        Assert.False(slots.Has(2));
        Assert.True(slots.Apply(Slow, 2, 10, 1f, Immunity.Knockback));
        Assert.Equal(-0.25f, slots.Sum(EnemyStatKey.SpeedPct, Table), 1e-5f);
    }

    [Fact]
    public void Full_ReplacesSoonestEnding()
    {
        var many = Enumerable.Range(0, 7).Select(i => new BuffDef { Id = $"b{i}" }).ToArray();
        var slots = new BuffSlots();
        for (var i = 0; i < 6; i++) slots.Apply(many[i], i, 100 + i, 1f, Immunity.None);
        slots.Apply(many[6], 6, 500, 1f, Immunity.None);
        Assert.False(slots.Has(0));   // 100 에 끝나는 칸이 바뀜
        Assert.True(slots.Has(6));
        Assert.True(slots.Has(5));
    }
}

public class TestKitTests
{
    [Fact]
    public void BruteForceSpatial_SkipsEnteringAndDead_AndReportsOverflow()
    {
        var t = new TestWorldBuilder().Build();
        t.AddEnemy(new Vector2(100, 100));
        t.AddEnemy(new Vector2(105, 100), life: LifeState.Entering);
        t.AddEnemy(new Vector2(110, 100), life: LifeState.Emerging);
        var dead = t.AddEnemy(new Vector2(115, 100));
        dead.Dead = true;
        t.AddEnemy(new Vector2(120, 100), life: LifeState.Stunned);
        t.AddEnemy(new Vector2(500, 500));

        Span<int> result = stackalloc int[8];
        var n = t.World.Spatial.Query(new Vector2(100, 100), 50, result);
        Assert.Equal(2, n);
        Assert.Equal(0, result[0]);
        Assert.Equal(4, result[1]);

        Span<int> tiny = stackalloc int[1];
        Assert.Equal(2, t.World.Spatial.Query(new Vector2(100, 100), 50, tiny));   // 넘쳐도 실제 개수
        Assert.Equal(0, tiny[0]);

        Assert.Equal(2, t.World.Spatial.QueryCapsule(new Vector2(90, 100), new Vector2(130, 100), 1, result));
        Assert.Equal(2, t.World.Spatial.CellCount(3, 3));
    }

    [Fact]
    public void StraightNav_ClearanceIsDistanceToBorder()
    {
        var nav = new StraightNav(new Vector2(200, 100));
        Assert.Equal(10f, nav.Clearance(new Vector2(10, 50)));
        Assert.True(nav.Clearance(new Vector2(-5, 50)) < 0);
        Assert.True(nav.HasClearLine(new Vector2(50, 50), new Vector2(150, 50), 20));
        Assert.False(nav.HasClearLine(new Vector2(50, 50), new Vector2(195, 50), 20));
        Assert.True(nav.IsInsideMap(new Vector2(0, 0)));
        Assert.False(nav.IsInsideMap(new Vector2(200, 0)));
    }

    [Fact]
    public void Builder_PlacesHeroAtMapStart_AndSetsServices()
    {
        var t = new TestWorldBuilder().WithMegaTiles(10).Build();
        // N = 20, 맵 중심 칸 (9.5, 9.5) → 월드 (640, 128 + 16·19 + 16) = (640, 448), 외곽 상자 1280 × (640 + 128)
        Assert.Equal(new Vector2(640, 448), t.Hero.Pos);
        Assert.Equal(new Vector2(1280, 768), t.World.MapSize);
        Assert.NotNull(t.World.Arena);
        Assert.Equal(20, t.World.Arena!.Size);
        Assert.Equal(t.Hero.Pos, t.World.Resolved!.HeroStart);
        Assert.Equal(FacingMath.ToVector(Compass.South), t.Hero.Facing);
        Assert.True(t.World.Nav.IsInsideMap(t.Hero.Pos));
        Assert.False(t.World.Nav.IsInsideMap(new Vector2(10, 10)));   // 상자 왼쪽 위 모서리는 마름모 밖
        Assert.NotNull(t.World.Nav);
        Assert.NotNull(t.World.Spatial);
        Assert.Same(t.Simulation, t.World.Simulation);
        Assert.False(t.World.Presentation.Enabled);
        var e = t.AddEnemy(new Vector2(10, 10));
        Assert.Equal(12f, e.Radius);                      // base_radius 12 × scale 1
    }

    [Fact]
    public void Builder_WithModules_InstallsModuleSystems()
    {
        var modules = new ModuleContext();
        var log = new List<(SimPhase, int)>();
        modules.AddWorldInstaller((w, b) => b.Add(SimPhase.Navigation, new PhaseRecorder(SimPhase.Navigation, log)));
        var t = new TestWorldBuilder().WithModules(modules).Build();
        t.Step(2);
        Assert.Equal(2, log.Count);
    }

    [Fact]
    public void Lighting_LoadsPointLights_ResolvedToWorld()
    {
        var lighting = new LightingDef
        {
            Ambient = new Rgb(215, 225, 200),
            Darkness = 0.5f,
            UnitMinLight = 0.6f,
            HeroLight = new HeroLightDef { Color = new Rgb(255, 240, 220), Radius = 420 },
            Lights =
            [
                new PointLightDef { Id = "altar", AtTile = new TileOrAnchor("center"), Color = new Rgb(255, 60, 40), Radius = 120,
                                    Pulse = new PulseDef { Period = 2, Amount = 0.15f } },
                new PointLightDef { Id = "torch", AtTile = new TileOrAnchor(new TileCoord(4, 4)), Radius = 140, Flicker = 0.1f },
            ],
        };
        var t = new TestWorldBuilder().WithDefs(TestDefs.Basic(10, lighting: lighting)).Build();
        var state = t.World.Lighting;
        Assert.Equal(0.5f, state.Darkness);
        Assert.Equal(0.6f, state.UnitMinLight);
        Assert.Equal(420f, state.HeroLightRadius);
        Assert.Equal(["altar", "torch"], state.Lights.Select(l => l.Id));   // 정의 순서
        Assert.True(state.TryGetLight("altar", out var altar));
        Assert.Equal(t.World.Resolved!.Anchor("center"), altar.Pos);          // 기준점 ID → 월드 px
        Assert.Equal(2f, altar.PulsePeriod);
        Assert.True(state.TryGetLight("torch", out var torch));
        Assert.Equal(t.World.Arena!.Iso.CellCenter(4, 4), torch.Pos);       // [tx,ty] → 월드 px

        Assert.True(state.SetLight("torch", torch with { Radius = 200 }));
        Assert.True(state.TryGetLight("torch", out torch));
        Assert.Equal(200f, torch.Radius);
        Assert.False(state.SetLight("nope", torch));
    }
}
