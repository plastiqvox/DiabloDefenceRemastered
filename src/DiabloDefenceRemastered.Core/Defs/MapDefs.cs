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
    /// <summary>안쪽 조각 이름 → 메가타일 번호(기둥·짧은 벽).</summary>
    public IReadOnlyDictionary<string, int> Pieces { get; init; } = new Dictionary<string, int>();
    /// <summary>
    /// 성질 표: 메가타일 → 하위 칸 4개(TIL 순서 위-왼, 위-오른, 아래-왼, 아래-오른 = (2mx,2my) (2mx+1,2my) (2mx,2my+1) (2mx+1,2my+1)).
    /// 글자 <c>.</c> 바닥, <c>W</c> 걷기만 막힘, <c>M</c> 투사체만 막힘, <c>X</c> 둘 다(원본 SOL bit0·bit2).
    /// 키 ⊆ 이 타일셋이 이름 붙인 번호(floor·borders·gates·pieces) — SOL 덤프가 되지 않게(13.5-12, R24).
    /// </summary>
    public IReadOnlyDictionary<int, string> Props { get; init; } = new Dictionary<int, string>();
    /// <summary>원본 SOL 과 다르게 둔 칸 중 원작 수정판이 고치는 SOL 오류만(가져오기 도구의 성질 대조가 쓴다).</summary>
    public IReadOnlyList<SolDiffDef> SolKnownDiffs { get; init; } = [];
    /// <summary>기획 이유로 원본과 다르게 둔 칸(이유 필수). <see cref="SolKnownDiffs"/> 와 섞지 않는다. MVP 에는 없다.</summary>
    public IReadOnlyList<SolDiffDef> DesignDiffs { get; init; } = [];
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

/// <summary>안쪽 조각 놓기: <c>at</c> 의 [mx,my] 마다, 또는 <c>line: [[mx0,my0],[mx1,my1]]</c>(가로·세로 줄).</summary>
public sealed record PiecePlacementDef
{
    /// <summary>타일셋 <c>pieces</c> 의 이름.</summary>
    public required string Piece { get; init; }
    public IReadOnlyList<Int2> At { get; init; } = [];
    public IReadOnlyList<Int2>? Line { get; init; }
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
