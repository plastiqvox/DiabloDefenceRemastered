namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 방 맵 검사 상수(v0.7, 6.10 끝 — <c>rules</c> 가 아니라 코드 상수: 맵 검사 <c>ArenaLayout.ValidateRooms</c> 는 <c>MapDataReader</c> 안에서 돌아 <c>rules</c> 를 모르고,
/// 가져오기 도구도 같은 검사를 돈다. <c>[해석]</c>).
/// </summary>
public static class RoomMapRules
{
    /// <summary>작은 방 검사를 하는 하위 칸 수 상한(N ≤ 36 — 작은 방 N 32, 큰 방 40·44).</summary>
    public const int SmallRoomMaxN = 36;
    /// <summary>작은 방의 문 진입점에서 둘레가 아닌 걷기 막힘 칸이 없어야 하는 거리(px).</summary>
    public const float DoorClearance = 150f;
    /// <summary>방 맵의 문 넷(이름 = 변). 이벤트의 <c>entry:</c> 이름이 맵이 바뀌어도 같게(13.5-23).</summary>
    public static IReadOnlyList<(string Id, GateSide Side)> Gates { get; } =
        [("gate_nw", GateSide.Nw), ("gate_ne", GateSide.Ne), ("gate_se", GateSide.Se), ("gate_sw", GateSide.Sw)];
    /// <summary>방 맵에 있어야 하는 기준점(보상 자리, 기획 10.8·11.4).</summary>
    public const string CenterAnchor = "center";
}
