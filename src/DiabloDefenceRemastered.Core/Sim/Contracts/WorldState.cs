namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 판의 승패 (설계서 3.5). Running → VictoryPending(3초 스테이지 틱) → Victory, Running → DefeatPending(2초) → Defeat.
/// 같은 틱에 둘 다면 승리가 먼저다. Running 이 아니면 Director 는 아무것도 안 하고 DamageResolve 는 주인공 피해를 무시한다.
/// </summary>
public enum RunOutcome : byte { Running, VictoryPending, Victory, DefeatPending, Defeat }

/// <summary>선택 대기. EndTick 이 켜고 PlayScene 이 같은 앱 틱에 오버레이를 올린다(3.3).</summary>
public enum ChoiceKind : byte { None, LevelUp, Chest }

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
