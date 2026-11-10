namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 판의 승패 (설계서 3.5). Running → VictoryPending(3초 스테이지 틱) → Victory, Running → DefeatPending(2초) → Defeat.
/// 같은 틱에 둘 다면 승리가 먼저다. Running 이 아니면 Director 는 아무것도 안 하고 DamageResolve 는 주인공 피해를 무시한다.
/// </summary>
public enum RunOutcome : byte { Running, VictoryPending, Victory, DefeatPending, Defeat }

/// <summary>선택 대기. EndTick 이 켜고 PlayScene 이 같은 앱 틱에 오버레이를 올린다(3.3). <see cref="Reward"/>(v0.7) = 마법서·성소 창(레벨업 오버레이 재사용, 6.18).</summary>
public enum ChoiceKind : byte { None, LevelUp, Chest, Reward }

/// <summary>점 빛 하나의 현재 값(월드 px). 깜빡임·맥동은 렌더가 스테이지 틱으로 계산한다(7.7).</summary>
public struct PointLight
{
    public string Id;
    public Vector2 Pos;
    public Rgb Color;
    public float Radius;
    public float Flicker;
    public float PulsePeriod;
    public float PulseAmount;
}

/// <summary>
/// 현재 조명 (<c>World.Lighting</c>). 스테이지 정의로 시작하고 <c>light_change</c> 이벤트(WP4)가 바꾸며 렌더(WP5)가 읽는다.
/// 점 빛은 정의 순서 그대로이고 자리는 로딩 때 <see cref="ResolvedMap"/> 으로 월드 px 로 푼 값이다(기준점 ID 또는 [tx,ty]).
/// </summary>
public sealed class LightingState
{
    private readonly List<PointLight> _lights = [];
    private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);

    public Rgb Ambient { get; set; } = Rgb.White;
    public float Darkness { get; set; }
    public Rgb HeroLightColor { get; set; } = Rgb.White;
    public float HeroLightRadius { get; set; }
    /// <summary>유닛(주인공·적·시체·획득물) 밝기 하한(0 = 끔, 7.7).</summary>
    public float UnitMinLight { get; set; }

    /// <summary>점 빛(정의 순서). 매 프레임 for 로 돌려도 할당이 없다.</summary>
    public IReadOnlyList<PointLight> Lights => _lights;

    /// <summary>ID 로 찾기(<c>light_change</c> 의 <c>lights.&lt;id&gt;.*</c>).</summary>
    public bool TryGetLight(string id, out PointLight light)
    {
        if (_index.TryGetValue(id, out var i)) { light = _lights[i]; return true; }
        light = default;
        return false;
    }

    /// <summary>있는 빛을 바꾼다. 없는 ID 면 false(새 빛은 만들지 않는다 — 자리는 정의에서만 온다).</summary>
    public bool SetLight(string id, in PointLight light)
    {
        if (!_index.TryGetValue(id, out var i)) return false;
        _lights[i] = light with { Id = id };
        return true;
    }

    public void Load(LightingDef lighting, ResolvedMap map)
    {
        Ambient = lighting.Ambient;
        Darkness = lighting.Darkness;
        HeroLightColor = lighting.HeroLight.Color;
        HeroLightRadius = lighting.HeroLight.Radius;
        UnitMinLight = lighting.UnitMinLight;
        _lights.Clear();
        _index.Clear();
        foreach (var l in lighting.Lights)
        {
            if (!_index.TryAdd(l.Id, _lights.Count)) throw new ArgumentException($"점 빛 ID '{l.Id}' 가 두 번 있다");
            _lights.Add(new PointLight
            {
                Id = l.Id,
                Pos = map.ToWorld(l.AtTile),
                Color = l.Color,
                Radius = l.Radius,
                Flicker = l.Flicker,
                PulsePeriod = l.Pulse?.Period ?? 0f,
                PulseAmount = l.Pulse?.Amount ?? 0f,
            });
        }
    }

    /// <summary>
    /// <c>light_change</c> 하나를 적용한다(v0.7.1, 순수 — 3.6 결정 5). 방 월드 생성자가 첫 틱 전에 지난 것(<c>RoomSchedule.Replay</c>)을 시간 순으로,
    /// 감독(WP4)이 방 안에서 시각이 된 것을 같은 함수로 부른다. 경로는 적힌 순서대로. 모르는 경로·없는 점 빛·값 개수가 틀리면 예외(로딩 검사 13.5-1 이 먼저 막는다).
    /// </summary>
    public void Apply(LightChangeParams change)
    {
        ArgumentNullException.ThrowIfNull(change);
        foreach (var set in change.Set) Apply(set);
    }

    private void Apply(LightSet set)
    {
        var path = set.Path;
        switch (path)
        {
            case "ambient": Ambient = ColorOf(set); return;
            case "darkness": Darkness = One(set); return;
            case "unit_min_light": UnitMinLight = One(set); return;
            case "hero_light.color": HeroLightColor = ColorOf(set); return;
            case "hero_light.radius": HeroLightRadius = One(set); return;
        }
        const string lightsPrefix = "lights.";
        if (path.StartsWith(lightsPrefix, StringComparison.Ordinal))
        {
            var rest = path.AsSpan(lightsPrefix.Length);
            var dot = rest.IndexOf('.');
            if (dot > 0)
            {
                var id = rest[..dot].ToString();
                var field = rest[(dot + 1)..].ToString();
                if (!TryGetLight(id, out var light)) throw new ArgumentException($"light_change: 점 빛 '{id}' 가 없다({path})");
                switch (field)
                {
                    case "color": light.Color = ColorOf(set); break;
                    case "radius": light.Radius = One(set); break;
                    case "flicker": light.Flicker = One(set); break;
                    case "pulse.period": light.PulsePeriod = One(set); break;
                    case "pulse.amount": light.PulseAmount = One(set); break;
                    default: throw new ArgumentException($"light_change: 모르는 경로 '{path}'");
                }
                SetLight(id, light);
                return;
            }
        }
        throw new ArgumentException($"light_change: 모르는 경로 '{path}'");
    }

    private static float One(LightSet set) =>
        set.Value is { Length: 1 } v ? v[0] : throw new ArgumentException($"light_change: '{set.Path}' 는 값 하나여야 한다");

    private static Rgb ColorOf(LightSet set)
    {
        if (set.Value is not { Length: 3 } v) throw new ArgumentException($"light_change: '{set.Path}' 는 [r,g,b] 여야 한다");
        static byte B(float x) => (byte)Math.Clamp((int)MathF.Round(x), 0, 255);
        return new Rgb(B(v[0]), B(v[1]), B(v[2]));
    }
}

/// <summary>
/// 주인공이 선 <b>안쪽 방</b>과 안쪽 방마다 누적 (v0.6.3, <c>World.InnerRoomPresence</c> — 기획 10.1 방 체류 압박·첫 플레이 로그). 방 번호 = <c>World.InnerRooms.Rooms</c> 의 순서.
/// v0.7: <c>World</c> 수명이라 판 단위 방(<c>World.Room</c>) 하나 — 그 맵 하나의 누적이다(3.6). 형 이름은 그대로 두고 <c>World</c> 속성 이름만 v0.7.1 에 바꿨다(11.2 "방" 의 두 뜻).
/// 쓰기: WP4 Director(2단계 — 주인공 칸의 방 번호로 <see cref="Current"/>·<see cref="EnteredTick"/>, 누적은 앞 틱의 <c>HeroDamaged</c>·<c>SupplyCollected</c> 로).
/// 읽기: WP4 방 체류 압박·<c>AutoplayPolicy</c>("방 안"), WP7 로그 <c>INNER_ROOMS</c>(v0.7.1 — 판 단위 방이 끝날 때마다 안쪽 방마다 체류 시간·받은 피해·주운 보급)·<c>StateHasher</c>(<see cref="Current"/>·<see cref="EnteredTick"/>).
/// 사람이 하는 판에도 센다(자동 조작의 <c>RoomVisitStats</c> 와 따로).
/// </summary>
public sealed class RoomPresence
{
    /// <summary>지금 주인공이 선 방 번호. 방 밖이면 −1.</summary>
    public int Current { get; set; } = -1;
    /// <summary><see cref="Current"/> 에 들어간 스테이지 틱(방 체류 압박의 머문 시간 = 지금 틱 − 이것).</summary>
    public int EnteredTick { get; set; }
    /// <summary>방마다 바닥 안에 있던 틱 수.</summary>
    public int[] TicksInside { get; private set; } = [];
    /// <summary>방마다 바닥 안에 있는 동안 받은 피해 합.</summary>
    public float[] DamageInside { get; private set; } = [];
    /// <summary>방마다 주운 보급 수(<c>SupplyCollected</c> 의 방 ID 로 셈).</summary>
    public int[] SuppliesTaken { get; private set; } = [];

    /// <summary>방 수에 맞춰 비운다(월드 생성 때 한 번).</summary>
    public void Reset(int roomCount)
    {
        Current = -1;
        EnteredTick = 0;
        TicksInside = new int[roomCount];
        DamageInside = new float[roomCount];
        SuppliesTaken = new int[roomCount];
    }
}

/// <summary>보스 막대 하나. 체력은 렌더가 그 적에서 읽는다. Final = 최종 보스(위 막대).</summary>
public readonly record struct BossBar(EntityId Id, string Name, bool Final);

/// <summary>
/// 지금 띄운 알림 문구(<c>World.Banner</c>). HUD 문구의 출처는 이것 하나다: 쓰기 WP4(<c>banner</c> 이벤트·보스 등장·정예 무리), 읽기 WP6(HUD).
/// 새 문구가 오면 앞 문구를 덮는다. 표시 이벤트 버퍼에는 알림 문구가 없다.
/// </summary>
public struct BannerState
{
    public string? Text;
    public int StartTick;
    public int EndTick;

    public readonly bool IsActive(int tick) => Text is not null && tick >= StartTick && tick < EndTick;

    /// <summary>스테이지 틱 <paramref name="tick"/> 부터 <paramref name="seconds"/> 초 동안 띄운다.</summary>
    public void Show(string text, int tick, float seconds)
    {
        Text = text;
        StartTick = tick;
        EndTick = tick + Math.Max(1, (int)MathF.Round(seconds * World.TicksPerSecond));
    }
}

/// <summary>
/// 공정성 계측 누적 수 (<c>World.Fairness</c>, v0.5.1, 설계서 10.1 공정성 열 — 기획 10.7 공정성 1~5). 방(월드) 하나의 수이고, 방이 끝나면 <c>RunSession</c> 이 <see cref="AddTo"/> 로 판 합(<c>RunState.Fairness</c>)에 더한다(v0.7).
/// 쓰기: WP3(<see cref="UnseenShots"/>·<see cref="HiddenHits"/>), WP4(<see cref="UnseenCharges"/>·<see cref="KiteSwitches"/>).
/// 읽기: WP2 <c>InvariantChecker</c>(열 <c>unseen_shots</c>·<c>unseen_charges</c>·<c>hidden_hits</c>·<c>kite_switches</c>), WP7 로그.
/// 기준은 앞 셋이 0, <c>kite_switches</c> 는 살아 있는 <c>kite</c> 적 하나당 초당 1 이하.
/// <b>저장 멤버 규칙</b>(v0.8.1 — <c>RunStats</c> 와 같음): 판 합(<c>RunState.Fairness</c>)이 계단 저장(<c>RunSnapshot</c>)으로 STJ 왕복하므로 모든 공개 멤버는 읽기·쓰기 가능한 값 형식(배열)이다.
/// partial 이 아니다(조각 없음) — 멤버를 더하면 계약 변경이고 모양 지문 시험이 깨진다(<c>RunSnapshot.CurrentFormat</c> 을 함께 올림).
/// </summary>
public sealed class FairnessCounters
{
    /// <summary>적 진영 투사체를 만드는 순간 쏜 적(보스·중간 보스 제외, 단 <see cref="EnemyProjectileSpawn.SightGated"/> 발사는 셈)이 또렷이 보이지 않았던 수.</summary>
    public int UnseenShots { get; set; }
    /// <summary>돌진을 시작하는 순간 같은 조건이었던 수.</summary>
    public int UnseenCharges { get; set; }
    /// <summary>적 본체(근접·자폭·돌진, 보스·중간 보스 제외)가 주인공을 때린 순간 <c>FogMath.EnemyAlpha</c>(틱 값)가 0 이었던 수.</summary>
    public int HiddenHits { get; set; }
    /// <summary><c>kite</c> 상태(다가감·물러남·쏨) 전환 수.</summary>
    public int KiteSwitches { get; set; }

    /// <summary>이 수들을 <paramref name="total"/> 에 더한다(v0.7 — 방이 끝날 때 <c>RunState.Fairness</c> 에, 3.6).</summary>
    public void AddTo(FairnessCounters total)
    {
        total.UnseenShots += UnseenShots;
        total.UnseenCharges += UnseenCharges;
        total.HiddenHits += HiddenHits;
        total.KiteSwitches += KiteSwitches;
    }
}
