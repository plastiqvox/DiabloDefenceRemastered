using DiabloDefenceRemastered.Core.Geometry;
using DiabloDefenceRemastered.Core.Nav;
using DiabloDefenceRemastered.Core.Sim.Movement;

namespace DiabloDefenceRemastered.Tests.Movement;

/// <summary>이동·충돌 시스템 (설계서 4.6·4.7, 10.1 <c>Movement</c>). NavModule 설치(실제 NavWorld·SpatialHash)로 돈다 — 다른 묶음 없이.</summary>
public class MovementTests
{
    internal static TestWorld Build(int megaTiles = 12)
    {
        var ctx = new ModuleContext();
        new NavModule().Register(ctx);
        return new TestWorldBuilder().WithMegaTiles(megaTiles).WithModules(ctx).Build();
    }

    private static TestWorld World() => Build();

    [Fact]
    public void Install_ReplacesStraightNavAndBruteSpatial()
    {
        var t = World();
        Assert.IsType<NavWorld>(t.World.Nav);
        Assert.IsType<Core.Spatial.SpatialHash>(t.World.Spatial);
        Assert.NotNull(t.Simulation.SystemAt(SimPhase.Locomotion));
        Assert.NotNull(t.Simulation.SystemAt(SimPhase.Collision));
    }

    [Fact]
    public void TwoEnemiesSamePoint_SeparateAndFinite()
    {
        var t = World();
        var p = t.Hero.Pos + new Vector2(150, 0);
        var a = t.AddEnemy(p);
        var b = t.AddEnemy(p);
        a.Intent = b.Intent = MoveIntent.Hold;
        t.Step();
        Assert.True(MathX.IsFinite(a.Pos) && MathX.IsFinite(b.Pos));
        Assert.True(Vector2.Distance(a.Pos, b.Pos) > 1f);
    }

    [Fact]
    public void TwentyMarchingFromOnePoint_ResolveWithin2s()
    {
        var t = World();
        var start = t.Hero.Pos + new Vector2(-200, -40);
        var list = new List<Enemy>();
        for (var i = 0; i < 20; i++)
        {
            var e = t.AddEnemy(start);
            e.Intent = MoveIntent.Heading(new Vector2(1, 0.2f));
            list.Add(e);
        }
        t.Step(120);
        foreach (var e in list) Assert.True(MathX.IsFinite(e.Pos));
        for (var i = 0; i < list.Count; i++)
            for (var j = i + 1; j < list.Count; j++)
                Assert.True(Vector2.Distance(list[i].Pos, list[j].Pos) >= list[i].Radius + list[j].Radius - 2f, $"{i}-{j} 겹침");
    }

    [Fact]
    public void SummonedInsidePillar_PushedOut()
    {
        var t = World();
        var pillar = t.CellCenter(6, 6);   // 기둥 (3,3) 위-왼 하위 칸
        var e = t.AddEnemy(pillar);
        e.Intent = MoveIntent.Hold;
        t.Step(3);
        var nav = (NavWorld)t.World.Nav;
        Assert.True(nav.WalkPenetration(e.Pos, false) <= 0.01f, $"{e.Pos}");
        Assert.True(nav.ExactClearance(e.Pos) >= e.Radius - 0.5f);
    }

    [Fact]
    public void EnteringEnemies_NotInHashNorSeparation()
    {
        var t = World();
        var p = t.Hero.Pos + new Vector2(150, 0);
        var active = t.AddEnemy(p);
        active.Intent = MoveIntent.Hold;
        var entering = t.AddEnemy(p + new Vector2(2, 0), life: LifeState.Entering);
        entering.Intent = MoveIntent.Hold;
        t.Step();
        Assert.Equal(p, active.Pos);   // 입장 중 적과 겹쳐도 안 밀림
        var buf = new int[8];
        Assert.Equal(1, t.World.Spatial.Query(p, 50f, buf));
    }

    [Fact]
    public void Intents_SixKinds()
    {
        var t = World();
        var hero = t.Hero.Pos;
        var origin = hero + new Vector2(200, 0);
        Enemy Add(MoveIntent intent)
        {
            var e = t.AddEnemy(origin);
            e.Intent = intent;
            return e;
        }
        // 서로 밀지 않게 하나씩 돌린다
        foreach (var (intent, check) in new (MoveIntent, Func<Enemy, bool>)[]
        {
            (MoveIntent.Hold, e => e.Pos == origin),
            (MoveIntent.SeekHero(), e => Vector2.Distance(e.Pos, hero) < 200f - 30f),
            (MoveIntent.SeekPoint(origin + new Vector2(0, 100)), e => e.Pos.Y > origin.Y + 30f),
            (MoveIntent.Away(hero), e => Vector2.Distance(e.Pos, hero) > 200f + 30f),
            (MoveIntent.Heading(new Vector2(0, -1)), e => e.Pos.Y < origin.Y - 30f),
            (MoveIntent.Straight(new Vector2(0, 1)), e => e.Pos.Y > origin.Y + 30f),
        })
        {
            var e = Add(intent);
            t.Step(60);
            Assert.True(check(e), $"{intent.Kind}: {e.Pos}");
            t.World.Enemies.Remove(e.Id);
            t.World.Enemies.Compact();
        }
    }

    [Fact]
    public void PhaseThrough_IgnoresPillar_StoppedByBoundary()
    {
        var t = World();
        var nav = (NavWorld)t.World.Nav;
        var collision = (CollisionSystem)t.Simulation.SystemAt(SimPhase.Collision)!;
        var pillar = t.CellCenter(6, 6);
        var p = pillar;
        collision.PushOut(ref p, 16f, boundaryOnly: true, new EntityId(1, 1));
        Assert.Equal(pillar, p);   // 안쪽 장애물은 건너뜀
        // 둘레(뒷벽 왼쪽 열 하위 (0, 12)) 안 → 밀려 나옴
        var wall = t.CellCenter(0, 12);
        var q = wall;
        collision.PushOut(ref q, 16f, boundaryOnly: true, new EntityId(1, 1));
        Assert.NotEqual(wall, q);
        Assert.True(nav.WalkPenetration(q, boundaryOnly: true) <= 0.01f);
        Assert.True(nav.Obstacles.Shapes.Where(s => s.IsBoundary && s.BlocksWalk).Any());
    }

    [Fact]
    public void HeroTouchingCount_AndBigEnemyPushesHero()
    {
        var t = World();
        var hero = t.Hero.Pos;
        for (var k = 0; k < 3; k++)
        {
            var e = t.AddEnemy(hero + MathX.FromAngle(k * 2.1f) * 20f);
            e.Intent = MoveIntent.Hold;
        }
        t.Step();
        Assert.Equal(3, t.Hero.TouchingCount);
        Assert.Equal(hero, t.Hero.Pos);   // 일반 적은 주인공을 못 민다
    }

    [Fact]
    public void Collision_WallStopsStraight_AndMoveBlockedTime()
    {
        var t = World();
        var pillar = t.CellCenter(6, 6);
        var e = t.AddEnemy(pillar + new Vector2(-60, 0));
        e.Intent = MoveIntent.Straight(new Vector2(1, 0));
        t.Step(90);
        var nav = (NavWorld)t.World.Nav;
        Assert.True(nav.WalkPenetration(e.Pos, false) <= 0.01f);
        Assert.True(e.MoveBlockedTime > 0.5f, $"막힌 시간 {e.MoveBlockedTime}");
    }

    [Fact]
    public void FlowEnemies_ReachHeroAroundPillars()
    {
        var t = World();
        var list = new List<Enemy>();
        var rng = new Random(2);
        var r = ResolvedMap.Resolve(t.World.Map!);
        foreach (var entry in r.Entries)
        {
            var e = t.AddEnemy(entry.Inside);
            e.Intent = MoveIntent.SeekHero();
            list.Add(e);
        }
        t.Step(60 * 15);
        foreach (var e in list) Assert.True(Vector2.Distance(e.Pos, t.Hero.Pos) < 80f, $"{e.Pos} → 주인공 {t.Hero.Pos}");
    }

    [Fact]
    public void AStarEnemy_FollowsPathAroundRoomWall()
    {
        var map = TestDefs.RoomMapOneDoor();
        var basic = TestDefs.Basic();
        var boss = TestDefs.Enemy(basic.SpriteSets[0], "test_boss", radius: 20f) with { Movement = MovementKind.Astar, Rank = DefRank.Boss };
        var defs = new GameDefs
        {
            Rules = basic.Rules, Sprites = basic.Sprites, SpriteSets = basic.SpriteSets, Weapons = basic.Weapons, Passives = basic.Passives,
            HeroClasses = basic.HeroClasses, Enemies = new DefTable<EnemyDef>([.. basic.Enemies.All, boss]),
            Tilesets = new DefTable<TilesetDef>([map.Tileset.Def]), Maps = new DefTable<MapDef>([map]), Stages = new DefTable<StageDef>([TestDefs.Stage(map)]),
        };
        var ctx = new ModuleContext();
        new NavModule().Register(ctx);
        var t = new TestWorldBuilder().WithDefs(defs).WithModules(ctx).Build();
        var iso = t.World.Arena!.Iso;
        t.PlaceHero(iso.ToWorld(new TileCoord(11, 11)));   // 방 안
        var e = t.AddEnemy(iso.ToWorld(new TileCoord(11, 3.5f)), "test_boss");   // 방 위쪽 벽 바깥
        e.Intent = MoveIntent.SeekHero();
        t.Step(60 * 20);
        Assert.True(Vector2.Distance(e.Pos, t.Hero.Pos) < 60f, $"보스 {e.Pos} → 주인공 {t.Hero.Pos}");
        Assert.True(((NavWorld)t.World.Nav).PathsSolved > 0);
    }

    [Fact]
    public void SteadyState_NoAllocations()
    {
        var t = World();
        var rng = new Random(4);
        var box = t.World.MapSize;
        var r = ResolvedMap.Resolve(t.World.Map!);
        for (var i = 0; i < 150; i++)
        {
            var p = r.HeroStart + new Vector2((float)rng.NextDouble() * 600 - 300, (float)rng.NextDouble() * 300 - 150);
            var e = t.AddEnemy(p);
            e.Intent = MoveIntent.SeekHero();
        }
        t.Step(120);
        var before = GC.GetAllocatedBytesForCurrentThread();
        t.Step(300);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Locomotion_Deterministic()
    {
        static ulong Run()
        {
            var t = World();
            var rng = new Random(8);
            for (var i = 0; i < 80; i++)
            {
                var e = t.AddEnemy(t.Hero.Pos + new Vector2((float)rng.NextDouble() * 500 - 250, (float)rng.NextDouble() * 300 - 150));
                e.Intent = MoveIntent.SeekHero();
            }
            t.Step(300);
            var h = 17UL;
            foreach (var e in t.World.Enemies.Items)
                h = h * 31 + (ulong)BitConverter.SingleToInt32Bits(e.Pos.X) * 7 + (ulong)BitConverter.SingleToInt32Bits(e.Pos.Y);
            return h;
        }
        Assert.Equal(Run(), Run());
    }
}
