using System.Text.Json.Serialization;

namespace DiabloDefenceRemastered.Core.Defs;

// 원작 타일 아레나 (설계서 6.11, 계약 WP1b). v0.2 의 배경 그림 맵·손 좌표 충돌·장애물 종류(obstacle_kinds)는 없앴다.
// 두 종류(tilesets·maps)는 Core Map/MapDataReader 가 읽는다 — extends·*_add·*_patch 를 쓰지 않는다(쓰면 오류).

/// <summary>
/// 타일셋 <c>tilesets[]</c>. 메가타일 번호와 하위 칸 성질 표. 번호를 아는 묶음(WP0)과 배치를 맞추는 묶음(WP5, 맵)의 파일이 나뉜다.
/// </summary>
public sealed record TilesetDef : IDef
{
    public required string Id { get; init; }
    /// <summary>벽 기둥 높이 Hc(px, L1·L2 160). <c>IsoMath</c> 의 H = Hc − 32.</summary>
    public int ColumnHeight { get; init; } = 160;
    /// <summary>바닥 메가타일(좌표 해시로 섞어 깖).</summary>
    public IReadOnlyList<int> Floor { get; init; } = [];
    /// <summary>테두리 이름 → 여덟 자리 번호.</summary>
    public IReadOnlyDictionary<string, BorderTilesDef> Borders { get; init; } = new Dictionary<string, BorderTilesDef>();
    /// <summary>테두리 이름 → 변별 문 번호(그 테두리에 쓰는 문).</summary>
    public IReadOnlyDictionary<string, GateSetDef> Gates { get; init; } = new Dictionary<string, GateSetDef>();
    /// <summary>안쪽 조각 이름 → 메가타일 번호(기둥·짧은 벽·안쪽 방 조각, v0.6). 이름 → 번호는 이 층의 번호다(L5·L6 은 자기 파일).</summary>
    public IReadOnlyDictionary<string, int> Pieces { get; init; } = new Dictionary<string, int>();
    /// <summary>
    /// 검증이 뜻을 알아야 하는 번호의 표시(v0.6, 13.5-19). 번호는 이 타일셋이 이름 붙인 번호여야 한다(13.5-12).
    /// <c>pillar</c> = <c>open_center</c> 안에 허용되는 조각, <c>closed_door</c>·<c>arch</c> = 문으로 쓰면 안 되는 조각(경고), <c>grate</c> = 미리보기·로그 이름.
    /// <c>LayoutHash</c> 에 넣지 않는다 — 그림을 바꾸지 않는다.
    /// </summary>
    public IReadOnlyDictionary<TileMark, IReadOnlyList<int>> Marks { get; init; } = new Dictionary<TileMark, IReadOnlyList<int>>();
    /// <summary>
    /// 성질 표: 메가타일 → 하위 칸 4개(TIL 순서 위-왼, 위-오른, 아래-왼, 아래-오른 = (2mx,2my) (2mx+1,2my) (2mx,2my+1) (2mx+1,2my+1)).
    /// 글자 8개 <c>.wLWmxMX</c>(v0.5, 6.11 — 원본 SOL 의 <c>sol &amp; 7</c> 과 1:1): 대문자 = 시야 막힘, <c>w</c>·<c>m</c>·<c>x</c> = 걷기만·투사체만·둘 다,
    /// <c>L</c> 은 시야만, <c>.</c> 은 바닥. 예: 둘레 벽 <c>X</c>(셋 다), L1 기둥 <c>x</c>(걷기·투사체, 시야 통과).
    /// 키 ⊆ 이 타일셋이 이름 붙인 번호(floor·borders·gates·pieces) — SOL 덤프가 되지 않게(13.5-12, R24).
    /// </summary>
    public IReadOnlyDictionary<int, string> Props { get; init; } = new Dictionary<int, string>();
    /// <summary>원본 SOL 과 다르게 둔 칸 중 원작 수정판이 고치는 SOL 오류만(가져오기 도구의 성질 대조가 쓴다).</summary>
    public IReadOnlyList<SolDiffDef> SolKnownDiffs { get; init; } = [];
    /// <summary>기획 이유로 원본과 다르게 둔 칸(이유 필수). <see cref="SolKnownDiffs"/> 와 섞지 않는다. MVP 에는 없다.</summary>
    public IReadOnlyList<SolDiffDef> DesignDiffs { get; init; } = [];
    /// <summary>
    /// 거울판 짝 표(v0.8, 6.11 "거울판" — 계약 WP1f). null 이면 이 타일셋의 맵은 <c>mirror: true</c> 를 쓸 수 없다.
    /// 짝·자리 짝 검사(13.5-12)는 <c>ArenaLayout.ValidateTileset</c>. <c>LayoutHash</c> 에 넣지 않는다 — 원판 배치·그림을 바꾸지 않는다.
    /// </summary>
    public TilesetMirrorDef? Mirror { get; init; }
    /// <summary>
    /// 변형 표(2026-11-13 사용자 요청 "대성당 그림을 더 활용"): 메가타일 → 같은 성질의 장식판(금 간 바닥·잔해, 십자가·조각상·부서진 벽).
    /// 배치를 다 푼 뒤 문 칸이 아닌 칸마다 좌표 해시로 <see cref="TileAccentDef.Chance"/> 만큼 바꾼다 — 성질이 같아 충돌·길찾기는 그대로, 그림만 달라진다.
    /// 거울판은 자기 ID 해시로 다시 고른다. 장식판 번호도 이름 붙인 번호다(성질 표에 둔다, 13.5-12).
    /// </summary>
    public IReadOnlyDictionary<int, TileAccentDef> Accents { get; init; } = new Dictionary<int, TileAccentDef>();
}

/// <summary>변형 하나: <see cref="Chance"/>(0..1) 만큼 <see cref="With"/> 중 하나(해시)로 바꾼다. 모두 원래 번호와 성질이 같아야 한다.</summary>
public sealed record TileAccentDef
{
    public float Chance { get; init; }
    public IReadOnlyList<int> With { get; init; } = [];
}

/// <summary>
/// 거울판 짝 표(v0.8, 6.11): <see cref="Pairs"/> = 서로 바뀌는 <c>pieces</c> 이름 둘(세로 벽 ↔ 가로 벽 등, <c>[a, b]</c>), <see cref="Self"/> = 뒤집어도 그대로인 조각.
/// 둘레·문·바닥은 넣지 않는다 — 자리로 고르므로 거울판이 바뀐 자리의 조각을 다시 고른다(대신 자리 짝 검사).
/// </summary>
public sealed record TilesetMirrorDef
{
    public IReadOnlyList<IReadOnlyList<string>> Pairs { get; init; } = [];
    public IReadOnlyList<string> Self { get; init; } = [];
}

/// <summary>테두리 여덟 자리(메가타일 격자 기준). corner_n = (0,0) 화면 맨 위, edge_ne = 윗줄(y = 0), edge_se = 오른쪽 열, edge_sw = 아랫줄, edge_nw = 왼쪽 열.</summary>
public sealed record BorderTilesDef
{
    public required int CornerN { get; init; }
    public required int EdgeNe { get; init; }
    public required int CornerE { get; init; }
    public required int EdgeSe { get; init; }
    public required int CornerS { get; init; }
    public required int EdgeSw { get; init; }
    public required int CornerW { get; init; }
    public required int EdgeNw { get; init; }
}

/// <summary>변별 문 조각. 없는 변에는 문을 둘 수 없다.</summary>
public sealed record GateSetDef
{
    public GateTilesDef? Nw { get; init; }
    public GateTilesDef? Ne { get; init; }
    public GateTilesDef? Se { get; init; }
    public GateTilesDef? Sw { get; init; }

    public GateTilesDef? Get(GateSide side) => side switch
    {
        GateSide.Nw => Nw,
        GateSide.Ne => Ne,
        GateSide.Se => Se,
        GateSide.Sw => Sw,
        _ => null,
    };
}

/// <summary>문 하나: 열린 메가타일 2개(<see cref="Open"/>)와 양옆 벽 끝 조각. 끝 조각을 생략하면 그 자리는 테두리 번호 그대로다.</summary>
public sealed record GateTilesDef
{
    public required int Open { get; init; }
    /// <summary>변 위 번호가 작은 쪽 끝.</summary>
    public int? EndA { get; init; }
    /// <summary>변 위 번호가 큰 쪽 끝.</summary>
    public int? EndB { get; init; }
}

/// <summary>
/// 타일 표시 이름(v0.6, <c>TilesetDef.Marks</c> 의 키). 사전 키로 읽으므로 값마다 JSON 이름을 단다(<see cref="StatKey"/> 와 같은 방식 — <c>closed_door</c>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TileMark>))]
public enum TileMark : byte
{
    /// <summary><c>open_center</c> 안에 허용되는 조각(기둥, 2개까지).</summary>
    [JsonStringEnumMemberName("pillar")] Pillar,
    /// <summary>창살(규칙은 성질 글자가 정함 — 이름은 미리보기·로그용).</summary>
    [JsonStringEnumMemberName("grate")] Grate,
    /// <summary>원작 닫힌 문(SOL 로 벽) — 문으로 쓰면 안 됨, 장식이면 <c>decor_why</c>.</summary>
    [JsonStringEnumMemberName("closed_door")] ClosedDoor,
    /// <summary>아치(틈이 하위 1칸) — 문으로 쓰면 안 됨, 장식이면 <c>decor_why</c>.</summary>
    [JsonStringEnumMemberName("arch")] Arch,
}

/// <summary>성질 표와 원본 SOL 이 다른 칸 하나: 메가타일·하위 칸(0..3, TIL 순서)·원본 글자·쓰는 글자·이유.</summary>
public sealed record SolDiffDef
{
    public required int Tile { get; init; }
    public required int Sub { get; init; }
    public required string Sol { get; init; }
    public required string Use { get; init; }
    public required string Why { get; init; }
}

/// <summary>맵의 변(메가타일 격자 기준): nw = 왼쪽 열(x = 0), ne = 윗줄(y = 0), se = 오른쪽 열, sw = 아랫줄.</summary>
public enum GateSide : byte { Nw, Ne, Se, Sw }

/// <summary>
/// 맵 <c>maps[]</c>. 위치는 모두 하위 타일 좌표(<see cref="TileCoord"/>, 칸 중심 = 정수)다 — 월드 px 는 <c>World.Resolved</c>.
/// 월드 크기는 <c>IsoMath</c> 가 N(= size × 2)과 기둥 높이로 계산한다.
/// </summary>
public sealed record MapDef : IDef
{
    public required string Id { get; init; }
    /// <summary>타일셋 ID. <c>MapDataReader</c> 가 연결한다.</summary>
    public required DefRef<TilesetDef> Tileset { get; init; }
    /// <summary>층 팔레트 이름(가져오기·그림만 씀, 가져오기 표 <c>tilesets.json</c> 의 palettes 키).</summary>
    public string Palette { get; init; } = "";
    /// <summary>메가타일 수 M(정사각). 하위 타일 N = 2M.</summary>
    public int Size { get; init; }
    public required ArenaLayoutDef Layout { get; init; }
    public IReadOnlyList<EntryDef> Entries { get; init; } = [];
    public required HeroStartDef HeroStart { get; init; }
    /// <summary>기준점 ID → 하위 타일 좌표. 점 빛·행진 목표가 가리킨다.</summary>
    [JsonPropertyName("anchors")]
    public IReadOnlyDictionary<string, TileCoord> AnchorsTile { get; init; } = new Dictionary<string, TileCoord>();
    /// <summary>가져오기 출력 폴더(<c>d1/maps/cathedral_arena</c>, 7.6).</summary>
    public string Art { get; init; } = "";
    /// <summary>주인공을 가리는 벽 반투명 규칙(7.6). 없으면 <c>light</c>.</summary>
    public WallFadeDef WallFade { get; init; } = new();
    /// <summary>
    /// 가운데 열린 공간(v0.6, 메가타일 좌표, 끝 미포함 — 기획 "메가 [6..12,6..12]" = <c>[6, 6, 13, 13]</c>). 안의 메가타일은 바닥 또는 <c>marks.pillar</c> 이고
    /// 기둥 ≤ 2(13.5-19). 무리를 끌고 도는 가운데가 넓게 남는지를 데이터로 지킨다. 방 기준점은 <see cref="AnchorsTile"/> 의 <c>room_*</c> 키.
    /// 맵에는 <c>requires</c> 를 두지 않는다 — 맵 그림의 묶음은 그 타일셋 가져오기 표 항목의 <c>bundle</c>(1.4).
    /// </summary>
    public RectI? OpenCenter { get; init; }
    /// <summary>
    /// 방 영역(v0.6.3, 6.11 "방 바닥"): 방 기준점 ID(<c>room_*</c>) → 메가타일 사각형(끝 미포함, <see cref="OpenCenter"/> 와 같은 표기).
    /// 방 바닥 = 이 사각형의 하위 칸 중 기준점 칸에서 걷기 막힘 없는 칸으로 4이웃 채우기가 닿는 칸(<c>RoomFloors.Compute</c>, 맵을 읽을 때 한 번).
    /// 벽 메가는 위 줄·왼쪽 열만 막으므로 방의 오른쪽·아래 벽 메가는 사각형에 넣지 않는다 — 넣으면 벽 바깥 반 칸이 문을 지나 바닥에 섞인다.
    /// 모든 <c>room_*</c> 기준점에 하나씩(13.5-19). 방 체류 압박·방 보급·방 들르기의 "방 안" 이 이것을 쓴다.
    /// </summary>
    public IReadOnlyDictionary<string, RectI> RoomAreas { get; init; } = new Dictionary<string, RectI>();
    /// <summary>
    /// 이 맵을 쓸 수 있는 방 종류(v0.7, 6.11 "방 맵", 기본 빈 목록). 비어 있으면 방 맵이 아니다(방 없는 월드·<c>--map</c> 미리보기에만).
    /// 비어 있지 않으면 문이 정확히 <c>gate_nw</c>·<c>gate_ne</c>·<c>gate_se</c>·<c>gate_sw</c> 넷이고 기준점 <c>center</c> 가 있어야 한다(13.5-23 ③, <c>ArenaLayout.ValidateRooms</c>).
    /// 액트의 방 맵 풀(<see cref="RoomMapPoolDef"/>)에 든 맵은 그 종류를 담아야 한다(13.5-23 ②).
    /// </summary>
    public IReadOnlyList<RoomKind> RoomKinds { get; init; } = [];
    /// <summary>
    /// 거울판도 쓴다(v0.8, JSON <c>mirror</c>, 기본 거짓 — 6.11 "거울판"). 참이면 <c>MapDataReader</c> 가 읽을 때 <c>MapMirror.Of</c> 로 거울판을 만들어 <see cref="Mirrored"/> 에 둔다.
    /// 타일셋 짝 표가 이 맵의 조각을 모두 덮어야 한다(아니면 로딩 오류).
    /// </summary>
    public bool Mirror { get; init; }
    /// <summary>이 맵의 거울판(v0.8, <see cref="Mirror"/> 가 참일 때 읽기가 채움). 정의 표에는 없다(ID <c>&lt;원판 ID&gt;@m</c>).</summary>
    [JsonIgnore]
    public MapDef? Mirrored { get; init; }
    /// <summary>거울판이면 원판 ID(v0.8 — 로그 <c>map=</c>·넓이 배율·<c>RoomRecord.MapId</c>), 원판이면 null.</summary>
    [JsonIgnore]
    public string? MirrorOf { get; init; }
    /// <summary>거울판인가(v0.8 — <c>MapMirror.Of</c> 가 만든 정의).</summary>
    [JsonIgnore]
    public bool IsMirror => MirrorOf is not null;
}

/// <summary>배치(6.11 배치 풀기 1~5단계의 입력).</summary>
public sealed record ArenaLayoutDef
{
    /// <summary>타일셋 <c>borders</c> 의 이름.</summary>
    public required string Border { get; init; }
    public IReadOnlyList<GatePlacementDef> Gates { get; init; } = [];
    public IReadOnlyList<PiecePlacementDef> Place { get; init; } = [];
}

/// <summary>문 하나. 입구 ID 는 문 ID 와 같게 둔다.</summary>
public sealed record GatePlacementDef
{
    public required string Id { get; init; }
    public GateSide Side { get; init; }
    /// <summary>열린 두 메가타일 중 앞쪽의 변 위 번호(nw·se 는 my, ne·sw 는 mx). 없으면 가운데 (M − 2) / 2.</summary>
    public int? At { get; init; }
}

/// <summary>
/// 안쪽 조각 놓기(6.11 배치 풀기 4단계): <c>piece</c> 를 <c>at</c> 의 [mx,my] 마다, 또는 <c>line: [[mx0,my0],[mx1,my1]]</c>(가로·세로 줄),
/// 또는 (v0.6) <c>grid</c> 블록 — <c>origin</c> 에서 시작하는 줄 목록(줄 = y, 칸 = 공백으로 나눈 조각 이름, <c>.</c> = 건드리지 않음, 줄 길이 자유).
/// 놓기 하나는 at·line·grid 중 하나만, grid 면 piece 없음. 한 칸에 두 번 놓으면 오류.
/// </summary>
public sealed record PiecePlacementDef
{
    /// <summary>타일셋 <c>pieces</c> 의 이름. <see cref="Grid"/> 놓기면 null.</summary>
    public string? Piece { get; init; }
    public IReadOnlyList<Int2> At { get; init; } = [];
    public IReadOnlyList<Int2>? Line { get; init; }
    /// <summary>방 블록(v0.6): 줄 = y(origin.y 부터), 칸 = 공백으로 나눈 조각 이름(<c>Split(' ', RemoveEmptyEntries)</c>), <c>.</c> = 건드리지 않음.</summary>
    public IReadOnlyList<string>? Grid { get; init; }
    /// <summary><see cref="Grid"/> 의 첫 칸 [mx, my].</summary>
    public Int2? Origin { get; init; }
    /// <summary>방·벽 이름(선택) — 오류 문구와 <c>--debug-nav tiles</c> 에만 쓴다.</summary>
    public string? Id { get; init; }
    /// <summary>
    /// 참이면 테두리 칸(문 칸 제외)을 덮을 수 있다(v0.6). 덮인 칸의 둘레 표시는 남는다(4.2). 덮어도 막힘은 줄지 않아야 한다 —
    /// 하위 칸마다 원래 테두리 조각이 막던 걷기·시야·투사체 비트를 덮는 조각도 막아야 한다(v0.6.1, 오류).
    /// </summary>
    public bool OverBorder { get; init; }
    /// <summary><c>marks</c> 의 <c>closed_door</c>·<c>arch</c> 번호를 일부러 장식으로 쓸 때의 이유 — 있으면 13.5-19 경고를 끈다.</summary>
    public string? DecorWhy { get; init; }
}

/// <summary>입구: 바깥 시작점(맵 밖) → 진입점(맵 안). 하위 타일 좌표.</summary>
public sealed record EntryDef
{
    public required string Id { get; init; }
    [JsonPropertyName("outside")]
    public TileCoord OutsideTile { get; init; }
    [JsonPropertyName("inside")]
    public TileCoord InsideTile { get; init; }
}

public sealed record HeroStartDef
{
    public TileCoord Tile { get; init; }
    public Compass Facing { get; init; } = Compass.South;
}

/// <summary>light = 주인공보다 앞이고 ① 주인공 그림과 겹치거나 ② 주인공 빛 반경 안의 벽(기본). hero = ① 만(미리보기 비교용).</summary>
public enum WallFadeMode : byte { Light, Hero }

public sealed record WallFadeDef
{
    public WallFadeMode Mode { get; init; } = WallFadeMode.Light;
    /// <summary>② 에 빛 반경 대신 쓸 반경(px).</summary>
    public float? Radius { get; init; }
}
