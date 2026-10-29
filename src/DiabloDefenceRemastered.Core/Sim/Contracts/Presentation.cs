namespace DiabloDefenceRemastered.Core.Sim.Contracts;

// 표시 이벤트(시뮬레이션 → 화면, 설계서 3.4·3.5·5.6). 모두 생긴 스테이지 틱(Tick)을 싣고,
// 그릴 때 나이 = 현재 스테이지 틱 − Tick 으로 계산한다(진행량을 누적하지 않는다).

/// <summary>
/// 규칙에 영향 없는 이펙트 한 번(폭발·맞음 불꽃·빛 기둥 …).
/// 크기: <paramref name="Radius"/> &gt; 0 이고 정의가 fit_radius 를 가지면 배율 = Radius / fit_radius, 아니면 정의 scale. 거기에 <paramref name="Scale"/> 을 곱한다.
/// </summary>
public readonly record struct FxEvent(int Tick, EffectDef Def, Vector2 Pos, float Scale, Rgb Tint, float Radius = 0f, Vector2 Dir = default);

/// <summary>피해 숫자. 같은 대상 0.1s 합치기는 게임 쪽.</summary>
public readonly record struct DamageNumber(int Tick, EntityId Target, Vector2 Pos, int Value, bool Crit, bool ToHero);

/// <summary>적 시체(죽음 애니메이션). 그 순간의 색조·알파·발광을 복사해 간다. Culled 는 내지 않는다.</summary>
public readonly record struct CorpseSpawned(int Tick, SpriteSetDef Sprites, Vector2 Pos, Vector2 Facing, Rgb Tint, float Scale,
                                            float Alpha, Rgb GlowColor, float GlowAlpha, DeathStyle Death);

/// <summary>화면 흔들림. 지금 남은 것 중 가장 센 것을 쓴다(7.2).</summary>
public readonly record struct ShakeEvent(int Tick, float Px, float Duration);

// 알림 문구는 표시 이벤트가 아니다: HUD 문구의 출처는 World.Banner(BannerState) 하나다(WP4 가 쓰고 WP6 이 읽는다).

/// <summary>
/// 표시 이벤트 버퍼. <b>틱마다 비우지 않는다</b> — 한 프레임에 여러 틱이 돌아도 앞 틱의 이벤트가 남고,
/// 프레임 끝에 게임이 소비(FxSystem.Consume 등)한 뒤 <see cref="Clear"/> 한다.
/// 그래픽 없는 실행(시험·시나리오)은 <see cref="Enabled"/> = false 로 쌓지 않는다.
/// </summary>
public sealed class PresentationBuffer
{
    public bool Enabled { get; set; } = true;

    public EventList<FxEvent> Effects { get; } = new(64);
    public EventList<DamageNumber> DamageNumbers { get; } = new(64);
    public EventList<CorpseSpawned> Corpses { get; } = new(32);
    public EventList<ShakeEvent> Shakes { get; } = new(8);

    public void Effect(in FxEvent e) { if (Enabled) Effects.Add(e); }
    public void Damage(in DamageNumber e) { if (Enabled) DamageNumbers.Add(e); }
    public void Corpse(in CorpseSpawned e) { if (Enabled) Corpses.Add(e); }
    public void Shake(in ShakeEvent e) { if (Enabled) Shakes.Add(e); }

    public void Clear()
    {
        Effects.Clear();
        DamageNumbers.Clear();
        Corpses.Clear();
        Shakes.Clear();
    }
}
