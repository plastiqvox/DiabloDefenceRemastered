namespace DiabloDefenceRemastered.Core.Sim;

/// <summary>
/// 시야(전장의 안개) — 시뮬레이션 상태 (설계서 4.11, 기획 10.7, 계약·구현 WP1c, 그 뒤 WP2 소유). <c>World.Visibility</c>.
/// 원거리 적의 사격·돌진 시작(WP4), 그리기(WP5)·미니맵(WP6)·해시(WP7)가 읽는다. 자동 조작은 읽지 않는다(봇은 안개 무시).
/// <list type="bullet">
/// <item>격자 = 하위 타일 칸, 맵 N×N 둘레에 <see cref="Margin"/> 칸 띠를 더한 확장 격자. 맵 밖 칸은 시야를 막지 않는다.</item>
/// <item>칸 c 가 <see cref="InSight"/> = 주인공 칸 중심 → c 중심 정수 선분이 지나는 칸(h·c 자신 제외)에 시야 막힘 칸이 없다.
///       선분이 칸 꼭짓점을 정확히 지나면 양옆 두 칸이 <b>둘 다</b> 막힘일 때만 막힌다.</item>
/// <item>두 줄 벽: 걷기·시야를 둘 다 막는 칸(<c>W</c>·<c>X</c>)은 같은 메가타일의 <c>W</c>·<c>X</c> 칸 하나가 선 규칙으로 보이면 함께 보인다(한 번만, 옆 메가타일로 퍼지지 않음).</item>
/// <item>다시 계산: 첫 계산, 주인공 칸이 바뀐 틱, 반경이 바뀐 틱. 가 본 칸 표시: 다시 계산한 틱과 <c>Tick % refresh_ticks == 0</c>.</item>
/// <item>첫 계산은 첫 <c>BeginTick</c> 이 한다 — 생성자는 계산하지 않는다(시험·<c>IRunSetup</c> 이 생성 뒤 주인공을 옮긴다).</item>
/// </list>
/// 결정성: 정수 선분·고정 순회 순서, 사전 없음. 할당: 생성자 밖에서 0.
/// </summary>
public sealed class VisibilityField
{
    /// <summary>확장 격자 띠 폭 P(칸). 주인공은 둘레 벽 안쪽이라 띠 밖 칸은 화면 거리 630px 이상 — 반경 상한 480 + 64 보다 멀다.</summary>
    public const int Pad = 20;

    /// <summary>계산 범위 = 주인공 칸 중심에서 칸 중심까지 거리 ≤ R + 이 값(발·대상 자리가 칸 중심에서 각각 최대 32px 어긋남).</summary>
    public const float RangeSlack = 64f;

    private readonly ArenaGrid? _arena;
    private readonly IsoMath _iso;
    private readonly int _n;         // 맵 한 변 N
    private readonly int _e;         // 확장 격자 한 변 N + 2P

    // 확장 격자(E×E) 배열 — 칸 (tx, ty) 의 번호 = (ty + P)·E + (tx + P)
    private readonly bool[] _blocks = [];      // 시야 막힘
    private readonly int[] _blockSum = [];     // 시야 막힘 칸 수의 2차원 누적합((E+1)×(E+1)) — 사이 상자에 막힘이 없으면 선을 걷지 않는다
    private readonly bool[] _wall = [];        // 걷기·시야 둘 다 막힘(W·X) — 두 줄 벽 규칙
    private readonly bool[] _inSight = [];
    private readonly int[] _changed = [];
    private readonly int[] _rangeStamp = [];   // 이 칸이 계산 범위에 든 마지막 계산 번호
    private readonly bool[] _line = [];        // 이번 계산의 선 결과(_rangeStamp == _stamp 인 칸만 유효)
    private int[] _range = [];
    private int[] _prevRange = [];
    private int _rangeCount, _prevCount;
    private int _stamp;

    // 가 본 칸(N×N 비트맵)
    private readonly ulong[] _explored = [];
    private ulong _exploredHash;
    private int _hashedVersion = -1;

    private bool _computed;
    private Int2 _heroCell;

    /// <summary><c>World</c> 생성자가 마지막에 만든다. 맵·<c>rules.sight</c>·조명을 읽어 둘 뿐 계산하지 않는다.</summary>
    public VisibilityField(World world)
    {
        _arena = world.Arena;
        Rules = world.Defs.Rules.Sight;
        FadeTicks = Rules is null ? 0 : Math.Max(0, (int)MathF.Round(Rules.Fade * World.TicksPerSecond, MidpointRounding.AwayFromZero));
        Radius = world.Lighting.HeroLightRadius;
        HeroPos = world.Hero.Pos;
        ViewRect = world.ViewRect;
        if (_arena is null) return;

        _iso = _arena.Iso;
        _n = _arena.Size;
        _e = _n + 2 * Pad;
        var count = _e * _e;
        _blocks = new bool[count];
        _wall = new bool[count];
        _inSight = new bool[count];
        _changed = new int[count];
        _rangeStamp = new int[count];
        _line = new bool[count];
        _range = new int[count];
        _prevRange = new int[count];
        Array.Fill(_changed, -FadeTicks);
        for (var ty = 0; ty < _n; ty++)
            for (var tx = 0; tx < _n; tx++)
            {
                var f = _arena.Flags(tx, ty);
                var i = Index(tx, ty);
                _blocks[i] = (f & CellFlags.BlocksSight) != 0;
                _wall[i] = (f & (CellFlags.BlocksSight | CellFlags.BlocksWalk)) == (CellFlags.BlocksSight | CellFlags.BlocksWalk);
            }
        var s = _e + 1;
        _blockSum = new int[s * s];
        for (var y = 0; y < _e; y++)
            for (var x = 0; x < _e; x++)
                _blockSum[(y + 1) * s + x + 1] = (_blocks[y * _e + x] ? 1 : 0)
                    + _blockSum[y * s + x + 1] + _blockSum[(y + 1) * s + x] - _blockSum[y * s + x];
        _explored = new ulong[(_n * _n + 63) / 64];
    }

    /// <summary>
    /// 맵(<c>World.Arena</c>)·주인공 빛(반경 &gt; 0)·<c>rules.sight</c> 가 다 있을 때. 거짓은 맵 없는 시험 월드뿐이다(실제 데이터는 13.5-15·6.14 가 막는다).
    /// 꺼지면 질의는 늘 참, <see cref="ClearReach"/> 는 +∞, <c>FogMath</c> 알파는 1. 반경은 틱 시작에 받아 두므로 켜짐도 틱 단위로 바뀐다.
    /// </summary>
    public bool Enabled => _arena is not null && Rules is not null && Radius > 0f;

    /// <summary>시야 반경 R = 틱 시작의 <c>World.Lighting.HeroLightRadius</c>. <c>light_change</c> 가 바꾸면 다음 틱부터 새 값이다.</summary>
    public float Radius { get; private set; }
    /// <summary>틱 시작의 주인공 위치. 질의의 거리는 이 값으로 잰다.</summary>
    public Vector2 HeroPos { get; private set; }
    /// <summary>틱 시작의 화면 사각형(<c>World.ViewRect</c>, BeginTick 이 시야 갱신 앞에 쓴 값).</summary>
    public RectF ViewRect { get; private set; }
    /// <summary>확장 격자 띠 폭 P.</summary>
    public int Margin => Pad;
    /// <summary><c>rules.sight</c>(6.10). 없으면 null(<see cref="Enabled"/> 거짓).</summary>
    public SightRulesDef? Rules { get; }
    /// <summary>F = round(fade × 60). 칸 보임이 바뀐 뒤 그림이 바뀌는 틱 수.</summary>
    public int FadeTicks { get; }

    /// <summary>맵 한 변 하위 칸 수 N(맵 없으면 0).</summary>
    public int Size => _n;

    /// <summary>선을 다시 계산한 횟수(시험·디버그 — 같은 칸 안 이동은 다시 계산하지 않음).</summary>
    internal int RecomputeCount { get; private set; }

    // ── 질의 (할당 0, O(1)) ─────────────────────────────────────────

    /// <summary>보임: |pos − HeroPos| ≤ R 이고 pos 의 칸이 <see cref="InSight"/>.</summary>
    public bool IsVisible(Vector2 pos)
    {
        if (!Enabled) return true;
        if (Vector2.DistanceSquared(pos, HeroPos) > Radius * Radius) return false;
        var c = _iso.CellOf(pos);
        return InSight(c.X, c.Y);
    }

    /// <summary>
    /// 또렷이 보임: 보임 + |pos − HeroPos| ≤ R − edge + 발 pos 와 몸 위 끝 (pos.x, pos.y − top) 이 둘 다
    /// <see cref="ViewRect"/> 를 view_inset 만큼 줄인 사각형 안(경계 포함). top = 몸 높이(<c>Enemy.BodyTop</c>).
    /// </summary>
    public bool IsClearlyVisible(Vector2 pos, float top)
    {
        if (!Enabled) return true;
        var r = Radius - Rules!.Edge;
        if (r < 0f || Vector2.DistanceSquared(pos, HeroPos) > r * r) return false;
        var inset = Rules.ViewInset;
        float x0 = ViewRect.X0 + inset, x1 = ViewRect.X1 - inset, y0 = ViewRect.Y0 + inset, y1 = ViewRect.Y1 - inset;
        if (pos.X < x0 || pos.X > x1 || pos.Y < y0 || pos.Y > y1) return false;
        var head = pos.Y - top;
        if (head < y0 || head > y1) return false;
        var c = _iso.CellOf(pos);
        return InSight(c.X, c.Y);
    }

    /// <summary>
    /// L: <see cref="HeroPos"/> 에서 방향 <paramref name="dir"/> 로 "또렷이 보임" 의 거리·화면 조건(벽 가림 제외)을 지키는 가장 먼 거리
    /// = min(R − edge, 그 방향 반직선이 안쪽 사각형(위 변은 top 만큼 아래로)을 나가는 거리), 0 이상. <c>kite</c> 띠 당기기(6.6).
    /// 방향이 0 이면 R − edge.
    /// </summary>
    public float ClearReach(Vector2 dir, float top)
    {
        if (!Enabled) return float.PositiveInfinity;
        var reach = Radius - Rules!.Edge;
        var len = dir.Length();
        if (len > 1e-6f)
        {
            var d = dir / len;
            var inset = Rules.ViewInset;
            float x0 = ViewRect.X0 + inset, x1 = ViewRect.X1 - inset, y0 = ViewRect.Y0 + inset + top, y1 = ViewRect.Y1 - inset;
            if (d.X > 0f) reach = MathF.Min(reach, (x1 - HeroPos.X) / d.X);
            else if (d.X < 0f) reach = MathF.Min(reach, (x0 - HeroPos.X) / d.X);
            if (d.Y > 0f) reach = MathF.Min(reach, (y1 - HeroPos.Y) / d.Y);
            else if (d.Y < 0f) reach = MathF.Min(reach, (y0 - HeroPos.Y) / d.Y);
        }
        return MathF.Max(0f, reach);
    }

    /// <summary>칸 보임(확장 격자, 맵 밖 포함). 확장 격자 밖은 거짓. 그리기·디버그용 — <see cref="Enabled"/> 를 보지 않는다.</summary>
    public bool InSight(int tx, int ty) => InExtended(tx, ty) && _inSight[Index(tx, ty)];

    /// <summary>칸 보임 값이 바뀐 스테이지 틱(바뀜 값 잇기 반영). 바뀐 적 없거나 확장 격자 밖이면 −F.</summary>
    public int ChangedTick(int tx, int ty) => InExtended(tx, ty) ? _changed[Index(tx, ty)] : -FadeTicks;

    /// <summary>가 본 칸(N×N 만, 맵 밖은 늘 거짓).</summary>
    public bool IsExplored(int tx, int ty)
    {
        if ((uint)tx >= (uint)_n || (uint)ty >= (uint)_n) return false;
        var b = ty * _n + tx;
        return (_explored[b >> 6] & (1UL << (b & 63))) != 0;
    }

    public int ExploredCount { get; private set; }

    /// <summary>가 본 칸이 늘 때마다 1 오른다(미니맵이 가림을 다시 굽는 신호, 8.2).</summary>
    public int ExploredVersion { get; private set; }

    /// <summary>가 본 칸 비트맵의 <c>StableHash</c>(64비트 — 두 사슬). <c>StateHasher</c>(WP7)가 넣는다. 판이 바뀔 때만 다시 센다.</summary>
    public ulong ExploredHash
    {
        get
        {
            if (_hashedVersion != ExploredVersion)
            {
                uint hi = StableHash.Of((uint)_n, 0x5EEDu), lo = StableHash.Of((uint)_n, 0xF06u);
                foreach (var w in _explored)
                {
                    hi = StableHash.Of(hi, (uint)w, (uint)(w >> 32));
                    lo = StableHash.Of(lo, (uint)(w >> 32), (uint)w);
                }
                _exploredHash = ((ulong)hi << 32) | lo;
                _hashedVersion = ExploredVersion;
            }
            return _exploredHash;
        }
    }

    /// <summary>월드 px → 칸 번호(<c>IsoMath</c> 역변환 반올림). 맵 없으면 (0, 0).</summary>
    public Int2 CellOf(Vector2 pos) => _arena is null ? default : _iso.CellOf(pos);

    // ── 갱신 ────────────────────────────────────────────────────────

    /// <summary>
    /// BeginTick(3.5 1단계, 화면 사각형 다음): 주인공 위치·반경·화면 사각형을 받고, 필요하면 선을 다시 계산하고 가 본 칸을 표시한다.
    /// </summary>
    internal void Update(World world)
    {
        var radius = world.Lighting.HeroLightRadius;
        var radiusChanged = radius != Radius;
        Radius = radius;
        HeroPos = world.Hero.Pos;
        ViewRect = world.ViewRect;
        if (!Enabled) return;

        var tick = world.Tick;
        var cell = _iso.CellOf(HeroPos);
        // 확장 격자 밖이면 배열 밖을 읽게 된다. 둘레 벽이 막아 실제로는 오지 않지만, 오면 지난 시야를 그대로 둔다.
        if (!InExtended(cell.X, cell.Y)) return;
        var recompute = !_computed || radiusChanged || cell != _heroCell;
        if (recompute) Compute(cell, tick);
        var refresh = Rules!.RefreshTicks;
        if (recompute || (refresh > 0 && tick % refresh == 0)) MarkExplored();
    }

    private void Compute(Int2 h, int tick)
    {
        var first = !_computed;
        _computed = true;
        _heroCell = h;
        RecomputeCount++;
        var stamp = ++_stamp;

        // 1. 범위 안 칸마다 선 결과
        var lim = Radius + RangeSlack;
        var lim2 = lim * lim;
        var k = (int)(lim / 28f) + 1;   // max(|dx|,|dy|) = k 인 칸의 최소 화면 거리 ≈ 28.6k
        var ox = h.X + Pad;
        var oy = h.Y + Pad;
        var count = 0;
        for (var dy = -k; dy <= k; dy++)
        {
            var ty = h.Y + dy;
            if (ty < -Pad || ty >= _n + Pad) continue;
            for (var dx = -k; dx <= k; dx++)
            {
                var tx = h.X + dx;
                if (tx < -Pad || tx >= _n + Pad) continue;
                var sx = 32 * (dx - dy);
                var sy = 16 * (dx + dy);
                if (sx * sx + sy * sy > lim2) continue;
                var i = Index(tx, ty);
                _rangeStamp[i] = stamp;
                _range[count++] = i;
                // 선이 지나는 칸·꼭짓점 양옆 칸은 모두 h 와 c 를 잇는 상자 안이다 → 상자에 막힘이 없으면 걷지 않아도 보임(결과는 같다)
                _line[i] = BlocksInBox(ox, oy, tx + Pad, ty + Pad) == 0 || LineClear(_blocks, _e, ox, oy, tx + Pad, ty + Pad);
            }
        }
        _rangeCount = count;

        // 2. 두 줄 벽(같은 메가타일의 W·X 칸) + 바뀜 기록
        for (var r = 0; r < count; r++)
        {
            var i = _range[r];
            var v = _line[i] || (_wall[i] && WallMateInLine(i, stamp));
            SetCell(i, v, tick, first);
        }

        // 3. 범위 밖으로 나간 칸 = 거짓으로 바뀜
        for (var r = 0; r < _prevCount; r++)
        {
            var i = _prevRange[r];
            if (_rangeStamp[i] != stamp && _inSight[i]) SetCell(i, false, tick, first);
        }

        (_range, _prevRange) = (_prevRange, _range);
        _prevCount = _rangeCount;
    }

    /// <summary>같은 메가타일의 다른 W·X 칸 중 이번 계산 범위에 들고 선 결과가 보임인 칸이 있나(한 번만 — 선 결과만 본다).</summary>
    private bool WallMateInLine(int i, int stamp)
    {
        var tx = i % _e - Pad;
        var ty = i / _e - Pad;
        var mx = tx >> 1;
        var my = ty >> 1;
        for (var b = 0; b < 2; b++)
            for (var a = 0; a < 2; a++)
            {
                var j = Index(2 * mx + a, 2 * my + b);
                if (j == i || !_wall[j] || _rangeStamp[j] != stamp) continue;
                if (_line[j]) return true;
            }
        return false;
    }

    /// <summary>칸 값 바꾸기 + 바뀜 값 잇기: 지난 바뀜에서 a 틱이 지났고 a &lt; F 면 ChangedTick ← t − (F − a), 아니면 t. 첫 계산은 t − F(바로 다 보임).</summary>
    private void SetCell(int i, bool v, int tick, bool first)
    {
        if (_inSight[i] == v) return;
        _inSight[i] = v;
        if (first) { _changed[i] = tick - FadeTicks; return; }
        var a = tick - _changed[i];
        _changed[i] = a < FadeTicks ? tick - (FadeTicks - a) : tick;
    }

    private void MarkExplored()
    {
        var r2 = Radius * Radius;
        var added = false;
        for (var r = 0; r < _prevCount; r++)   // 계산 뒤 바꿔 끼웠으므로 지금 범위 = _prevRange
        {
            var i = _prevRange[r];
            if (!_inSight[i]) continue;
            var tx = i % _e - Pad;
            var ty = i / _e - Pad;
            if ((uint)tx >= (uint)_n || (uint)ty >= (uint)_n) continue;
            if (Vector2.DistanceSquared(_iso.CellCenter(tx, ty), HeroPos) > r2) continue;
            added |= Mark(tx, ty);
            if (!_wall[i]) continue;
            // 두 줄 벽: 같은 메가타일의 W·X 칸 모두
            int mx = tx >> 1, my = ty >> 1;
            for (var b = 0; b < 2; b++)
                for (var a = 0; a < 2; a++)
                {
                    int x = 2 * mx + a, y = 2 * my + b;
                    if (_wall[Index(x, y)]) added |= Mark(x, y);
                }
        }
        if (added) ExploredVersion++;
    }

    private bool Mark(int tx, int ty)
    {
        var b = ty * _n + tx;
        var bit = 1UL << (b & 63);
        if ((_explored[b >> 6] & bit) != 0) return false;
        _explored[b >> 6] |= bit;
        ExploredCount++;
        return true;
    }

    private bool InExtended(int tx, int ty) => _arena is not null && tx >= -Pad && ty >= -Pad && tx < _n + Pad && ty < _n + Pad;

    private int Index(int tx, int ty) => (ty + Pad) * _e + (tx + Pad);

    /// <summary>배열 좌표 두 칸을 꼭짓점으로 하는 상자 안 시야 막힘 칸 수.</summary>
    private int BlocksInBox(int ax, int ay, int bx, int by)
    {
        int x0 = Math.Min(ax, bx), x1 = Math.Max(ax, bx) + 1, y0 = Math.Min(ay, by), y1 = Math.Max(ay, by) + 1;
        var s = _e + 1;
        return _blockSum[y1 * s + x1] - _blockSum[y0 * s + x1] - _blockSum[y1 * s + x0] + _blockSum[y0 * s + x0];
    }

    /// <summary>
    /// 선 걷기(정수만): 배열 좌표 h 에서 c 까지 칸 중심 선분이 지나는 칸(h·c 제외)에 막힘이 없나. 칸 경계는 반정수라
    /// 다음 x 경계 t = (2i+1)/(2|dx|), y 경계 t = (2j+1)/(2|dy|) 를 (2i+1)|dy| ↔ (2j+1)|dx| 로 비교한다. 같으면 꼭짓점 —
    /// 양옆 두 칸이 둘 다 막힘일 때만 막히고 대각선으로 한 칸 간다. <paramref name="blocks"/> 는 stride 폭의 행 우선 배열.
    /// </summary>
    internal static bool LineClear(ReadOnlySpan<bool> blocks, int stride, int hx, int hy, int cx, int cy)
    {
        int dx = cx - hx, dy = cy - hy;
        int ax = Math.Abs(dx), ay = Math.Abs(dy);
        int sx = Math.Sign(dx), sy = Math.Sign(dy);
        int x = hx, y = hy, i = 0, j = 0;
        while (i < ax || j < ay)
        {
            int step;   // 0 = x, 1 = y, 2 = 꼭짓점(대각선)
            if (i == ax) step = 1;
            else if (j == ay) step = 0;
            else
            {
                var lhs = (2 * i + 1) * ay;
                var rhs = (2 * j + 1) * ax;
                step = lhs < rhs ? 0 : lhs > rhs ? 1 : 2;
            }
            if (step == 0) { x += sx; i++; }
            else if (step == 1) { y += sy; j++; }
            else
            {
                if (blocks[y * stride + x + sx] && blocks[(y + sy) * stride + x]) return false;
                x += sx; y += sy; i++; j++;
            }
            if ((x != cx || y != cy) && blocks[y * stride + x]) return false;
        }
        return true;
    }
}
