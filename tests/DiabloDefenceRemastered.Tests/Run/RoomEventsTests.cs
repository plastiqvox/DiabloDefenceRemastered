namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>10.1 <c>Run</c> 행 — <c>RoomEvents.Schedule</c>·<c>RemapEntry</c>(서로 바꾸기)·<c>LightingState.Apply</c>.</summary>
public class RoomEventsTests
{
    private static readonly GameDefs Defs = RoomActs.Defs();
    private static RoomRulesDef Rules => Defs.Rules.Rooms!;

    private static RoomPlan PlanFor(RoomDef def, string gateIn = "gate_sw") =>
        new(1, 0, def, Defs.Maps.Get(RoomActs.RoomA), gateIn, [], [], 1);

    private static StageDef Act(params PartDef[] events) => Defs.Stages.Get(RoomActs.Act1Id) with { Events = events };

    [Fact]
    public void T_ToRoomClock_GraceFloor_OutOfRangeDropped_DefinitionOrderOnTies()
    {
        var a = RoomActs.Event("a", 60f);     // 방 시작 = 0:00 → 유예 0:03
        var b = RoomActs.Event("b", 75f);     // 0:15
        var c = RoomActs.Event("c", 61f);     // 0:01 → 0:03, a 와 같은 틱 → 정의 순서
        var late = RoomActs.Event("late", 135f);   // 방 끝(끝 미포함) → 버림
        var early = RoomActs.Event("early", 30f);  // 앞 방 → 버림
        var noAt = new PartDef(PartKind.StageEvent, "x", null, null, new object(), new object());
        var s = RoomEvents.Schedule(Act(b, a, late, early, c, noAt), PlanFor(TestDefs.Room(RoomKind.Normal, 60f, 135f, 2)), Rules, 0);
        Assert.Equal([("a", 180), ("c", 180), ("b", 900)], s.Events.Select(e => (e.Event.Type, e.ClockTick)));
        Assert.Empty(s.Replay);
    }

    [Fact]
    public void BossRoom_HasNoEnd()
    {
        var s = RoomEvents.Schedule(Act(RoomActs.Event("x", 1000f)), PlanFor(TestDefs.Room(RoomKind.Boss, 360f, null, 6)), Rules, 0);
        Assert.Equal([(1000 - 360) * 60], s.Events.Select(e => e.ClockTick));
    }

    [Fact]
    public void PastLightChange_Replayed_InTimeOrder_OthersDropped()
    {
        var l2 = RoomActs.LightChange(90f, new LightSet("darkness", [0.5f]));
        var l1 = RoomActs.LightChange(30f, new LightSet("darkness", [0.3f]));
        var banner = RoomActs.Event("banner", 40f);
        var s = RoomEvents.Schedule(Act(l2, banner, l1), PlanFor(TestDefs.Room(RoomKind.Normal, 135f, 210f, 3)), Rules, 0);
        Assert.Equal([l1, l2], s.Replay);
        Assert.Empty(s.Events);
    }

    [Fact]
    public void StartAt_DropsEarlierRoomEvents_ButStateEventsGoToReplay()
    {
        var past = RoomActs.LightChange(10f, new LightSet("darkness", [0.2f]));
        var inRoomLight = RoomActs.LightChange(70f, new LightSet("darkness", [0.4f]));   // 방 시계 0:10
        var inRoomBanner = RoomActs.Event("banner", 70f);
        var afterStart = RoomActs.Event("march", 100f);                                    // 0:40
        var s = RoomEvents.Schedule(Act(past, inRoomLight, inRoomBanner, afterStart), PlanFor(TestDefs.Room(RoomKind.Normal, 60f, 135f, 2)), Rules, 30 * 60);
        Assert.Equal([past, inRoomLight], s.Replay);
        Assert.Equal([("march", 40 * 60)], s.Events.Select(e => (e.Event.Type, e.ClockTick)));
    }

    [Fact]
    public void RemapEntry_SwapsEntryAndOpposite()
    {
        // 기획 10.3 6:30 행진(at_entry gate_se · toward_entry gate_nw)
        var fromSw = new TestWorldBuilder().WithRoom(gateIn: "gate_sw").Build().World.Room!;
        Assert.Equal("gate_se", fromSw.RemapEntry("gate_se"));
        Assert.Equal("gate_nw", fromSw.RemapEntry("gate_nw"));
        Assert.Equal("gate_ne", fromSw.RemapEntry("gate_sw"));   // 들어온 문 → 맞은편
        Assert.Equal("gate_sw", fromSw.RemapEntry("gate_ne"));   // 맞은편 → 들어온 문

        var fromSe = new TestWorldBuilder().WithRoom(gateIn: "gate_se").Build().World.Room!;
        var start = fromSe.RemapEntry("gate_se");
        var target = fromSe.RemapEntry("gate_nw");
        Assert.Equal("gate_nw", start);
        Assert.Equal("gate_se", target);                          // 시작과 목표가 함께 바뀜
        Assert.NotEqual(start, target);
        Assert.Equal("gate_ne", fromSe.RemapEntry("gate_ne"));   // 들어온 문도 맞은편도 아닌 문
        Assert.Equal("gate_sw", fromSe.RemapEntry("gate_sw"));
    }

    [Fact]
    public void LightingApply_FivePathKinds()
    {
        var l = new LightingState();
        var map = ResolvedMap.Resolve(TestDefs.Map());
        l.Load(new LightingDef { Lights = [new PointLightDef { Id = "fire", AtTile = new TileOrAnchor("center"), Radius = 100f }] }, map);
        l.Apply(new LightChangeParams(
        [
            new LightSet("ambient", [10f, 20f, 30f]),
            new LightSet("darkness", [0.7f]),
            new LightSet("unit_min_light", [0.6f]),
            new LightSet("hero_light.color", [255f, 200f, 100f]),
            new LightSet("hero_light.radius", [400f]),
            new LightSet("lights.fire.radius", [150f]),
            new LightSet("lights.fire.color", [1f, 2f, 3f]),
            new LightSet("lights.fire.flicker", [0.2f]),
            new LightSet("lights.fire.pulse.period", [2f]),
            new LightSet("lights.fire.pulse.amount", [0.1f]),
        ]));
        Assert.Equal(new Rgb(10, 20, 30), l.Ambient);
        Assert.Equal(0.7f, l.Darkness);
        Assert.Equal(0.6f, l.UnitMinLight);
        Assert.Equal(new Rgb(255, 200, 100), l.HeroLightColor);
        Assert.Equal(400f, l.HeroLightRadius);
        Assert.True(l.TryGetLight("fire", out var fire));
        Assert.Equal(150f, fire.Radius);
        Assert.Equal(new Rgb(1, 2, 3), fire.Color);
        Assert.Equal(0.2f, fire.Flicker);
        Assert.Equal(2f, fire.PulsePeriod);
        Assert.Equal(0.1f, fire.PulseAmount);

        Assert.Throws<ArgumentException>(() => l.Apply(new LightChangeParams([new LightSet("lights.none.radius", [1f])])));
        Assert.Throws<ArgumentException>(() => l.Apply(new LightChangeParams([new LightSet("fog", [1f])])));
        Assert.Throws<ArgumentException>(() => l.Apply(new LightChangeParams([new LightSet("darkness", [1f, 2f])])));
    }
}
