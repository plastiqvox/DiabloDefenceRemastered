using DiabloDefenceRemastered.Core.Art;
using DiabloDefenceRemastered.Tests.Data;

namespace DiabloDefenceRemastered.Tests.Art;

/// <summary>
/// 문 봉인 = 원작 L1 창살(v0.9.4 사용자 결정 V13, 기획 10.8 "봉인 표시", 설계서 7.11 — 단계 2 wp5): 닫힌 문은 창살 그대로, 출구로 열린 문은 0.6초 뒤 창살 없음,
/// 들어온 문·고르지 않은 문은 그대로. 변 → 조각(거울판은 짝 조각), 그림을 빌려 올 맵 찾기(없으면 코드 그림 대체).
/// </summary>
public class GateGrateTests
{
    [Fact]
    public void Closed_Grate_ExitSinksOver06s_EnteredAndUnchosenStay()
    {
        var t = new TestWorldBuilder().WithDefs(TestDefs.Basic()).WithRoom(RoomPhase.Survive, gateIn: "gate_se").Build();
        var w = t.World;
        var room = w.Room!;
        Assert.Equal(36, GateGrates.OpenTicks);                                           // 0.6초
        foreach (var id in w.Gates.Keys) Assert.Equal(0f, GateGrates.Sink(room, id, w.Tick));   // 생존 단계: 모든 문이 닫힘

        // 문 단계: 출구 하나(gate_nw)만 열림
        var exitGate = w.Gates.Keys.First(id => id != "gate_se");
        room.Exits = [new RoomExit(exitGate, null, RoomKind.Normal, null, false, null)];
        room.EnterPhase(RoomPhase.Doors, 100);
        Assert.Equal(0f, GateGrates.Sink(room, exitGate, 100));
        Assert.Equal(0.5f, GateGrates.Sink(room, exitGate, 118), 3);
        Assert.Equal(1f, GateGrates.Sink(room, exitGate, 136));                          // 0.6초 뒤 창살 없음
        Assert.Equal(1f, GateGrates.Sink(room, exitGate, 1000));
        Assert.Equal(0f, GateGrates.Sink(room, "gate_se", 1000));                       // 들어온 문 그대로
        foreach (var id in w.Gates.Keys.Where(id => id != exitGate)) Assert.Equal(0f, GateGrates.Sink(room, id, 1000));   // 고르지 않은 문 그대로

        room.EnterPhase(RoomPhase.Exit, 1001);                                           // 나감 단계: 열린 문은 다 내려간 채
        Assert.Equal(1f, GateGrates.Sink(room, exitGate, 1001));
        Assert.Equal(0f, GateGrates.Sink(room, "gate_se", 1001));

        Assert.Equal(1f, GateGrates.Sink(null, "gate_se", 0));                           // 방 없는 월드: 봉인 없음
    }

    /// <summary>실제 맵 전부(원판·거울판): 변마다 조각, 거울판은 짝 조각(타일셋 짝 표), 문 틈 메가 둘.</summary>
    [Fact]
    public void RealMaps_SidePiece_MirrorUsesPairedPiece_TwoMegasPerGate()
    {
        var l1 = RealData.L1;
        Assert.Equal(36, l1.Pieces["grate_h"]);
        Assert.Equal(35, l1.Pieces["grate_v"]);
        Assert.Contains(l1.Mirror!.Pairs, p => p.Order(StringComparer.Ordinal).SequenceEqual(["grate_h", "grate_v"]));
        static string Pair(string piece) => piece == "grate_h" ? "grate_v" : "grate_h";

        var checkedMirror = 0;
        foreach (var map in RealData.Maps.Maps)
        {
            var grid = ArenaLayout.Expand(map);
            foreach (var (id, zone) in GateZones.Compute(map, grid))
            {
                var piece = GateGrates.PieceName(zone.Side);
                Assert.Equal(zone.Side is GateSide.Ne or GateSide.Sw, piece == "grate_h");
                Assert.Equal(2, GateGrates.Megas(zone).Count);
                if (map.Mirrored is not { } mirror) continue;
                var mzones = GateZones.Compute(mirror, ArenaLayout.Expand(mirror));
                var mz = mzones[GateZones.MirrorGate(id)];
                Assert.Equal(GateZones.MirrorSide(zone.Side), mz.Side);
                Assert.Equal(Pair(piece), GateGrates.PieceName(mz.Side));
                checkedMirror++;
            }
        }
        Assert.True(checkedMirror >= 20, $"거울판 문 {checkedMirror}");
    }

    /// <summary>그림을 빌려 올 맵: 액트 1 맵 풀(원판 + 거울판)에 가로·세로 창살이 다 있다. 창살을 쓴 맵이 없으면 null → 코드 그림 대체.</summary>
    [Fact]
    public void FindSource_ActPoolHasBothPieces_NoGrateMaps_FallbackNull()
    {
        var maps = RealData.Maps.Maps;
        var pool = maps.SelectMany(m => m.Mirrored is { } mm ? new[] { m, mm } : [m]).ToList();
        var h = GateGrates.FindSource(pool, "grate_h");
        var v = GateGrates.FindSource(pool, "grate_v");
        Assert.NotNull(h);
        Assert.NotNull(v);
        Assert.Equal(36, h.Tile);
        Assert.Equal(35, v.Tile);
        Assert.Equal(36, ArenaLayout.Expand(h.Map).MegaTile(h.Mega.X, h.Mega.Y));
        Assert.Equal(35, ArenaLayout.Expand(v.Map).MegaTile(v.Mega.X, v.Mega.Y));

        var noGrates = maps.Where(m => m.Id == "cathedral_room_a").ToList();               // 기둥 홀 — 창살 없음
        Assert.Null(GateGrates.FindSource(noGrates, "grate_h"));
        Assert.Null(GateGrates.FindSource(noGrates, "grate_v"));
        Assert.Null(GateGrates.FindSource(pool, "no_such_piece"));
        Assert.Null(GateGrates.FindSource([], "grate_h"));
    }
}
