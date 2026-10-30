namespace DiabloDefenceRemastered.Core.Map;

/// <summary>
/// 하위 타일 ↔ 월드(등각 아레나 그림의 화면 px) 변환 한 곳 (설계서 4.1, 계약 WP1b).
/// N = 하위 타일 수(메가타일 수 × 2), Hc = 그 층의 벽 기둥 높이(L1·L2 160), H = Hc − 32.
/// <list type="bullet">
/// <item>칸 (tx, ty) 바닥 마름모 중심 C = (32N + 32(tx − ty), H + 16(tx + ty) + 16). 꼭짓점 C ± (0,16), C ± (32,0).</item>
/// <item>역: u = (x − 32N)/32, v = (y − H − 16)/16 → tx = (v + u)/2, ty = (v − u)/2. 칸 번호 = 반올림.</item>
/// <item>외곽 상자 64N × (32N + H) — 카메라·<c>World.MapSize</c>. 맵 경계 = N×N 을 덮는 바깥 마름모.</item>
/// </list>
/// 축: tx 가 늘면 화면 오른쪽 아래, ty 가 늘면 왼쪽 아래.
/// </summary>
public readonly record struct IsoMath
{
    /// <summary>바닥 마름모 반폭·반높이(px).</summary>
    public const int HalfWidth = 32;
    public const int HalfHeight = 16;

    public IsoMath(int subTiles, int columnHeight)
    {
        if (subTiles <= 0) throw new ArgumentOutOfRangeException(nameof(subTiles), subTiles, "하위 타일 수는 1 이상");
        if (columnHeight < 32) throw new ArgumentOutOfRangeException(nameof(columnHeight), columnHeight, "기둥 높이는 32 이상");
        N = subTiles;
        ColumnHeight = columnHeight;
    }

    /// <summary>맵 정의와 타일셋으로(N = size × 2, Hc = column_height).</summary>
    public static IsoMath For(MapDef map, TilesetDef tileset) => new(map.Size * 2, tileset.ColumnHeight);

    /// <summary>하위 타일 수(한 변).</summary>
    public int N { get; }
    /// <summary>벽 기둥 높이 Hc.</summary>
    public int ColumnHeight { get; }
    /// <summary>H = Hc − 32. 맨 위 칸(0,0)의 벽 꼭대기가 y = 0 에 닿는 값.</summary>
    public int H => ColumnHeight - 32;

    /// <summary>외곽 상자 64N × (32N + H). N 40 → 2560×1408.</summary>
    public Vector2 BoxSize => new(64 * N, 32 * N + H);

    /// <summary>칸 (tx, ty) 바닥 마름모 중심(월드 px). 가져오기 도구의 바닥 조각·기둥과 렌더가 같은 식을 쓴다(7.6).</summary>
    public Vector2 CellCenter(int tx, int ty) => new(32 * N + 32 * (tx - ty), H + 16 * (tx + ty) + 16);

    /// <summary>하위 타일 좌표(실수) → 월드 px.</summary>
    public Vector2 ToWorld(TileCoord t) => new(32f * N + 32f * (t.Tx - t.Ty), H + 16f * (t.Tx + t.Ty) + 16f);

    /// <summary>월드 px → 하위 타일 좌표(실수). <see cref="ToWorld"/> 의 역.</summary>
    public TileCoord ToTile(Vector2 world)
    {
        var u = (world.X - 32f * N) / 32f;
        var v = (world.Y - H - 16f) / 16f;
        return new TileCoord((v + u) * 0.5f, (v - u) * 0.5f);
    }

    /// <summary>월드 px 가 든 칸 번호(격자 밖일 수 있다 — <see cref="IsInsideGrid"/>).</summary>
    public Int2 CellOf(Vector2 world) => ToTile(world).Cell;

    public bool IsInsideGrid(int tx, int ty) => (uint)tx < (uint)N && (uint)ty < (uint)N;

    /// <summary>바깥 마름모 안인가(맵 안). 칸 좌표로 [−0.5, N−0.5) × [−0.5, N−0.5).</summary>
    public bool IsInsideMap(Vector2 world)
    {
        var t = ToTile(world);
        return t.Tx >= -0.5f && t.Tx < N - 0.5f && t.Ty >= -0.5f && t.Ty < N - 0.5f;
    }

    /// <summary>바깥 마름모 꼭짓점: 위 (32N, H), 오른 (64N, H + 16N), 아래 (32N, H + 32N), 왼 (0, H + 16N).</summary>
    public Vector2 DiamondTop => new(32 * N, H);
    public Vector2 DiamondRight => new(64 * N, H + 16 * N);
    public Vector2 DiamondBottom => new(32 * N, H + 32 * N);
    public Vector2 DiamondLeft => new(0, H + 16 * N);

    /// <summary>
    /// 바깥 마름모까지 부호 있는 거리(px, 안이면 양수 = 가장 가까운 변까지 거리, 밖이면 음수).
    /// 마름모는 볼록이라 안쪽 점의 경계 거리 = 네 변 직선까지 거리의 최솟값이다. 밖은 가장 많이 넘은 변 기준(근사).
    /// </summary>
    public float SignedDistanceToBoundary(Vector2 world)
    {
        // 변의 기울기 ±1/2 → 법선 (1, ±2)/√5. 중심에서의 |dx|/2 + |dy| ≤ 16N 이 안쪽.
        var c = new Vector2(32 * N, H + 16 * N);
        var dx = MathF.Abs(world.X - c.X);
        var dy = MathF.Abs(world.Y - c.Y);
        return (16f * N - (dx * 0.5f + dy)) * (2f / MathF.Sqrt(5f));
    }

    /// <summary>칸 번호 반올림(칸 중심 = 정수, 0.5 는 큰 쪽).</summary>
    public static int RoundToCell(float t) => (int)MathF.Floor(t + 0.5f);
}
