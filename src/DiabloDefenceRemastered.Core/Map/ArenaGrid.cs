namespace DiabloDefenceRemastered.Core.Map;

/// <summary>하위 칸 성질(설계서 6.11 성질 표 글자 + 둘레 표시).</summary>
[Flags]
public enum CellFlags : byte
{
    None = 0,
    /// <summary>걷기 막힘(<c>W</c>·<c>X</c>, 원본 SOL bit0).</summary>
    BlocksWalk = 1,
    /// <summary>투사체 막힘(<c>M</c>·<c>X</c>, 원본 SOL bit2).</summary>
    BlocksProjectiles = 2,
    /// <summary>테두리·문 끝 조각에서 온 칸. 막힌 칸이면 그 도형은 <c>IsBoundary</c> — 장애물 통과 유닛도 막힌다(4.2-4).</summary>
    Boundary = 4,
    /// <summary>격자 밖(<see cref="ArenaGrid.Flags"/> 가 격자 밖 좌표에 돌려준다. 걷기·투사체 막힘도 같이 켜져 있다).</summary>
    Outside = 8,
}

/// <summary>
/// 배치 풀기 결과 (설계서 6.11 5단계, 계약 WP1b): 메가타일 번호 M×M + 하위 칸 성질 N×N + 둘레 표시 + <see cref="LayoutHash"/>.
/// <c>ArenaLayout.Expand</c> 만 만든다. 충돌(WP2 <c>ArenaCollision</c>)·그림 굽기(WP0)·벽 무리 나누기(WP5)가 같은 격자를 읽는다.
/// 칸 좌표: 메가타일 (mx, my) → 하위 칸 (2mx,2my) (2mx+1,2my) (2mx,2my+1) (2mx+1,2my+1).
/// </summary>
public sealed class ArenaGrid
{
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
    /// </summary>
    public string LayoutHash { get; }

    public int MegaTile(int mx, int my)
    {
        if ((uint)mx >= (uint)MegaSize || (uint)my >= (uint)MegaSize)
            throw new ArgumentOutOfRangeException(nameof(mx), $"메가타일 ({mx},{my}) 는 {MegaSize}×{MegaSize} 밖이다");
        return _mega[my * MegaSize + mx];
    }

    public bool IsInside(int tx, int ty) => (uint)tx < (uint)Size && (uint)ty < (uint)Size;

    /// <summary>하위 칸 성질. 격자 밖이면 <see cref="CellFlags.Outside"/>(걷기·투사체 막힘 포함).</summary>
    public CellFlags Flags(int tx, int ty) => IsInside(tx, ty) ? _cells[ty * Size + tx] : OutsideFlags;

    public bool BlocksWalk(int tx, int ty) => (Flags(tx, ty) & CellFlags.BlocksWalk) != 0;
    public bool BlocksProjectiles(int tx, int ty) => (Flags(tx, ty) & CellFlags.BlocksProjectiles) != 0;
    public bool IsBoundary(int tx, int ty) => (Flags(tx, ty) & CellFlags.Boundary) != 0;

    /// <summary>격자 안이고 걷기가 막히지 않은 칸.</summary>
    public bool IsWalkable(int tx, int ty) => IsInside(tx, ty) && (_cells[ty * Size + tx] & CellFlags.BlocksWalk) == 0;

    /// <summary>성질 글자(<c>.</c> <c>W</c> <c>M</c> <c>X</c>, 격자 밖 <c>#</c>) — 미리보기·로그용.</summary>
    public char Prop(int tx, int ty) => IsInside(tx, ty) ? PropChar(_cells[ty * Size + tx]) : '#';

    public static char PropChar(CellFlags f) => (f & (CellFlags.BlocksWalk | CellFlags.BlocksProjectiles)) switch
    {
        CellFlags.BlocksWalk => 'W',
        CellFlags.BlocksProjectiles => 'M',
        CellFlags.BlocksWalk | CellFlags.BlocksProjectiles => 'X',
        _ => '.',
    };
}
