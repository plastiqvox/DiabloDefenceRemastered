using DiabloDefenceRemastered.Core.Data.Loading;
using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Core.Modules;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Sim.Heroes;
using DiabloDefenceRemastered.Core.Sim.Progression;
using DiabloDefenceRemastered.Tests.Combat;
using DiabloDefenceRemastered.Tests.Data;
using DiabloDefenceRemastered.Tests.Shops;
using static DiabloDefenceRemastered.Tests.Heroes.BlinkKit;

namespace DiabloDefenceRemastered.Tests.Heroes;

/// <summary>
/// 주인공 순간이동(보스 묶음 B — 설계서 6.20 ⑫~⑱·㉒, 기획 v0.9.5 3절 V21): 방향(이동 입력·서 있으면 바라보는 쪽)·160px·벽 앞에서 멈춤·24px 미만 못 함(칸 그대로)·칸 0 못 함,
/// 시전 0.15s(누른 뒤 9틱) 동안 제자리·맞음, 도착 무적 0.2s(피해 0·탄이 지나감), 충전 6s 에 한 칸, 방 시작 가득, 닫힌 문 틈 밖으로 안 나감, 죽음·끝난 판 무시, 도착 때 넉백 지움,
/// 판 해시(같은 씨앗 두 번 = 같은 해시, 순간이동 칸이 해시에 듦), 실제 데이터 엄격 로딩.
/// </summary>
public sealed class HeroBlinkTests
{
    private static readonly Vector2 Right = new(1f, 0f);

    /// <summary>누른 틱 + <paramref name="ticks"/> 틱을 돌린다(누른 뒤에도 같은 이동 입력을 계속 줌 — 시전 중 이동 무시 확인).</summary>
    private static void PressAndRun(TestWorld t, Vector2 move, int ticks)
    {
        t.AppTick(Blink(move));
        for (var i = 0; i < ticks; i++) t.AppTick(Walk(move));
    }

    [Fact]
    public void MovesAlongMoveInput()
    {
        var t = World();
        var start = t.Hero.Pos;
        PressAndRun(t, Right, 9);
        Assert.Equal(start.X + 160f, t.Hero.Pos.X, 3);
        Assert.Equal(start.Y, t.Hero.Pos.Y, 3);
        Assert.Equal(t.Hero.Pos, t.Hero.PrevPos);                    // 그림이 끌리지 않게(도착 틱 Pos = PrevPos)
        Assert.Equal(1, t.Hero.BlinkCharges);
        Assert.Equal(1, t.World.RunStats.Blinks);
        var cue = Assert.Single(ShopKit.Cues(t.World, CueKind.Blink));
        Assert.Equal(t.Hero.Pos, cue.Pos);
        Assert.Equal(2, t.World.Presentation.Effects.AsSpan().ToArray().Count(e => e.Def == Fx));   // 떠난 자리·도착 자리
    }

    [Fact]
    public void NoInput_UsesFacing()
    {
        var t = World();
        t.Hero.Facing = new Vector2(0f, -1f);
        var start = t.Hero.Pos;
        PressAndRun(t, Vector2.Zero, 9);
        Assert.Equal(start + new Vector2(0f, -160f), t.Hero.Pos);
    }

    [Fact]
    public void Distance160()
    {
        // 짧은 대각선 입력(길이 0.5)도 정규화 — 거리는 늘 160
        var t = World();
        var start = t.Hero.Pos;
        var move = new Vector2(0.3f, 0.4f);
        PressAndRun(t, move, 9);
        Assert.Equal(160f, Vector2.Distance(start, t.Hero.Pos), 2);
        Assert.Equal(new Vector2(0.6f, 0.8f).X, t.Hero.Facing.X, 4);
    }

    [Fact]
    public void DistanceStat_AddsToRule_FloorsAtMinDistance()
    {
        // blink_distance(숙련 Lv1·4 +24)는 규칙 거리에 더한다. 음수로 깎여도 min_distance 아래로는 안 줄어듦
        var t = World();
        t.Hero.Stats[StatKey.BlinkDistance] = 24f;
        var start = t.Hero.Pos;
        PressAndRun(t, Right, 9);
        Assert.Equal(start.X + 184f, t.Hero.Pos.X, 3);

        var rules = t.World.Defs.Rules.Hero!.Blink!;
        t.Hero.Stats[StatKey.BlinkDistance] = -1000f;
        Assert.Equal(rules.MinDistance, Core.Sim.Heroes.HeroBlink.Distance(t.Hero, rules));
    }

    [Fact]
    public void StopsBeforeWall()
    {
        // 벽 x = 시작 + 100: 4px 마다 clearance ≥ 몸 반경 14 인 마지막 자리 = 시작 + 84
        var t = World(nav: w => new WallXNav(w.Nav, w.MapSize.X * 0.5f + 100f));
        var start = t.Hero.Pos;
        PressAndRun(t, Right, 9);
        Assert.Equal(start.X + 84f, t.Hero.Pos.X, 3);
        Assert.True(t.World.Nav.Clearance(t.Hero.Pos) >= t.Hero.Radius);
    }

    [Fact]
    public void Under24px_DeniedKeepsCharge()
    {
        // 벽 x = 시작 + 30: 갈 수 있는 것은 16px(< 24) — 칸을 쓰지 않고 못 함
        var t = World(nav: w => new WallXNav(w.Nav, w.MapSize.X * 0.5f + 30f));
        var start = t.Hero.Pos;
        t.AppTick(Blink(Right));
        Assert.Equal(2, t.Hero.BlinkCharges);
        Assert.Equal(0f, t.Hero.BlinkCast);
        Assert.Equal(t.World.Tick - 1, t.Hero.BlinkDeniedTick);
        var denied = Assert.Single(ShopKit.Cues(t.World, CueKind.BlinkDenied));
        Assert.Null(denied.Subject);
        Assert.Equal(0, t.World.RunStats.Blinks);
        for (var i = 0; i < 20; i++) t.AppTick();
        Assert.Equal(start, t.Hero.Pos);
        Assert.Empty(ShopKit.Cues(t.World, CueKind.Blink));
    }

    [Fact]
    public void NoCharge_Denied()
    {
        var t = World();
        t.Hero.BlinkCharges = 0;
        t.AppTick(Blink(Right));
        Assert.Single(ShopKit.Cues(t.World, CueKind.BlinkDenied));
        Assert.Equal(0f, t.Hero.BlinkCast);

        // 시전 중 다시 누름도 못 함(칸 그대로)
        var u = World();
        u.AppTick(Blink(Right));
        u.AppTick(Blink(Right));
        Assert.Equal(1, u.Hero.BlinkCharges);
        Assert.Single(ShopKit.Cues(u.World, CueKind.BlinkDenied));
    }

    [Fact]
    public void Cast015_HoldsNineTicksThenMoves()
    {
        var t = World();
        var start = t.Hero.Pos;
        t.AppTick(Blink(Right));
        Assert.Equal(0.15f, t.Hero.BlinkCast, 5);
        for (var i = 1; i <= 8; i++)
        {
            t.AppTick(Walk(new Vector2(0f, 1f)));                         // 시전 중 이동 입력은 무시(제자리·방향 고정)
            Assert.Equal(start, t.Hero.Pos);
            Assert.Equal(MoveKind.Hold, t.Hero.Intent.Kind);
            Assert.Equal(Right, t.Hero.Facing);
            Assert.True(t.Hero.BlinkCast > 0f, $"{i}틱째에 시전이 끝났다");
            Assert.Equal(ClipKind.CastMagic, t.Hero.Anim.Clip);
        }
        t.AppTick(Walk(new Vector2(0f, 1f)));                             // 누른 뒤 9틱째(0.15s) 도착 — 방향은 누른 틱의 입력
        Assert.Equal(start + Right * 160f, t.Hero.Pos);
        Assert.Equal(0f, t.Hero.BlinkCast);
        Assert.Equal(MoveKind.Hold, t.Hero.Intent.Kind);
        t.AppTick(Walk(new Vector2(0f, 1f)));
        Assert.Equal(MoveKind.Heading, t.Hero.Intent.Kind);              // 다음 틱부터 다시 걷기
    }

    [Fact]
    public void HitDuringCast_TakesDamage()
    {
        var t = World();
        t.AppTick(Blink(Right));
        var hp = t.Hero.Hp;
        t.World.Damage.ToHero(EntityId.None, 10f);
        t.AppTick();
        Assert.True(t.Hero.BlinkCast > 0f);
        Assert.Equal(hp - 10f, t.Hero.Hp);
    }

    [Fact]
    public void ArrivalInvuln_NoDamageAndBulletPasses()
    {
        var t = World();
        PressAndRun(t, Right, 9);
        Assert.Equal(0.2f, t.Hero.BlinkInvulnTime, 4);
        var hp = t.Hero.Hp;

        // 장판·몸 같은 요청은 버려지고(넉백도), 탄은 몸을 지나간다(지워지지 않음)
        t.World.Damage.ToHero(EntityId.None, 10f, Right, 40f);
        var pj = CombatKit.Projectile("t_pj_bullet", 5f, 600f, range: 900f, blocked: false);
        var id = t.World.Spawner.SpawnProjectile(new EnemyProjectileSpawn(pj, t.Hero.Pos + new Vector2(-20f, 0f), Right, 9, EntityId.None));
        for (var i = 0; i < 4; i++) t.AppTick();                          // 10px/틱 — 4틱 뒤 주인공 20px 너머
        Assert.Equal(hp, t.Hero.Hp);
        Assert.Equal(0f, t.Hero.KnockbackTime);
        var bullet = t.World.Projectiles.Get(id);
        Assert.NotNull(bullet);
        Assert.True(bullet!.Pos.X > t.Hero.Pos.X, "탄이 주인공을 지나가지 않았다");

        // 0.2s(12틱) 뒤에는 다시 맞는다
        for (var i = 0; i < 8; i++) t.AppTick();
        Assert.Equal(0f, t.Hero.BlinkInvulnTime);
        t.World.Damage.ToHero(EntityId.None, 10f);
        t.AppTick();
        Assert.Equal(hp - 10f, t.Hero.Hp);
    }

    [Fact]
    public void Recharge_OneEvery6s()
    {
        var t = World();
        t.AppTick(Blink(Right));
        Assert.Equal(1, t.Hero.BlinkCharges);
        Assert.Equal(0f, t.Hero.BlinkRecharge);
        for (var i = 0; i < 359; i++) t.AppTick();
        Assert.Equal(1, t.Hero.BlinkCharges);
        t.AppTick();                                                      // 누른 뒤 360틱 = 6초
        Assert.Equal(2, t.Hero.BlinkCharges);
        Assert.Equal(0f, t.Hero.BlinkRecharge);                           // 가득이면 0
        for (var i = 0; i < 400; i++) t.AppTick();
        Assert.Equal(2, t.Hero.BlinkCharges);                             // 최대를 넘지 않음

        // 판 시간이 멈추면(레벨업 창) 충전도 멈춤
        var u = World();
        u.AppTick(Blink(Right));
        for (var i = 0; i < 30; i++) u.AppTick();
        var r = u.Hero.BlinkRecharge;
        ((ProgressionService)u.World.Progression!).AddXp(5);
        u.AppTick();
        Assert.Equal(ChoiceKind.LevelUp, u.World.AwaitingChoice);
        r = u.Hero.BlinkRecharge;
        for (var i = 0; i < 120; i++) Assert.Equal(StageStep.AwaitingChoice, u.AppTick(Blink(Right)));
        Assert.Equal(r, u.Hero.BlinkRecharge);
        Assert.Equal(1, u.Hero.BlinkCharges);
    }

    [Fact]
    public void RoomStart_FullCharges()
    {
        var t = World();
        Assert.Equal(2, t.Hero.BlinkCharges);
        Assert.Equal(0f, t.Hero.BlinkRecharge);

        // 개발 옵션 --passives 숙련 Lv2 → 3칸 가득
        var dev = CombatKit.World(Defs());
        new HeroRunSetup().Apply(dev.World, new RunOptions { Passives = [new LoadoutEntry(Mastery, 2)] });
        Assert.Equal(3, dev.Hero.BlinkCharges);

        // 짐으로 넘긴 다음 방: 칸은 짐에 없다 — 앞 방에서 다 썼어도 새 방은 가득(숙련 Lv2 = 3칸)
        var a = World();
        SetMastery(a.World, 2);
        a.Hero.BlinkCharges = 0;
        var carry = new HeroCarrier().Capture(a.World);
        var b = World();
        new HeroCarrier().Restore(b.World, carry);
        Assert.Equal(3, b.Hero.BlinkCharges);
        Assert.Equal(0f, b.Hero.BlinkRecharge);
    }

    [Fact]
    public void ClosedDoor_StaysInsideMap()
    {
        // 실제 내비(WP2 NavWorld — 벽·문 틈 도형)의 합성 방 맵. 정리 전(생존 단계)이라 문은 닫혀 있다. 문 틈 안쪽에서 바깥으로 순간이동해도 맵 안에 선다
        var t = new TestWorldBuilder().WithSeed(1).WithDefs(Defs()).WithRoom(RoomPhase.Survive, fakeSpawner: false).WithModules(CombatKit.Modules())
            .WithSystem(SimPhase.DeathHooks, new FakeDeathHooks()).WithPresentation().Setup(w => w.Nav = NavWorld.Build(w)).Build();
        var w = t.World;
        var nav = w.Nav;
        Assert.NotEmpty(w.Gates);
        var inside = w.Resolved!.HeroStart;
        var checkedGates = 0;
        foreach (var (gateId, zone) in w.Gates)
        {
            if (zone.Cells.Count == 0) continue;
            var gate = Vector2.Zero;
            foreach (var c in zone.Cells) gate += t.CellCenter(c.X, c.Y);
            gate /= zone.Cells.Count;
            var inward = Vector2.Normalize(inside - gate);
            var from = gate + inward * 40f;
            for (var k = 0; k < 20 && nav.Clearance(from) < t.Hero.Radius; k++) from += inward * 8f;
            Assert.True(nav.Clearance(from) >= t.Hero.Radius, $"{gateId}: 시작 자리를 못 찾음");
            t.PlaceHero(from);
            t.Hero.BlinkCharges = 2;
            t.Hero.BlinkCast = 0f;
            t.Hero.BlinkInvulnTime = 0f;
            PressAndRun(t, -inward, 9);
            Assert.True(nav.IsInsideMap(t.Hero.Pos), $"{gateId}: 맵 밖 {t.Hero.Pos}");
            Assert.True(nav.Clearance(t.Hero.Pos) >= t.Hero.Radius - 1e-3f, $"{gateId}: 벽 안 {t.Hero.Pos} clearance {nav.Clearance(t.Hero.Pos)}");
            checkedGates++;
        }
        Assert.True(checkedGates >= 2);
    }

    [Fact]
    public void DeadOrEnded_Ignored()
    {
        var t = World();
        t.Hero.Dead = true;
        t.World.Command = Blink(Right);
        t.Step();
        Assert.Equal(2, t.Hero.BlinkCharges);
        Assert.Equal(0f, t.Hero.BlinkCast);
        Assert.Empty(ShopKit.Cues(t.World, CueKind.BlinkDenied));

        var u = World();
        u.World.Outcome = RunOutcome.Victory;
        u.World.Command = Blink(Right);
        u.Step();
        Assert.Equal(2, u.Hero.BlinkCharges);
        Assert.Empty(ShopKit.Cues(u.World, CueKind.BlinkDenied));
        Assert.Equal(StageStep.Finished, u.AppTick(Blink(Right)));

        // 규칙이 없는 정의: 늘 못 함(칸 0)
        var n = World(Defs(noBlink: true));
        Assert.Equal(0, n.Hero.BlinkCharges);
        n.AppTick(Blink(Right));
        Assert.Single(ShopKit.Cues(n.World, CueKind.BlinkDenied));
    }

    [Fact]
    public void Knockback_CancelledOnArrival()
    {
        var t = World();
        t.AppTick(Blink(Right));
        for (var i = 0; i < 8; i++) t.AppTick();
        t.Hero.KnockbackVel = new Vector2(300f, 0f);
        t.Hero.KnockbackTime = 0.12f;
        t.Hero.Vel = new Vector2(50f, 0f);
        t.AppTick();                                                      // 도착 틱
        Assert.Equal(0f, t.Hero.KnockbackTime);
        Assert.Equal(Vector2.Zero, t.Hero.KnockbackVel);
        Assert.Equal(Vector2.Zero, t.Hero.Vel);
    }

    [Fact]
    public void Hash_SameSeedTwice_SameHash_BlinkFieldsInHash()
    {
        static ulong Run()
        {
            var t = World(seed: 5);
            var moves = new[] { Right, new Vector2(0f, 1f), Vector2.Zero, new Vector2(-1f, -1f) };
            for (var i = 0; i < 600; i++)
                t.AppTick(i % 50 == 0 ? Blink(moves[i / 50 % moves.Length]) : Walk(moves[i / 50 % moves.Length]));
            Assert.True(t.World.RunStats.Blinks >= 3, $"순간이동 {t.World.RunStats.Blinks}번");   // 칸 2 + 6초에 하나 — 나머지 누름은 못 함
            return StateHasher.Room(t.World);
        }
        Assert.Equal(Run(), Run());

        var baseHash = StateHasher.Room(World().World);
        var changes = new Action<Hero>[]
        {
            h => h.BlinkCharges = 1, h => h.BlinkRecharge = 0.5f, h => h.BlinkCast = 0.1f, h => h.BlinkDir = Right, h => h.BlinkInvulnTime = 0.1f,
        };
        var seen = new HashSet<ulong> { baseHash };
        foreach (var change in changes)
        {
            var w = World().World;
            change(w.Hero);
            Assert.True(seen.Add(StateHasher.Room(w)));
        }
    }

    [Fact]
    public void RealData_LoadsStrict_BlinkRulesMasteryLabelsFxSound()
    {
        var ctx = GameModules.CreateContext();
        var r = DataLoader.Load(RealData.Dir, ctx.Parts, ctx.Validators);
        Assert.False(r.HasErrors, string.Join("\n", r.Errors));
        var defs = r.Defs;

        var blink = defs.Rules.Hero!.Blink!;
        Assert.Equal((1, 6f, 0.15f, 96f, 0.2f, 24f), (blink.Charges, blink.Recharge, blink.Cast, blink.Distance, blink.Invuln, blink.MinDistance));   // 2026-11-13: 1칸·96px 시작
        var fx = blink.Effect!.Effect.Def;
        Assert.Equal("fx_blink", fx.Id);
        Assert.Same(defs.Sprites.Get("miniltng"), fx.Sprite!.Def);
        Assert.Equal(BlendKind.Additive, fx.Blend);
        Assert.Equal(new Rgb(170, 200, 255), fx.Tint);

        var mastery = defs.Passives.Get("blink_mastery");
        Assert.Equal(5, mastery.MaxLevel);
        Assert.Empty(mastery.PerLevel);
        Assert.Null(mastery.Icon!.Tint);
        Assert.Equal("icon_teleport", mastery.Icon.Sprite.Id);
        Assert.Equal([StatKey.BlinkDistance, StatKey.BlinkCharges, StatKey.BlinkRechargePct, StatKey.BlinkDistance, StatKey.BlinkCast],
                     mastery.Levels!.Select(l => Assert.Single(l).Key));
        Assert.Equal([24f, 1f, -0.15f, 24f, -0.10f], mastery.Levels!.Select(l => Assert.Single(l).Value));

        var labels = defs.Rules.LevelUp!.StatLabels;
        Assert.Equal("순간이동 칸", labels["blink_charges"].Label);
        Assert.Equal("s", labels["blink_cast"].Unit);
        Assert.Equal(StatFormat.Pct, labels["blink_recharge_pct"].Format);
        Assert.Equal("순간이동 거리", labels["blink_distance"].Label);

        var sound = defs.SoundBindings.Ui["blink"].Def;
        Assert.Equal("snd_blink", sound.Id);
        Assert.Equal(["d1/sound/sfx/misc/holybolt.wav"], sound.Files);
        Assert.Equal(0.7f, sound.Volume);
        Assert.False(defs.SoundBindings.Ui.ContainsKey("blink_denied"));
    }
}
