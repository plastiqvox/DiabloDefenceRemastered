using DiabloDefenceRemastered.Core.Nav.Validation;

namespace DiabloDefenceRemastered.Tests.Nav;

/// <summary>
/// 13.5-4~6·문 틈 닿음·작은 방 150px 정밀 검사를 <b>모든 액트 풀의 모든 맵</b>과 거울판에 (설계서 11.1 WP2 v0.7·v0.8). 검사기는 정의 표의 맵마다 돈다.
/// 합성 액트(<see cref="RoomActs"/> — 작은 방 N 32 둘·보스 방 N 40, 짝 표 타일셋 거울판)와 검사기 등록 경로.
/// </summary>
public class ActPoolNavTests
{
    [Fact]
    public void Validator_AllActMapsAndMirrors_NoErrors()
    {
        var defs = RoomActs.Defs(mirror: true);
        Assert.Contains(defs.Maps.All, m => m.Mirrored is not null);
        var ctx = new ValidationContext { Defs = defs };
        new NavPathValidator().Validate(defs, ctx);
        Assert.True(ctx.Errors.Count == 0, string.Join("\n", ctx.Errors));
    }

    [Fact]
    public void Validator_ReportsMirroredMapByName()
    {
        // 거울판에서 난 오류도 원판 파일 이름 + "(거울판 …@m)" 으로 나온다 — 막힌 문을 단 방 맵을 거울판 자리에 넣어 본다
        var blocked = TestDefs.RoomMap([new() { Piece = "wall_v", Line = [new(1, 1), new(1, 18)] }], id: "test_gate_blocked")
            with { RoomKinds = [RoomKind.Normal] };
        var mirrored = blocked with { Id = "test_gate_blocked@m", MirrorOf = "test_gate_blocked" };
        var ctx = new ValidationContext();
        var defs = new GameDefs { Maps = new DefTable<MapDef>([blocked with { Mirrored = mirrored }]) };
        new NavPathValidator().Validate(defs, ctx);
        Assert.Contains(ctx.Errors, e => e.File == "maps/test_gate_blocked.json" && e.Message.StartsWith("(거울판 test_gate_blocked@m)", StringComparison.Ordinal));
        Assert.Contains(ctx.Errors, e => e.Message.StartsWith("13.5-23 문 틈에 못 닿음", StringComparison.Ordinal));
    }

    [Fact]
    public void NavModule_RegistersValidator()
    {
        var ctx = new ModuleContext();
        new Core.Nav.NavModule().Register(ctx);
        Assert.Contains(ctx.Validators, v => v is NavPathValidator);
    }
}
