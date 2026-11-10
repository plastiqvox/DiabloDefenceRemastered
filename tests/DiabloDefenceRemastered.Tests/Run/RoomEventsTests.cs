namespace DiabloDefenceRemastered.Tests.Run;

/// <summary>10.1 <c>Run</c> 행 — <c>RoomEvents.Schedule</c>·<c>CheckPlacement</c>(v0.8)·<c>RemapEntry</c>/<c>RemapMarch</c>(v0.8.1 시작 문 기준)·<c>LightingState.Apply</c>.</summary>
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
    public void StartAt_EntranceEventsMovedToStartClock_SoEliteRoomCanEnd()
    {
        // 정예 방(4:00~)을 --start-at 0:30 으로: 0:03 의 miniboss 는 버리지 않고 방 시계 0:30 으로(v0.8, 기획 v0.8.1 15절), 다른 지난 이벤트는 버림
        var miniboss = RoomActs.Event("miniboss", 210f);
        var ring = RoomActs.Event("ring", 215f);                                            // 0:05 → 버림
        var later = RoomActs.Event("elite_pack", 250f);                                     // 0:40
        var elite = TestDefs.Room(RoomKind.Elite, 210f, 270f, 4);
        var s = RoomEvents.Schedule(Act(ring, later, miniboss), PlanFor(elite), Rules, 30 * 60);
        Assert.Equal([("miniboss", 30 * 60), ("elite_pack", 40 * 60)], s.Events.Select(e => (e.Event.Type, e.ClockTick)));

        // 보스 방 0:03 보스 → 시작 시각 1:00 으로
        var boss = RoomEvents.Schedule(Act(RoomActs.Event("boss", 363f)), PlanFor(TestDefs.Room(RoomKind.Boss, 360f, null, 6)), Rules, 60 * 60);
        Assert.Equal([("boss", 60 * 60)], boss.Events.Select(e => (e.Event.Type, e.ClockTick)));
    }

    [Fact]
    public void CheckPlacement_StrongBefore20sIsError_NoClimaxNearEndIsWarning_EntranceIsFine()
    {
        // 액트 1: 방 2(1:00~2:15, 생존 75s) 0:10 의 ring → 오류, 방 2 끝 0:45 안 절정 없음 → 경고, 정예 방 miniboss 0:03 은 통과
        var early = RoomActs.Event("ring", 70f);
        var miniboss = RoomActs.Event("miniboss", 210f);
        var errors = RoomEvents.CheckPlacement(Act(early, miniboss), Rules);
        var error = Assert.Single(errors, e => e.Severity == DataSeverity.Error);
        Assert.Equal("$.events[0]", error.JsonPath);
        Assert.Contains("ring", error.Message);
        Assert.Contains(errors, e => e.Severity == DataSeverity.Warning && e.JsonPath == "$.rooms[1]");   // 방 번호 2
        Assert.DoesNotContain(errors, e => e.JsonPath == "$.rooms[0]");                                   // 1번 방은 절정 경고 대상 아님
        Assert.DoesNotContain(errors, e => e.JsonPath == "$.events[1]");                                  // miniboss 0:03 통과

        // 0:20 의 강적은 통과, 각 일반 방(2·3·5) 끝 0:45 안에 절정(march·ring·elite_pack·escort)이 있으면 경고 없음
        var ok = RoomEvents.CheckPlacement(Act(
            RoomActs.Event("escort", 80f),          // 방 2 0:20 — 경계는 통과
            RoomActs.Event("march", 100f),          // 방 2 끝 0:45 안(0:40 ≥ 1:15 − 0:45)
            RoomActs.Event("ring", 180f),           // 방 3(2:15~3:30) 0:45 — 끝 0:45 안
            RoomActs.Event("elite_pack", 330f)), Rules);   // 방 5(4:30~6:00) 1:00 — 끝 0:45 안
        Assert.Empty(ok);
    }

    [Fact]
    public void RemapEntry_OnlyWhenStartIsEntry_V081()
    {
        // 3.6 결정 5 끝(v0.8.1 시작 문 기준 바꾸기): gate_sw 로 들어온 방
        var fromSw = new TestWorldBuilder().WithRoom(gateIn: "gate_sw").Build().World.Room!;
        Assert.Equal("gate_ne", fromSw.RemapEntry("gate_ne"));   // v0.7.1 의 "== gate_sw"(서로 바꾸기)를 뒤집음 — 맞은편 문 이벤트는 그대로
        Assert.Equal("gate_ne", fromSw.RemapEntry("gate_sw"));   // 들어온 문 → 맞은편
        Assert.Equal("gate_se", fromSw.RemapEntry("gate_se"));
        Assert.Equal("gate_nw", fromSw.RemapEntry("gate_nw"));

        // 기획 10.3 6:30 행진(at_entry gate_se · toward_entry gate_nw)
        Assert.Equal(("gate_se", "gate_nw"), fromSw.RemapMarch("gate_se", "gate_nw"));   // gate_sw 로 들어오면 그대로
        var fromSe = new TestWorldBuilder().WithRoom(gateIn: "gate_se").Build().World.Room!;
        Assert.Equal(("gate_nw", "gate_se"), fromSe.RemapMarch("gate_se", "gate_nw"));   // gate_se 로 들어오면 시작 → 맞은편, 목표(맞은편) → 들어온 문
        Assert.Equal(("gate_nw", "gate_ne"), fromSe.RemapMarch("gate_se", "gate_ne"));   // 목표가 나머지 문이면 그대로
        Assert.Equal(("gate_ne", "gate_se"), fromSe.RemapMarch("gate_ne", "gate_se"));   // 시작 문이 다르면 목표가 들어온 문이어도 그대로
    }

    [Fact]
    public void RemapEntry_MirroredBossRoom_PairNameFirst()
    {
        // 거울판 보스 방: 들어온 문 gate_sw → 짝 gate_se, 보스 entry gate_ne → 짝 gate_nw(맞은편) — 들어온 문이 아니므로 그대로
        var defs = RoomActs.Defs(mirror: true);
        var act = defs.Stages.Get(RoomActs.Act1Id);
        var bossMap = defs.Maps.Get(RoomActs.BossRoom).Mirrored!;
        var room = RoomWorld(defs, act, act.Rooms[5], bossMap, "gate_se").Room!;
        Assert.True(room.Plan.Mirrored);
        Assert.Equal("gate_nw", room.RemapEntry("gate_ne"));
        Assert.Equal("gate_nw", room.RemapEntry("gate_sw"));   // 원판 들어온 문 이름(gate_sw) → 짝 gate_se = 들어온 문 → 맞은편 gate_nw
        Assert.Equal(("gate_ne", "gate_sw"), room.RemapMarch("gate_nw", "gate_se"));   // 짝 이름만(시작 gate_ne 는 들어온 문이 아님)
    }

    [Fact]
    public void RemapEntry_NoOppositeGate_KeepsEntry()
    {
        // 맞은편 문이 없는 합성 맵: 들어온 문인 시작 문도 그대로(13.5-23 ⑤ 가 오류로 잡음)
        var defs = RoomActs.Defs();
        var act = defs.Stages.Get(RoomActs.Act1Id);
        var full = defs.Maps.Get(RoomActs.RoomA);
        var oneDoor = full with
        {
            Id = "test_one_door",
            Layout = full.Layout with { Gates = [.. full.Layout.Gates.Where(g => g.Id != "gate_ne")] },
        };
        var room = RoomWorld(defs, act, act.Rooms[0], oneDoor, "gate_sw").Room!;
        Assert.Equal("gate_sw", room.RemapEntry("gate_sw"));
        Assert.Equal(("gate_sw", "gate_nw"), room.RemapMarch("gate_sw", "gate_nw"));
    }

    private static World RoomWorld(GameDefs defs, StageDef act, RoomDef def, MapDef map, string gateIn)
    {
        var plan = new RoomPlan(1, act.Rooms.ToList().IndexOf(def), def, map, gateIn, [], [], 1);
        var run = new RunState(1, act, null, new ActPlan(act, 1, [plan]));
        return new World(defs, new RoomStart(run, plan, null, 1f, false));
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
