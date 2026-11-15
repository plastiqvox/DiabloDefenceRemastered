using System.Globalization;
using DiabloDefenceRemastered.Core.Sim.Run;

namespace DiabloDefenceRemastered.Core.Dev;

/// <summary>콘솔 명령 결과: 보일 줄, 옮겨 갈 방 자리(0 부터 — 있으면 부르는 쪽이 방 넘김 화면 처리와 함께 <see cref="RunSession.Warp"/>).</summary>
public readonly record struct CheatResult(string Message, int? WarpTo = null);

/// <summary>
/// 치트 콘솔 명령(2026-11-15 사용자 요청 "임시로 패턴보고 싶은데 치트키로 넘어가게", "`로 열려하는건? 퀘이크처럼") — 임시. 판 안에서 ` 로 연다.
/// <list type="bullet">
/// <item><c>help</c> — 명령 목록</item>
/// <item><c>clear</c> — 지금 방 적을 모두 무너뜨리고 보상·포탈 단계로(<see cref="RoomFlow.CheatClear"/>)</item>
/// <item><c>next</c> — 다음 방으로 바로, <c>room N</c> — 이 액트의 N 번째 방으로(1 부터), <c>boss</c> — 이 액트 마지막(보스) 방으로</item>
/// <item><c>god</c> — 무적 켜고 끄기(다음 방에도 이어짐)</item>
/// <item><c>rooms</c> — 이 액트 방 목록(번호·종류·맵)</item>
/// </list>
/// </summary>
public static class CheatCommands
{
    public static CheatResult Run(RunSession session, string line)
    {
        var parts = line.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return new CheatResult("");
        var world = session.World;
        var rooms = session.Run.Act.Rooms;
        var here = session.Run.RoomIndex;
        switch (parts[0])
        {
            case "help":
                return new CheatResult("clear · next · room N · boss · rooms · god · help");
            case "clear":
                return new CheatResult(RoomFlow.CheatClear(world) ? "방 클리어" : "지금은 클리어할 수 없다(이미 보상·포탈 단계이거나 판 끝)");
            case "next":
                return here + 1 < rooms.Count ? Warp(here + 1, rooms.Count) : new CheatResult("마지막 방이다 — clear 로 끝내기");
            case "boss":
                return Warp(rooms.Count - 1, rooms.Count);
            case "room":
                if (parts.Length < 2 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1 || n > rooms.Count)
                    return new CheatResult($"room 1~{rooms.Count}");
                return Warp(n - 1, rooms.Count);
            case "rooms":
                return new CheatResult(string.Join("  ", rooms.Select((r, i) => $"{i + 1}:{r.Kind.ToString().ToLowerInvariant()}{(i == here ? "*" : "")}")));
            case "god":
                session.CheatGod = !session.CheatGod;
                world.Options = world.Options with { God = session.CheatGod };
                return new CheatResult(session.CheatGod ? "무적 켬" : "무적 끔");
            default:
                return new CheatResult($"모르는 명령 '{parts[0]}' — help");
        }
    }

    private static CheatResult Warp(int index, int count) =>
        new($"방 {index + 1}/{count} 로", index);
}
