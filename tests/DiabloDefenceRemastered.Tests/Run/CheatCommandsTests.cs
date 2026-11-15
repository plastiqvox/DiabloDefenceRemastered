using DiabloDefenceRemastered.Core.Dev;

namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>
/// 치트 콘솔 명령(2026-11-15 임시 — "패턴보고 싶은데 치트키로 넘어가게", "`로 퀘이크처럼"): clear 는 보상 단계로, next·room·boss 는 같은 액트 방으로
/// (지금 방은 문 없이 닫음, 짐 유지), god 은 다음 방에도 이어짐. 합성 액트(<see cref="RoomActs"/>).
/// </summary>
public class CheatCommandsTests
{
    private static ModuleContext Modules()
    {
        var m = new ModuleContext();
        m.AddWorldInstaller((w, _) =>
        {
            w.Presentation.Enabled = false;
            w.Nav = new StraightNav(w.Arena!.Iso);
            w.Spatial = new BruteForceSpatial(w);
            w.Spawner = new FakeSpawner(w);
        });
        return m;
    }

    private static RunSession Session() =>
        new(RoomActs.Defs(), Modules(), new RunStart(11, RoomActs.Defs().Stages.Get(RoomActs.Act1Id), null, RunOptions.Default, 0));

    [Fact]
    public void Clear_SurviveGoesToReward_OnlyBeforeReward()
    {
        var s = Session();
        var w = s.World;
        for (var i = 0; i < 5 * 60 && w.Room!.Phase != RoomPhase.Survive; i++) StageStepper.AppTick(w, PlayerCommand.None);
        Assert.Equal(RoomPhase.Survive, w.Room!.Phase);

        var r = CheatCommands.Run(s, "clear");
        Assert.Equal("방 클리어", r.Message);
        Assert.Null(r.WarpTo);
        Assert.Equal(RoomPhase.Reward, w.Room.Phase);
        foreach (var e in w.Enemies.Items) Assert.False(e.IsAlive);
        Assert.NotEqual("방 클리어", CheatCommands.Run(s, "clear").Message);   // 이미 보상 단계
    }

    [Fact]
    public void NextRoomBoss_WarpKeepsActAndClosesRoomWithoutDoor()
    {
        var s = Session();
        var count = s.Run.Act.Rooms.Count;
        Assert.True(count >= 3);

        var r = CheatCommands.Run(s, "next");
        Assert.Equal(1, r.WarpTo);
        s.Warp(r.WarpTo!.Value);
        Assert.Equal(1, s.Run.RoomIndex);
        Assert.Single(s.Run.Rooms);
        Assert.Null(s.Run.Rooms[0].Door);                    // 문 없이 닫음

        r = CheatCommands.Run(s, "room 3");
        Assert.Equal(2, r.WarpTo);
        s.Warp(2);
        Assert.Equal(2, s.Run.RoomIndex);

        Assert.Equal(count - 1, CheatCommands.Run(s, "boss").WarpTo);
        Assert.Null(CheatCommands.Run(s, $"room {count + 1}").WarpTo);   // 범위 밖
        Assert.Null(CheatCommands.Run(s, "room x").WarpTo);
        Assert.StartsWith("모르는 명령", CheatCommands.Run(s, "fly").Message);
        Assert.Contains("clear", CheatCommands.Run(s, "help").Message);
        Assert.Contains("3:", CheatCommands.Run(s, "rooms").Message);
    }

    [Fact]
    public void God_TogglesAndCarriesToNextRoom()
    {
        var s = Session();
        Assert.False(s.World.Options.God);
        Assert.Equal("무적 켬", CheatCommands.Run(s, "GOD ").Message);   // 대소문자·띄어쓰기 무시
        Assert.True(s.World.Options.God);
        s.Warp(1);
        Assert.True(s.World.Options.God);
        Assert.Equal("무적 끔", CheatCommands.Run(s, "god").Message);
        Assert.False(s.World.Options.God);
    }
}
