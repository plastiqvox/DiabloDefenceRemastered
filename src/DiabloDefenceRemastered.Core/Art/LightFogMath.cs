namespace DiabloDefenceRemastered.Core.Art;

/// <summary>빛 하나의 종류(설계서 7.7 빛 상한 64 — 주인공·점 빛 먼저, 남는 자리를 짧은 빛 중 주인공에게 가까운 순으로).</summary>
public enum LightKind : byte { Hero, Point, Short }

/// <summary>빛 상한 고르기 입력 하나.</summary>
public readonly record struct LightCandidate(LightKind Kind, Vector2 Pos);

/// <summary>
/// 빛 계산 (설계서 7.7, 소유 WP5). 그래픽 없는 값만 — 바탕 밝기, 깜빡임·맥동 배율, 빛 상한 고르기. 혼합·텍스처는 게임 <c>LightRenderer</c>.
/// </summary>
public static class LightMath
{
    /// <summary>빛 상한.</summary>
    public const int MaxLights = 64;

    /// <summary><c>LightRT</c> 바탕 = 주변광 × (1 − 어둠), 채널마다 0..1.</summary>
    public static Vector3 Base(Rgb ambient, float darkness)
    {
        var k = Math.Clamp(1f - darkness, 0f, 1f) / 255f;
        return new Vector3(ambient.R * k, ambient.G * k, ambient.B * k);
    }

    /// <summary>
    /// 기억 밝기 M = 주변광 × (1 − 어둠) × <c>rules.sight.memory_ratio</c>(7.10 합치기). 어둠이 <c>light_change</c> 로 바뀌면 같은 프레임에 따른다.
    /// </summary>
    public static Vector3 Memory(Rgb ambient, float darkness, float memoryRatio) => Base(ambient, darkness) * Math.Clamp(memoryRatio, 0f, 1f);

    /// <summary>
    /// 세기 배율: 깜빡임 = 1 − amount × u(u = <c>StableHash(seed, lightId, stageTick)</c> 0..1), 맥동 = 1 + amount × sin(2π·t / 주기)(t = 스테이지 시간). 둘을 곱한다(3.4 — 프레임 난수 아님).
    /// </summary>
    public static float Intensity(ulong seed, string lightId, int stageTick, float flicker, float pulsePeriod, float pulseAmount)
    {
        var k = 1f;
        if (flicker > 0f)
            k *= 1f - Math.Clamp(flicker, 0f, 1f) * StableHash.ToUnit(StableHash.Of(StableHash.Of(seed), StableHash.Of(lightId), (uint)stageTick));
        if (pulsePeriod > 0f && pulseAmount != 0f)
            k *= 1f + pulseAmount * MathF.Sin(MathF.Tau * (stageTick * World.Dt) / pulsePeriod);
        return Math.Max(0f, k);
    }

    /// <summary>
    /// 빛 상한 고르기: 주인공·점 빛을 들어온 순서로 먼저, 남는 자리를 짧은 빛 중 주인공에게 가까운 순(같으면 들어온 순)으로.
    /// <paramref name="picked"/> 에 고른 번호를 쓰고 그 수를 돌려준다. <paramref name="scratch"/> 는 후보 수 이상(할당 0).
    /// </summary>
    public static int Pick(ReadOnlySpan<LightCandidate> lights, Vector2 heroPos, Span<int> picked, Span<int> scratch, int max = MaxLights)
    {
        var count = 0;
        var limit = Math.Min(max, picked.Length);
        var shorts = 0;
        for (var i = 0; i < lights.Length; i++)
        {
            if (lights[i].Kind == LightKind.Short) { scratch[shorts++] = i; continue; }
            if (count < limit) picked[count++] = i;
        }
        if (count >= limit || shorts == 0) return count;

        // 짧은 빛: 거리 → 번호 순 삽입 정렬(후보는 화면 안 폭발 수십 개)
        var s = scratch[..shorts];
        for (var i = 1; i < s.Length; i++)
        {
            var v = s[i];
            var dv = Vector2.DistanceSquared(lights[v].Pos, heroPos);
            var j = i - 1;
            while (j >= 0)
            {
                var dj = Vector2.DistanceSquared(lights[s[j]].Pos, heroPos);
                if (dj < dv || (dj == dv && s[j] < v)) break;
                s[j + 1] = s[j];
                j--;
            }
            s[j + 1] = v;
        }
        for (var i = 0; i < s.Length && count < limit; i++) picked[count++] = s[i];
        return count;
    }
}

/// <summary>
/// 안개 그리기 판단 (설계서 7.10, 소유 WP5) — 그래픽 없는 부분. 혼합 상태·렌더 타깃은 게임 <c>FogRenderer</c>.
/// </summary>
public static class FogShade
{
    /// <summary>
    /// 경계 흐림 띠 값: 반경 R − edge 안 1, R 에서 0, 사이는 선형(7.10 ② — 띠 텍스처와 같은 식). edge ≤ 0 이면 R 안 1.
    /// </summary>
    public static float Band(float distance, float radius, float edge) =>
        edge > 0f ? Math.Clamp((radius - distance) / edge, 0f, 1f) : (distance <= radius ? 1f : 0f);

    /// <summary>
    /// 대상 모양을 <c>FogRT</c> 에 덧그려야 하나(7.10 4단계 — 필요한 것만): ① 그림 사각형이 시야 원 R − edge 밖에 걸침(가장 먼 귀퉁이가 R − edge 밖),
    /// ② 사각형 네 귀퉁이·발의 칸 중 <c>CellLevel</c> &lt; 1 인 칸이 있음. 늘 그리는 대상(③)은 부르는 쪽이 따로 넣는다. 안개가 꺼졌으면 거짓.
    /// </summary>
    public static bool NeedsShape(VisibilityField f, RectF rect, Vector2 foot, Vector2 heroPos, int stageTick)
    {
        if (!f.Enabled) return false;
        var inner = f.Radius - (f.Rules?.Edge ?? 0f);
        var inner2 = inner * inner;
        if (Vector2.DistanceSquared(new Vector2(rect.X0, rect.Y0), heroPos) > inner2
            || Vector2.DistanceSquared(new Vector2(rect.X1, rect.Y0), heroPos) > inner2
            || Vector2.DistanceSquared(new Vector2(rect.X0, rect.Y1), heroPos) > inner2
            || Vector2.DistanceSquared(new Vector2(rect.X1, rect.Y1), heroPos) > inner2)
            return true;
        return Below1(f, new Vector2(rect.X0, rect.Y0), stageTick) || Below1(f, new Vector2(rect.X1, rect.Y0), stageTick)
            || Below1(f, new Vector2(rect.X0, rect.Y1), stageTick) || Below1(f, new Vector2(rect.X1, rect.Y1), stageTick)
            || Below1(f, foot, stageTick);
    }

    private static bool Below1(VisibilityField f, Vector2 p, int tick)
    {
        var c = f.CellOf(p);
        return FogMath.CellLevel(f, c.X, c.Y, tick) < 1f;
    }

    /// <summary>
    /// 벽 기둥이 <c>FogRT</c> 에 받는 값 = 그 칸 <c>CellLevel</c> × 칸 중심의 경계 흐림 값(7.10 3단계). 안개가 꺼졌으면 1.
    /// </summary>
    public static float WallLevel(VisibilityField f, IsoMath iso, int tx, int ty, Vector2 heroPos, int stageTick)
    {
        if (!f.Enabled) return 1f;
        var level = FogMath.CellLevel(f, tx, ty, stageTick);
        if (level <= 0f) return 0f;
        var d = Vector2.Distance(iso.CellCenter(tx, ty), heroPos);
        return level * Band(d, f.Radius, f.Rules?.Edge ?? 0f);
    }
}

/// <summary>
/// 문 봉인 (설계서 7.11, 소유 WP5): <c>Room.Phase &lt; Doors</c> 이거나 <c>Exits</c> 에 없는 문(들어온 문 포함)은 붉은 막. 방 없는 월드는 봉인이 없다.
/// </summary>
public static class GateSeals
{
    public static bool IsSealed(RoomState? room, string gateId)
    {
        if (room is null) return false;
        if (room.Phase < RoomPhase.Doors) return true;
        return !TryGetExit(room, gateId, out _);
    }

    /// <summary>열린 출구 정보(문 위 아이콘·이름).</summary>
    public static bool TryGetExit(RoomState room, string gateId, out RoomExit exit)
    {
        foreach (var e in room.Exits)
            if (string.Equals(e.GateId, gateId, StringComparison.Ordinal)) { exit = e; return true; }
        exit = default;
        return false;
    }

    /// <summary>
    /// 문 위 이름(7.11): 정예 방 = <c>정예</c>(덤 보상이 있으면 <c>정예 상자 + 이름</c> 꼴), 보스 방 = 보스 이름, 계단 = <c>&lt;다음 액트&gt;로</c>, 그 밖 = 보상 이름.
    /// </summary>
    public static string Label(in RoomExit exit)
    {
        if (exit.Stairs) return exit.NextActName is { Length: > 0 } act ? $"{act}로" : "계단";
        var reward = exit.Reward?.Name;
        return exit.NextKind switch
        {
            RoomKind.Elite => reward is { Length: > 0 } ? $"정예 상자 + {reward}" : "정예",
            RoomKind.Boss => exit.NextBoss?.Name is { Length: > 0 } boss ? boss : "보스",
            _ => reward ?? "",
        };
    }

    /// <summary>층 문구(v0.8 — <c>NextFloor</c> 가 0 이 아니면 <c>▼ n층</c>).</summary>
    public static string? FloorLabel(in RoomExit exit) => exit.NextFloor != 0 ? $"▼ {exit.NextFloor}층" : null;

    /// <summary>
    /// 봉인 그림의 바닥 자리(월드 px). 문 틈 칸 중 방 안쪽 줄(문턱)에 낮은 창살 발 자리를 칸마다 둘(칸의 안쪽 끝에서 0.2칸 안 — 칸 안에 머문다),
    /// 룬은 문 틈 칸 가운데. 칸이 없으면 창살 0. 그리기(크기·색)는 게임 <c>WorldRenderer</c>.
    /// </summary>
    public static GateSealShape Shape(GateZone zone, IsoMath iso)
    {
        if (zone.Cells.Count == 0) return new GateSealShape(default, []);
        // 문턱 = 방 안쪽 줄: nw(왼쪽 열)·se(오른쪽 열)는 x, ne(윗줄)·sw(아랫줄)는 y 가 깊이 축
        var (inward, along) = zone.Side switch
        {
            GateSide.Nw => (new Int2(1, 0), new Int2(0, 1)),
            GateSide.Se => (new Int2(-1, 0), new Int2(0, 1)),
            GateSide.Ne => (new Int2(0, 1), new Int2(1, 0)),
            _ => (new Int2(0, -1), new Int2(1, 0)),
        };
        var inner = int.MinValue;
        foreach (var c in zone.Cells) inner = Math.Max(inner, c.X * inward.X + c.Y * inward.Y);
        var sum = Vector2.Zero;
        var bars = new List<Vector2>(8);
        foreach (var c in zone.Cells)
        {
            var center = iso.CellCenter(c.X, c.Y);
            sum += center;
            if (c.X * inward.X + c.Y * inward.Y != inner) continue;
            var inPx = iso.CellCenter(c.X + inward.X, c.Y + inward.Y) - center;
            var alongPx = iso.CellCenter(c.X + along.X, c.Y + along.Y) - center;
            var foot = center + inPx * 0.3f;
            bars.Add(foot - alongPx * 0.25f);
            bars.Add(foot + alongPx * 0.25f);
        }
        return new GateSealShape(sum / zone.Cells.Count, bars);
    }
}

/// <summary>봉인 그림 자리(<see cref="GateSeals.Shape"/>): 룬 가운데와 창살 발 자리(월드 px, 문턱을 따라 차례로).</summary>
public readonly record struct GateSealShape(Vector2 Center, IReadOnlyList<Vector2> Bars);
