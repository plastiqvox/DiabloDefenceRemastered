using DiabloDefenceRemastered.Core.Dev;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Dev;

/// <summary>상태 해시(설계서 3.4, 10.1 <c>Dev</c> — WP7).</summary>
public sealed class StateHasherTests
{
    [Fact]
    public void SameState_SameValue()
    {
        var a = new TestWorldBuilder().WithSeed(5).Build();
        var b = new TestWorldBuilder().WithSeed(5).Build();
        a.AddEnemy(new Vector2(300, 300));
        b.AddEnemy(new Vector2(300, 300));
        a.Step(3);
        b.Step(3);
        Assert.Equal(StateHasher.Room(a.World), StateHasher.Room(b.World));
    }

    [Fact]
    public void DifferentSeedOrEnemyOrHeroClass_DifferentValue()
    {
        var a = new TestWorldBuilder().WithSeed(5).Build();
        Assert.NotEqual(StateHasher.Room(a.World), StateHasher.Room(new TestWorldBuilder().WithSeed(6).Build().World));
        var b = new TestWorldBuilder().WithSeed(5).Build();
        b.AddEnemy(new Vector2(300, 300));
        Assert.NotEqual(StateHasher.Room(a.World), StateHasher.Room(b.World));
        var basic = TestDefs.Basic();
        var other = basic.HeroClasses[0] with { Id = "other_class" };
        var c = new TestWorldBuilder().WithSeed(5).WithHeroClass(other).Build();
        Assert.NotEqual(StateHasher.Room(a.World), StateHasher.Room(c.World));
    }

    /// <summary>가 본 칸 집합만 다르면 다른 값(개수가 아니라 집합 — 3.4).</summary>
    [Fact]
    public void ExploredSetOnly_DifferentValue()
    {
        var a = RealData.CathedralWorld();
        var b = RealData.CathedralWorld();
        var start = a.Hero.Pos;
        var far = a.CellCenter(10, 30);
        a.PlaceHero(far);
        a.Step(1);
        a.PlaceHero(start);
        a.Step(1);
        b.PlaceHero(start);
        b.Step(2);
        Assert.Equal(a.Hero.Pos, b.Hero.Pos);
        Assert.NotEqual(a.World.Visibility.ExploredHash, b.World.Visibility.ExploredHash);
        Assert.NotEqual(StateHasher.Room(a.World), StateHasher.Room(b.World));
    }

    [Fact]
    public void RoomHash_IgnoresRunStatsAndFlow_RunHashDoesNot()
    {
        var defs = RoomActs.Defs();
        RunSession Start() => new(defs, new ModuleContext(), new RunStart(9, defs.Stages[0], defs.HeroClasses[0], RunOptions.Default));
        var a = Start();
        var b = Start();
        Assert.Equal(StateHasher.Room(a.World), StateHasher.Room(b.World));
        Assert.Equal(StateHasher.Run(a.Run, a.World), StateHasher.Run(b.Run, b.World));
        b.Run.Stats.Kills += 3;
        Assert.Equal(StateHasher.Room(a.World), StateHasher.Room(b.World));
        Assert.NotEqual(StateHasher.Run(a.Run, a.World), StateHasher.Run(b.Run, b.World));
    }

    [Fact]
    public void RoomStateChanges_ChangeRoomHash()
    {
        var a = new TestWorldBuilder().WithSeed(5).WithRoom(RoomPhase.Survive, clockTicks: 100).Build();
        var b = new TestWorldBuilder().WithSeed(5).WithRoom(RoomPhase.Survive, clockTicks: 101).Build();
        var c = new TestWorldBuilder().WithSeed(5).WithRoom(RoomPhase.Cleanup, clockTicks: 100).Build();
        Assert.NotEqual(StateHasher.Room(a.World), StateHasher.Room(b.World));
        Assert.NotEqual(StateHasher.Room(a.World), StateHasher.Room(c.World));
    }

    [Fact]
    public void CsvRow_HasFiveColumns()
    {
        var defs = RoomActs.Defs();
        var s = new RunSession(defs, new ModuleContext(), new RunStart(9, defs.Stages[0], defs.HeroClasses[0], RunOptions.Default));
        var row = StateHasher.CsvRow(s.World).Split(',');
        Assert.Equal(5, row.Length);
        Assert.Equal("1", row[0]);
        Assert.Equal("1", row[1]);
        Assert.Equal(StateHasher.Hex(StateHasher.Run(s.Run, s.World)), row[4]);
    }
}
