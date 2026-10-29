namespace DiabloDefenceRemastered.Core.Sim.Contracts;

/// <summary>
/// 판의 승패 (설계서 3.5). Running → VictoryPending(3초 스테이지 틱) → Victory, Running → DefeatPending(2초) → Defeat.
/// 같은 틱에 둘 다면 승리가 먼저다. Running 이 아니면 Director 는 아무것도 안 하고 DamageResolve 는 주인공 피해를 무시한다.
/// </summary>
public enum RunOutcome : byte { Running, VictoryPending, Victory, DefeatPending, Defeat }

/// <summary>선택 대기. EndTick 이 켜고 PlayScene 이 같은 앱 틱에 오버레이를 올린다(3.3).</summary>
public enum ChoiceKind : byte { None, LevelUp, Chest }

/// <summary>장애물 종류 하나의 현재 빛.</summary>
public struct KindLight
{
    public Rgb Color;
    public float Radius;
    public float Flicker;
    public float PulsePeriod;
    public float PulseAmount;
}

/// <summary>
/// 현재 조명 (<c>World.Lighting</c>). 스테이지 정의로 시작하고 <c>light_change</c> 이벤트(WP4)가 바꾸며 렌더(WP5)가 읽는다.
/// 종류별 빛 = 장애물 종류 정의의 빛 위에 스테이지 <c>kind_lights</c> 를 덮어쓴 값(빛이 없던 종류에 새로 붙일 수도 있다).
/// </summary>
public sealed class LightingState
{
    private readonly Dictionary<string, KindLight> _kinds = new(StringComparer.Ordinal);

    public Rgb Ambient { get; set; } = Rgb.White;
    public float Darkness { get; set; }
    public Rgb HeroLightColor { get; set; } = Rgb.White;
    public float HeroLightRadius { get; set; }

    /// <summary>찾기 전용(순회 순서에 기대지 않는다).</summary>
    public bool TryGetKind(string kindId, out KindLight light) => _kinds.TryGetValue(kindId, out light);

    public void SetKind(string kindId, KindLight light) => _kinds[kindId] = light;

    public void Load(LightingDef lighting, DefTable<ObstacleKindDef> kinds)
    {
        Ambient = lighting.Ambient;
        Darkness = lighting.Darkness;
        HeroLightColor = lighting.HeroLight.Color;
        HeroLightRadius = lighting.HeroLight.Radius;
        _kinds.Clear();
        foreach (var kind in kinds.All)
            if (kind.Light is { } l)
                _kinds[kind.Id] = new KindLight { Color = l.Color, Radius = l.Radius, Flicker = l.Flicker };
        foreach (var (kindId, o) in lighting.KindLights)
        {
            _kinds.TryGetValue(kindId, out var k);
            if (o.Color is { } c) k.Color = c;
            if (o.Radius is { } r) k.Radius = r;
            if (o.Flicker is { } f) k.Flicker = f;
            if (o.Pulse is { } p) { k.PulsePeriod = p.Period; k.PulseAmount = p.Amount; }
            _kinds[kindId] = k;
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
