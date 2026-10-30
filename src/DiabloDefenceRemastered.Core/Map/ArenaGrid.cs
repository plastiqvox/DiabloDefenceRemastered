namespace DiabloDefenceRemastered.Core.Map;

/// <summary>하위 칸 성질(설계서 6.11 성질 표 글자 + 둘레 표시).</summary>
[Flags]
public enum CellFlags : byte
{
    None = 0,
    /// <summary>걷기 막힘(<c>w</c>·<c>W</c>·<c>x</c>·<c>X</c>, 원본 SOL bit0).</summary>
    BlocksWalk = 1,
    /// <summary>투사체 막힘(<c>m</c>·<c>x</c>·<c>M</c>·<c>X</c>, 원본 SOL bit2).</summary>
    BlocksProjectiles = 2,
    /// <summary>테두리·문 끝 조각에서 온 칸. 막힌 칸이면 그 도형은 <c>IsBoundary</c> — 장애물 통과 유닛도 막힌다(4.2-4).</summary>
    Boundary = 4,
    /// <summary>격자 밖(<see cref="ArenaGrid.Flags"/> 가 격자 밖 좌표에 돌려준다. 걷기·투사체 막힘도 같이 켜져 있고, 시야 막힘은 없다).</summary>
    Outside = 8,
    /// <summary>시야 막힘(대문자 <c>L</c>·<c>W</c>·<c>M</c>·<c>X</c>, 원본 SOL bit1 — v0.5, 4.11). 1·2·4·8 은 그대로 두고 비트를 더했다.</summary>
    BlocksSight = 16,
}

/// <summary>
/// 배치 풀기 결과 (설계서 6.11 5단계, 계약 WP1b·WP1c): 메가타일 번호 M×M + 하위 칸 성질 N×N + 둘레 표시 + <see cref="LayoutHash"/>.
/// <c>ArenaLayout.Expand</c> 만 만든다. 충돌(WP2 <c>ArenaCollision</c>)·시야(<c>VisibilityField</c>)·그림 굽기(WP0)·벽 무리 나누기(WP5)가 같은 격자를 읽는다.
/// 칸 좌표: 메가타일 (mx, my) → 하위 칸 (2mx,2my) (2mx+1,2my) (2mx,2my+1) (2mx+1,2my+1).
/// </summary>
public sealed class ArenaGrid
{
    // 격자 밖은 걷기·투사체만 막는다. 시야는 막지 않는다 — 문 틈 밖에서 걸어 들어오는 적도 같은 선 규칙으로 보인다(4.11)
    private const CellFlags OutsideFlags = CellFlags.Outside | CellFlags.BlocksWalk | CellFlags.BlocksProjectiles;

    private readonly int[] _mega;
    private readonly CellFlags[] _cells;

    internal ArenaGrid(MapDef map, TilesetDef tileset, IsoMath iso, int[] mega, CellFlags[] cells, string layoutHash)
    {
        Map = map;
        Tileset = tileset;
        Iso = iso;
        MegaSize = map.Size;
        _mega = mega;
        _cells = cells;
        LayoutHash = layoutHash;
    }

    public MapDef Map { get; }
    public TilesetDef Tileset { get; }
    /// <summary>이 맵의 좌표 변환(N = 2M, Hc = 타일셋 column_height).</summary>
    public IsoMath Iso { get; }
    /// <summary>메가타일 수 M(한 변).</summary>
    public int MegaSize { get; }
    /// <summary>하위 타일 수 N = 2M(한 변).</summary>
    public int Size => MegaSize * 2;

    /// <summary>
    /// 번호 격자·타일셋 ID·팔레트 + 타일셋 정의 전체(성질 표·보정 목록·이름 → 번호·기둥 높이)를 정해진 순서로 이은 <c>StableHash</c>(16진 8자리).
    /// 가져오기 도구가 <c>arena.json</c> 에 적고, 시작 검사가 저장소 배치로 다시 계산해 그림이 낡았는지 본다(1.4).
    /// 성질 글자를 그대로 이으므로 시야 글자만 바뀌어도(<c>x</c> ↔ <c>X</c>) 값이 바뀐다.
    /// </summary>
    public string LayoutHash { get; }

    public int MegaTile(int mx, int my)
    {
        if ((uint)mx >= (uint)MegaSize || (uint)my >= (uint)MegaSize)
            throw new ArgumentOutOfRangeException(nameof(mx), $"메가타일 ({mx},{my}) 는 {MegaSize}×{MegaSize} 밖이다");
        return _mega[my * MegaSize + mx];
    }

    public bool IsInside(int tx, int ty) => (uint)tx < (uint)Size && (uint)ty < (uint)Size;

    /// <summary>하위 칸 성질. 격자 밖이면 <see cref="CellFlags.Outside"/>(걷기·투사체 막힘 포함, 시야 막힘 없음).</summary>
    public CellFlags Flags(int tx, int ty) => IsInside(tx, ty) ? _cells[ty * Size + tx] : OutsideFlags;

    public bool BlocksWalk(int tx, int ty) => (Flags(tx, ty) & CellFlags.BlocksWalk) != 0;
    public bool BlocksProjectiles(int tx, int ty) => (Flags(tx, ty) & CellFlags.BlocksProjectiles) != 0;
    /// <summary>시야 막힘 칸(성질 표 대문자). 격자 밖은 거짓(4.11).</summary>
    public bool BlocksSight(int tx, int ty) => (Flags(tx, ty) & CellFlags.BlocksSight) != 0;
    public bool IsBoundary(int tx, int ty) => (Flags(tx, ty) & CellFlags.Boundary) != 0;

    /// <summary>격자 안이고 걷기가 막히지 않은 칸.</summary>
    public bool IsWalkable(int tx, int ty) => IsInside(tx, ty) && (_cells[ty * Size + tx] & CellFlags.BlocksWalk) == 0;

    /// <summary>성질 글자(<see cref="ArenaLayout.PropChars"/> 8개, 격자 밖 <c>#</c>) — 미리보기·로그용.</summary>
    public char Prop(int tx, int ty) => IsInside(tx, ty) ? PropChar(_cells[ty * Size + tx]) : '#';

    /// <summary>성질 → 글자. 글자 번호 = 원본 <c>sol &amp; 7</c>(bit0 걷기, bit1 시야, bit2 투사체 — 6.11 표). 둘레·격자 밖 표시는 보지 않는다.</summary>
    public static char PropChar(CellFlags f) => ArenaLayout.PropChars[SolOf(f)];

    /// <summary>성질 → 원본 SOL 세 비트(<c>sol &amp; 7</c>).</summary>
    internal static int SolOf(CellFlags f) =>
        ((f & CellFlags.BlocksWalk) != 0 ? 1 : 0) | ((f & CellFlags.BlocksSight) != 0 ? 2 : 0) | ((f & CellFlags.BlocksProjectiles) != 0 ? 4 : 0);

    /// <summary>원본 SOL 세 비트(<c>sol &amp; 7</c>) → 성질.</summary>
    internal static CellFlags FromSol(int sol) =>
        ((sol & 1) != 0 ? CellFlags.BlocksWalk : CellFlags.None)
        | ((sol & 2) != 0 ? CellFlags.BlocksSight : CellFlags.None)
        | ((sol & 4) != 0 ? CellFlags.BlocksProjectiles : CellFlags.None);
}
